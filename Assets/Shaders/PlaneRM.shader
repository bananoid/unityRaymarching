﻿Shader "Unlit/PlaneRM"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
        _MaxIterations ("Max Iterations", int) = 200
        _MaxDistance ("Max Distance", float) = 100   
        _MinDistance ("Min Distance", float) = 0.001

        _Id ("Id", float) = 0

        _EnableRM ("Scene Index", int) = 1

        _SceneIndex ("Scene Index", int) = 0
        _CumTime ("Cum Time", float) = 0
        _PointLight ("Point Light", Vector) = (0,0,0,1)
        _PlaneBox ("Plane Box", Vector) = (0,0,1,1)
        _RoomDepth ("Room Depth", float) = 1
        // float _CumTime;
        // float4 _sphere1, _sphere2, _box1;

        // _AoIntensity ("Ao Intensity", float) = 0.01
        // _AoStepSize ("Ao StepSize", float) = 0.2
        // _AoIterations ("Ao Iterations", float) = 30

        _CameraShift ("Camera Shift", float) = 0
        _CameraShiftAngle ("Camera Shift Angle", float) = 0

        rndScale ("Random Scale", float) = 0
     
        _GlitchIntensity ("Glitch Intensity", float) = 1
        _GlitchSpeed ("Glitch Speed", float) = 1
        _GlitchScale ("Glitch Scale", float) = 1
        _GlitchType ("Glitch Type", int) = 1

    }
    SubShader
    {
        Tags { "RenderType"="Overlay" "Queue"="Overlay"}
        LOD 100
        Cull Off ZWrite Off ZTest Always

        GrabPass { "_GrabTexture" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"

            sampler2D _GrabTexture;
            sampler2D_float _CameraDepthTexture;

            int _MaxIterations;
            float _MaxDistance;
            float _MinDistance;
            
            int _SceneIndex;
            float _CumTime;
            float4 _sphere1, _sphere2, _box1;

            float4 _PointLight;
            float _Id;
            int _EnableRM;

            #include "Random.cginc"
            #include "DistanceFunctions.cginc"
            #include "SceneDF.cginc"
            #include "Raymarch.cginc"
            #include "Glitch.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 ro: TEXCOORD1;
                float3 hitPos: TEXCOORD2;
                float4 grabUv: TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.ro = _WorldSpaceCameraPos; 
                o.hitPos = mul(unity_ObjectToWorld, v.vertex);
                o.grabUv = UNITY_PROJ_COORD(ComputeGrabScreenPos(o.vertex));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {   
                float2 screenPos = i.grabUv.xy/i.grabUv.w;
                float4 col = 1;
                float4 gh = 0;
                   
                if(_EnableRM > 0){
                    float3 rayOrigin = i.ro;
                    float3 rayDirection = normalize(i.hitPos - rayOrigin);

                    float depth = 1;    
                    
                    // depth = tex2Dproj(_CameraDepthTexture, i.grabUv + gh);
                    // depth = Linear01Depth(depth);
                    // depth *= length(rayDirection);

                    float4 rmResult = raymarching(rayOrigin, rayDirection, depth);
                    
                    col.xyz = rmResult.xyz;

                }else{
                    if(_GlitchIntensity > 0){
                        gh = glitch(i.uv);
                        float4 projPos = i.grabUv; 
                        projPos += gh * _GlitchIntensity;
                        col = tex2Dproj(_GrabTexture, projPos);
                        col.rgb = lerp(col.rgb, step(0.2,gh.rgb), step(0.99,gh.w));
                    }else{
                        col = tex2Dproj(_GrabTexture, i.grabUv);
                    }      
                }    
            
                // col = gh;
                    
                return col;
            }
            ENDCG
        }
    }
}
