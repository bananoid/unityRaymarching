// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Unlit/VJ"
{
    Properties
    {
        colorA ("ColorA", Color) = (1,0,0,1)
        colorB ("ColorB", Color) = (1,1,1,1)
        colorC ("ColorC", Color) = (0,0,1,1)

        lightPos ("Light Posision", Vector) = (0,0,0)
        lightFalloff ("Light Fallof", Vector) = (0,1,0)
        lineFade ("Line Fade", float) = 0.01
        lineSize ("Line Size", float) = 0.5
        lineFreq ("Line Freq", float) = 30.
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM


            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 colorA;
            float4 colorB;
            float4 colorC;

            float3 lightPos;
            float2 lightFalloff;

            float lineFade;
            float lineSize;
            float lineFreq;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal: NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                half3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 localPos : TEXCOORD3;
                float3 normal : NORMAL;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.localPos = v.vertex;
                o.normal = v.normal;
                o.uv = v.uv;
                return o;
            }

            float fract(float v){
                
            }

            fixed4 frag (v2f i) : SV_Target
            {
                const float pi = 3.141592653589793238462;

                fixed4 col = float4(0,0,0,1);

                float lines = sin(i.worldPos.z * lineFreq + _Time * 100) * 0.5 + 0.5;
                lines = smoothstep(lineSize-lineFade,lineSize+lineFade,lines);
                col.r = lines;

                float light = distance(i.worldPos, lightPos);
                light = smoothstep(lightFalloff.x, lightFalloff.y, light);
                    
                //circle = sin(circle * 70. - _Time * 100) * 0.5 + 0.5;
                //circle = smoothstep(0.45,0.55, circle);       

                light = 1.0 - light;    
                //col.rgb = float3(light,light,light) * lines * 3.0;
                col.rgb = light.xxx;

                float3 gradient = float3(0,0,0);    
                float gradientPos = i.localPos.z * 1.0 + 0.5;
                gradientPos += _Time * 10;
    
                //gradient = clamp(gradientPos*2.0,0,1) * lerp(colorB,colorC,  clamp(gradientPos*2.0 - 1.0,0,1) ).rgb;
                //gradient += clamp(2-gradientPos*2.0,0,1) * lerp(colorA,colorB,  clamp(gradientPos*2.0,0,1) ).rgb;

                gradient = colorA * (sin(gradientPos * 1.0 * pi - pi) * 0.5 + 0.5) * step(0.5, gradientPos);
                gradient += colorB * (sin(gradientPos * 1.0 * pi) * 0.5 + 0.5);
                gradient += colorC * (sin(gradientPos * 1.0 * pi - pi) * 0.5 + 0.5) * step( gradientPos, 0.5);

                col.rgb *= gradient;

                return col;
            }
            ENDCG
        }
    }
}
