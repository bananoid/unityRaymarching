using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using MidiJack;
using UnityEngine.UI;

public struct RoomData : IComponentData {
    public float w,h,x,y,d;
    public float id;
}

[System.Serializable]
public class RoomPresetParam{
    public float2 range;
    public bool interpolate;
    public float speed;
    public float endValue;
    public float value;
}

[System.Serializable]
public enum RoomPresetKeys {
    // cols,
    // maxSplits,
    cameraFov,
    cameraShift,
    cameraShiftAngle,
}

[System.Serializable]
public class RoomPreset {
    public Dictionary<RoomPresetKeys, RoomPresetParam> parameters;
}

public class RoomsGenerator : MonoBehaviour
{       
    [Header("Texture")]
    public List<Texture> colorTextures;
    private Texture colorTexture;
    [Range(0,1)] public float colorTextureInxF = 0;
    [Range(0,1)] public float colorMaskTh = 0;
    [Range(0,1)] public float colorMaskIntesity = 0.5f; 
    [Range(0,1)] public float colorSpread = 1; 

    [Header("UI")]
    public Text textColorTexInx;
    public Text textSeedSpeed;
    public Text textSceneIndexMin;
    public Text textSceneIndexMax;
    public Text textSceneIndex;
    private int2 sceneIndexRange = new int2(0,4);

    [Header("Seed")]
    private uint oldSeed = 0;
    [SerializeField]
    private uint seed = 1;
    
    private Unity.Mathematics.Random random;

    [Header("Camera")]
    public Camera mainCamera;
    // [Range(0,1)] public float cameraFov01 = 0.4f; 
    // private float2 camFovRange = new float2(4.0f,170f);
    private float oldCameraFov01 = 0;
    [Range(0,1)] public float cameraShiftIntensity = 1;
    // public float cameraShiftAngle = 0;
    public float cameraShiftAngleDivergence = 0;

    [Header("Light")]
    public Light pointLight;
    public float pointLightRoomCenter;
    
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
    public int RMSceneCount = 4;

    public bool raymarchEnabled = true;
    public GameObject roomPlanelPref;
    [Range(0,10)] public int rmSceneIndex = 0;
    [Range(0,0.1f)] public float rmRndScale = 0; 

    private List<RoomData> roomsData;

    private List<GameObject> rmPanelsPool;
    private List<GameObject> rmPanels;

    [Header("Entity objs")]
    public GameObject roomBoxPrefab;
    public Shader objectShader;
    public List<GameObject> objPrefabs;
    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;
    private List<Entity> masterObjsEntities;
    private List<float> masterObjsScale;
    private List<Entity> spawnedObjs;

    [Header("Glitch")]
    public int glitchType = 1;
    [Range(0,1)] public float glitchIntensity = 1;
    public float glitchSpeed = 1;
    public float glitchScale = 1;

    [Header("Clock")]
    public ClockEventType seedClockSpeed = 0;

    [Header("Presets")]
    private List<RoomPreset> presets;
    private int currentPresetIndex = -1;
    private RoomPreset currentPreset;

    [Header("Line")]
    float cumTime = 0;
    float cumTimeSpeed = 1;
    public float lineIntesity = 1;
    public float lineSize = 0.5f;
    public float lineFreq = 30f;

    RoomsSystem roomsSystem;
    void Start()
    {
        roomsSystem = World
                        .DefaultGameObjectInjectionWorld
                        .GetOrCreateSystem<RoomsSystem>();

        random = new Unity.Mathematics.Random(seed + 2345831274); 

        SetFactoryPreset();

        rmPanels = new List<GameObject>();
        rmPanelsPool = new List<GameObject>();
        roomsData = new List<RoomData>();

        roomPlanelPref.SetActive(false);

        InitializeObjEntities();
    }   
    
    private void Update() {

        cumTime += Time.deltaTime * cumTimeSpeed;
        if(cumTime > 1000){
            cumTime = 0;
        }

        UpdateParamsFromInput();
        SetColorTexture();
        UpdateCurrentPreset();

        CalcRows();

        if(oldSeed != seed && seed > 0){
            oldSeed = seed;
            random = new Unity.Mathematics.Random(seed + 2345831274); 
            GenerateRooms();
            UpdateCamera();
        }
        
        UpdateCamera();

        UpdateMaterial();
    }

    void CalcRows(){
        rows = (int)((float)cols/mainCamera.aspect);
        gridSize = 6f/(float)cols;
        totW = cols * gridSize;
        totH = rows * gridSize;
    }

