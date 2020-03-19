using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using MidiJack;

public enum ClockEventType{
    Off = -1,
    Beat = 0,
    Bar = 1,
    Bar4 = 2,
    Bar8 = 3,
    Bar16 = 4
}

[System.Serializable]
public class ClockEnvent : UnityEvent<ClockEventType>{}

public class ClockTrigger : MonoBehaviour
{
    [Header("Time")]
    [Range(0,4)] public int clockPrecision = 4;
    public static ClockTrigger instance;
    public float bpm = 120;
    private float clockInterval, clockTimer;
    public static bool clockTrig;
    public static int clockCount;

    private float tapTimeout = 2;
    private float lastTapTime = 0;
    private float[] tapTime = new float[256];
    public static int tap = 0;
    public static bool customBeat;

    private bool resetBeat = false;

    private int beatDivider = 8;

    public int beatCount = 0;
    public bool enableBeatTrack = true;
    public int barCount = 0;
    public bool enableBarTrack = true;
    public int bar4Count = 0;
    public int bar8Count = 0;
    public int bar16Count = 0;


    [Header("UI")]
    public Text bpmText;
    public Text beatCountText;

    [Header("Events")]
    public ClockEnvent clockEnvent;

    void Awake()
    {
       if(instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject); 
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ResetClock();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            ResetBeat();
        }

        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            enableBeatTrack = !enableBeatTrack;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            enableBarTrack = !enableBarTrack;
        }

        if (
            MidiMaster.GetKnob(
                MidiMap.channel, 
                (int)MidiMapCC.ClockPrecisionUp) > 0
        ){
            clockPrecision = 4;
            Debug.Log("clockPrecision up");
        }

        if (
            MidiMaster.GetKnob(
                MidiMap.channel, 
                (int)MidiMapCC.ClockPrecisionDown) > 0
        ){
            clockPrecision = 0;
            Debug.Log("clockPrecision down");
        }

        Tapping();
        BeatDetection();
    }

    void Tapping()
    {

        if (
            Input.GetKeyDown(KeyCode.Space) ||
            MidiMaster.GetKeyDown(MidiMap.channel, (int)MidiMapNote.ClockTap)
        )
        {
            Debug.Log("Clock Tap ");


            float deltaTime = Time.realtimeSinceStartup - lastTapTime;
            lastTapTime = Time.realtimeSinceStartup;

            if (deltaTime > tapTimeout)
            {
                tap = 0;
                for (int i = 0; i < tapTime.Length; i++)
                {
                    tapTime[i] = 0;
                }
            }

            if (tap < tapTime.Length)
            {
                tapTime[tap] = Time.realtimeSinceStartup;
                tap++;
            }

            if (tap > 3)
            {
                float averageTime = 0;

                for (int i=1; i < tap; i++)
                {
                    averageTime += tapTime[i] - tapTime[i-1];
                }
                averageTime /= tap-1;

                bpm = (float)System.Math.Round((double)60 / averageTime, clockPrecision);

            }

            if(tap >= tapTime.Length)
            {
                tap = 0;
            }

            if(tap % 4 == 0){
                clockTimer = 0;
                clockCount = 0;
            }

            bpmText.color = Color.red;
        }

        if (
            Input.GetKeyUp(KeyCode.Space) ||
            MidiMaster.GetKeyUp(MidiMap.channel, (int)MidiMapNote.ClockTap)
        )
        {
            bpmText.color = Color.white;
        }

    }

    void ResetClock()
    {
        tap = 0;
        clockTimer = 0;
        clockCount = 0;
    }

    void ResetBeat()
    {
        resetBeat = true;
    }

    void BeatDetection()
    {

        clockTrig = false;
        clockInterval = 60 / bpm;

        clockInterval /= beatDivider;

        clockTimer += Time.deltaTime;
        if(clockTimer >= clockInterval)
        {
            clockTimer -= clockInterval;
            clockTrig = true;
            clockCount++;

            if (resetBeat)
            {
                beatCount = 0;
                barCount = 0;
                bar4Count = 0;
                bar8Count = 0;
                bar16Count = 0;

                resetBeat = false;
            }

            if(clockCount % beatDivider == 0)
            {
                if (enableBeatTrack)
                {
                    clockEnvent.Invoke(ClockEventType.Beat);
                
                }

                beatCount++;

                if (beatCount % 4 == 0)
                {
                    if (enableBarTrack)
                    {
                        clockEnvent.Invoke(ClockEventType.Bar);
                    }

                    barCount++;
                }

                if (beatCount % 8 == 0)
                {
                    clockEnvent.Invoke(ClockEventType.Bar4);
                    bar4Count++;
                }

                if (beatCount % 16 == 0)
                {
                    clockEnvent.Invoke(ClockEventType.Bar8);
                    bar8Count++;
                }

                if (beatCount % 32 == 0)
                {
                    clockEnvent.Invoke(ClockEventType.Bar16);
                    bar16Count++;
                }

            }

            beatCountText.text = beatCount.ToString();
        }

        bpmText.text = bpm.ToString();
    }
}
