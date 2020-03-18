#define PI 3.14159265359
#define PHI (1.618033988749895)

float4 _PlaneBox;
float _RoomDepth;

// float4 SineSphere(float3 p){
//     float4 Sphere1 = float4(float3(0.5,0,1), sdSphere(p - _sphere1.xyz, _sphere1.w));
//     Sphere1.w = abs(sin(Sphere1.w * 2.0 + _CumTime )) - 0.6;
//     // Sphere1.w = abs(sin(Sphere1.w * 2.0)) - 0.6;
//     float4 Sphere2 = float4(float3(0.1,0.5,0.9), sdSphere(p - _sphere2.xyz, _sphere2.w));
//     float4 combine = opSS(Sphere1,Sphere2,0.2);    
//     return combine;    
// }
// float random (vec3 st) {
//     return fract(sin(dot(st.xy,
//                          vec3(12.9898,78.233,92.3214)))*
//         43758.5453123);
// }

float random(float st){
    return frac(sin(dot(st, 12.9898))* 43758.5453123);
}

float3 randomPos (float3 st) {
    float3 res; 
    res.x = random(st.x+32.432234);   
    res.y = random(st.y+22.423432);   
    res.z = random(st.z+12.543432);   
    return res;
}

float4 Scene00(float3 p){
    
    float3 boxS = 0;
    boxS.xy = _PlaneBox.zw * 0.5; 
    boxS.z = _RoomDepth;

    // float qPosR = 1.1;
    // float3 qPos = floor(p/qPosR)*qPosR;
    // float3 rndPos = randomPos(p+frac(_Time))*2-1;//
    // float rndScale = 0.8;
    // rndPos *= rndScale;    
        
    float box = sdOpenBox(p, boxS);
    
    float3 boxCenter = float3(0,0,-boxS.z*0.5);

    // boxCenter.z += sin(_Time * 10 * boxS.x) * boxS.z * 0.2;
    // boxCenter.x += cos(_Time * 10.2345 * boxS.x) * boxS.x;
    // boxCenter.y = sin(_Time * 2.45 * boxS.x) * boxS.y;
    float sphere1 = sdSphere(p + boxCenter, 0.5);
    
    boxCenter.z += sin(_Time * 10 * boxS.x) * boxS.z * 0.2;
    boxCenter.x += cos(_Time * 10.2345 * boxS.x) * boxS.x;
    boxCenter.y = sin(_Time * 2.45 * boxS.x) * boxS.y;
    float sphere2 = sdSphere(p+boxCenter, 0.5);
    
    float3 roomColor = float3(0.2,0.4,0.9) * 0.4;
    float3 objColor = float3(0.9,0.2,0.1) * 3;

    float4 combine = opUS(
        float4(objColor,sphere1), 
        float4(objColor,sphere2),
        0.4);

    combine = opUS(
        combine, 
        float4(roomColor,box),
        0.0);    
    
    return combine;
}

float4 Scene01(float3 p){

    rotateAxe(0.2, p.yz);
    // float3 color = (1,0,0);
    // return float4(color, box);
    float zPos = _Time * 20.0;
    float xPos = cos(_Time * 15.0 + 1.3) * 3.;

    float3 sPos = p + _sphere2.xyz + float3(xPos,0,zPos);

    sPos.y += sin(sPos.z * 1.3 + _Time * 10)*0.1;
    
    float mod = 2;
    sPos.xz = (frac((sPos.xz+0.5)/mod)-0.5)*mod;

    float4 Sphere1 = float4(float3(0.5,0.3,0.5), sdSphere(sPos, _sphere2.w));

    float box = sdBox(p, float3(20,0.0,30)) * -1.;
    float4 boxC = float4(float3(0.0,0.5,1.0), box); 
    float4 combine = opSS(Sphere1,boxC,0.0);    
    return combine;
}

float4 Scene02(float3 p){   
    float box = sdBox(p, float3(20,1.0,10)) * -1.;
    float4 boxC = float4(float3(0.0,0.5,1.0), box); 
    return boxC;
}  

//Gyroid
float4 Scene03(float3 p){
    float plane = sdPlane(p, float4(0,0,-1,0.3));
    // float box = sdBox(p, float3(10000,10000,5));

    float scale = 8.;
    p.y += _Time * 1.02;
    p.z += _Time * 1.3450;
    p += _sphere2.xyz;

    p.y *= 0.5;
    float gyroid = abs(sdGyroid(p,scale)) - 0.07;
    
    float combine = max(plane, gyroid);

    float3 color = float3(1,1,1);
    color += smoothstep(0.4, 0.6,gyroid);
    return float4(color, combine);
}


//Petal 6
float4 Scene04(float3 p){  
    float t = _Time * 30;
    float y = 2;
    rotateAxe(PI * .5 + cos(t*0.1) *.3, p.yz);
    float3 coneP = p;
    rotateAxe(3.14, coneP.yz);
    float plane = sdPlane(p, float4(0,1,0,y));
    float cone = sdRoundCone(coneP, 4, 0, 30);

    float3 sPos = p;
    float rep = PI / 3.;

    // sPos.x = length( sPos.xz ) - 3;
    // sPos.x += 4;
    float angle = atan2(sPos.z,sPos.x);
    float dist = distance(sPos.xz, float2(0,0) );
    angle = (frac((angle+0.5)/rep)-0.5)*rep;
    // rotateAxe(_Time * 10,sPos.xz);
    // sPos.x += 4;

    sPos.x = cos(angle) * dist;
    sPos.z = sin(angle) * dist;

    float repGrid = 1.5;
    // sPos.x += 1.5;
    sPos.x -= 5;
    // sPos.xz = (frac((sPos.xz+0.5)/repGrid)-0.5)*repGrid;
    // rotateAxe(t, sPos.xz);
    sPos.y -= t;
    float yRep = _sphere2.w * 1.8;
    sPos.y = (frac((sPos.y+0.5)/yRep)-0.5)*yRep;
    // sPos.y += y + _sphere2.w * 0.5;

    float sphere = sdSphere(sPos, _sphere2.w);
    // float sphere = sdBox(sPos, _sphere2.www * float3(0.5,0.5,0.5));
    // p.y = frac(p.y/rep)*rep;
    // float sphere = sdTorus(p + float3(0,0,0), float2(4,1.05)) * rep;

    float combine = plane;
    combine  = opSS( cone,combine, 4.3);
    combine = opSS( sphere, combine, 0);
    float4 scene = float4(float3(1.0,1.0,1.0),cone); 
    return combine;
}  

float _CameraShift;
float _CameraShiftAngle;
float rndScale;

float4 distanceField(float3 p) {    
    if(rndScale > 0){
        float3 rndPos = randomPos(p+frac(_Time))*2-1;
        rndPos *= rndScale * max( pow(p.z,0.4) , 0.01);
        p += rndPos;
    }

    p.xy -= _PlaneBox.xy;
    
    p.x += p.z * sin(_CameraShiftAngle) * _CameraShift;
    p.y += p.z * cos(_CameraShiftAngle) * _CameraShift;
    
    if(_SceneIndex == 0){
        return Scene00(p);
    }else if(_SceneIndex == 1){
        return Scene01(p);
    }else if(_SceneIndex == 2){
        return Scene02(p);
    }else if(_SceneIndex == 3){
        return Scene03(p);
    }else if(_SceneIndex == 4){
        return Scene04(p);
    }

    return float4(float3(1.0,0.0,1.0), sdSphere(p, 2.5));
}