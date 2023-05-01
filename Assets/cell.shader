// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/cell"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
           // Blend SrcAlpha OneMinusSrcAlpha
                    Cull Off
        Zwrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 wPos :TEXCOORD1;
                float4 colour : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            Buffer<float3> position_buffer;
            Buffer<float4> colour_buffer;

            v2f vert (appdata v, uint instanceID : SV_INSTANCEID)
            {
                float4 my_position = float4(position_buffer[instanceID].xyz, 0);


                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex + my_position);
               
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.colour = colour_buffer[instanceID];
                return o;
            }



            fixed4 frag(v2f i) : SV_Target
            {


                // sample the texture
                fixed4 col = i.colour;// tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
