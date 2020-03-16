using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;

public struct RoomData : IComponentData {
    public float w,h,x,y,d;
    public int id;
}
public class RoomsGenerator : MonoBehaviour
{   
    public bool roomPlaneEnabled = true;
    private uint oldSeed = 0;
    [SerializeField]
    // [ShowProperty]
    private uint seed = 1;

    public GameObject roomPlane;
    public List<GameObject> objects;
    public Camera mainCamera;
    private float oldCameraFov = 0;
    public Vector2 roomDepthRange = new Vector2(1,4);

    private float gridSize = 1;
    public float gutter = 0.0f;
    public int cols = 4;
    private int rows;

    public uint maxSplits = 12;
    public uint maxIteration = 3;

    private List<GameObject> roomsPool;
    private List<GameObject> rooms;
    private Unity.Mathematics.Random random;

    private List<RoomData> roomsData;

    private RaymarchHelper raymarchHelper;
    public Light pointLight;

    [Range(0,10)]
    public int sceneIndex = 0;

    [Range(0,1)]
    public float cameraShift = 0;
    public float cameraShiftAngle = 0;
    public float cameraShiftAngleDivergence = 0;

    public GameObject roomBoxPrefab;

    [Header("Entity objs")]
    public List<GameObject> objPrefabs;
    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;
    private List<Entity> masterObjsEntities;
    private List<float> masterObjsScale;
    private List<Entity> spawnedObjs;

    [Header("Material")]
    public Vector3 gradientDirection;

    void Start()
    {
        random = new Unity.Mathematics.Random(seed + 2345831274); 

        rooms = new List<GameObject>();
        roomsPool = new List<GameObject>();
        roomsData = new List<RoomData>();

        raymarchHelper = GetComponent<RaymarchHelper>();
        roomPlane.SetActive(false);

        InitializeObjEntities();
    }   
    
    private void Update() {
        if(oldSeed != seed && seed > 0){
            oldSeed = seed;
            random = new Unity.Mathematics.Random(seed + 2345831274); 
            CalcRows();
            GenerateRooms();
            UpdateCameraPosizion();
        }
        
        if(oldCameraFov != mainCamera.fieldOfView){
            oldCameraFov = mainCamera.fieldOfView;
            CalcRows(); 
            UpdateCameraPosizion();
        }

        UpdateMaterial();
    }

    void CalcRows(){
        rows = (int)((float)cols/mainCamera.aspect);
        gridSize = 6f/(float)cols;
    }

    void GenerateRooms()
    {
        float totW = cols * gridSize;
        float totH = rows * gridSize;

        roomsData = GenerateRoomsData(
            new RoomData { 
                x = 0,
                y = 0,
                w = totW,
                h = totH
            });
        
        GameObject room;
        // float boundsOffset = 0.01f;

        RelaseAllRooms();

        if(roomPlaneEnabled){
            foreach(RoomData rd in roomsData){
                RoomData roomData = rd; 

                room = GetRoomFromPool();    
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

        GenerateRoomEntity();
    }

    void UpdateMaterial(){
        int i = 0;
        foreach(GameObject room in rooms){
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
                planeMat.SetInt("_SceneIndex", sceneIndex);

                if(pointLight){
                    planeMat.SetVector("_PointLight", new Vector4(
                        pointLight.transform.position.x,
                        pointLight.transform.position.y,
                        pointLight.transform.position.z,
                        pointLight.range
                    ) );
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
        mainCamera.farClipPlane = camDist * roomDepthRange.y;
    }

    GameObject GetRoomFromPool(){
        GameObject obj;
        if(roomsPool.Count == 0){
            obj = Instantiate(roomPlane);
        }else{
            obj = roomsPool[0];
            roomsPool.RemoveAt(0);
        }
        rooms.Add(obj);
        return obj;        
    }
    void RelaseAllRooms(){
        foreach(GameObject obj in rooms){
            obj.SetActive(false);
            roomsPool.Add(obj);
        }
        rooms = new List<GameObject>();
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
                
                // masterObjsScale.Add(0.3f);
            }
        }
    }

    private void OnDestroy() {
        blobAssetStore.Dispose();
    }

    //ECS
    void GenerateRoomEntity(){
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        float spawnRadius = 1;

        foreach(var e in spawnedObjs){
            entityManager.DestroyEntity(e);
        }

        int count = 30;
        int rndInx;
        
        if(masterObjsEntities.Count==0){
            return;
        }

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
                    up = math.up()
                }
            );

            var pos = random.NextFloat3(-spawnRadius, spawnRadius);
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
                End = scale,
                Time = 0f,
                Speed = 2f
            });

            entityManager.AddComponentData(obj, new Scale
            {
                Value = scale,
            });
        }

    }
}