Shader "Unlit/PlaneRM"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaxIterations ("Max Iterations", int) = 200
        _MaxDistance ("Max Distance", float) = 100   
        _MinDistance ("Min Distance", float) = 0.001

        _SceneIndex ("Scene Index", int) = 0
        _CumTime ("Cum Time", float) = 0
        _PointLight ("Point Light", Vector) = (0,0,0,1)
        
        // float _CumTime;
        // float4 _sphere1, _sphere2, _box1;
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

            int _MaxIterations;
            float _MaxDistance;
            float _MinDistance;
            
            int _SceneIndex;
            float _CumTime;
            float4 _sphere1, _sphere2, _box1;

            float4 _PointLight;

            #include "DistanceFunctions.cginc"
            #include "SceneDF.cginc"
            #include "Raymarch.cginc"

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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.ro = _WorldSpaceCameraPos; 
                o.hitPos = mul(v.vertex, unity_ObjectToWorld);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = 1;    

                // float2 uv = i.uv-.5;
                float3 rayOrigin = i.ro;
                float3 rayDirection = normalize(i.hitPos - rayOrigin);
                float4 result = raymarching(rayOrigin, rayDirection, depth);

                // result.xyz =  lerp(col.xyz,result.xyz,result.w);

                // return fixed4(result.xyz,1.0);
                
                // float3 col = 0;
                // col = rayDirection;
                // return fixed4(col,1.0);
                
                return fixed4(result.xyz,1.0);
            }
            ENDCG
        }
    }
}
