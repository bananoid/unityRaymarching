using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Lasp;

[ExecuteInEditMode]
public class RaymarchHelper : MonoBehaviour
{
    public PostProcessProfile _profile;
    PostProcessVolume _volume;   
    public float audioPeakIntensity = 10;
    public float speed = 1;

    public RaymarchPostProcess raymarchPostProcess; 
    // public PostProcessVolume postProcessVolume;
    // public PostProcessEffectSettings 
    // Start is called before the first frame update
    void Start()
    {
        // _volume = GetComponent<PostProcessVolume>();
        // raymarchPostProcess = _volume.sharedProfile.GetSetting<RaymarchPostProcess>();
        // raymarchPostProcess = _volume.profile.GetSetting<RaymarchPostProcess>();
        // raymarchPostProcess = ScriptableObject.CreateInstance<RaymarchPostProcess>();
        
        // _profile = ScriptableObject.CreateInstance<PostProcessProfile>();
        _profile.hideFlags = HideFlags.DontSave;

        raymarchPostProcess = _profile.GetSetting<RaymarchPostProcess>();

        _volume = gameObject.AddComponent<PostProcessVolume>();
        _volume.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        _volume.sharedProfile = _profile;
        _volume.isGlobal = true;
        _volume.priority = 1000;
        
        raymarchPostProcess.cumTime.value = 0;
        
        LateUpdate();
    }

    // Update is called once per frame
    // void Update()
    // {
    //     raymarchPostProcess.cumTime += Time.deltaTime * speed;        
    // }

    void OnDestroy(){
        // DestroyAsset(raymarchPostProcess);
        // DestroyAsset(_profile);
    }

    float fraction(float value){
        return value - (int) value;
    }
    void LateUpdate(){
        float audioPeak = MasterInput.GetPeakLevel(FilterType.LowPass);
        float cumTime = raymarchPostProcess.cumTime.value;
        cumTime += Time.deltaTime * (speed + (1 + audioPeak * audioPeakIntensity));
        float mod = Mathf.PI * 20;
        cumTime = fraction(cumTime / mod) * mod;
   
        raymarchPostProcess.cumTime.value = cumTime;
    }

    static void DestroyAsset(Object o){
        if (o == null) return;
        if (Application.isPlaying)
            Object.Destroy(o);
        else
            Object.DestroyImmediate(o);
    }
}