    void SetColorTexture(){
        int inx = (int)math.round(colorTextureInxF * (colorTextures.Count-1));
        colorTexture = colorTextures[inx];   
        textColorTexInx.text = "CtInx: "+ inx; 
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
                // planeMat.SetInt("_SceneIndex", rmSceneIndex);

                if(pointLight){
                    float3 worldCenter = pointLight.transform.position;
                    float3 roomCenter = new float3(
                        position.x,
                        position.y,
                        pointLight.transform.position.z
                    );

                    float3 lp = math.lerp(worldCenter, roomCenter, pointLightRoomCenter);
                    
                    float4 lightDesc = new float4(
                        lp.x,
                        lp.y,
                        lp.z,
                        pointLight.range
                    );

                    planeMat.SetVector("_PointLight", lightDesc);    
                    roomsSystem.lightDesc = lightDesc;
                
                }else{
                    Debug.Log("no point light");
                }   

                planeMat.SetFloat("_CameraShift", getCurParam(RoomPresetKeys.cameraShift) * cameraShiftIntensity);
                
                float csa = getCurParam(RoomPresetKeys.cameraShiftAngle) + cameraShiftAngleDivergence*i;
                planeMat.SetFloat("_CameraShiftAngle", csa);

                planeMat.SetFloat("rndScale", rmRndScale);
                planeMat.SetInt("_EnableRM", raymarchEnabled ? 1 : 0);
                
                planeMat.SetFloat("_GlitchIntensity", glitchIntensity);
                planeMat.SetFloat("_GlitchSpeed", glitchSpeed);
                planeMat.SetFloat("_GlitchScale", glitchScale);
                planeMat.SetInt("_GlitchType", glitchType);
                

                planeMat.SetFloat("_CumTime", cumTime);

                planeMat.SetFloat("lineIntesity", lineIntesity);
                planeMat.SetFloat("lineSize", lineSize);
                planeMat.SetFloat("lineFreq", lineFreq);

                // planeMat.SetFloat("ColorMaskTh", colorMaskTh);
                // planeMat.SetFloat("ColorMaskIntesity", colorMaskIntesity);
                // planeMat.SetFloat("ColorSpread", colorSpread);

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
        numSplits = math.max(numSplits, 1);
        
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
            
            if(cellSize < gridSize){
                break;
            }

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
                r.id = random.NextFloat();

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

        float fov = getCurParam(RoomPresetKeys.cameraFov); 

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
        
        int variationsCount = 10;
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

            GameObject plane = room.transform.GetChild(0)?.gameObject;
            if(plane){            
                int rndSceneInx = random.NextInt(sceneIndexRange.x,sceneIndexRange.y+1);
                Material planeMat = plane.GetComponent<Renderer>().material;
                
                planeMat.SetFloat("_Id", rd.id);

                planeMat.SetInt("_SceneIndex", rndSceneInx);
                planeMat.SetTexture("_MainTex", colorTexture);

                textSceneIndex.text = "SINX "+ rndSceneInx;

            }
        }
    }

    //ECS
    void GenerateRoomEntity(){
        foreach(var e in spawnedObjs){
            entityManager.DestroyEntity(e);
        }

        if(raymarchEnabled){
            return;
        }

        int rndInx;
        
        if(masterObjsEntities.Count==0){
            return;
        }

        float seed = random.NextFloat();
    
        foreach(var roomData in roomsData){
            float4 roomRect; 

            float tH = totW / mainCamera.aspect;
            float hOff = (tH - totH) * 0.5f;

            roomRect.x = roomData.x/totW;
            roomRect.y = (roomData.y+hOff)/tH;
            roomRect.z = roomRect.x + roomData.w/totW;
            roomRect.w = roomRect.y + roomData.h/tH;
            
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

                pos += random.NextFloat3(-0.2f,0.2f) * new float3(roomData.w,roomData.h,1);

                entityManager.SetComponentData(obj,
                    new Translation { Value = pos }
                );

                var rot = random.NextQuaternionRotation();
                entityManager.SetComponentData(obj,
                    new Rotation { Value = rot }
                );
                    
                entityManager.AddComponentData(obj, new ImpulseData
                {
                    Start = scale * 1.1f,
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

                // float h = random.NextFloat(-0.05f,0.05f);
                // h = h%1;
                // if(h<0){
                //     h = 1+h;
                // }
                // var color = Color.HSVToRGB(
                //     h,
                //     0.9f,
                //     1
                // );
                // material.SetVector("colorA", color );
                
                var gradient = random.NextFloat4();
                gradient.w = random.NextFloat(0.5f,1.1f);
                material.SetTexture("_MainTex", colorTexture);
                material.SetVector("gradientDesc", gradient);
                
                material.SetFloat("_Seed", seed);
                material.SetFloat("_ObjId", random.NextFloat());

                material.SetFloat("ColorMaskTh", colorMaskTh);
                material.SetFloat("ColorMaskIntesity", colorMaskIntesity);
                material.SetFloat("ColorSpread", colorSpread);

                Mesh mesh = renderMesh.mesh;

                entityManager.AddSharedComponentData(obj, new RenderMesh
                {
                    mesh = mesh,
                    material = material,
                });
            }

        }

    }

    //Clock Triggers
    public void ClockTrigged(ClockEventType type){

        if(type == seedClockSpeed){
            seed += 1;
            LoadPreset(0);
        }
    }

    //Presets
    public void SetFactoryPreset(){
        presets = new List<RoomPreset>();

        RoomPreset preset;

        float defIntSpeed = 2f;
        //Preset 0
        preset = new RoomPreset{
            parameters = new Dictionary<RoomPresetKeys, RoomPresetParam>()
            {
                {RoomPresetKeys.cameraFov, new RoomPresetParam{
                    range = new float2(5f,30f),
                    interpolate = true,
                    speed = defIntSpeed,
                }},
                {RoomPresetKeys.cameraShift, new RoomPresetParam{
                    range = new float2(0,1),
                    interpolate = true,
                    speed = defIntSpeed,
                }},
                {RoomPresetKeys.cameraShiftAngle, new RoomPresetParam{
                    range = new float2(0,10),
                    interpolate = true,
                    speed = defIntSpeed,
                }},
            }    
        };
        presets.Add(preset);

        LoadPreset(0);
    }

    public void LoadPreset(int inx){
        inx = math.clamp(inx, 0, presets.Count-1);
        currentPresetIndex = inx;
        currentPreset = presets[inx];
        RandomizePreset(currentPreset);
    }

    public void RandomizePreset(RoomPreset preset){
        foreach(var dVal in preset.parameters){    
            var param = dVal.Value;

            param.value = random.NextFloat(param.range.x,param.range.y);    
            if(param.interpolate){
                param.endValue = random.NextFloat(param.range.x,param.range.y);    
            }else{
                param.endValue = param.value;
            }
        }
    }

    public void UpdateCurrentPreset(){
        if(currentPreset == null){
            return;
        }

        foreach(var dVal in currentPreset.parameters){   
            var param = dVal.Value; 
            if(!param.interpolate){
                continue;
            }
            param.value = math.lerp(param.value, param.endValue, param.speed * Time.deltaTime);
        }
    }

    public float getCurParam(RoomPresetKeys key){
        return currentPreset.parameters[key].value;
    }

    void UpdateParamsFromInput(){
        //Screen splits
        float maxSplitsF = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.RGMaxSplit ); 
        maxSplits = (uint)math.remap(0,1,1,4,maxSplitsF);

        //Seed Clocl Speed
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            seedClockSpeed = ClockEventType.Beat;
            textSeedSpeed.text = "SS: Beat";
        }
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            seedClockSpeed = ClockEventType.Bar;
            textSeedSpeed.text = "SS: Bar";
        }
        if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            seedClockSpeed = ClockEventType.Bar4;
            textSeedSpeed.text = "SS: Bar4";
        }
        if(Input.GetKeyDown(KeyCode.Alpha4))
        {
            seedClockSpeed = ClockEventType.Bar8;
            textSeedSpeed.text = "SS: Bar8";
        }

        //Camera shift
        cameraShiftIntensity = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.RGCameraShiftIntensity );

        //Scene Index Random Range 
        float sceneIndexF = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.RGSceneIndex ) * RMSceneCount; 
        float sceneIndexSpreadF = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.RGSceneIndexSpread ) * RMSceneCount * 0.5f;      
        sceneIndexRange.x = (int)(sceneIndexF - sceneIndexSpreadF);
        sceneIndexRange.y = (int)(sceneIndexF + sceneIndexSpreadF);
        sceneIndexRange.x = math.clamp(sceneIndexRange.x, 0,RMSceneCount);
        sceneIndexRange.y = math.clamp(sceneIndexRange.y, 0,RMSceneCount);
        textSceneIndexMin.text = "SINX min "+sceneIndexRange.x;
        textSceneIndexMax.text = "SINX max "+sceneIndexRange.y;

        //RayMarch Enable toggle
        if(Input.GetKeyDown(KeyCode.Alpha0))
        {
            raymarchEnabled = !raymarchEnabled;
        }

        //Glitch
        glitchIntensity = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.GlitchIntesity ) * 4;

        //PointLight
        float smoothSpeed = 3;

        float lrc = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.PointLightRoomCenter, 0.5f );
        pointLightRoomCenter =  math.lerp(pointLightRoomCenter, lrc, smoothSpeed * Time.deltaTime);

        float lr = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.PointLightSize, 1 ) * 20;
        pointLight.range = math.lerp(pointLight.range, lr, smoothSpeed * Time.deltaTime);
        
        float3 plPos = pointLight.transform.position; 
        plPos.z = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.PointLightZ, 0.5f ) * 15 - 5;
        pointLight.transform.position = math.lerp(pointLight.transform.position, plPos, smoothSpeed * Time.deltaTime);

        //Color
        colorTextureInxF =  MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.ColorTexInx, 0.5f );
        colorMaskTh = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.ColorMaskTh, 0.5f );
        colorMaskIntesity = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.ColorMaskIntesity, 0.5f );
        colorSpread = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.ColorSpread, 0.5f );

        //Line
        cumTimeSpeed = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.cumTimeSpeed,0.1f);
        lineIntesity = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.lineIntesity,0);
        lineSize = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.lineSize,0.5f);
        lineFreq = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.lineFreq,0f);
        
        //rmRndScale
        rmRndScale = MidiMaster.GetKnob(MidiMap.channel, (int)MidiMapCC.rmRndScale,0f) * 1;
    }
}