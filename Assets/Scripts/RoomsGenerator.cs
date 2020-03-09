using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.UIElements;
using Unity.Mathematics;

public class RoomsGenerator : MonoBehaviour
{
    private uint oldSeed = 0;
    [SerializeField]
    // [ShowProperty]
    private uint seed = 1;

    public GameObject roomBox;
    public List<GameObject> objects;
    public Camera mainCamera;
    private float oldCameraFov = 0;
    public int splits = 4;
    public float maxRoomDepth = 4;

    public float gridSize = 1;
    public float gutter = 0.0f;
    public int cols, rows;

    [SerializeField]
    private List<GameObject> roomsPool;
    [SerializeField]
    private List<GameObject> rooms;
    private Unity.Mathematics.Random random;

    void Start()
    {
        rooms = new List<GameObject>();
        roomsPool = new List<GameObject>();
    }

    private void Update() {
        if(oldSeed != seed && seed > 0){
            oldSeed = seed;
            random = new Unity.Mathematics.Random(seed); 
            GenerateRooms();
            UpdateCameraPosizion();
        }
        if(oldCameraFov != mainCamera.fieldOfView){
            oldCameraFov = mainCamera.fieldOfView;
            UpdateCameraPosizion();
        }
    }

    void GenerateRooms()
    {
        cols = splits;
        rows = (int)((float)splits/mainCamera.aspect);

        float totW = cols * gridSize;
        float totH = rows * gridSize;


        GameObject room;
        int roomId = 0;
        float boundsOffset = 0.01f;

        RelaseAllRooms();

        float gutterScale = gridSize - gutter;    
        for (int i = 0; i < cols; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                room = GetRoomFromPool();
                room.transform.parent = transform;
                // room.layer = roomId;
                room.SetActive(true);
                Vector3 scale = new Vector3(gridSize, gridSize, gridSize) * gutterScale;
                Vector3 positon = new Vector3();

                scale.z *= random.NextFloat(1.0f, 4.0f);

                positon.x = i * gridSize - totW * 0.5f + gridSize * 0.5f;
                positon.y = j * gridSize - totH * 0.5f + gridSize * 0.5f;
                positon.z = scale.z * 0.5f;
                room.transform.localScale = scale;
                room.transform.localPosition = positon;

                Material mat = room.GetComponent<Renderer>().sharedMaterial;
                mat.SetVector("minBounds", positon - scale * .5f - Vector3.one * boundsOffset);
                mat.SetVector("maxBounds", positon + scale * .5f + Vector3.one * boundsOffset);

                roomId++;
            }
        }

    }

    void UpdateCameraPosizion(){
        float camDist;
        float totH = rows * gridSize;
        float totW = cols * gridSize;

        //Vertical Fit
        // float fovRad = mainCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
        // camDist = totH / (Mathf.Tan(fovRad)*2.0f);

        //Horizontal Fit
        var radAngle = mainCamera.fieldOfView * Mathf.Deg2Rad;
        var radHFOV = Mathf.Atan(Mathf.Tan(radAngle / 2) * mainCamera.aspect);
        camDist = totW / (Mathf.Tan(radHFOV)*2.0f);
        
        mainCamera.transform.position = new Vector3(0,0,-camDist);
        mainCamera.farClipPlane = camDist + gridSize * maxRoomDepth;
    }

    GameObject GetRoomFromPool(){
        GameObject obj;
        if(roomsPool.Count == 0){
            obj = Instantiate(roomBox);
        }else{
            obj = roomsPool[0];
            roomsPool.RemoveAt(0);
        }
        rooms.Add(obj);
        return obj;        
    }
    void RelaseAllRooms(){
        foreach(GameObject obj in rooms){
            roomsPool.Add(obj);
        }
        rooms = new List<GameObject>();
    }
}