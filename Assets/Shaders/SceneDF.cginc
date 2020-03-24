#define PI 3.14159265359
#define PHI (1.618033988749895)

#include "noise/ClassicNoise2D.hlsl"

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

float4 RoomBox(float3 p){
    float4 boxS;

    boxS.xy = _PlaneBox.zw * 0.5; 
    boxS.z = _RoomDepth;
    boxS.w = float3(0,0,-boxS.z*0.5);

    return boxS;
}

float3 WorldColor(float3 p){
    float3 col = palette(
        p.y*0.024512 + 
        p.x*0.01312 + 
        p.z*0.01643 + _Time * 0.1, 
        float3(0.5, 0.5, 0.5), 
        float3(0.5, 0.5, 0.5), 
        float3(1.0, 1.0, 1.0), 
        // float3(0.00, 0.33, 0.67)
        float3(0.00, 0.33, 0.67	)
    );
    col.g *= 0.8;
    return col + 0.2;
}

//Box Room
float4 Scene00(float3 p){
    //Room Box
    float4 roomBox = RoomBox(p); 

    //Color
    float3 col = WorldColor(p);

    float box = sdOpenBox(p, roomBox.xyz);   
    
    //Result
    float4 result;
    result.xyz = col;
    result.w = box;
    return result;
}

//Landscape Room
float4 Scene01(float3 p){
    //Room Box
    float4 roomBox = RoomBox(p); 
    
    //Color
    float3 col = WorldColor(p);

    float3 surfP = p;
    float3 waveP = p;
    waveP.xy /= roomBox.xy; 
    
    float height = roomBox.y;
    height *= 1-p.z*0.03;
    
    float waveSize = 0.3;
    float waveFreq = 4;
    float waveSpeed = 0.4;

    waveP.z += _CumTime * waveSpeed * 120;
    float wave = cnoise(waveP.xz * waveSize * waveFreq) * waveSize;

    // wave = clamp(wave,-0.2,0.2);

    surfP.y += wave * height;

    float planeBot = sdPlane(surfP, float4(0,1,0,height));
    float planeTop = sdPlane(surfP, float4(0,-1,0,height));
    float frontPlane = sdPlane(p, float4(0,0,1,0.4)) + waveSize;

    float combine; 
    combine = opU(planeBot,planeTop);
    // combine = planeBot;
    // combine = planeTop;
    combine = opS(frontPlane,combine);

    //Result
    float4 result;
    result.xyz = col;
    result.w = combine;
    return result;
}

float4 Scene02(float3 p){
    
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

    float scale = pow(boxS.x*boxS.y,0.3);

    float t = _CumTime;

    boxCenter.z += sin(t * 10 * boxS.x) * boxS.z * 0.2;
    boxCenter.x += cos(t * 10.2345 * boxS.x) * boxS.x;
    boxCenter.y = sin(t * 2.45 * boxS.x) * boxS.y;
    float sphere1 = sdSphere(p + boxCenter, 0.5 * scale);
    
    boxCenter.z += sin(t * 10 * boxS.x) * boxS.z * 0.2;
    boxCenter.x += cos(t * 10.2345 * boxS.x) * boxS.x;
    boxCenter.y = sin(t * 2.45 * boxS.x) * boxS.y;
    float sphere2 = sdSphere(p+boxCenter, 0.5 * scale);
    
    float3 col = WorldColor(p);

    float3 roomColor = col;
    float3 objColor = col + float3(0.9,0.2,0.1) * 3;

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

// float4 _Scene01(float3 p){

//     rotateAxe(0.2, p.yz);
//     // float3 color = (1,0,0);
//     // return float4(color, box);
//     float zPos = _Time * 20.0;
//     float xPos = cos(_Time * 15.0 + 1.3) * 3.;

//     float3 sPos = p + _sphere2.xyz + float3(xPos,0,zPos);

//     sPos.y += sin(sPos.z * 1.3 + _Time * 10)*0.1;
    
//     float mod = 2;
//     sPos.xz = (frac((sPos.xz+0.5)/mod)-0.5)*mod;

//     float4 Sphere1 = float4(float3(0.5,0.3,0.5), sdSphere(sPos, _sphere2.w));

//     float box = sdBox(p, float3(20,0.0,30)) * -1.;
//     float4 boxC = float4(float3(0.0,0.5,1.0), box); 
//     float4 combine = opSS(Sphere1,boxC,0.0);    
//     return combine;
// }



//Gyroid
float4 Scene03(float3 p){
    float3 color = WorldColor(p);

    float4 roomBox = RoomBox(p); 
    float plane = sdPlane(p, float4(0,0,-1,0.3));

    float scale = 10. / pow(roomBox.x*roomBox.y,0.3);
    p.y += _CumTime * 10.0;
    p.z += _Time * 1.3450;
    p += _sphere2.xyz;

    p.y *= 0.5;
    float gyroid = abs(sdGyroid(p,scale)) - 0.07;
    
    float combine = max(plane, gyroid);

    //Color
    color += smoothstep(0.4, 0.6,gyroid);
    return float4(color, combine);
}

//Box Room
float4 _Scene04(float3 p){
    //Room Box
    float4 roomBox = RoomBox(p); 

    //Color
    float3 col = WorldColor(p);

    float box = sdOpenBox(p, roomBox.xyz);   
    
    // float sphere =  
    float combine = box;
    // combine = opU(box,)

    //Result
    float4 result;
    result.xyz = col;
    result.w = combine;
    return result;
}

//Petal 6
float4 Scene04(float3 p){  
    float4 roomBox = RoomBox(p); 
    float scale = pow(roomBox.x*roomBox.y,0.3);

    float t = _CumTime * 30;
    float y = 2;
    rotateAxe(PI * .5, p.yz);
    
    float3 coneP = p;
    rotateAxe(3.14, coneP.yz);

    float plane = sdPlane(p, float4(0,1,0,y));
    float cone = sdRoundCone(coneP, 4, 0, 30);

    float3 sPos = p;
    float rep = PI / 8.;

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
    sPos.x -= scale * 2;
    // sPos.xz = (frac((sPos.xz+0.5)/repGrid)-0.5)*repGrid;
    // rotateAxe(t, sPos.xz);
    sPos.y -= t;
    float yRep = 2 * 1.8;
    sPos.y = (frac((sPos.y+0.5)/yRep)-0.5)*yRep;
    // sPos.y += y + _sphere2.w * 0.5;

    float sphere = sdSphere(sPos, scale * 0.3);
    // float sphere = sdBox(sPos, _sphere2.www * float3(0.5,0.5,0.5));
    // p.y = frac(p.y/rep)*rep;
    // float sphere = sdTorus(p + float3(0,0,0), float2(4,1.05)) * rep;
    // float frontPlane = sdPlane(p, float4(0,0,1,0.4));
    float combine = sphere;
    // combine = opS(frontPlane,combine);
    // combine  = opSS( cone,combine, 4.3);
    // combine = opSS( sphere, combine, 0);
    float3 color = WorldColor(p * 0.1);
    float4 scene = float4(color,combine); 

    return scene;
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