using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.UIElements;
using Unity.Mathematics;

public struct RoomData {
    public float w,h,x,y,d;
    public int id;
}
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
    public float maxRoomDepth = 4;

    public float gridSize = 1;
    public float gutter = 0.0f;
    public int cols = 4;
    public int rows;

    [SerializeField]
    private List<GameObject> roomsPool;
    [SerializeField]
    private List<GameObject> rooms;
    private Unity.Mathematics.Random random;

    public List<RoomData> roomsData;

    void Start()
    {
        rooms = new List<GameObject>();
        roomsPool = new List<GameObject>();
        roomsData = new List<RoomData>();
    }

    private void Update() {
        if(oldSeed != seed && seed > 0){
            oldSeed = seed;
            random = new Unity.Mathematics.Random(seed); 
            CalcRows();
            GenerateRooms();
            UpdateCameraPosizion();
        }
        if(oldCameraFov != mainCamera.fieldOfView){
            oldCameraFov = mainCamera.fieldOfView;
            CalcRows();
            UpdateCameraPosizion();
        }
    }

    void CalcRows(){
        rows = (int)((float)cols/mainCamera.aspect);
    }

    void GenerateRooms()
    {
        GenerateRoomsData();

        float totW = cols * gridSize;
        float totH = rows * gridSize;

        GameObject room;
        float boundsOffset = 0.01f;

        RelaseAllRooms();

        foreach(RoomData roomData in roomsData){
            room = GetRoomFromPool();    
            room.transform.parent = transform;
            room.SetActive(true);

            Vector3 scale = new Vector3(roomData.w, roomData.h, roomData.d);
            float z = roomData.d * 0.5f;
            Vector3 position = new Vector3(roomData.x,roomData.y, z);
            
            position.x -= totW * 0.5f - gridSize * 0.5f;
            position.y -= totH * 0.5f - gridSize * 0.5f;

            room.transform.localScale = scale;
            room.transform.localPosition = position;

            Material mat = room.GetComponent<Renderer>().sharedMaterial;
            mat.SetVector("minBounds", position - scale * .5f - Vector3.one * boundsOffset);
            mat.SetVector("maxBounds", position + scale * .5f + Vector3.one * boundsOffset);
        }

    }

    void GenerateRoomsData(){
        int maxSplits = 4;
        int maxIteration = 3;

        roomsData = new List<RoomData>();

        for (int xi = 0; xi < cols; xi++)
        {
            for (int yi = 0; yi < rows; yi++)
            {
                RoomData roomData = new RoomData();
                roomData.x = xi * gridSize + gutter;
                roomData.y = yi * gridSize + gutter;

                roomData.w = gridSize - gutter*2;
                roomData.h = gridSize - gutter*2;
                roomData.d = gridSize * random.NextFloat(1.0f, 4.0f);
                
                roomsData.Add(roomData);
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