Shader "VJ/RayMatching"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            #include "DistanceFunctions.cginc"
            
            sampler2D _MainTex;
            uniform sampler2D _CameraDepthTexture;
            uniform float4x4 _CamFrustum, _CamToWorld;
            uniform int _MaxIterations;
            uniform float _Accuracy; 
            uniform float _maxDistance, _box1round, _boxSphereSmooth, _sphereIntersectSmooth;
            uniform float4 _sphere1, _sphere2, _box1;
            uniform float3 _modInterval;
            uniform float3 _LightDirection, _LightCol;
            uniform float _LightIntensity;
            uniform fixed4 _mainColor;

            uniform fixed4 _ShadowColor;
            uniform float2 _ShadowDistance;
            uniform float _ShadowIntensity, _ShadowPenumbra;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 ray : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                half index = v.vertex.z;
                v.vertex.z = 0;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                o.ray = _CamFrustum[(int)index].xyz;
                o.ray /= abs(o.ray.z);
                o.ray = mul(_CamToWorld, o.ray);  
                return o;
            }

            float BoxSphere(float3 p){
                float Sphere1 = sdSphere(p - _sphere1.xyz, _sphere1.w);
                float Box1 = sdRoundBox(p - _box1.xyz, _box1.www, _box1round);
                float combine1 = opSS(Sphere1, Box1, _boxSphereSmooth);
                float Sphere2 = sdSphere(p - _sphere2.xyz, _sphere2.w);
                float combine2 = opIS(Sphere2, combine1, _sphereIntersectSmooth);
                return combine2;
            }

            float4 distanceField(float3 p){
                // float ground =  sdPlane(p, float4(0,1,0,0));
                // float boxSphere1 = BoxSphere(p);
                // return opU(ground, boxSphere1);

                float4 Sphere1 = float4(float3(0.5,0,1), sdSphere(p - _sphere1.xyz, _sphere1.w));
                Sphere1.w = abs(sin(Sphere1.w * 2.0 + _Time * 10.0 )) - 0.6;
                float4 Sphere2 = float4(float3(0.1,0.5,0.9), sdSphere(p - _sphere2.xyz, _sphere2.w));
                float4 combine = opSS(Sphere1,Sphere2,_sphereIntersectSmooth);    
                return combine;
            }

            float3 getNormal(float3 p, float d ){
                const float2 offset = float2(0.0001, 0.0);
                float3 n = float3(
                    // distanceField(p + offset.xyy).w - distanceField(p - offset.xyy).w,
                    // distanceField(p + offset.yxy).w - distanceField(p - offset.yxy).w,
                    // distanceField(p + offset.yyx).w - distanceField(p - offset.yyx).w
                    distanceField(p + offset.xyy).w - d,
                    distanceField(p + offset.yxy).w - d,
                    distanceField(p + offset.yyx).w - d
                );
                return normalize(n);
            }

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

            uniform float _AoStepSize; 
            uniform float _AoIntensity; 
            uniform int _AoIterations;

            float AmbientOcclusion(float3 p, float3 n){
                float step = _AoStepSize;
                float ao = 0.0;
                float dist;
                for(int i=1; i< _AoIterations; i++){
                    dist = step * i;
                    ao += max(0.0, (dist - distanceField(p + n * dist).w) / dist);  
                }
                return 1.0 - ao * _AoIntensity;
            }

            float3  Shading(float3 p, float3 n, fixed3 color){
                //Diffuse color;
                float3 result = color;
                
                // Directional Light
                if(_LightIntensity > 0){
                    float3 light = _LightCol * dot(-_LightDirection, n) * _LightIntensity;
                    result *= light;
                }

                // Shadows
                if(_ShadowIntensity > 0){
                    float shadow = softShadow(p, -_LightDirection, _ShadowDistance.x, _ShadowDistance.y, _ShadowPenumbra) * 0.5 + 0.5;
                    shadow = max( 0.0, pow(shadow, _ShadowIntensity));
                    result *= shadow + _ShadowColor * _ShadowIntensity;
                }

                //Ambient Occlusion
                if(_AoIntensity > 0){
                    float ao = AmbientOcclusion(p,n);
                    result *= ao + _ShadowColor * _AoIntensity;
                }
                
                return result;
            }

            fixed4 raymarching(float3 ro, float3 rd, float depth){
                fixed4 result = fixed4(0,0,0,1);
                const int max_iteration = _MaxIterations;
                float t = 0; //distance travel along the ray direction

                for(int i = 0; i < max_iteration; i++){
                    if(t > _maxDistance || t >= depth){
                        //Environment 
                        return fixed4(rd,0);
                        break;
                    }

                    float3 p = ro + rd * t;
                    //check for hit in distancefield
                    float4 d = distanceField(p);
                    if(d.w < _Accuracy){
                        //Shading
                        float3 n = getNormal(p, d.w);
                        float3 s = Shading(p, n, d.rgb);
                        result = fixed4(s,1);
                        break;
                    }
                    t += d.w;
                }

                return result;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, i.uv).r);
                depth *= length(i.ray);
                fixed3 col = tex2D(_MainTex, i.uv);
                float3 rayDirection = normalize(i.ray.xyz);
                float3 rayOrigin = _WorldSpaceCameraPos;
                fixed4 result = raymarching(rayOrigin, rayDirection, depth);
                return fixed4( lerp(col, result.xyz, result.w), 1.0);

            }
            ENDCG
        }
    }
}
