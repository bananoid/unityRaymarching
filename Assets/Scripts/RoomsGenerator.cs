﻿using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;

public struct RoomData : IComponentData {
    public float w,h,x,y,d;
    public int id;
}

public class RoomsGenerator : MonoBehaviour
{   
    [Header("Seed")]
    private uint oldSeed = 0;
    [SerializeField]
    private uint seed = 1;
    
    private Unity.Mathematics.Random random;

    [Header("Camera")]
    public Camera mainCamera;
    [Range(0,1)] public float cameraFov01 = 0.4f; 
    private float2 camFovRange = new float2(2.0f,170f);
    private float oldCameraFov01 = 0;
    [Range(0,1)] public float cameraShift = 0;
    public float cameraShiftAngle = 0;
    public float cameraShiftAngleDivergence = 0;

    [Header("Light")]
    public Light pointLight;
    
    [Header("Room Layout")]
    public int cols = 4;
    public uint maxSplits = 12;
    public uint maxIteration = 3;
    public float gutter = 0.0f;
    public Vector2 roomDepthRange = new Vector2(1,4);
    private float gridSize = 1;
    private int rows;
    private float totW;
    private float totH;

    [Header("RM Panels")]
    public bool roomPlaneEnabled = true;
    public GameObject roomPlanelPref;
    [Range(0,10)]
    public int rmSceneIndex = 0;

    private List<RoomData> roomsData;

    private List<GameObject> rmPanelsPool;
    private List<GameObject> rmPanels;

    [Header("Entity objs")]
    public bool entityObjsEnabled = true;
    public GameObject roomBoxPrefab;
    public Shader objectShader;
    public List<GameObject> objPrefabs;
    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;
    private List<Entity> masterObjsEntities;
    private List<float> masterObjsScale;
    private List<Entity> spawnedObjs;

    void Start()
    {
        random = new Unity.Mathematics.Random(seed + 2345831274); 

        rmPanels = new List<GameObject>();
        rmPanelsPool = new List<GameObject>();
        roomsData = new List<RoomData>();

        roomPlanelPref.SetActive(false);

        InitializeObjEntities();
    }   
    
    private void Update() {
        if(oldSeed != seed && seed > 0){
            oldSeed = seed;
            random = new Unity.Mathematics.Random(seed + 2345831274); 
            CalcRows();
            GenerateRooms();
            UpdateCamera();
        }
        
        if(oldCameraFov01 != cameraFov01){
            oldCameraFov01 = cameraFov01;
            CalcRows(); 
            UpdateCamera();
        }

        UpdateMaterial();
    }

    void CalcRows(){
        rows = (int)((float)cols/mainCamera.aspect);
        gridSize = 6f/(float)cols;
        totW = cols * gridSize;
        totH = rows * gridSize;
    }

    void GenerateRooms()
    {
        roomsData = GenerateRoomsData(
            new RoomData { 
                x = 0,
                y = 0,
                w = totW,
                h = totH
            });
    
        GenerateRaymarchPanels();    
        
        GenerateRoomEntity();

    }

    void UpdateMaterial(){
        int i = 0;
        foreach(GameObject room in rmPanels){
            i++;

            GameObject plane = room.transform.GetChild(0)?.gameObject;
            
            Vector3 scale = room.transform.localScale;
            Vector3 position = room.transform.localPosition;

            if(plane){
                Material planeMat = plane.GetComponent<Renderer>().material;
                planeMat.SetVector("_PlaneBox", new Vector4(
                    position.x,position.y,
                    scale.x, scale.y
                ));
                planeMat.SetFloat("_RoomDepth", scale.z);
                planeMat.SetInt("_SceneIndex", rmSceneIndex);

                if(pointLight){
                    planeMat.SetVector("_PointLight", new Vector4(
                        pointLight.transform.position.x,
                        pointLight.transform.position.y,
                        pointLight.transform.position.z,
                        pointLight.range
                    ) );
                }else{
                    Debug.Log("no point light");
                }   

                planeMat.SetFloat("_CameraShift", cameraShift);
                float csa = cameraShiftAngle + cameraShiftAngleDivergence*i;
                planeMat.SetFloat("_CameraShiftAngle", csa);
            }
        }
    }

