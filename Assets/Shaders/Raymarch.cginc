float3 _ColorA;
float3 _ColorB;
float3 _ColorC;
float3 _ColorD;
float _ColorTime;
float _ColorScale;
float _ColorSplit;

float3 WorldColor(float3 p){
    p.z += _ColorTime;
    float pos = cnoise((p.xz+p.y)*_ColorScale) + _Id * _ColorSplit;
     
    float3 col = palette( 
        pos,
        _ColorA, 
        _ColorB, 
        _ColorC, 
        _ColorD 
        
    );

    return col;
}

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
    float _AoIntensity = 0.1; 
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

float lineIntesity;
float lineSize;

float lineFreq;

float3  Shading(float3 p, float3 n, float3 color, float lineInt){
    //Diffuse color;
    float3 result = color;
    
    float spherLight = distance(p, _PointLight.xyz);
    spherLight = 1 - smoothstep(0, _PointLight.w, spherLight);

    float maxDist = 1 - ((p.z+10)/(_MaxDistance));
    maxDist = clamp(0,1,maxDist);

    if(lineInt > 0 && lineIntesity > 0){
        float lineDir = p.z;
        // float lineDir = distance(p.xz, float2(0,_PointLight.z));
        float lines = sin(lineDir * lineFreq * 3 + _LineTime * 30)*0.5+0.5;
        float lineS = 0.03;
        lines = smoothstep(lineSize-lineS,lineSize+lineS, lines);
        lines = clamp(0,1,lines);
        return color * lines.xxx * maxDist * spherLight;
    }

    // Directional Light
    // if(_LightIntensity > 0){
    //     float3 light = _LightCol * dot(-_LightDir, n) * _LightIntensity;
    //     result *= light;
    // }

    float3 light = dot(normalize(float3(1,1,0)), n) * .01;
    result = result + max(light,0);

    // Shadows
    // if(_ShadowIntensity > 0){
    //     float shadow = softShadow(p, -_LightDir, _ShadowDistance.x, _ShadowDistance.y, _ShadowPenumbra) * 0.5 + 0.5;
    //     shadow = max( 0.0, pow(shadow, _ShadowIntensity));
    //     result *= shadow + _ShadowColor * _ShadowIntensity;
    // }

    //Ambient Occlusion
    float ao = AmbientOcclusion(p,n);
    result *= ao;

    
    result *=  spherLight;
    

    result = max(result,0);
    result *= maxDist;
    return result;
    // return maxDist.xxx;
}

fixed4 raymarching(float3 rayOrigin, float3 rayDirection, float depth) {
    fixed4 result = float4(0, 0, 0, 0);
    float t = 0.01; // Distance Traveled from ray origin (ro) along the ray direction (rd)

    float3 sPos = _MaxDistance;
    float4 sDis;

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
        float4 d = distanceField(p);
        if (d.w <= _MinDistance) // We have hit something
        {            
            sPos = p;
            sDis = d;    
            break;
        }

        t += d.w;
    }

    float3 n = getNormal(sPos, sDis.w);
    float3 wColor = WorldColor(sPos);
    wColor.rgb *= sDis.x;
    float3 s = Shading(sPos, n, wColor, sDis.y );
    result = fixed4(s,1);

    return result;
}