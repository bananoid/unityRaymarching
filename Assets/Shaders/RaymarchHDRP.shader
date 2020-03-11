Shader "Raymarch/RaymarchHDRP"
{

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

            #pragma target 3.5

            #pragma vertex vert
            #pragma fragment frag

            //#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
            //#include "HLSLSupport.cginc"
            #include "UnityCG.cginc"

            Texture3D _VolumeATex;
            
            //TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
            uniform sampler2D _MainTex;
            uniform sampler2D_float _CameraDepthTexture, sampler_CameraDepthTexture;
            half4 _MainTex_ST;
            uniform float4 _CamWorldSpace;
            uniform float4x4 _CamFrustum,  _CamToWorld;
            uniform int _MaxIterations;
            uniform float _MaxDistance;
            uniform float _MinDistance;

            uniform float4 _MainTex_TexelSize;

            //Time
            uniform float _CumTime;

            //Scene 
            uniform int _SceneIndex;

            //Light
            uniform float3 _LightDir, _LightCol;
            uniform float _LightIntensity;
            uniform fixed4 _ShadowColor;
            uniform float2 _ShadowDistance;
            uniform float _ShadowIntensity, _ShadowPenumbra;

            uniform float _AoStepSize; 
            uniform float _AoIntensity; 
            uniform int _AoIterations;

            //Scene
            uniform float _sphereIntersectSmooth;
            uniform float4 _sphere1, _sphere2, _box1;
            #include "DistanceFunctions.cginc"
            #include "SceneDF.cginc"

            struct AttributesDefault
            {
                float3 vertex : POSITION;
                half2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
             float4 vertex : SV_POSITION;
             float2 texcoord : TEXCOORD0;
             float2 texcoordStereo : TEXCOORD1;
             float4 ray : TEXCOORD2;
            };

            // Vertex manipulation
            float2 TransformTriangleVertexToUV(float2 vertex)
            {
                float2 uv = (vertex + 1.0) * 0.5;
                return uv;
            }

            v2f vert(AttributesDefault v  )
            {
                v2f o;
                v.vertex.z = 0.1;
                o.vertex = float4(v.vertex.xy, 0.0, 1.0);
                o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);
                o.texcoordStereo = TransformStereoScreenSpaceTex(o.texcoord, 1.0);
 
                int index = (o.texcoord.x / 2) + o.texcoord.y;
                o.ray = _CamFrustum[index];
 
                return o;
            }

            float4 distanceField(float3 p) {
                // return SineSphere(p);
                // return Corridor01(p);
                
                if(_SceneIndex == 0){
                    return SineSphere(p);
                }else if(_SceneIndex == 1){
                    return Scene01(p);
                }else if(_SceneIndex == 2){
                    return Scene02(p);
                }else if(_SceneIndex == 3){
                    return Scene03(p);
                }else if(_SceneIndex == 4){
                    return Scene04(p);
                }

                return float4(float3(1.0,0.0,1.0), sdSphere(p, 4));
            }

            float3 getNormal(float3 p, float d ){
                const float2 offset = float2(0.0001, 0.0);
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
                // float3 result = color;
                float3 result = float3(1,0,0);
                // float3 result = n.xxx + n.yyy * 0.5 + 0.5;
                

                float spherLight = distance(p, _sphere1.xyz);
                spherLight = 1 - smoothstep(0, _sphere1.w, spherLight);

                // result = (n *0.5 + 0.5);
                result *= spherLight;


                // Directional Light
                // if(_LightIntensity > 0){
                //     float3 light = _LightCol * dot(-_LightDir, n) * _LightIntensity;
                //     result *= light;
                // }

                float3 light = dot(-_LightDir, n) * _LightIntensity;
                result = result + light;

                // Shadows
                // if(_ShadowIntensity > 0){
                //     float shadow = softShadow(p, -_LightDir, _ShadowDistance.x, _ShadowDistance.y, _ShadowPenumbra) * 0.5 + 0.5;
                //     shadow = max( 0.0, pow(shadow, _ShadowIntensity));
                //     result *= shadow + _ShadowColor * _ShadowIntensity;
                // }

                //Ambient Occlusion
                // if(_AoIntensity > 0){
                //     float ao = AmbientOcclusion(p,n);
                //     result *= ao + _ShadowColor * _AoIntensity;
                // }

                float depth = 1-(p.z)*0.1;
                depth = clamp(0,1,depth);
            
                float lines = sin(p.y * 20 + _Time * 100)*0.5+0.5;
                float lineSMin = 0.3;
                float lineSMax = 0.1;
                lines = smoothstep(lineSMin, lineSMax, lines);
                lines = clamp(0,1,lines);
                // result = float3(lines,lines,lines);   
                // result += lines*0.3;   

                // result = float3(depth,depth,depth);   
                
                return result;
            }

            fixed4 raymarching(float3 rayOrigin, float3 rayDirection, float depth) {
                fixed4 result = float4(0, 0, 0, 1);
                float t = 0.01; // Distance Traveled from ray origin (ro) along the ray direction (rd)

                for (int i = 0; i < _MaxIterations; i++)
                {
                    if (t > _MaxDistance || t >= depth)
                    {
                        // result = float4(rayDirection, 0); // color backround from ray direction for debugging
                        result = float4(0, 0, 0, 1);
                        break;
                    }

                    float3 p = rayOrigin + rayDirection * t;    // This is our current position
                    float4 d = distanceField(p); // should be a sphere at (0, 0, 0) with a radius of 1
                    if (d.w <= _MinDistance) // We have hit something
                    {
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

            float4 frag(v2f i) : SV_Target
            {
                i.texcoord.y = 1 - i.texcoord.y;
                float4 col = tex2D(_MainTex, i.texcoord);
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(i.texcoord));
                depth = Linear01Depth(depth);
                depth *= length(i.ray);

                float3 rayOrigin = _CamWorldSpace;
                float3 rayDirection = normalize(i.ray);
                float4 result = raymarching(rayOrigin, rayDirection, depth);

                return fixed4(result.xyz,1.0);
            }

            ENDHLSL
        }
    }
}