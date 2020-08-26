using SharpDX;
using SharpDX.MediaFoundation;
using SharpDX.X3DAudio;
using SharpDX.XAudio2;
using SharpDX.XAudio2.Fx;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpDXAudioTest
{
    class VoiceInstance : IDisposable
    {
        private const int WaitPrecision = 1;
        private const int BufferCount = 3;
        private const int BufferDefaultSize = 32 * 1024; // default size 32Kb

        private readonly MasteringVoice masteringVoice;
        private readonly AudioBuffer[] audioBuffers;
        private readonly DataBuffer[] memBuffers;
        private readonly Stopwatch clock = new Stopwatch();
        private readonly ManualResetEvent playEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent waitForPlayToOutput = new ManualResetEvent(false);
        private readonly AutoResetEvent bufferEndEvent = new AutoResetEvent(false);
        private AudioDecoder audioDecoder;
        private TimeSpan playPosition;
        private TimeSpan nextPlayPosition;
        private TimeSpan playPositionStart;
        private int playCounter;
        private int currentSample = 0;
        private bool disposed = false;

        private readonly int inputChannels;

        private SubmixVoice submixVoice;
        private DspSettings dspSettings;

        private bool initialized3D = false;
        private X3DAudio x3dInstance;
        private bool useRedirectToLFE;
        private int outputChannels;
        private Listener listener;
        private Emitter emitter;

        /// <summary>
        /// Gets the XAudio2 <see cref="SourceVoice"/> created by this decoder.
        /// </summary>
        /// <value>The source voice.</value>
        public SourceVoice SourceVoice { get; private set; }
        /// <summary>
        /// Gets the state of this instance.
        /// </summary>
        /// <value>The state.</value>
        public AudioPlayerState State { get; private set; } = AudioPlayerState.Stopped;
        /// <summary>
        /// Gets the duration in seconds of the current sound.
        /// </summary>
        /// <value>The duration.</value>
        public TimeSpan Duration
        {
            get { return audioDecoder.Duration; }
        }
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
        /// Gets or sets a value indicating whether to the sound is looping when the end of the buffer is reached.
        /// </summary>
        /// <value><c>true</c> if to loop the sound; otherwise, <c>false</c>.</value>
        public bool IsRepeating { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceInstance" /> class.
        /// </summary>
        /// <param name="xaudio2">The xaudio2 engine.</param>
        /// <param name="audioStream">The input audio stream.</param>
        public VoiceInstance(XAudio2 device, MasteringVoice masteringVoice, string fileName, bool useReverb, int outputSample)
        {
            this.masteringVoice = masteringVoice;

            // Pre-allocate buffers
            audioBuffers = new AudioBuffer[BufferCount];
            memBuffers = new DataBuffer[BufferCount];

            for (int i = 0; i < BufferCount; i++)
            {
                audioBuffers[i] = new AudioBuffer();
                memBuffers[i] = new DataBuffer(BufferDefaultSize);
            }

            audioDecoder = new AudioDecoder(File.OpenRead(fileName));
            inputChannels = audioDecoder.WaveFormat.Channels;

            SourceVoice = new SourceVoice(device, audioDecoder.WaveFormat, VoiceFlags.None, 2.0f, null);
            SourceVoice.BufferEnd += SourceVoiceBufferEnd;

            // Read in the wave file
            VoiceSendDescriptor[] sendDescriptors;

            if (useReverb)
            {
                // Create reverb effect
                using (var reverbEffect = new Reverb(device))
                {
                    // Create a submix voice

                    // Performance tip: you need not run global FX with the sample number
                    // of channels as the final mix.  For example, this sample runs
                    // the reverb in mono mode, thus reducing CPU overhead.
                    EffectDescriptor[] effectChain =
                    {
                        new EffectDescriptor(reverbEffect, 1)
                        {
                            InitialState = true,
                        }
                    };

                    submixVoice = new SubmixVoice(device, 1, outputSample, SubmixVoiceFlags.None, 0, effectChain);

                    // Play the wave using a source voice that sends to both the submix and mastering voices
                    sendDescriptors = new[]
                    {
                        // LPF direct-path
                        new VoiceSendDescriptor { Flags = VoiceSendFlags.UseFilter, OutputVoice = this.masteringVoice },
                        // LPF reverb-path -- omit for better performance at the cost of less realistic occlusion
                        new VoiceSendDescriptor { Flags = VoiceSendFlags.UseFilter, OutputVoice = submixVoice },
                    };
                }
            }
            else
            {
                // Play the wave using a source voice that sends to both the submix and mastering voices
                sendDescriptors = new[]
                {
                    // LPF direct-path
                    new VoiceSendDescriptor { Flags = VoiceSendFlags.UseFilter, OutputVoice = this.masteringVoice },
                };
            }

            SourceVoice.SetOutputVoices(sendDescriptors);

            // Starts the playing thread
            Task.Factory.StartNew(PlayAsync, TaskCreationOptions.LongRunning);
        }
        /// <summary>
        /// Destructor
        /// </summary>
        ~VoiceInstance()
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
                DisposePlayer();
            }
        }

        /// <summary>
        /// Plays the sound.
        /// </summary>
        public void Play()
        {
            if (State == AudioPlayerState.Stopped)
            {
                SourceVoice.Start(0);

                playCounter++;
                waitForPlayToOutput.Reset();
                State = AudioPlayerState.Playing;
                playEvent.Set();
                waitForPlayToOutput.WaitOne();
            }
            else if (State == AudioPlayerState.Paused)
            {
                Resume();
            }
        }
        /// <summary>
        /// Pauses the sound.
        /// </summary>
        public void Pause()
        {
            if (State == AudioPlayerState.Playing)
            {
                SourceVoice.Stop();

                clock.Stop();
                State = AudioPlayerState.Paused;
                playEvent.Reset();
            }
        }
        /// <summary>
        /// Resumes the play
        /// </summary>
        public void Resume()
        {
            if (State == AudioPlayerState.Paused)
            {
                SourceVoice.Start();

                clock.Start();
                State = AudioPlayerState.Playing;
                playEvent.Set();
            }
        }
        /// <summary>
        /// Stops the sound.
        /// </summary>
        public void Stop()
        {
            if (State != AudioPlayerState.Stopped)
            {
                SourceVoice.Stop(0);

                playPosition = TimeSpan.Zero;
                nextPlayPosition = TimeSpan.Zero;
                playPositionStart = TimeSpan.Zero;
                playCounter++;

                clock.Stop();
                State = AudioPlayerState.Stopped;
                playEvent.Reset();
            }
        }

        /// <summary>
        /// Gets the current volume
        /// </summary>
        public float GetVolume()
        {
            this.SourceVoice.GetVolume(out float volume);

            return volume;
        }
        /// <summary>
        /// Sets the volume
        /// </summary>
        /// <param name="volume">Volume value</param>
        public void SetVolume(float volume)
        {
            this.SourceVoice.SetVolume(volume);
        }

        /// <summary>
        /// Internal method to play the sound.
        /// </summary>
        private void PlayAsync()
        {
            try
            {
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
                    if (State == AudioPlayerState.Playing && endOfSong && !IsRepeating)
                    {
                        Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw;
            }
            finally
            {
                DisposePlayer();
            }
        }
        /// <summary>
        /// Plays all sound samples
        /// </summary>
        /// <param name="endOfSound">End of sound flag</param>
        private void PlayAllSamples(out bool endOfSound)
        {
            endOfSound = false;

            int nextBuffer = 0;

            clock.Restart();
            playPositionStart = nextPlayPosition;
            playPosition = playPositionStart;
            int currentPlayCounter = playCounter;

            // Get the decoded samples from the specified starting position.
            var sampleIterator = audioDecoder.GetSamples(playPositionStart).GetEnumerator();
            currentSample = 0;

            bool isFirstTime = true;

            while (true)
            {
                if (disposed)
                {
                    break;
                }

                while (State != AudioPlayerState.Stopped)
                {
                    // While the player is not stopped, wait for the play event
                    if (playEvent.WaitOne(WaitPrecision))
                    {
                        Console.WriteLine("playEvent.WaitOne - Waiting for play");
                        break;
                    }
                }

                // If the player is stopped, then break of this loop
                if (State == AudioPlayerState.Stopped)
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
                while (State != AudioPlayerState.Stopped && !disposed && SourceVoice.State.BuffersQueued == BufferCount)
                {
                    bufferEndEvent.WaitOne(WaitPrecision);
                }
                Console.WriteLine("bufferEndEvent.WaitOne - Load new buffer");

                // If the player is stopped or disposed, then break of this loop
                if (State == AudioPlayerState.Stopped)
                {
                    nextPlayPosition = TimeSpan.Zero;
                    break;
                }

                // Check that there is a next sample
                if (!sampleIterator.MoveNext())
                {
                    endOfSound = true;
                    break;
                }

                Console.WriteLine($"Sample: {currentSample++}");

                // If there was a change in the play position, restart the sample iterator.
                if (currentPlayCounter != playCounter)
                {
                    break;
                }

                // Retrieve a pointer to the sample data
                var audioBuffer = PrepareBuffer(sampleIterator.Current, nextBuffer);

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
                SourceVoice.SubmitSourceBuffer(audioBuffer, null);

                // Go to next entry in the ringg audio buffer
                nextBuffer = ++nextBuffer % BufferCount;
            }
        }
        /// <summary>
        /// Reads the buffer data from the decoder sample pointer, and writes into the next audio buffer to submit to the Source Voice
        /// </summary>
        /// <param name="bufferPointer">Buffer pointer</param>
        /// <param name="nextBuffer">Next buffer index</param>
        /// <returns>Returns the audio buffer prepared to submit</returns>
        private AudioBuffer PrepareBuffer(DataPointer bufferPointer, int nextBuffer)
        {
            // Check that our ring buffer has enough space to store the audio buffer.
            if (bufferPointer.Size > memBuffers[nextBuffer].Size)
            {
                memBuffers[nextBuffer].Dispose();
                memBuffers[nextBuffer] = new DataBuffer(bufferPointer.Size);
            }

            // Copy to data fuffer
            memBuffers[nextBuffer].Set(0, bufferPointer.ToArray());

            // Set the pointer to the data.
            audioBuffers[nextBuffer].AudioDataPointer = memBuffers[nextBuffer].DataPointer;
            audioBuffers[nextBuffer].AudioBytes = bufferPointer.Size;

            return audioBuffers[nextBuffer];
        }

        /// <summary>
        /// Initializes the 3d support
        /// </summary>
        /// <param name="audioState">Audio state</param>
        /// <param name="emitterInstance">Emitter</param>
        /// <param name="listenerInstance">Listener</param>
        public void Initialize3D(AudioState audioState, EmitterInstance emitterInstance, ListenerInstance listenerInstance)
        {
            this.x3dInstance = audioState.X3DInstance;
            this.useRedirectToLFE = audioState.UseRedirectToLFE;
            this.outputChannels = audioState.OutputChannels;

            // Setup 3D audio structs
            this.listener = new Listener
            {
                Position = listenerInstance.Position,
                OrientFront = listenerInstance.OrientFront,
                OrientTop = listenerInstance.OrientTop,
                Cone = listenerInstance.Cone,
            };

            this.emitter = new Emitter
            {
                Position = emitterInstance.Position,
                OrientFront = emitterInstance.OrientFront,
                OrientTop = emitterInstance.OrientTop,
                Cone = emitterInstance.Cone,

                ChannelCount = this.inputChannels,
                ChannelRadius = 1.0f,
                ChannelAzimuths = new float[this.inputChannels],

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

            this.dspSettings = new DspSettings(inputChannels, outputChannels);

            this.initialized3D = true;
        }
        /// <summary>
        /// Calculates 2d instance positions
        /// </summary>
        /// <param name="fElapsedTime">Elpased time</param>
        /// <param name="listenerInstance">Listener instance</param>
        /// <param name="emitterInstance">Emitter instance</param>
        public void Calculate2D(float fElapsedTime, ListenerInstance listenerInstance, EmitterInstance emitterInstance)
        {
            if (!initialized3D)
            {
                return;
            }

            Calculate(fElapsedTime, listenerInstance, emitterInstance, false);

            Apply3D();
        }
        /// <summary>
        /// Calculates 3d instance positions
        /// </summary>
        /// <param name="fElapsedTime">Elpased time</param>
        /// <param name="listenerInstance">Listener instance</param>
        /// <param name="emitterInstance">Emitter instance</param>
        public void Calculate3D(float fElapsedTime, ListenerInstance listenerInstance, EmitterInstance emitterInstance)
        {
            if (!initialized3D)
            {
                return;
            }

            Calculate(fElapsedTime, listenerInstance, emitterInstance, true);

            Apply3D();
        }
        /// <summary>
        /// Calculates instance positions
        /// </summary>
        /// <param name="fElapsedTime">Elpased time</param>
        /// <param name="listenerInstance">Listener instance</param>
        /// <param name="emitterInstance">Emitter instance</param>
        /// <param name="calc3D">Use 3d coordinates</param>
        private void Calculate(float fElapsedTime, ListenerInstance listenerInstance, EmitterInstance emitterInstance, bool calc3D)
        {
            if (listenerInstance.Position != this.listener.Position)
            {
                Vector3 v1 = listenerInstance.Position;
                Vector3 v2 = this.listener.Position;

                if (calc3D)
                {
                    // Calculate listener orientation
                    this.listener.OrientFront = Vector3.Normalize(v1 - v2);
                }
                else
                {
                    // Calculate listener orientation in x-z plane
                    var vDelta = v1 - v2;
                    vDelta.Y = 0f;

                    this.listener.OrientFront = Vector3.Normalize(vDelta);
                }
            }

            if (listenerInstance.UseCone)
            {
                this.listener.Cone = listenerInstance.Cone;
            }
            else
            {
                this.listener.Cone = null;
            }

            if (listenerInstance.UseInnerRadius)
            {
                this.emitter.InnerRadius = 2.0f;
                this.emitter.InnerRadiusAngle = MathUtil.PiOverFour;
            }
            else
            {
                this.emitter.InnerRadius = 0.0f;
                this.emitter.InnerRadiusAngle = 0.0f;
            }

            if (fElapsedTime > 0)
            {
                Vector3 v1 = listenerInstance.Position;
                Vector3 v2 = this.listener.Position;
                Vector3 lVelocity = (v1 - v2) / fElapsedTime;

                this.listener.Position = v1;
                this.listener.Velocity = lVelocity;

                v1 = emitterInstance.Position;
                v2 = this.emitter.Position;
                Vector3 eVelocity = (v1 - v2) / fElapsedTime;

                this.emitter.Position = v1;
                this.emitter.Velocity = eVelocity;
            }

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

            this.x3dInstance.Calculate(
                this.listener, this.emitter,
                dwCalcFlags,
                this.dspSettings);
        }
        /// <summary>
        /// Apply 3d configuration to voice
        /// </summary>
        private void Apply3D()
        {
            if (!initialized3D)
            {
                return;
            }

            if (this.SourceVoice == null)
            {
                return;
            }

            var sourceVoice = this.SourceVoice;

            // Apply X3DAudio generated DSP settings to XAudio2
            sourceVoice.SetFrequencyRatio(this.dspSettings.DopplerFactor);

            sourceVoice.SetOutputMatrix(this.masteringVoice, inputChannels, outputChannels, this.dspSettings.MatrixCoefficients);
            sourceVoice.SetOutputFilterParameters(
                this.masteringVoice,
                new FilterParameters
                {
                    Type = FilterType.LowPassFilter,
                    Frequency = 2.0f * (float)Math.Sin(MathUtil.Pi / 6.0f * this.dspSettings.LpfDirectCoefficient),
                    OneOverQ = 1.0f
                });

            if (this.submixVoice == null)
            {
                return;
            }

            sourceVoice.SetOutputMatrix(this.submixVoice, 1, 1, new[] { this.dspSettings.ReverbLevel });
            sourceVoice.SetOutputFilterParameters(
                this.submixVoice,
                new FilterParameters
                {
                    Type = FilterType.LowPassFilter,
                    Frequency = 2.0f * (float)Math.Sin(MathUtil.Pi / 6.0f * this.dspSettings.LpfReverbCoefficient),
                    OneOverQ = 1.0f
                });
        }

        /// <summary>
        /// Set reverb to voice
        /// </summary>
        /// <param name="nReverb">Reverb index</param>
        public bool SetReverb(int nReverb)
        {
            if (submixVoice == null)
            {
                return false;
            }

            if (nReverb < 0 || nReverb >= AudioConstants.NumPresets)
            {
                return false;
            }

            var native = AudioConstants.GetPreset(nReverb);
            submixVoice.SetEffectParameters(0, native);

            return true;
        }

        /// <summary>
        /// Gets the voice matrix coheficients
        /// </summary>
        public float[] GetMatrixCoefficients()
        {
            return dspSettings.MatrixCoefficients.ToArray();
        }

        /// <summary>
        /// Frees internal resources
        /// </summary>
        private void DisposePlayer()
        {
            Console.WriteLine("DisposePlayer Begin");

            if (disposed)
            {
                return;
            }

            disposed = true;

            audioDecoder?.Dispose();
            audioDecoder = null;

            submixVoice?.DestroyVoice();
            submixVoice?.Dispose();
            submixVoice = null;

            SourceVoice?.Stop(0);
            SourceVoice?.DestroyVoice();
            SourceVoice?.Dispose();
            SourceVoice = null;

            for (int i = 0; i < BufferCount; i++)
            {
                memBuffers[i].Dispose();
            }

            Console.WriteLine("DisposePlayer End");
        }

        /// <summary>
        /// On source voice buffer ends
        /// </summary>
        /// <param name="obj">Data pointer</param>
        private void SourceVoiceBufferEnd(IntPtr obj)
        {
            bufferEndEvent.Set();
        }
    }

    /// <summary>
    /// State of the audio player.
    /// </summary>
    public enum AudioPlayerState
    {
        /// <summary>
        /// The player is stopped (default).
        /// </summary>
        Stopped,
        /// <summary>
        /// The player is playing a sound.
        /// </summary>
        Playing,
        /// <summary>
        /// The player is paused.
        /// </summary>
        Paused,
    }
}
