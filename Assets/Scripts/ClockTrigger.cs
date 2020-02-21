using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClockTrigger : MonoBehaviour
{
    [Header("UI")]
    public Text bpmText;
    public Text beatCountText;

    [Header("Time")]
    public static ClockTrigger instance;
    public float bpm = 120;
    private float beatInterval, beatTimer;
    public static bool beatFull;
    public static int beatCountFull;

    private float tapTimeout = 2;
    private float lastTapTime = 0;
    public float[] tapTime = new float[16];
    public static int tap = 0;
    public static bool customBeat;
    

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

    // Update is called once per frame
    void Update()
    {
        Tapping();
        BeatDetection();
    }

    void Tapping()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            float deltaTime = Time.realtimeSinceStartup - lastTapTime;
            lastTapTime = Time.realtimeSinceStartup;
            Debug.Log(deltaTime +" > "+ tapTimeout);
            if (deltaTime > tapTimeout)
            {
                tap = 0;
            }

            if (tap < tapTime.Length)
            {
                tapTime[tap] = Time.realtimeSinceStartup;
                tap++;
            }
            if (tap == tapTime.Length)
            {
                float averageTime =
                    (tapTime[1] - tapTime[0]) +
                    (tapTime[2] - tapTime[1]) +
                    (tapTime[3] - tapTime[2]);
                averageTime /= 3;
                bpm = (float)System.Math.Round((double)60 / averageTime, 2);
                tap = 0;
                beatTimer = 0;
                beatCountFull = 0;
            }

            bpmText.color = Color.red;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            bpmText.color = Color.white;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            tap = 0;
            beatTimer = 0;
            beatCountFull = 0;
        }

    }

    void BeatDetection()
    {

        beatFull = false;
        beatInterval = 60 / bpm;
        beatTimer += Time.deltaTime;
        if(beatTimer >= beatInterval)
        {
            beatTimer -= beatInterval;
            beatFull = true;
            beatCountFull++;
            beatCountText.text = beatCountFull.ToString();
        }

        bpmText.text = bpm.ToString();
    }
}
