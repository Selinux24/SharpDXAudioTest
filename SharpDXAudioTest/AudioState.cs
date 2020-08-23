using SharpDX.Multimedia;
using SharpDX.X3DAudio;
using SharpDX.XAudio2;
using SharpDX.XAudio2.Fx;
using System;
using System.IO;

namespace SharpDXAudioTest
{
    class AudioState : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        /// XAudio2 device
        /// </summary>
        private XAudio2 device;
        /// <summary>
        /// Mastering voice
        /// </summary>
        private MasteringVoice MasteringVoice;

        /// <summary>
        /// Input sample rate
        /// </summary>
        public int InputSampleRate { get; private set; }
        /// <summary>
        /// Input channels
        /// </summary>
        public int InputChannels { get; private set; } = 1;
        /// <summary>
        /// Speakers configuration
        /// </summary>
        public Speakers Speakers { get; private set; }
        /// <summary>
        /// Output channels
        /// </summary>
        public int OutputChannels { get; private set; }
        /// <summary>
        /// Use redirect to LFE
        /// </summary>
        public bool UseRedirectToLFE
        {
            get
            {
                return (Speakers & Speakers.LowFrequency) != 0;
            }
        }

        /// <summary>
        /// 3D instance
        /// </summary>
        public X3DAudio X3DInstance { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sampleRate">Sample rate</param>
        /// <param name="flags">Creation flags</param>
        /// <param name="version">Version</param>
        /// <param name="processor">Processor</param>
        public AudioState(int sampleRate = 48000, XAudio2Flags flags = XAudio2Flags.None, XAudio2Version version = XAudio2Version.Default, ProcessorSpecifier processor = ProcessorSpecifier.DefaultProcessor)
        {
            // Initialize XAudio2
            this.device = new XAudio2(flags, processor, version);

            // Create a mastering voice
            this.MasteringVoice = new MasteringVoice(this.device, 2, sampleRate);

            // Check device details to make sure it's within our sample supported parameters
            if (this.device.Version == XAudio2Version.Version27)
            {
                var details = this.MasteringVoice.VoiceDetails;
                this.InputSampleRate = details.InputSampleRate;
                this.OutputChannels = details.InputChannelCount;
                int channelMask = this.MasteringVoice.ChannelMask;
                this.Speakers = (Speakers)channelMask;
            }
            else
            {
                this.MasteringVoice.GetVoiceDetails(out var details);
                this.InputSampleRate = details.InputSampleRate;
                this.OutputChannels = details.InputChannelCount;
                this.MasteringVoice.GetChannelMask(out int channelMask);
                this.Speakers = (Speakers)channelMask;
            }

            if (this.OutputChannels > AudioConstants.OUTPUTCHANNELS)
            {
                throw new Exception($"Too much output channels");
            }

            if (this.Speakers == Speakers.None)
            {
                this.Speakers = Speakers.FrontLeft | Speakers.FrontRight;
            }

            // Initialize X3DAudio
            //  Speaker geometry configuration on the final mix, specifies assignment of channels
            //  to speaker positions, defined as per WAVEFORMATEXTENSIBLE.dwChannelMask
            //  SpeedOfSound - speed of sound in user-defined world units/second, used
            //  only for doppler calculations, it must be >= FLT_MIN
            this.X3DInstance = new X3DAudio(this.Speakers, X3DAudio.SpeedOfSound);

            // Done
            this.Initialized = true;
        }
        /// <summary>
        /// Destructor
        /// </summary>
        ~AudioState()
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
                this.X3DInstance = null;

                if (this.MasteringVoice != null)
                {
                    this.MasteringVoice.DestroyVoice();
                    this.MasteringVoice = null;
                }

                if (this.device != null)
                {
                    this.device.StopEngine();
                    this.device.Dispose();
                    this.device = null;
                }
            }
        }

        /// <summary>
        /// Initialize voice instance
        /// </summary>
        /// <param name="wavFilePath">Wav file</param>
        /// <param name="useReverb">Use reverb with the voice</param>
        /// <returns>Returns a voice instance</returns>
        public VoiceInstance InitializeVoice(string wavFilePath, bool useReverb = false)
        {
            // Read in the wave file
            WaveFormat waveFormat;
            AudioBuffer loopedAudioBuffer;
            uint[] decodedPacketsInfo;
            using (var stream = new SoundStream(File.OpenRead(wavFilePath)))
            {
                var buffer = stream.ToDataStream();

                waveFormat = stream.Format;
                decodedPacketsInfo = stream.DecodedPacketsInfo;
                loopedAudioBuffer = new AudioBuffer
                {
                    Stream = buffer,
                    AudioBytes = (int)buffer.Length,
                    Flags = BufferFlags.EndOfStream,
                    LoopCount = AudioBuffer.LoopInfinite,
                };
            }

            SubmixVoice mixVoice = null;
            VoiceSendDescriptor[] sendDescriptors;

            if (useReverb)
            {
                // Create reverb effect
                using (var reverbEffect = new Reverb(this.device))
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

                    mixVoice = new SubmixVoice(this.device, 1, this.InputSampleRate, SubmixVoiceFlags.None, 0, effectChain);

                    // Play the wave using a source voice that sends to both the submix and mastering voices
                    sendDescriptors = new[]
                    {
                        // LPF direct-path
                        new VoiceSendDescriptor { Flags = VoiceSendFlags.UseFilter, OutputVoice = this.MasteringVoice },
                        // LPF reverb-path -- omit for better performance at the cost of less realistic occlusion
                        new VoiceSendDescriptor { Flags = VoiceSendFlags.UseFilter, OutputVoice = mixVoice },
                    };
                }
            }
            else
            {
                // Play the wave using a source voice that sends to both the submix and mastering voices
                sendDescriptors = new[]
                {
                    // LPF direct-path
                    new VoiceSendDescriptor { Flags = VoiceSendFlags.UseFilter, OutputVoice = this.MasteringVoice },
                };
            }

            SourceVoice srcVoice = new SourceVoice(this.device, waveFormat, VoiceFlags.None, 2.0f, null);
            srcVoice.SetOutputVoices(sendDescriptors);

            // Submit the wave sample data using an XAUDIO2_BUFFER structure
            srcVoice.SubmitSourceBuffer(loopedAudioBuffer, decodedPacketsInfo);

            var result = new VoiceInstance(this.MasteringVoice, mixVoice, srcVoice);

            result.SetReverb(0);

            return result;
        }

        public void Start()
        {
            this.device?.StartEngine();
        }
        public void Stop()
        {
            this.device?.StopEngine();
        }
        public float GetVolume()
        {
            float volume = 0;
            this.MasteringVoice?.GetVolume(out volume);

            return volume;
        }
        public void SetVolume(float volume)
        {
            this.MasteringVoice?.SetVolume(volume);
        }
    }
}
