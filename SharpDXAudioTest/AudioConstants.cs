﻿using SharpDX.X3DAudio;
using SharpDX.XAudio2.Fx;

namespace SharpDXAudioTest
{
    static class AudioConstants
    {
        public const int INPUTCHANNELS = 1;  // number of source channels
        public const int OUTPUTCHANNELS = 8; // maximum number of destination channels supported in this sample

        // Constants to define our world space
        public const int XMIN = -10;
        public const int XMAX = 10;
        public const int ZMIN = -10;
        public const int ZMAX = 10;

        public static readonly CurvePoint[] DefaultLinearCurve = new CurvePoint[]
        {
            new CurvePoint(){ Distance = 0.0f, DspSetting = 1.0f, },
            new CurvePoint(){ Distance = 1.0f, DspSetting = 0.0f, },
        };

        // Specify LFE level distance curve such that it rolls off much sooner than
        // all non-LFE channels, making use of the subwoofer more dramatic.
        public static readonly CurvePoint[] EmitterLfeCurve = new CurvePoint[]
        {
            new CurvePoint(){ Distance = 0.0f, DspSetting = 1.0f },
            new CurvePoint(){ Distance = 0.25f, DspSetting = 0.0f },
            new CurvePoint(){ Distance = 1.0f, DspSetting = 0.0f },
        };

        // Specify reverb send level distance curve such that reverb send increases
        // slightly with distance before rolling off to silence.
        // With the direct channels being increasingly attenuated with distance,
        // this has the effect of increasing the reverb-to-direct sound ratio,
        // reinforcing the perception of distance.
        public static readonly CurvePoint[] EmitterReverbCurve = new CurvePoint[]
        {
            new CurvePoint(){ Distance = 0.0f, DspSetting = 0.5f },
            new CurvePoint(){ Distance = 0.75f, DspSetting = 1.0f },
            new CurvePoint(){ Distance = 1.0f, DspSetting = 0.0f },
        };

        public static int NumPresets { get { return PresetParams.Length; } }
        public static readonly ReverbParameters[] PresetParams =
        {
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Default,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Generic,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.PaddedCell,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Room,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.BathRoom,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.LivingRoom,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.StoneRoom,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Auditorium,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.ConcertHall,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Cave,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Arena,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Hangar,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.CarpetedHallway,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Hallway,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.StoneCorridor,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Alley,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Forest,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.City,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Mountains,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Quarry,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Plain,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.ParkingLot,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.SewerPipe,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.UnderWater,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.SmallRoom,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.MediumRoom,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.LargeRoom,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.MediumHall,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.LargeHall,
            (ReverbParameters)ReverbI3DL2Parameters.Presets.Plate,
        };
    }
}