Shader "VRPortalToolkit/Portal Stereo"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", any) = "white" {}
        [MainColor] _Color("Color", Color) = (1, 1, 1, 1)

        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Z Test", Float) = 4
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp("Stencil Operation", Float) = 0
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _StencilWriteMask("Stencil Write Mask", Float) = 255
    }

    SubShader
    {
        Tags
         {
             "IgnoreProjector" = "True"
             "Queue" = "Geometry"
         }

         Pass
         {
              ZWrite On
              ZTest[_ZTest]
              Cull[_PortalCullMode]

              Stencil
              {
                   Ref[_PortalStencilRef]
                   Comp[_StencilComp]
                   Pass[_StencilOp]
                   ReadMask[_StencilReadMask]
                   WriteMask[_StencilWriteMask]
              }

              CGPROGRAM
              #pragma vertex vert
              #pragma fragment frag
              #pragma require 2darray

              #include "UnityCG.cginc"

              float4 _Color;

#if defined(STEREO_INSTANCING_ON) || defined(STEREO_MULTIVIEW_ON)
              UNITY_INSTANCING_BUFFER_START(Props)
                  UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
                  UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST_2)
              UNITY_INSTANCING_BUFFER_END(Props)

              UNITY_DECLARE_TEX2DARRAY(_MainTex);
#else
              sampler2D _MainTex;
              float4 _MainTex_ST;
#endif

              struct appdata
              {
                  float4 vertex : POSITION;

                  // Allow single pass instancing
                  UNITY_VERTEX_INPUT_INSTANCE_ID
              };

              struct v2f
              {
                   float4 vertex : SV_POSITION;
                   float4 screenPos : TEXCOORD0;

                   // Allow single pass instancing
                   UNITY_VERTEX_OUTPUT_STEREO
              };

              float2 tilingAndOffset(float2 uv, float4 to)
              {
                  return uv * to.xy + to.zw;
              }

              v2f vert(appdata v)
              {
                   v2f o;

                   // Allow single pass instancing
                   UNITY_SETUP_INSTANCE_ID(v);
                   UNITY_INITIALIZE_OUTPUT(v2f, o);
                   UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                   o.vertex = UnityObjectToClipPos(v.vertex);
                   o.screenPos = ComputeScreenPos(o.vertex);

                   return o;
              }

              fixed4 frag(v2f i) : SV_Target
              {
                   // Allow single pass instancing
                   UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                   float2 screenUV = i.screenPos.xy / i.screenPos.w;
                   
#if defined(STEREO_INSTANCING_ON) || defined(STEREO_MULTIVIEW_ON)
                   float4 st = _MainTex_ST_2 * unity_StereoEyeIndex + _MainTex_ST * (1 - unity_StereoEyeIndex);
                   return UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(tilingAndOffset(screenUV, st), unity_StereoEyeIndex)) * _Color.rgba;
#else
                   return tex2D(_MainTex, TRANSFORM_TEX(screenUV, _MainTex));
#endif
               }
              ENDCG
         }
    }
}