Shader "Unlit/twoFace"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        /*
        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = float4(1,0,0,1);
                return col;
            }
            ENDCG
        }
        */
    
        Pass
        {
            Cull off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                fixed4 col = float4(0,0,1,1);

                float3 minBounds = float3(-2,-1,-5);
                float3 maxBounds = float3( 2, 1, 5);

                float boundsMask =
                    step( i.worldPos.x, maxBounds.x) *    
                    step( minBounds.x, i.worldPos.x) *
                    step( i.worldPos.y, maxBounds.y) *    
                    step( minBounds.y, i.worldPos.y) *
                    step( i.worldPos.z, maxBounds.z) *    
                    step( minBounds.z, i.worldPos.z);
     
                col.rgb = float3(boundsMask,boundsMask,boundsMask);


                return col;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
