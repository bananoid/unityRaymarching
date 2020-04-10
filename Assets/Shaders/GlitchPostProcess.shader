Shader "VJ/GlitchPostProcess"
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

            #include "UnityCG.cginc"

            float _Id;

            #include "Random.cginc"
            #include "Glitch.cginc"

            //TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
            uniform sampler2D _MainTex;
            half4 _MainTex_ST;
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
  
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 col;
                float4 gh = 0;

                gh = glitch(i.texcoord);
            
                col = tex2D(_MainTex, i.texcoord);
                col.rgb = lerp(col.rgb, step(0.2,gh.rgb), step(0.99,gh.w));

                return col;
            }

            ENDHLSL
        }
    }
}