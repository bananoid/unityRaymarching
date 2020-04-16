#define PI 3.14159265359
#define PHI (1.618033988749895)

#include "noise/ClassicNoise2D.hlsl"
#include "noise/ClassicNoise3D.hlsl"

float4 _PlaneBox;
float _RoomDepth;
#define defaultColor float3(1,1,1);

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

//Box Room
float4 Scene00(float3 p){
    //Room Box
    float4 roomBox = RoomBox(p); 

    //Color
    float3 col = defaultColor;

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
    float3 col = defaultColor;

    float3 surfP = p;
    float3 waveP = p;
    waveP.xy /= roomBox.xy; 
    
    float height = roomBox.y;
    height *= 1-p.z*0.03;
    
    float waveSize = 0.4;
    float waveFreq = 1;
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

// Two ball 
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

    float t = _CumTime * 0.3;

    boxCenter.z += sin(t * 10 * boxS.x) * boxS.z * 0.2;
    boxCenter.x += cos(t * 10.2345 * boxS.x) * boxS.x;
    boxCenter.y = sin(t * 2.45 * boxS.x) * boxS.y;
    float sphere1 = sdSphere(p + boxCenter, 0.5 * scale);
    
    boxCenter.z += sin(t * 10 * boxS.x) * boxS.z * 0.2;
    boxCenter.x += cos(t * 10.2345 * boxS.x) * boxS.x;
    boxCenter.y = sin(t * 2.45 * boxS.x) * boxS.y;
    float sphere2 = sdSphere(p+boxCenter, 0.5 * scale);
    
    float3 col = defaultColor;

    float3 roomColor = col;
    float3 objColor = float3(3,0,1);

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
    float3 color = defaultColor;

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

float pReflect(inout float3 p, float3 planeNormal, float offset) {
	float t = dot(p, planeNormal)+offset;
	if (t < 0.) {
		p = p - (2.*t)*planeNormal;
	}
	return sign(t);
}

void pR(inout float2 p, float a) {
	p = cos(a)*p + sin(a)*float2(p.y, -p.x);
}

float pModPolar(inout float2 p, float repetitions) {
	float angle = 2.*PI/repetitions;
	float a = atan2(p.y, p.x) + angle/2.;
	float r = length(p);
	float c = floor(a/angle);
	a = mod(a,angle) - angle/2.;
	p = float2(cos(a), sin(a))*r;
	// For an odd number of repetitions, fix cell index of the cell in -x direction
	// (cell index would be e.g. -5 and 5 in the two halves of the cell):
	if (abs(c) >= (repetitions/2.)) c = abs(c);
	return c;
}

float3 pModIcosahedron(inout float3 p) {

    float3 v1 = normalize(float3(1, 1, 1 ));
    float3 v2 = normalize(float3(0, 1, PHI+1.));

    float sides = 3.;
    float dihedral = acos(dot(v1, v2));
    float halfDdihedral = dihedral / 2.;
    float faceAngle = 2. * PI / sides;
    
    p.z = abs(p.z);    
    pR(p.yz, halfDdihedral);
    
   	p.x = -abs(p.x);
    
    pR(p.zy, halfDdihedral);
    p.y = -abs(p.y);
    pR(p.zy, -halfDdihedral);

    pR(p.xy, faceAngle);
    
    pR(p.zy, halfDdihedral);
    p.y = -abs(p.y);
    pR(p.zy, -halfDdihedral);

    pR(p.xy, faceAngle);
     
    pR(p.zy, halfDdihedral);
    p.y = -abs(p.y);
    pR(p.zy, -halfDdihedral);

    pR(p.xy, faceAngle);
  
    pR(p.zy, halfDdihedral);
   	p.y = -abs(p.y);
    pR(p.zy, -halfDdihedral);

    p.z = -p.z;
	pModPolar(p.yx, sides);
    pReflect(p, float3(-1, 0, 0), 0.);

	return p;
}


float CubeWithSphereHole(float3 p, float objScale){
    float objA = sdBox(p, objScale * float3(1,1,1) * (sin(_CumTime * (2+_Id) + _Id * 10)*0.1 + 0.5));
    float objB = sdSphere(p, objScale * 0.5);
    return opSS(objB,objA, 0.2);
}

float Icos(float3 p, float objScale){
    pModIcosahedron(p);
    p.z += 0.5;
    rotateAxe(_CumTime + _Id * 100, p.yz);
    pModIcosahedron(p);
    p.z -= 0.5;
    float objA = sdBox(p + float3(0.0,0,2.5), objScale * float3(0.1,1.3,0.1) * (sin(_CumTime * (2+_Id) + _Id * 10)*0.1 + 0.5));
    return objA;
}

// top bottom + obj
float4 LandScapeAndObjec(float3 p, int subInx){ 
    //Room Box
    float4 roomBox = RoomBox(p); 
    float objScale = min(roomBox.x, roomBox.y) * 1;

    float3 roomColor = float3(1.,1,0);
    float3 objColor = float3(1.1,0,1);
    
    float height = roomBox.y;
    float3 boxCenter = float3(0,0,-roomBox.z);

    float planeBot = sdPlane(p, float4(0,1,0,height));
    float planeTop = sdPlane(p, float4(0,-1,0,height));
    float frontPlane = sdPlane(p, float4(0,0,1,0.4));

    p += boxCenter;    
    float3 rotSpeed = float3(10.1243,12.91912,1.643246);
    rotateAxe(_CumTime * rotSpeed.x, p.yz);
    rotateAxe(_CumTime * rotSpeed.y, p.xz);
    
    float objComb = 0;
    
    if(subInx == 0){
        objComb = CubeWithSphereHole(p, objScale); 
    } else if(subInx == 1){
        objComb = Icos(p, objScale); 
    }

    float roomComb; 
    roomComb = opU(planeBot,planeTop);
    roomComb = opS(frontPlane,roomComb);
    
    float4 combine; 
    combine = opUS( float4(objColor,objComb) , float4(roomColor,roomComb), 0.1);


    //Result
    return combine;
}

// Simple top bottom
float4 TopBottom(float3 p){ 
    //Room Box
    float4 roomBox = RoomBox(p); 
    
    //Color
    float3 col = defaultColor;
    float height = roomBox.y;
    
    float planeBot = sdPlane(p, float4(0,1,0,height));
    float planeTop = sdPlane(p, float4(0,-1,0,height));
    float frontPlane = sdPlane(p, float4(0,0,1,0.4));
    
    float combine; 
    combine = opU(planeBot,planeTop);
    combine = opS(frontPlane,combine);

    //Result
    float4 result;
    result.xyz = col;
    result.w = combine;
    return result;
}

//Box Room
float4 _Scene04(float3 p){
    //Room Box
    float4 roomBox = RoomBox(p); 

    //Color
    float3 col = defaultColor;

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
float4 __Scene04(float3 p){  
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
    float3 color = defaultColor;
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
        return LandScapeAndObjec(p, 0);
    }else if(_SceneIndex == 5){
        return LandScapeAndObjec(p, 1);
    }else if(_SceneIndex == 6){
        return LandScapeAndObjec(p, 2);
    }else if(_SceneIndex == 7){
        return LandScapeAndObjec(p, 3);
    }

    return float4(float3(1.0,0.0,1.0), sdSphere(p, 2.5));
}