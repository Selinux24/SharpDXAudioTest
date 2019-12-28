using SharpDX;
using SharpDX.X3DAudio;

namespace SharpDXAudioTest
{
    interface IAgent
    {
        string Name { get; set; }
        Vector3 Position { get; set; }
        Vector3 OrientFront { get; set; }
        Vector3 OrientTop { get; set; }
        Cone Cone { get; set; }
        bool UseCone { get; set; }
    }
}
