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
    
    //Col 1
    RGCameraShiftIntensity = 0x4D,
    RGMaxSplit = 0x31,
    RGSceneIndex = 0x1D,
    RGSceneIndexSpread = 0x0D,
    
    //Col 2
    GlitchIntesity = 0x4F,

    //Col 3
    PointLightSize = 0x32,
    PointLightZ = 0x4E,
    PointLightOscilation = 0x1E,
    PointLightRoomCenter = 0x0E,
    
    //Col 4
    ColorTexInx = 0x50,
    ColorMaskTh = 0x20,
    ColorMaskIntesity = 0x10,
    ColorSpread =0x34,

}