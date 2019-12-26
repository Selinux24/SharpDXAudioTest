using SharpDX.Multimedia;
using SharpDX.X3DAudio;
using SharpDX.XAPO.Fx;
using SharpDX.XAudio2;
using System.IO;

namespace SharpDXAudioTest
{
    class AudioState
    {
        public bool Initialized { get; set; }

        // XAudio2
        public XAudio2 XAudio2 { get; set; }

        public MasteringVoice MasteringVoice { get; set; }
        public int InputSampleRate { get; set; }
        public int ChannelMask { get; set; }
        public Speakers Speakers { get; set; }
        public int Channels { get; set; }
        public bool UseRedirectToLFE
        {
            get
            {
                return (Speakers & Speakers.LowFrequency) != 0;
            }
        }

        // 3D
        public X3DAudio X3DInstance { get; set; }

        public VoiceInstance Initialize(string wavFilePath, bool useReverb)
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

            int sampleRate;
            if (XAudio2.Version == XAudio2Version.Version27)
            {
                var details = MasteringVoice.VoiceDetails;
                sampleRate = details.InputSampleRate;
            }
            else
            {
                MasteringVoice.GetVoiceDetails(out var details);
                sampleRate = details.InputSampleRate;
            }

            SubmixVoice mixVoice = null;
            VoiceSendDescriptor[] sendDescriptors;

            if (useReverb)
            {
                // Create reverb effect
                using (var reverbEffect = new Reverb(XAudio2))
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

                    mixVoice = new SubmixVoice(XAudio2, 1, sampleRate, SubmixVoiceFlags.None, 0, effectChain);

                    // Play the wave using a source voice that sends to both the submix and mastering voices
                    sendDescriptors = new[]
                    {
                        // LPF direct-path
                        new VoiceSendDescriptor { Flags = VoiceSendFlags.UseFilter, OutputVoice = MasteringVoice },
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
                    new VoiceSendDescriptor { Flags = VoiceSendFlags.UseFilter, OutputVoice = MasteringVoice },
                };
            }

            SourceVoice srcVoice = new SourceVoice(XAudio2, waveFormat, VoiceFlags.None, 2.0f, null);
            srcVoice.SetOutputVoices(sendDescriptors);

            // Submit the wave sample data using an XAUDIO2_BUFFER structure
            srcVoice.SubmitSourceBuffer(loopedAudioBuffer, decodedPacketsInfo);

            var result = new VoiceInstance(MasteringVoice, mixVoice, srcVoice);

            result.SetReverb(0);

            return result;
        }
    }
}
