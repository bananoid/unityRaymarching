float3 getNormal(float3 p, float d ){
    // const float2 offset = float2(0.03, 0.0);
    const float2 offset = float2(0.001, 0.0);
    float3 n = float3(
        distanceField(p + offset.xyy).w - d,
        distanceField(p + offset.yxy).w - d,
        distanceField(p + offset.yyx).w - d
    );
    return normalize(n);
}

// Shading
float hardShadow(float3 ro, float3 rd, float mint, float maxt){
    for(float t = mint; t < maxt; ){
        float h = distanceField(ro + rd* t).w;
        if(h < 0.001){
            return 0.0;
        }
        t += h;
    }
    return 1.0;
}

float softShadow(float3 ro, float3 rd, float mint, float maxt, float k){
    float result = 1.0;
    for(float t = mint; t < maxt; ){
        float h = distanceField(ro + rd* t).w;
        if(h < 0.001){
            return 0.0;
        }
        result = min(result, k*h/t);
        t += h;
    }
    return result;
}



float AmbientOcclusion(float3 p, float3 n){
    float _AoIntensity = 0.4; 
    float _AoStepSize = 0.05; 
    int _AoIterations = 15;
    
    float step = _AoStepSize;
    float ao = 0.0;
    float dist;
    for(int i=1; i< _AoIterations; i++){
        dist = step * i;
        ao += max(0.0, (dist - distanceField(p + n * dist).w) / dist);  
    }
    return 1.0 - ao * _AoIntensity/_AoIterations;
}

//Light
float3 _LightDir, _LightCol;
float _LightIntensity;
fixed4 _ShadowColor;
float2 _ShadowDistance;
float _ShadowIntensity, _ShadowPenumbra;

float3  Shading(float3 p, float3 n, float3 color){
    
    //Diffuse color;
    float3 result = color;
    // float3 result = float3(0.8,0.4,0.4) * 0.7;
    // float3 result = float3(0.3,0.8,0.6) * 0.7;
    // float3 result = float3(1,0,0);
    // float3 result = n.xxx + n.yyy * 0.5 + 0.5;
    
    float spherLight = distance(p, _PointLight.xyz);
    spherLight = 1 - smoothstep(0, _PointLight.w, spherLight);
    // spherLight = 1 - smoothstep(0, 3, spherLight);

    // result = (n *0.5 + 0.5);

    // Directional Light
    // if(_LightIntensity > 0){
    //     float3 light = _LightCol * dot(-_LightDir, n) * _LightIntensity;
    //     result *= light;
    // }

    float3 light = dot(normalize(float3(1,1,0)), n) * .9;
    result = result + max(light,0);

    // Shadows
    // if(_ShadowIntensity > 0){
    //     float shadow = softShadow(p, -_LightDir, _ShadowDistance.x, _ShadowDistance.y, _ShadowPenumbra) * 0.5 + 0.5;
    //     shadow = max( 0.0, pow(shadow, _ShadowIntensity));
    //     result *= shadow + _ShadowColor * _ShadowIntensity;
    // }

    //Ambient Occlusion
    // if(_AoIntensity > 0){
    float ao = AmbientOcclusion(p,n);
    result *= ao ;
    // return float3(ao,ao,ao);
    // }
    
    float lines = sin(p.z * 20 + _Time * 100)*0.5+0.5;
    float lineSMin = 0.3;
    float lineSMax = 0.1;
    lines = smoothstep(lineSMin, lineSMax, lines);
    lines = clamp(0,1,lines);
    
    result *= spherLight;
    return result;
}

fixed4 raymarching(float3 rayOrigin, float3 rayDirection, float depth) {
    fixed4 result = float4(0, 0, 0, 0);
    float t = 0.01; // Distance Traveled from ray origin (ro) along the ray direction (rd)

    for (int i = 0; i < _MaxIterations; i++)
    {
        // if (t > _MaxDistance || t >= depth)
        if (t > _MaxDistance)
        {
            result = float4(0, 0, 0, 0);
            break;
            // discard;
        }

        float3 p = rayOrigin + rayDirection * t;    // This is our current position
        float4 d = distanceField(p); // should be a sphere at (0, 0, 0) with a radius of 1
        if (d.w <= _MinDistance) // We have hit something
        {
            //Shading
            float3 n = getNormal(p, d.w);
            float3 s = Shading(p, n, d.rgb);
            result = fixed4(s,1);
            // result = fixed4(n*0.5 + 0.5,1);
            break;
        }

        t += d.w;
    }
    return result;
}