using SharpDX.Multimedia;
using SharpDX.X3DAudio;
using SharpDX.XAudio2;
using System;

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
        /// <param name="filePath">Sound file</param>
        /// <param name="useReverb">Use reverb with the voice</param>
        /// <returns>Returns a voice instance</returns>
        public VoiceInstance InitializeVoice(string filePath, bool useReverb = false)
        {
            VoiceInstance player = new VoiceInstance(device, this.MasteringVoice, filePath, useReverb, OutputSampleRate)
            {
                IsRepeating = true
            };

            player.SetReverb(0);

            return player;
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
