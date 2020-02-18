float4 SineSphere(float3 p){
    float4 Sphere1 = float4(float3(0.5,0,1), sdSphere(p - _sphere1.xyz, _sphere1.w));
    Sphere1.w = abs(sin(Sphere1.w * 2.0 + _Time * 10.0 )) - 0.6;
    float4 Sphere2 = float4(float3(0.1,0.5,0.9), sdSphere(p - _sphere2.xyz, _sphere2.w));
    float4 combine = opSS(Sphere1,Sphere2,_sphereIntersectSmooth);    
    return combine;    
}