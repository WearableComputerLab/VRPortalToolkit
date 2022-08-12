
Shader "VRPortalToolkit/Stencil Portal"
{
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 0 // None
		_StencilSourceID("Stencil Source ID", Float) = 0
		_StencilTargetID("Stencil Target ID", Float) = 1
		_StencilSourceToTarget("Stencil Source To Target", Float) = 1 // _StencilSourceToTarget == _StencilSourceReference XOR _StencilTargetReference
		_ClippingCentre("Clipping Plane Centre", Vector) = (0,0,0,0)
		_ClippingNormal("Clipping Plane Normal", Vector) = (0,0,0,0)
	}

	SubShader
	{
		Tags
		{
			"RenderType" = "Stencil Portal" 
			"Queue" = "Geometry-100" 
			"IgnoreProjector" = "True"
		}
		
		// Depth Pass
		Pass
		{
			ZWrite On
			Cull [_CullMode]
			ColorMask 0

			Stencil
			{
				Ref [_StencilSourceID]
				Comp Equal
				ReadMask 255
			}

		    CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			float3 _ClippingCentre;
			float3 _ClippingNormal;
			
			struct appdata 
			{
				float4 vertex : POSITION;
			};
			
			struct v2f 
			{
				float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD0;
			};
			
			v2f vert(appdata v) 
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}
			
			half4 frag(v2f i) : COLOR 
			{
            	clip(dot(_ClippingCentre - i.worldPos, _ClippingNormal));
				return half4(1,1,0,1);
			}
			ENDCG
        }

		// Write Stencil
		Pass
		{
			ZWrite Off
			Cull [_CullMode]

			Stencil
			{
				Ref [_StencilSourceID]
				Comp Equal
				Pass Invert
				ReadMask 255
				WriteMask [_StencilSourceToTarget]
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			fixed4 _Color;
			float3 _ClippingCentre;
			float3 _ClippingNormal;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

			half4 frag(v2f i) : COLOR
			{
            	clip(dot(_ClippingCentre - i.worldPos, _ClippingNormal));
				return _Color;
			}
			ENDCG
		}

		// Clear Depth
		Pass
		{
			ZWrite On
			ZTest Always
			Cull [_CullMode]
			ColorMask 0

			Stencil
			{
				Ref [_StencilTargetID]
				Comp Equal
				ReadMask 255
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			float3 _ClippingCentre;
			float3 _ClippingNormal;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				#ifdef UNITY_REVERSED_Z
				o.pos = float4(v.vertex.xy * 2.0, 0, 1);
				#else
				o.pos = float4(v.vertex.xy * 2.0, 1, 1);
				#endif
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				return half4(1,1,0,1);
			}
			ENDCG
		}
	}
}