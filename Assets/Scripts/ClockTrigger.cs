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
        BeatDetection();
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
