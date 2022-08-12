Shader "VRPortalToolkit/Clear Depth"
{
	Properties
	{
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 0
		//_StencilRef("Stencil Ref ID", Float) = 0
	}

    SubShader
    {
		Tags
		{
			"IgnoreProjector" = "True"
			"Queue" = "Geometry-100"
		}
		
		Pass
		{
			ZWrite On
			Cull[_CullMode]
			ZTest Always
			ColorMask 0

			Stencil
			{
				Ref [_PortalStencilRef]
				Comp Equal
				ReadMask 255
			}

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;

				// Allow single pass instancing
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos : SV_POSITION;

				// Allow single pass instancing
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata v)
			{
				v2f o;

				// Allow single pass instancing
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.pos = UnityObjectToClipPos(v.vertex);

				return o;
			}

			float frag(v2f i) : SV_Depth
			{
				// Allow single pass instancing
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				#ifdef UNITY_REVERSED_Z
			 	return 0;
				#else
				return 1;
				#endif
			}
			ENDCG
		}
	}
}