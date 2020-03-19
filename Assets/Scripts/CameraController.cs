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
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F))
        {
            Screen.SetResolution(1920,1080,!Screen.fullScreen);
            syphonServer.enabled = true;
        }
    }
}
