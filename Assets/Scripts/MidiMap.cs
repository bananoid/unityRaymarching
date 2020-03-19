using MidiJack;

public class MidiMap{
    public static MidiChannel channel = MidiChannel.Ch1;
}

public enum MidiMapNote{
    ClockTap = 0x69,
}

public enum MidiMapCC{
    ClockPrecisionDown = 0x6A,
    ClockPrecisionUp = 0x6B
}