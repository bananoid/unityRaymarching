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

            uniform float4 _MainTex_TexelSize;

            //Time
            uniform float _CumTime;

            //Scene 
            uniform int _SceneIndex;

            float4 _PointLight;
            
            //Scene
            uniform float _sphereIntersectSmooth;
            uniform float4 _sphere1, _sphere2, _box1;
            #include "DistanceFunctions.cginc"
            #include "SceneDF.cginc"
            #include "Raymarch.cginc"

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
                
                result.xyz =  lerp(col.xyz,result.xyz,result.w);

                return fixed4(result.xyz,1.0);
                // return col;
            }

            ENDHLSL
        }
    }
}