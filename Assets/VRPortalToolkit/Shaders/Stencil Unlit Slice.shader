Shader "Unlit/Stencil Unlit Slice"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}

        _SliceNormal("Slice Plane Normal", Vector) = (0,0,0,0)
        _SliceCentre("Slice Plane Centre", Vector) = (0,0,0,0)

		_StencilID("Stencil ID", Float) = 0
		_ClippingCentre("Clipping Plane Centre", Vector) = (0,0,0,0)
		_ClippingNormal("Clipping Plane Normal", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Lighting Off
        LOD 100
		
        Stencil
		{
			Ref [_StencilID]
			Comp Equal
			ReadMask 255
		}

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            fixed4 _Color;

            float3 _SliceNormal;
            float3 _SliceCentre;
            float3 _ClippingNormal;
            float3 _ClippingCentre;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD1;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                UNITY_TRANSFER_FOG(o, o.pos);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                clip(dot(_SliceCentre - i.worldPos, _SliceNormal));
                clip(dot(_ClippingCentre - i.worldPos, _ClippingNormal));

                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color * i.color;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}