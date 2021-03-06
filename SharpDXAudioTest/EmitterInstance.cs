﻿using SharpDX;
using SharpDX.X3DAudio;

namespace SharpDXAudioTest
{
    public class EmitterInstance : IAgent
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 OrientFront { get; set; } = Vector3.ForwardLH;
        public Vector3 OrientTop { get; set; } = Vector3.Up;
        public Cone Cone { get; set; } = null;
        public bool UseCone { get; set; } = false;
    }
}
