using SharpDX;
using SharpDX.X3DAudio;
using SharpDX.XAudio2;
using System;
using System.Linq;

namespace SharpDXAudioTest
{
    /// <summary>
    /// Voice instance
    /// </summary>
    class VoiceInstance : IDisposable
    {
        private readonly MasteringVoice masteringVoice;
        private X3DAudio x3dInstance;
        private bool useRedirectToLFE;

        private int inputChannels;
        private int outputChannels;
        private SourceVoice sourceVoice;
        private SubmixVoice submixVoice;
        private DspSettings dspSettings;
        private Listener listener;
        private Emitter emitter;

        /// <summary>
        /// Constructor
        /// </summary>
        public VoiceInstance(MasteringVoice masteringVoice, SubmixVoice submixVoice, SourceVoice sourceVoice)
        {
            this.masteringVoice = masteringVoice;
            this.submixVoice = submixVoice;
            this.sourceVoice = sourceVoice;
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
                if (this.sourceVoice != null)
                {
                    this.sourceVoice.Stop(0);
                    this.sourceVoice.DestroyVoice();
                    this.sourceVoice = null;
                }

                if (this.submixVoice != null)
                {
                    this.submixVoice.DestroyVoice();
                    this.submixVoice = null;
                }
            }
        }


        public void Initialize3D(AudioState audioState, EmitterInstance emitterInstance, ListenerInstance listenerInstance)
        {
            this.x3dInstance = audioState.X3DInstance;
            this.useRedirectToLFE = audioState.UseRedirectToLFE;
            this.inputChannels = audioState.InputChannels;
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
        }
        public void Calculate2D(float fElapsedTime, ListenerInstance listenerInstance, EmitterInstance emitterInstance)
        {
            Calculate(fElapsedTime, listenerInstance, emitterInstance, false);

            Apply3D();
        }
        public void Calculate3D(float fElapsedTime, ListenerInstance listenerInstance, EmitterInstance emitterInstance)
        {
            Calculate(fElapsedTime, listenerInstance, emitterInstance, true);

            Apply3D();
        }
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
        private void Apply3D()
        {
            if (this.sourceVoice == null)
            {
                return;
            }

            // Apply X3DAudio generated DSP settings to XAudio2
            this.sourceVoice.SetFrequencyRatio(this.dspSettings.DopplerFactor);

            this.sourceVoice.SetOutputMatrix(this.masteringVoice, inputChannels, outputChannels, this.dspSettings.MatrixCoefficients);
            this.sourceVoice.SetOutputFilterParameters(
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

            this.sourceVoice.SetOutputMatrix(this.submixVoice, 1, 1, new[] { this.dspSettings.ReverbLevel });
            this.sourceVoice.SetOutputFilterParameters(
                this.submixVoice,
                new FilterParameters
                {
                    Type = FilterType.LowPassFilter,
                    Frequency = 2.0f * (float)Math.Sin(MathUtil.Pi / 6.0f * this.dspSettings.LpfReverbCoefficient),
                    OneOverQ = 1.0f
                });
        }

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
        public void Start()
        {
            this.sourceVoice?.Start(0);
        }
        public void Stop()
        {
            this.sourceVoice?.Stop(0);
        }
        public float GetVolume()
        {
            float volume = 0;
            this.sourceVoice?.GetVolume(out volume);

            return volume;
        }
        public void SetVolume(float volume)
        {
            this.sourceVoice?.SetVolume(volume);
        }

        public float[] GetMatrixCoefficients()
        {
            return dspSettings.MatrixCoefficients.ToArray();
        }
    }
}
