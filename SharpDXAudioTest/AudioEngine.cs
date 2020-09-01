using SharpDX;
using SharpDX.Multimedia;
using SharpDX.X3DAudio;
using SharpDX.XAudio2;
using System;

namespace SharpDXAudioTest
{
    class AudioEngine : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        /// XAudio2 device
        /// </summary>
        public XAudio2 Device { get; private set; }
        /// <summary>
        /// Mastering voice
        /// </summary>
        public MasteringVoice MasteringVoice { get; private set; }
        /// <summary>
        /// Speakers configuration
        /// </summary>
        public Speakers Speakers { get; private set; }
        /// <summary>
        /// Output sample rate
        /// </summary>
        public int OutputSampleRate { get; private set; }
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
        /// Gets or sets the master volume
        /// </summary>
        public float Volume
        {
            get
            {
                this.MasteringVoice.GetVolume(out float volume);
                return volume;
            }
            set
            {
                float volume = MathUtil.Clamp(value, 0.0f, 1.0f);
                this.MasteringVoice.SetVolume(volume);
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
        public AudioEngine(int sampleRate = 48000, XAudio2Flags flags = XAudio2Flags.None, XAudio2Version version = XAudio2Version.Default, ProcessorSpecifier processor = ProcessorSpecifier.DefaultProcessor)
        {
            // Initialize XAudio2
            this.Device = new XAudio2(flags, processor, version);

            // Create a mastering voice
            this.MasteringVoice = new MasteringVoice(this.Device, 2, sampleRate);

            // Check device details to make sure it's within our sample supported parameters
            if (this.Device.Version == XAudio2Version.Version27)
            {
                var details = this.MasteringVoice.VoiceDetails;
                this.OutputSampleRate = details.InputSampleRate;
                this.OutputChannels = details.InputChannelCount;
                int channelMask = this.MasteringVoice.ChannelMask;
                this.Speakers = (Speakers)channelMask;
            }
            else
            {
                this.MasteringVoice.GetVoiceDetails(out var details);
                this.OutputSampleRate = details.InputSampleRate;
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
        ~AudioEngine()
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

                if (this.Device != null)
                {
                    this.Device.StopEngine();
                    this.Device.Dispose();
                    this.Device = null;
                }
            }
        }

        /// <summary>
        /// Initialize effect instance
        /// </summary>
        /// <param name="filePath">Sound file</param>
        /// <param name="looped">Looped sound</param>
        /// <returns>Returns a effect instance</returns>
        public AudioEffect InitializeEffect(string filePath, bool looped = false)
        {
            return new AudioEffect(this, filePath, looped);
        }

        /// <summary>
        /// Starts the audio engine
        /// </summary>
        public void Start()
        {
            this.Device?.StartEngine();
        }
        /// <summary>
        /// Stops the audio engine
        /// </summary>
        public void Stop()
        {
            this.Device?.StopEngine();
        }
    }
}