    List<RoomData> GenerateRoomsData(
        RoomData inRect = new RoomData(),
        uint iteration = 0,
        List<RoomData> rd = null
    )
    {
        
        if(rd == null){
            rd = new List<RoomData>();
        }

        float maxSplits = this.maxSplits;    
        uint numSplits = (uint) ((random.NextFloat()*maxSplits) % maxSplits) + 1;
        
        bool hSplit = iteration % 2 == 0;

        float parentCellPos;
        float parentCellSize;
        
        if(hSplit){
            parentCellPos = inRect.x;
            parentCellSize = inRect.w; 
        }else{
            parentCellPos = inRect.y;
            parentCellSize = inRect.h; 
        }

        float splitSize = parentCellSize / (float)numSplits;
        splitSize = math.ceil(splitSize / gridSize) * gridSize; 
       
        float cellPos = parentCellPos;
        float cellSize;

        RoomData roomData = inRect;

        for(int i=0; i<numSplits; i++){
        
            cellPos += i > 0 ? splitSize : 0;
            cellSize = splitSize;  

            float rSize = (parentCellPos + parentCellSize) - (cellPos + cellSize);
            if(rSize <= 0){
                cellSize += rSize;
            }

            if(hSplit){
                roomData.x = cellPos;
                roomData.w = cellSize;
            }else{
                roomData.y = cellPos;
                roomData.h = cellSize;
            }

            roomData.d = random.NextFloat(
                roomDepthRange.x, 
                roomDepthRange.y);

            if(iteration < maxIteration){
                GenerateRoomsData(roomData, iteration+1, rd);
            }else{
                RoomData r = roomData;
                
                r.x += gutter;
                r.y += gutter;
                r.w -= gutter * 2;
                r.h -= gutter * 2;

                rd.Add(r);
            }    

            if(rSize <= 0){
                break;
            }
        }

        return rd;
    }

    void UpdateCamera(){
        float camDist;

        float fov = math.remap(0,1,camFovRange.x,camFovRange.y ,cameraFov01); 

        //Vertical Fit
        // float fovRad = fov * Mathf.Deg2Rad * 0.5f;
        // camDist = totH / (Mathf.Tan(fovRad)*2.0f);

        //Horizontal Fit
        var radAngle = fov * Mathf.Deg2Rad;
        var radHFOV = Mathf.Atan(Mathf.Tan(radAngle / 2) * mainCamera.aspect);
        camDist = totW / (Mathf.Tan(radHFOV)*2.0f);
        
        mainCamera.transform.position = new Vector3(0,0,-camDist);
        mainCamera.farClipPlane = camDist * roomDepthRange.y;

        mainCamera.fieldOfView = fov;
    }

    GameObject GetRMPanelFromPool(){
        GameObject obj;
        if(rmPanelsPool.Count == 0){
            obj = Instantiate(roomPlanelPref);
        }else{
            obj = rmPanelsPool[0];
            rmPanelsPool.RemoveAt(0);
        }
        rmPanels.Add(obj);
        return obj;        
    }

    void ClearRMPanels(){
        foreach(GameObject obj in rmPanels){
            obj.SetActive(false);
            rmPanelsPool.Add(obj);
        }
        rmPanels.Clear();
    }

    void InitializeObjEntities(){
        spawnedObjs = new List<Entity>();

        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        blobAssetStore = new BlobAssetStore();
        var settings = GameObjectConversionSettings.FromWorld(world, blobAssetStore);

        masterObjsEntities = new List<Entity>();
        masterObjsScale = new List<float>();
        
        int variationsCount = 2;
        foreach(var obj in objPrefabs){
            for(int i=0; i<variationsCount; i++){     
                var entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(obj,settings);
                masterObjsEntities.Add(entity);

                float scale = random.NextFloat(1.0f, 10.0f) * 0.04f;
                masterObjsScale.Add(scale);                
            }
        }
    }

