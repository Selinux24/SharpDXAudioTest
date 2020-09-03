using SharpDX;
using SharpDX.Multimedia;
using SharpDX.X3DAudio;
using SharpDX.XAudio2;
using SharpDX.XAudio2.Fx;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpDXAudioTest
{
    /// <summary>
    /// Voice instance
    /// </summary>
    class AudioEffect : IAudioEffect, IDisposable
    {
        private const int WaitPrecision = 1;

        private readonly XAudio2 device;
        private readonly Speakers speakers;
        private readonly MasteringVoice masteringVoice;
        private readonly int inputChannelCount;
        private readonly int inputSampleRate;
        private readonly SourceVoice sourceVoice;
        private readonly int voiceInputChannels;
        private readonly AudioFile voice;

        private float pan;
        private float[] panOutputMatrix;
        private float pitch;

        private SubmixVoice submixVoice;
        private ReverbPresets? reverbPreset;
        private DspSettings dspSettings;

        private X3DAudio x3dInstance;
        private bool useRedirectToLFE;
        private int outputChannels;
        private Listener listener;
        private Emitter emitter;

        private readonly Stopwatch clock = new Stopwatch();
        private readonly ManualResetEvent playEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent waitForPlayToOutput = new ManualResetEvent(false);
        private readonly AutoResetEvent bufferEndEvent = new AutoResetEvent(false);
        private TimeSpan playPosition;
        private TimeSpan nextPlayPosition;
        private TimeSpan playPositionStart;
        private int playCounter;
        private bool disposed = false;

        /// <summary>
        /// Gets a value indicating whether this instance is looped.
        /// </summary>
        public bool IsLooped { get; set; }
        /// <summary>
        /// Gets or sets the pan value of the sound effect.
        /// </summary>
        /// <remarks>The value is clamped to (-1f, 1f) range.</remarks>
        /// <exception cref="ObjectDisposedException">Is thrown if the current instance was already disposed.</exception>
        public float Pan
        {
            get
            {
                return pan;
            }
            set
            {
                value = MathUtil.Clamp(value, -1.0f, 1.0f);

                if (MathUtil.NearEqual(pan, value))
                {
                    return;
                }

                pan = value;

                UpdateOutputMatrix();
            }
        }
        /// <summary>
        /// Gets or sets the pitch value of the sound effect.
        /// </summary>
        /// <remarks>The value is clamped to (-1f, 1f) range.</remarks>
        /// <exception cref="ObjectDisposedException">Is thrown if the current instance was already disposed.</exception>
        public float Pitch
        {
            get
            {
                return pitch;
            }
            set
            {
                value = MathUtil.Clamp(value, -1.0f, 1.0f);

                if (MathUtil.NearEqual(pitch, value))
                {
                    return;
                }

                pitch = value;

                sourceVoice.SetFrequencyRatio(XAudio2.SemitonesToFrequencyRatio(pitch));
            }
        }
        /// <summary>
        /// Gets or sets the volume of the current sound effect instance.
        /// </summary>
        /// <remarks>The value is clamped to (0f, 1f) range.</remarks>
        /// <exception cref="ObjectDisposedException">Is thrown if the current instance was already disposed.</exception>
        public float Volume
        {
            get
            {
                sourceVoice.GetVolume(out float volume);
                return volume;
            }
            set
            {
                float volume = MathUtil.Clamp(value, 0.0f, 1.0f);
                sourceVoice.SetVolume(volume);
            }
        }

        /// <summary>
        /// Gets or sets whether the master voice uses 3D audio or not
        /// </summary>
        public bool UseAudio3D { get; set; }
        /// <summary>
        /// Emitter
        /// </summary>
        public EmitterInstance Emitter { get; set; }
        /// <summary>
        /// Listener
        /// </summary>
        public ListenerInstance Listener { get; set; }

        /// <summary>
        /// Gets the duration in seconds of the current sound.
        /// </summary>
        /// <value>The duration.</value>
        public TimeSpan Duration
        {
            get { return voice.Duration; }
        }
        /// <summary>
        /// Gets the state of this instance.
        /// </summary>
        /// <value>The state.</value>
        public AudioEffectState State { get; private set; } = AudioEffectState.Stopped;
        /// <summary>
        /// The instance is due to dispose
        /// </summary>
        public bool DueToDispose { get; private set; }
        /// <summary>
        /// Gets or sets the position in seconds.
        /// </summary>
        /// <value>The position.</value>
        public TimeSpan Position
        {
            get { return playPosition; }
            set
            {
                playPosition = value;
                nextPlayPosition = value;
                playPositionStart = value;
                clock.Restart();
                playCounter++;
            }
        }

        /// <summary>
        /// Event fired when the audio starts
        /// </summary>
        public event GameAudioHandler AudioStart;
        /// <summary>
        /// Event fired when the audio ends
        /// </summary>
        public event GameAudioHandler AudioEnd;
        /// <summary>
        /// Event fired when a loop ends
        /// </summary>
        public event GameAudioHandler LoopEnd;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioEffect" /> class.
        /// </summary>
        /// <param name="audioState">Audio state</param>
        /// <param name="fileName">File name</param>
        /// <param name="looped">Looped</param>
        public AudioEffect(AudioEngine audioState, string fileName, bool looped = false)
        {
            device = audioState.Device;
            speakers = audioState.Speakers;
            masteringVoice = audioState.MasteringVoice;
            inputChannelCount = audioState.OutputChannels;
            inputSampleRate = audioState.OutputSampleRate;
            IsLooped = looped;

            // Read in the file
            voice = new AudioFile(fileName);
            voiceInputChannels = voice.WaveFormat.Channels;

            VoiceSendDescriptor[] sendDescriptors = new[]
            {
                // LPF direct-path
                new VoiceSendDescriptor { Flags = VoiceSendFlags.UseFilter, OutputVoice = masteringVoice },
            };

            sourceVoice = new SourceVoice(device, voice.WaveFormat, VoiceFlags.UseFilter, XAudio2.MaximumFrequencyRatio);
            sourceVoice.SetOutputVoices(sendDescriptors);
            sourceVoice.BufferEnd += SourceVoiceBufferEnd;

            // Starts the playing thread
            Task.Factory.StartNew(PlayAsync, TaskCreationOptions.LongRunning);
        }
        /// <summary>
        /// Destructor
        /// </summary>
        ~AudioEffect()
        {
            // Finalizer calls Dispose(false)  
            Dispose(false);
        }
        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose resources
        /// </summary>
        /// <param name="disposing">Free managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                disposed = true;

                Console.WriteLine("DisposePlayer Begin");

                voice?.Dispose();

                submixVoice?.DestroyVoice();
                submixVoice?.Dispose();

                sourceVoice?.Stop(0);
                sourceVoice?.DestroyVoice();
                sourceVoice?.Dispose();

                Console.WriteLine("DisposePlayer End");
            }
        }

        /// <summary>
        /// Plays the current instance. If it is already playing - the call is ignored.
        /// </summary>
        public void Play()
        {
            if (State == AudioEffectState.Stopped)
            {
                sourceVoice.Start(0);

                playCounter++;
                waitForPlayToOutput.Reset();
                State = AudioEffectState.Playing;
                playEvent.Set();
                waitForPlayToOutput.WaitOne();
            }
            else if (State == AudioEffectState.Paused)
            {
                Resume();
            }
        }
        /// <summary>
        /// Stops the playback of the current instance indicating whether the stop should occur immediately of at the end of the sound.
        /// </summary>
        /// <param name="immediate">A value indicating whether the playback should be stopped immediately or at the end of the sound.</param>
        public void Stop(bool inmediate = true)
        {
            if (State != AudioEffectState.Stopped)
            {
                sourceVoice.Stop(0);

                playPosition = TimeSpan.Zero;
                nextPlayPosition = TimeSpan.Zero;
                playPositionStart = TimeSpan.Zero;
                playCounter++;

                clock.Stop();
                State = AudioEffectState.Stopped;
                playEvent.Reset();
            }
        }
        /// <summary>
        /// Pauses the playback of the current instance.
        /// </summary>
        public void Pause()
        {
            if (State == AudioEffectState.Playing)
            {
                sourceVoice.Stop();

                clock.Stop();
                State = AudioEffectState.Paused;
                playEvent.Reset();
            }
        }
        /// <summary>
        /// Resumes playback of the current instance.
        /// </summary>
        public void Resume()
        {
            if (State == AudioEffectState.Paused)
            {
                sourceVoice.Start();

                clock.Start();
                State = AudioEffectState.Playing;
                playEvent.Set();
            }
        }
        /// <summary>
        /// Resets the current instance.
        /// </summary>
        public void Reset()
        {
            Stop();
            Play();
        }

        /// <summary>
        /// Internal method to play the sound.
        /// </summary>
        private void PlayAsync()
        {
            try
            {
                AudioStart?.Invoke(this, new GameAudioEventArgs());

                while (true)
                {
                    if (disposed)
                    {
                        break;
                    }

                    // Check that this instanced is not disposed
                    while (true)
                    {
                        if (playEvent.WaitOne(WaitPrecision))
                        {
                            Console.WriteLine("playEvent.WaitOne - Waiting for play");
                            break;
                        }
                    }

                    // Playing all the samples
                    PlayAllSamples(out bool endOfSong);

                    // If the song is not looping (by default), then stop the audio player.
                    if (State == AudioEffectState.Playing && endOfSong)
                    {
                        if (!IsLooped)
                        {
                            AudioEnd?.Invoke(this, new GameAudioEventArgs());
                            Stop();
                        }
                        else
                        {
                            LoopEnd?.Invoke(this, new GameAudioEventArgs());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw;
            }
        }
        /// <summary>
        /// Plays all sound samples
        /// </summary>
        /// <param name="endOfSound">End of sound flag</param>
        private void PlayAllSamples(out bool endOfSound)
        {
            endOfSound = false;

            clock.Restart();
            playPositionStart = nextPlayPosition;
            playPosition = playPositionStart;
            int currentPlayCounter = playCounter;

            // Get the decoded samples from the specified starting position.
            voice.SetPosition(playPositionStart);

            bool isFirstTime = true;

            while (true)
            {
                if (disposed)
                {
                    break;
                }

                while (State != AudioEffectState.Stopped)
                {
                    // While the player is not stopped, wait for the play event
                    if (playEvent.WaitOne(WaitPrecision))
                    {
                        Console.WriteLine("playEvent.WaitOne - Waiting for play");
                        break;
                    }
                }

                // If the player is stopped, then break of this loop
                if (State == AudioEffectState.Stopped)
                {
                    nextPlayPosition = TimeSpan.Zero;
                    break;
                }

                // If there was a change in the play position, restart the sample iterator.
                if (currentPlayCounter != playCounter)
                {
                    break;
                }

                // If the player is not stopped and the buffer queue is full, wait for the end of a buffer.
                while (State != AudioEffectState.Stopped && !disposed && sourceVoice.State.BuffersQueued == voice.BufferCount)
                {
                    bufferEndEvent.WaitOne(WaitPrecision);
                }
                Console.WriteLine("bufferEndEvent.WaitOne - Load new buffer");

                // If the player is stopped or disposed, then break of this loop
                if (State == AudioEffectState.Stopped)
                {
                    nextPlayPosition = TimeSpan.Zero;
                    break;
                }

                // If there was a change in the play position, restart the sample iterator.
                if (currentPlayCounter != playCounter)
                {
                    break;
                }

                // Retrieve a pointer to the sample data
                if (!voice.GetNextAudioBuffer(out var audioBuffer))
                {
                    endOfSound = true;
                    break;
                }

                // If this is a first play, restart the clock and notify play method.
                if (isFirstTime)
                {
                    clock.Restart();
                    isFirstTime = false;

                    Console.WriteLine("waitForPlayToOutput.Set (First time)");
                    waitForPlayToOutput.Set();
                }

                // Update the current position used for sync
                playPosition = playPositionStart + clock.Elapsed;

                // Submit the audio buffer to xaudio2
                sourceVoice.SubmitSourceBuffer(audioBuffer, null);
            }
        }

        /// <summary>
        /// Initializes the 3d support
        /// </summary>
        /// <param name="audioState">Audio state</param>
        /// <param name="emitterInstance">Emitter</param>
        /// <param name="listenerInstance">Listener</param>
        public void Initialize3D(AudioEngine audioState, EmitterInstance emitterInstance, ListenerInstance listenerInstance)
        {
            x3dInstance = audioState.X3DInstance;
            useRedirectToLFE = audioState.UseRedirectToLFE;
            outputChannels = audioState.OutputChannels;

            Emitter = emitterInstance;
            Listener = listenerInstance;

            // Setup 3D audio structs
            listener = new Listener
            {
                Position = listenerInstance.Position,
                OrientFront = listenerInstance.OrientFront,
                OrientTop = listenerInstance.OrientTop,
                Cone = listenerInstance.Cone,
            };

            emitter = new Emitter
            {
                Position = emitterInstance.Position,
                OrientFront = emitterInstance.OrientFront,
                OrientTop = emitterInstance.OrientTop,
                Cone = emitterInstance.Cone,

                ChannelCount = this.voiceInputChannels,
                ChannelRadius = 1.0f,
                ChannelAzimuths = new float[this.voiceInputChannels],

                // Use of Inner radius allows for smoother transitions as
                // a sound travels directly through, above, or below the listener.
                // It also may be used to give elevation cues.
                InnerRadius = 2.0f,
                InnerRadiusAngle = MathUtil.PiOverFour,

                VolumeCurve = AudioConstants.DefaultLinearCurve,
                LfeCurve = AudioConstants.EmitterLfeCurve,
                LpfDirectCurve = null, // use default curve
                LpfReverbCurve = null, // use default curve
                ReverbCurve = AudioConstants.EmitterReverbCurve,
                CurveDistanceScaler = 14.0f,
                DopplerScaler = 1.0f
            };

            dspSettings = new DspSettings(voiceInputChannels, outputChannels);

            UseAudio3D = true;
        }
        /// <summary>
        /// Apply 3d configuration to voice
        /// </summary>
        public void Apply3D(float elapsedSeconds)
        {
            if (!UseAudio3D)
            {
                return;
            }

            Calculate3D(elapsedSeconds);

            UpdateVoices();
        }
        /// <summary>
        /// Calculates instance positions
        /// </summary>
        /// <param name="elapsedSeconds">Elpased time</param>
        private void Calculate3D(float elapsedSeconds)
        {
            if (Listener.UseInnerRadius)
            {
                emitter.InnerRadius = 2.0f;
                emitter.InnerRadiusAngle = MathUtil.PiOverFour;
            }
            else
            {
                emitter.InnerRadius = 0.0f;
                emitter.InnerRadiusAngle = 0.0f;
            }

            if (elapsedSeconds > 0)
            {
                listener.Velocity = (Listener.Position - listener.Position) / elapsedSeconds;

                emitter.Velocity = (Emitter.Position - emitter.Position) / elapsedSeconds;
            }

            listener.Cone = Listener.UseCone ? Listener.Cone : null;
            listener.Position = Listener.Position;
            listener.OrientFront = Listener.OrientFront;
            listener.OrientTop = Listener.OrientTop;

            emitter.Position = Emitter.Position;
            emitter.OrientFront = Emitter.OrientFront;
            emitter.OrientTop = Emitter.OrientTop;

            CalculateFlags dwCalcFlags =
                CalculateFlags.Matrix |
                CalculateFlags.Doppler |
                CalculateFlags.LpfDirect |
                CalculateFlags.LpfReverb |
                CalculateFlags.Reverb;

            if (this.useRedirectToLFE)
            {
                // On devices with an LFE channel, allow the mono source data
                // to be routed to the LFE destination channel.
                dwCalcFlags |= CalculateFlags.RedirectToLfe;
            }

            x3dInstance.Calculate(listener, emitter, dwCalcFlags, dspSettings);
        }
        /// <summary>
        /// Updates the instance voices
        /// </summary>
        private void UpdateVoices()
        {
            if (sourceVoice == null)
            {
                return;
            }

            UpdateOutputMatrix();

            // Apply X3DAudio generated DSP settings to XAudio2
            sourceVoice.SetFrequencyRatio(dspSettings.DopplerFactor);

            sourceVoice.SetOutputMatrix(masteringVoice, voiceInputChannels, outputChannels, GetOutputMatrix());
            sourceVoice.SetOutputFilterParameters(
                masteringVoice,
                new FilterParameters
                {
                    Type = FilterType.LowPassFilter,
                    Frequency = 2.0f * (float)Math.Sin(MathUtil.Pi / 6.0f * dspSettings.LpfDirectCoefficient),
                    OneOverQ = 1.0f
                });

            if (!reverbPreset.HasValue)
            {
                return;
            }

            if (submixVoice == null)
            {
                return;
            }

            sourceVoice.SetOutputMatrix(submixVoice, voiceInputChannels, outputChannels, GetOutputMatrix());
            sourceVoice.SetOutputFilterParameters(
                submixVoice,
                new FilterParameters
                {
                    Type = FilterType.LowPassFilter,
                    Frequency = 2.0f * (float)Math.Sin(MathUtil.Pi / 6.0f * dspSettings.LpfDirectCoefficient),
                    OneOverQ = 1.0f
                });
        }

        /// <summary>
        /// Creates a new reverb voice
        /// </summary>
        private void CreateReverbVoice()
        {
            // Create reverb effect
            using (var reverbEffect = new Reverb(device))
            {
                // Create a submix voice
                submixVoice = new SubmixVoice(device, inputChannelCount, inputSampleRate);

                // Performance tip: you need not run global FX with the sample number
                // of channels as the final mix.  For example, this sample runs
                // the reverb in mono mode, thus reducing CPU overhead.
                var desc = new EffectDescriptor(reverbEffect)
                {
                    InitialState = true,
                    OutputChannelCount = inputChannelCount,
                };
                submixVoice.SetEffectChain(desc);
            }
        }
        /// <summary>
        /// Gets the reverb effect
        /// </summary>
        public ReverbPresets? GetReverb()
        {
            return reverbPreset;
        }
        /// <summary>
        /// Set reverb to voice
        /// </summary>
        /// <param name="reverb">Reverb index</param>
        public bool SetReverb(ReverbPresets? reverb)
        {
            if (submixVoice == null)
            {
                CreateReverbVoice();
            }

            if (reverb.HasValue)
            {
                // Play the wave using a source voice that sends to both the submix and mastering voices
                VoiceSendDescriptor[] sendDescriptors = new[]
                {
                    // LPF direct-path
                    new VoiceSendDescriptor { Flags = VoiceSendFlags.UseFilter, OutputVoice = masteringVoice },
                    // LPF reverb-path -- omit for better performance at the cost of less realistic occlusion
                    new VoiceSendDescriptor { Flags = VoiceSendFlags.UseFilter, OutputVoice = submixVoice },
                };
                sourceVoice.SetOutputVoices(sendDescriptors);

                var native = AudioConstants.GetPreset(reverb.Value, submixVoice.VoiceDetails.InputSampleRate);
                submixVoice.SetEffectParameters(0, native);
                submixVoice.EnableEffect(0);
            }
            else
            {
                // Play the wave using a source voice that sends to both the submix and mastering voices
                VoiceSendDescriptor[] sendDescriptors = new[]
                {
                    // LPF direct-path
                    new VoiceSendDescriptor { Flags = VoiceSendFlags.UseFilter, OutputVoice = masteringVoice },
                };
                sourceVoice.SetOutputVoices(sendDescriptors);

                submixVoice.DisableEffect(0);
            }

            reverbPreset = reverb;

            return true;
        }

        /// <summary>
        /// Gets the output matrix configuration
        /// </summary>
        /// <returns>Returns an array of floats from 0 to 1.</returns>
        public float[] GetOutputMatrix()
        {
            return panOutputMatrix.ToArray();
        }
        /// <summary>
        /// Initializes the output matrix
        /// </summary>
        private void InitializeOutputMatrix()
        {
            if (panOutputMatrix?.Length > 0)
            {
                return;
            }

            var voice = reverbPreset.HasValue ? submixVoice : (Voice)masteringVoice;

            int destinationChannels = voice.VoiceDetails.InputChannelCount;
            int sourceChannels = sourceVoice.VoiceDetails.InputChannelCount;

            panOutputMatrix = new float[destinationChannels * sourceChannels];

            // Default to full volume for all channels/destinations
            for (var i = 0; i < panOutputMatrix.Length; i++)
            {
                panOutputMatrix[i] = 1.0f;
            }
        }
        /// <summary>
        /// Updates the output matrix
        /// </summary>
        private void UpdateOutputMatrix()
        {
            InitializeOutputMatrix();

            var outputMatrix = dspSettings.MatrixCoefficients.ToArray();

            int sourceChannels = sourceVoice.VoiceDetails.InputChannelCount;

            float panLeft = 0.5f - (pan * 0.5f);
            float panRight = 0.5f + (pan * 0.5f);

            //The level sent from source channel S to destination channel D is specified in the form outputMatrix[SourceChannels × D + S]
            for (int s = 0; s < sourceChannels; s++)
            {
                switch ((AudioSpeakers)speakers)
                {
                    case AudioSpeakers.Mono:
                        panOutputMatrix[(sourceChannels * 0) + s] = 1 * outputMatrix[s];
                        break;

                    case AudioSpeakers.Stereo:
                    case AudioSpeakers.Surround:
                        panOutputMatrix[(sourceChannels * 0) + s] = panLeft * 2f * outputMatrix[(sourceChannels * 0) + s];
                        panOutputMatrix[(sourceChannels * 1) + s] = panRight * 2f * outputMatrix[(sourceChannels * 1) + s];
                        break;

                    case AudioSpeakers.Quad:
                        panOutputMatrix[(sourceChannels * 0) + s] = panOutputMatrix[(sourceChannels * 2) + s] = panLeft * 2f * outputMatrix[(sourceChannels * 0) + s];
                        panOutputMatrix[(sourceChannels * 1) + s] = panOutputMatrix[(sourceChannels * 3) + s] = panRight * 2f * outputMatrix[(sourceChannels * 1) + s];
                        break;

                    case AudioSpeakers.FivePointOne:
                    case AudioSpeakers.FivePointOneSurround:
                    case AudioSpeakers.SevenPointOne:
                        panOutputMatrix[(sourceChannels * 0) + s] = panOutputMatrix[(sourceChannels * 4) + s] = panLeft * 2f * outputMatrix[(sourceChannels * 0) + s];
                        panOutputMatrix[(sourceChannels * 1) + s] = panOutputMatrix[(sourceChannels * 5) + s] = panRight * 2f * outputMatrix[(sourceChannels * 1) + s];
                        break;

                    case AudioSpeakers.SevenPointOneSurround:
                        panOutputMatrix[(sourceChannels * 0) + s] = panOutputMatrix[(sourceChannels * 4) + s] = panOutputMatrix[(sourceChannels * 6) + s] = panLeft * 2f * outputMatrix[(sourceChannels * 0) + s];
                        panOutputMatrix[(sourceChannels * 1) + s] = panOutputMatrix[(sourceChannels * 5) + s] = panOutputMatrix[(sourceChannels * 7) + s] = panRight * 2f * outputMatrix[(sourceChannels * 1) + s];
                        break;

                    default:
                        // don't do any panning here
                        break;
                }
            }
        }

        /// <summary>
        /// On source voice buffer ends
        /// </summary>
        /// <param name="obj">Data pointer</param>
        private void SourceVoiceBufferEnd(IntPtr obj)
        {
            bufferEndEvent.Set();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{voice.FileName}";
        }
    }
}
