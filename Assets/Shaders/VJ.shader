// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Unlit/VJ"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0

        colorA ("ColorA", Color) = (1,0,0,1)
        colorB ("ColorB", Color) = (1,1,1,1)
        colorC ("ColorC", Color) = (0,0,1,1)
        gradientDesc ("Gradient Direction", Vector) = (0,1,0,1)

        lightPos ("Light Posision", Vector) = (0,0,0)
        lightFalloff ("Light Fallof", Vector) = (0,1,0)
        lineFade ("Line Fade", float) = 0.01
        lineSize ("Line Size", float) = 0.5
        lineFreq ("Line Freq", float) = 30.
        lineSpeed ("Line Speed", float) = 30.
        lineIntesity ("Line Intesity", float) = 1.

        minBounds ("Min Room Bounds", Vector) = (-2,-1,-5)
        maxBounds ("Min Room Bounds", Vector) = ( 2, 1, 5)

        
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull [_Cull]
        
        Pass
        {
            CGPROGRAM
            #pragma target 3.5

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 colorA;
            float4 colorB;
            float4 colorC;
            float4 gradientDesc;

            float3 lightPos;
            float2 lightFalloff;

            float lineFade;
            float lineSize;
            float lineFreq;
            float lineSpeed;
            float lineIntesity;


            float3 minBounds;
            float3 maxBounds;

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
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 localPos : TEXCOORD3;
                float3 normal : NORMAL;
                float4 grabUv : TEXCOORD4;
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
                o.grabUv = UNITY_PROJ_COORD(ComputeGrabScreenPos(o.vertex));
                return o;
            }

            float boxWF(float3 pos){
                float boxWF;
                float wfS = 0.1;
                boxWF = smoothstep(1-wfS, 1.0, length(pos.y * 2));
                boxWF += smoothstep(1-wfS, 1.0, length(pos.x * 2));
                boxWF += smoothstep(1-wfS, 1.0, length(pos.z * 2));
                boxWF = smoothstep(0.9, 1.0, boxWF*0.9);
                return boxWF;
            }

            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                const float pi = 3.141592653589793238462;

                fixed4 col = float4(0,0,0,1);

                float lines = sin(i.worldPos.z * lineFreq + _Time * lineSpeed) * 0.5 + 0.5;
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
                    
                float gradientPos = 
                    length((i.localPos + gradientDesc.w) * normalize(gradientDesc.xyz));

                
                // gradientPos += _Time * 2;

                gradient = colorA * gradientPos;

                // gradient = colorA * (sin(gradientPos*pi*3 - pi*0.5) * 0.5 + 0.5) * step(0.5, sin(gradientPos*pi*3*0.5)*0.5+0.5);
                // gradient+= colorB * (sin(gradientPos*pi*3 - pi*0.5 - pi*3) * 0.5 + 0.5) * step(0.5, sin(gradientPos*pi*3*0.5 - pi*0.5)*0.5+0.5);
                // gradient+= colorC * (sin(gradientPos*pi*3 - pi*0.5 - pi*6) * 0.5 + 0.5) * step(0.5, sin(gradientPos*pi*3*0.5 + pi*3)*0.5+0.5);  
                // gradient+= colorA * (sin(gradientPos*pi*3 - pi*0.5 - pi*9) * 0.5 + 0.5) * step(0.5, sin(gradientPos*pi*3*0.5 + pi*0.5)*0.5+0.5);  

                //gradient = sin(colorA.rgb * gradientPos * 2. + _Time * 1 ) * 0.5 + 0.5;

                col.rgb *= gradient;
                // col.rgb += float3(lines,lines,lines);
                col.rgb += lines * (1-i.localPos.z*2) * lineIntesity;
                // col.rgb *= gradient;

                //Bounds
                // float boundsMask =
                //     step( i.worldPos.x, maxBounds.x) *    
                //     step( minBounds.x, i.worldPos.x) *
                //     step( i.worldPos.y, maxBounds.y) *    
                //     step( minBounds.y, i.worldPos.y) *
                //     step( i.worldPos.z, maxBounds.z) *    
                //     step( minBounds.z, i.worldPos.z);                
                // if(boundsMask == 0){
                //     discard;
                // }    
                
                return col * light;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
