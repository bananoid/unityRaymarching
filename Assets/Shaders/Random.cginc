float random(float2 st) {
    return frac(sin(dot(st.xy,
                         float2(12.9898,78.233)))*
        43758.5453123);
}

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