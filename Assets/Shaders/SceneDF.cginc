#define PI 3.14159265359
#define PHI (1.618033988749895)

float4 SineSphere(float3 p){
    float4 Sphere1 = float4(float3(0.5,0,1), sdSphere(p - _sphere1.xyz, _sphere1.w));
    Sphere1.w = abs(sin(Sphere1.w * 2.0 + _CumTime )) - 0.6;
    // Sphere1.w = abs(sin(Sphere1.w * 2.0)) - 0.6;
    float4 Sphere2 = float4(float3(0.1,0.5,0.9), sdSphere(p - _sphere2.xyz, _sphere2.w));
    float4 combine = opSS(Sphere1,Sphere2,0.2);    
    return combine;    
}

float4 Scene00(float3 p){
    float4 Sphere1 = float4(float3(1,0,1), sdSphere(p, 3.0));
    return Sphere1;
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

    float scale = 1.;
    p.y += _Time * 10.0;
    p.z += _Time * 1.3450;
    p += _sphere2.xyz;

    // p.y *= 0.5;
    float gyroid = abs(sdGyroid(p,scale)) - 0.1;
    
    float combine = max(plane, gyroid);

    float3 color = float3(1,0,1);
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

float4 distanceField(float3 p) {
    // return SineSphere(p);
    // return Corridor01(p);
    
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

    return float4(float3(1.0,0.0,1.0), sdSphere(p, 1));
}