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

            //TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
            uniform sampler2D _MainTex;
            uniform sampler2D_float _CameraDepthTexture, sampler_CameraDepthTexture;
            half4 _MainTex_ST;
            uniform float4 _CamWorldSpace;
            uniform float4x4 _CamFrustum,  _CamToWorld;
            uniform int _MaxIterations;
            uniform float _MaxDistance;
            uniform float _MinDistance;
            uniform float3 _LightDir;
            float4 _Tint;

            uniform float4 _MainTex_TexelSize;

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

            float sdSphere(float3 position, float3 origin, float radius)
            {
                return distance(position, origin) - radius;
            }

            float distanceField(float3 p) {
                return sdSphere(p, float3(1, 0, 0), 2);
            }

            float3 getNormal(float3 p)
            {
                const float2 offset = float2(0.001, 0.0);
                
                float3 n = float3(
                    distanceField(p + offset.xyy) - distanceField(p - offset.xyy),
                    distanceField(p + offset.yxy) - distanceField(p - offset.yxy),
                    distanceField(p + offset.yyx) - distanceField(p - offset.yyx));

                return normalize(n);
            }


            fixed4 raymarching(float3 rayOrigin, float3 rayDirection, float depth) {
                fixed4 result = float4(1, 1, 1, 1);
                float t = 0.01; // Distance Traveled from ray origin (ro) along the ray direction (rd)

                for (int i = 0; i < _MaxIterations; i++)
                {
                    if (t > _MaxDistance || t >= depth)
                    {
                        result = float4(rayDirection, 0); // color backround from ray direction for debugging
                        break;
                    }

                    float3 p = rayOrigin + rayDirection * t;    // This is our current position
                    float d = distanceField(p); // should be a sphere at (0, 0, 0) with a radius of 1
                    if (d <= _MinDistance) // We have hit something
                    {
                        // shading
                        float3 n = getNormal(p);
                        float light = dot(-_LightDir, n);
                        result = float4(fixed3(1, 1, 1) * light, 1); // yellow sphere should be drawn at (0, 0, 0)
                        break;
                    }

                    t += d;
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

                return fixed4(col * (1.0 - result.w) + result.xyz * result.w, 1.0);
            }

            ENDHLSL
        }
    }
}