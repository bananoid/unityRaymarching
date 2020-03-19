using MidiJack;

public class MidiMap{
    public static MidiChannel channel = MidiChannel.Ch1;
}

public enum MidiMapNote{
    ClockTap = 0x69,
}

public enum MidiMapCC{
    ClockPrecisionDown = 0x6A,
    ClockPrecisionUp = 0x6B,
    
    RGMaxSplit = 0x31,
    RGSceneIndex = 0x1D,
    RGSceneIndexSpread = 0x0D,
    
    GlitchIntesity = 0x4F,

    PointLightSize = 0x32,
    PointLightZ = 0x4E,
    PointLightOscilation = 0x1E,
    PointLightRoomCenter = 0x0E,
}