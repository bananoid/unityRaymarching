// float glitch01(float2 uv){
//     return length(uv);
// }

float4 glitch(float2 uv){
    float seed = _Id;
    // seed = random(seed);

    float ratio = 9./16.;
    float4 res = 0;
    res.xy = uv;

    float timeSpeed = 0.004;
    float tq = floor(_Time/timeSpeed)*timeSpeed; 
    tq = random(tq) * 40;

    float blockSize = 0.125 * 0.01;
    blockSize *= tq+1;

    float2 pos = uv - 0.5;
    // pos += _Time * random(floor(pos.x/blockSize)*blockSize + seed );
    pos += _Time;
    pos.y *= ratio;
    pos.y *= 0.1;
    pos = floor(pos/blockSize)*blockSize;    
    float noise = random(pos);

    // noise = step(.5,noise);
    
    // float noise = length(pos);
    res.r = random(pos + seed ); 
    res.g = random(pos + seed +1); 
    res.b = random(pos + seed +2); 
    res.w = random(pos + seed +3);

    res.w = noise;
    // res.rgba = planeSeed;
    return res;
}
