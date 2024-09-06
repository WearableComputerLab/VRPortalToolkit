Shader "VRPortalToolkit/Examples/OpacityOverlay"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", 2DArray) = "white" {}
        [MainColor] _Color("Color", Color) = (1, 1, 1, 1)

        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp("Stencil Operation", Float) = 0
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _StencilWriteMask("Stencil Write Mask", Float) = 255
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull[_PortalCullMode]
            Blend SrcAlpha OneMinusSrcAlpha
            
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

            //uniform float4 _MainTex_ST;
            //uniform float4 _MainTex_ST_2; // used for stereo

            float4 _Color;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST_2)
            UNITY_INSTANCING_BUFFER_END(Props)

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

            UNITY_DECLARE_TEX2DARRAY(_MainTex);

            fixed4 frag(v2f i) : SV_Target
            {
                // Allow single pass instancing
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                float4 st = _MainTex_ST_2 * unity_StereoEyeIndex + _MainTex_ST * (1 - unity_StereoEyeIndex);

                fixed4 color = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(tilingAndOffset(screenUV, st), unity_StereoEyeIndex)) * _Color.rgba;
                color.a = _Color.a;
                return color;
            }
            ENDCG
        }
    }
}