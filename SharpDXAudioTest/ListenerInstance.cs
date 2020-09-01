using SharpDX;
using SharpDX.X3DAudio;

namespace SharpDXAudioTest
{
    public class ListenerInstance : IAgent
    {
        public static Cone DefaultCone
        {
            get
            {
                // Specify sound cone to add directionality to listener for artistic effect:
                // Emitters behind the listener are defined here to be more attenuated,
                // have a lower LPF cutoff frequency,
                // yet have a slightly higher reverb send level.
                return new Cone
                {
                    InnerAngle = MathUtil.Pi * 5.0f / 6.0f,
                    OuterAngle = MathUtil.Pi * 11.0f / 6.0f,
                    InnerVolume = 1.0f,
                    OuterVolume = 0.75f,
                    InnerLpf = 0.0f,
                    OuterLpf = 0.25f,
                    InnerReverb = 0.708f,
                    OuterReverb = 1.0f
                };
            }
        }

        public string Name { get; set; }
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 OrientFront { get; set; } = Vector3.ForwardLH;
        public Vector3 OrientTop { get; set; } = Vector3.Up;
        public Cone Cone { get; set; } = DefaultCone;
        public bool UseCone { get; set; } = true;
        public bool UseInnerRadius { get; set; } = true;
    }
}
