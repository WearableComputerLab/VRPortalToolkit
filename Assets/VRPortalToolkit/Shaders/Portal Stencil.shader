Shader "VRPortalToolkit/Portal Stencil"
{
    Properties
    {
        [Enum(Off,0,On,1)] _ZWrite("Z Write", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Z Test", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Float) = 8
        //_StencilRef("Stencil Reference", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp("Stencil Operation", Float) = 0
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _StencilWriteMask("Stencil Write Mask", Float) = 255
    }

        SubShader
    {
        Tags
         {
             "IgnoreProjector" = "True"
             "Queue" = "Geometry-300"
         }

         Pass
         {
              ZWrite[_ZWrite]
              ZTest[_ZTest]
              Cull[_CullMode]
              ColorMask 0

              Stencil
              {
                   Ref[_PortalStencilRef]
                   Comp[_StencilComp]
                   Pass[_StencilOp]
                   ReadMask[_StencilReadMask]
                   WriteMask[_StencilWriteMask]
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
                   float4 vertex : SV_POSITION;
                   float2 uv : TEXCOORD0;

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

                   o.vertex = UnityObjectToClipPos(v.vertex);

                   return o;
              }
              
              fixed4 frag(v2f i) : SV_Target
              {
                  // Allow single pass instancing
                   UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                   return fixed4(0,0,0,0);
              }
              ENDCG
         }
     }
}
