// BOOLEAN OPERATORS //

// Union
float opU(float d1, float d2)
{
	return min(d1, d2);
}

// Subtraction
float opS(float d1, float d2)
{
	return max(-d1, d2);
}

// Intersection
float opI(float d1, float d2)
{
	return max(d1, d2);
}

// Mod Position Axis
float pMod1 (inout float p, float size)
{
	float halfsize = size * 0.5;
	float c = floor((p+halfsize)/size);
	p = fmod(p+halfsize,size)-halfsize;
	p = fmod(-p+halfsize,size)-halfsize;
	return c;
}

float4 opUS( float4 d1, float4 d2, float k ) 
{
    float h = clamp( 0.5 + 0.5*(d2.w-d1.w)/k, 0.0, 1.0 );
 	float3 color = lerp(d2.rgb, d1.rgb, h);
    float dist = lerp( d2.w, d1.w, h ) - k*h*(1.0-h); 
 	return float4(color,dist);
}

float opSS( float d1, float d2, float k ) 
{
    float h = clamp( 0.5 - 0.5*(d2+d1)/k, 0.0, 1.0 );
    return lerp( d2, -d1, h ) + k*h*(1.0-h); 
}

float4 opSS( float4 d1, float4 d2, float k ) 
{
    float h = clamp( 0.5 - 0.5*(d2.w+d1.w)/k, 0.0, 1.0 );
    float3 color = lerp( d2.rgb, d1.rgb, h );
	float dist = lerp( d2.w, -d1.w, h ) + k*h*(1.0-h);
	return float4(color, dist);
}

float opIS( float d1, float d2, float k ) 
{
    float h = clamp( 0.5 - 0.5*(d2-d1)/k, 0.0, 1.0 );
    return lerp( d2, d1, h ) + k*h*(1.0-h); 
}

void rotateAxe(float a, inout float2 p) {
    float s = sin(a);
    float c = cos(a);
    float2x2 m = float2x2(c, -s, s, c);
	p = mul(m, p);
}


// float2x2 rotateMx(float a, inout float2 p) {
//     float s = sin(a);
//     float c = cos(a);
//     float2x2 m = float2x2(c, -s, s, c);
// 	return m;
// }

struct SDFrVolumeData
{
    float4x4 WorldToLocal;
    float3 Extents;
};

SamplerState sdfr_sampler_linear_clamp;

inline float DistanceFunctionTex3DFast(in float3 rayPosWS, in SDFrVolumeData data, in Texture3D tex)
{
	float4x4 w2l = data.WorldToLocal;
	float3 extents = data.Extents;

	float3 rayPosLocal = mul(w2l, float4(rayPosWS, 1)).xyz;
	rayPosLocal /= extents.xyz * 2;
	rayPosLocal += 0.5; //texture space
	//values are -1 to 1
	float sample = tex.SampleLevel(sdfr_sampler_linear_clamp, rayPosLocal, 0).r;
	sample *= length(extents); //scale by magnitude of bound extent
	return sample;
}

// SHAPES

// Plane
float sdPlane(float3 p, float4 n){
	return dot(p, n.xyz) + n.w;
}


// Sphere
// s: radius
float sdSphere(float3 p, float s)
{
	return length(p) - s;
}

// Box
// b: size of box in x/y/z
float sdBox(float3 p, float3 b)
{
	float3 d = abs(p) - b;
	return min(max(d.x, max(d.y, d.z)), 0.0) +
		length(max(d, 0.0));
}

float sdOpenBox(float3 p, float3 b)
{
	float3 d = abs(p) - b;
	float box = min(max(d.x, d.y), 0.0) +
		length(max(d.xy, 0.0));

	box = box * -1;
	
	float front = sdPlane(p, float4(0,0,-1,0));
	box = max(box, front);
	
	// float back = sdPlane(p, float4(0,0,-1,b.z));
	// box = min(-box, back);

	return box;
}

//Round Box
float sdRoundBox( in float3 p,  in float3 b, in float r){
	float3 q =  abs(p) - b;
	return min(max(q.x, max(q.y,q.z)),0.0) + length(max(q,0.0)) - r;
} 

//Gyroid
float sdGyroid(in float3 p, in float scale){
	p *= scale;
    return dot(sin(p), cos(p.zxy * 0.35345))/scale;
}
// Cone
float sdCone( float3 p, float2 c )
{
  // c is the sin/cos of the angle
  float q = length(p.xy);
  return dot(c,float2(q,p.z));
}

float sdRoundCone( in float3 p, in float r1, float r2, float h )
{
    float2 q = float2( length(p.xz), p.y );
    
    float b = (r1-r2)/h;
    float a = sqrt(1.0-b*b);
    float k = dot(q,float2(-b,a));
    
    if( k < 0.0 ) return length(q) - r1;
    if( k > a*h ) return length(q-float2(0.0,h)) - r2;
        
    return dot(q, float2(a,b) ) - r1;
}

float sdTorus( float3 p, float2 t )
{
  float2 q = float2(length(p.xz)-t.x,p.y);
  return length(q)-t.y;
}