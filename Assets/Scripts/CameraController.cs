using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Klak.Syphon;

public class CameraController : MonoBehaviour
{
    public SyphonServer syphonServer;
    // Start is called before the first frame update
    void Start()
    {
        syphonServer = GetComponent<SyphonServer>();
        Screen.SetResolution(1920/2,1080/2,false);
        syphonServer.enabled = true;
    }

    // Update is called once per frame
    // void Update()
    // {
    //     if(Input.GetKeyDown(KeyCode.S))
    //     {
    //         Screen.SetResolution(1920,1080,false);
    //         syphonServer.enabled = true;
    //     }
    // }
}