    private void OnDestroy() {
        blobAssetStore.Dispose();
    }

    void GenerateRaymarchPanels(){
        ClearRMPanels();

        if(!roomPlaneEnabled){
            return;
        }

        GameObject room;
        foreach(RoomData rd in roomsData){
            RoomData roomData = rd; 

            room = GetRMPanelFromPool();    
            room.transform.parent = transform;
            room.SetActive(true);

            Vector3 scale = new Vector3(roomData.w, roomData.h, roomData.d);
            float z = roomData.d * 0.5f;
            Vector3 position = new Vector3(roomData.x,roomData.y, z);
            
            position.x -= totW * 0.5f - roomData.w * 0.5f;
            position.y -= totH * 0.5f - roomData.h * 0.5f;

            room.transform.localScale = scale;
            room.transform.localPosition = position;
        }
    }

    //ECS
    void GenerateRoomEntity(){
        foreach(var e in spawnedObjs){
            entityManager.DestroyEntity(e);
        }

        if(!entityObjsEnabled){
            return;
        }

        int rndInx;
        
        if(masterObjsEntities.Count==0){
            return;
        }
    
        foreach(var roomData in roomsData){
            float4 roomRect; 

            roomRect.x = roomData.x/totW;
            roomRect.y = roomData.y/totH;
            roomRect.z = roomRect.x + roomData.w/totW;
            roomRect.w = roomRect.y + roomData.h/totH;
            
            int count = (int)((roomData.w*roomData.h)/6 * 10) + 1;

            for(int i=0; i<count; i++){
                rndInx = random.NextInt(masterObjsEntities.Count);
                
                var masterObj = masterObjsEntities[rndInx];
                var scale = masterObjsScale[rndInx];

                var obj = entityManager.Instantiate(masterObj);
                spawnedObjs.Add(obj);

                entityManager.SetComponentData(obj,
                    new RoomObjectComponent
                    {
                        weight = random.NextFloat(0.3f, 2f),
                        // weight = 1,
                        up = math.up()
                    }
                );

                var pos = new float3(roomData.x,roomData.y,0);
                pos.x -= totW * 0.5f - roomData.w * 0.5f;
                pos.y -= totH * 0.5f - roomData.h * 0.5f;

                entityManager.SetComponentData(obj,
                    new Translation { Value = pos }
                );

                var rot = random.NextQuaternionRotation();
                entityManager.SetComponentData(obj,
                    new Rotation { Value = rot }
                );
                    
                entityManager.AddComponentData(obj, new ImpulseData
                {
                    Start = scale * 1.5f,
                    // Start = scale,
                    End = scale,
                    Time = 0f,
                    Speed = 2f
                });

                entityManager.AddComponentData(obj, new Scale
                {
                    Value = scale,
                });
                
                var renderMesh = entityManager.GetSharedComponentData<RenderMesh>(obj);
                
                var material = new Material(objectShader);
                // var material = renderMesh.material;
                
                material.SetVector("roomRect", roomRect );

                float h = random.NextFloat(-0.1f,0.1f);
                h = h%1;
                if(h<0){
                    h = 1+h;
                }
                var color = Color.HSVToRGB(
                    h,
                    0.9f,
                    1
                );
                material.SetVector("colorA", color );
                
                var gradient = random.NextFloat4();
                gradient.w = random.NextFloat(0.5f,1.1f);
                material.SetVector("gradientDesc", gradient );

                Mesh mesh = renderMesh.mesh;

                entityManager.AddSharedComponentData(obj, new RenderMesh
                {
                    mesh = mesh,
                    material = material,
                });
            }

        }

    }
}