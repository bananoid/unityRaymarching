float4 SineSphere(float3 p){
    float4 Sphere1 = float4(float3(0.5,0,1), sdSphere(p - _sphere1.xyz, _sphere1.w));
    Sphere1.w = abs(sin(Sphere1.w * 2.0 + _CumTime )) - 0.6;
    // Sphere1.w = abs(sin(Sphere1.w * 2.0)) - 0.6;
    float4 Sphere2 = float4(float3(0.1,0.5,0.9), sdSphere(p - _sphere2.xyz, _sphere2.w));
    float4 combine = opSS(Sphere1,Sphere2,_sphereIntersectSmooth);    
    return combine;    
}

float4 Corridor01(float3 p){

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

float4 Corridor02(float3 p){
    float box = sdBox(p, float3(20,1.0,30)) * -1.;
    float4 boxC = float4(float3(0.0,0.5,1.0), box); 
    return boxC;
}  