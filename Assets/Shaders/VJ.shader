// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Unlit/VJ"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0

        _MainTex ("Texture", 2D) = "white" {}

        _Seed ("float", float) = 0
        _ObjId ("float", float) = 0

        gradientDesc ("Gradient Desc", Vector) = (1,0.2,0.6,1)

        lightDesc ("Light Posision", Vector) = (0,0,0,100)
        
        lineFade ("Line Fade", float) = 0.01
        lineSize ("Line Size", float) = 0.5
        lineFreq ("Line Freq", float) = 30.
        lineSpeed ("Line Speed", float) = 30.
        lineIntesity ("Line Intesity", float) = 0.

        ColorMaskTh ("ColorMaskTh", float) = 0.
        ColorMaskIntesity ("ColorMaskIntesity", float) = 0.3
        ColorSpread ("ColorSpread", float) = 1.
        
        roomRect ("Room rect",Vector ) = (0,0,1,1)
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
            #include "Random.cginc"

            sampler2D _MainTex;
            
            float _Seed;
            float _ObjId;

            float ColorMaskTh;    
            float ColorMaskIntesity;
            float ColorSpread;

            float4 gradientDesc;

            float4 lightDesc;

            float lineFade;
            float lineSize;
            float lineFreq;
            float lineSpeed;
            float lineIntesity;

            float4 roomRect;

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
                float4 screenPos: TEXCOORD5;
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
                o.screenPos = ComputeScreenPos(o.vertex); 
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

                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                
                float roomMask = 
                    step( roomRect.x, screenUV.x)*
                    step( screenUV.x, roomRect.z)*
                    step( roomRect.y, screenUV.y)*
                    step( screenUV.y, roomRect.w);
                if(roomMask == 0){
                    discard;
                }  
                
                fixed4 col = float4(0,0,0,1);

                float lines = sin(i.worldPos.z * lineFreq + _Time * lineSpeed) * 0.5 + 0.5;
                lines = smoothstep(lineSize-lineFade,lineSize+lineFade,lines);
                col.r = lines;

                float light = distance(i.worldPos, lightDesc.xyz);
                light = smoothstep(0, lightDesc.w, light);
                    
                //circle = sin(circle * 70. - _Time * 100) * 0.5 + 0.5;
                //circle = smoothstep(0.45,0.55, circle);       

                light = 1.0 - light;    
                //col.rgb = float3(light,light,light) * lines * 3.0;
                col.rgb = light.xxx;

                float3 gradient = float3(0,0,0);
                    
                float gradientPos = 
                    length((i.localPos + gradientDesc.w*(_ObjId+0.5)*2) * normalize(gradientDesc.xyz));

                
                // gradientPos += _Time * 2;
                float marbleScale = 0.002; 
                float gradientMarble = (gradientPos+_Time*10)*marbleScale;

                float2 texUV = gradientMarble + randomUV(_ObjId) * ColorSpread + randomUV(_Seed);
                texUV.x += sin(_Time * 0.02134) * ColorSpread;    
                texUV.y += cos(_Time * 0.03434) * ColorSpread;

                float4 texCol = tex2D(_MainTex, texUV );    
                col = texCol;

                float maskThresholdRadius = 0.01;
                float mtA = ColorMaskTh + maskThresholdRadius;
                float mtB = ColorMaskTh - maskThresholdRadius;

                // float objIdOsc = sin(_ObjId*7 + _Time*100.2) * 0.5 + 0.5;    
                float colorMask = smoothstep(mtA, mtB, _ObjId);
                col *= colorMask;
                col += (1-colorMask)*ColorMaskIntesity*_ObjId;
                
                // col = colorMask.xxxx;
                // col = _Seed.xxxx;
                // col = objIdOsc.xxxx;

                gradient = col * gradientPos;

                col.rgb *= gradient;
                col.rgb += lines * (1-i.localPos.z*2) * lineIntesity;
                col *= light;    
                       
                return col; 
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
