Shader "VRPortalToolkit/Examples/ContoursOverlay"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", 2DArray) = "white" {}
        [MainColor] _Color("Color", Color) = (1, 1, 1, 1)

        _OutlineThickness("Outline Thickness", Float) = 1

        _ColorSensitivity("Color Sensitivity", Float) = 0.1
        _DepthSensitivity("Depths Sensitivity", Float) = 0.1
        _NormalsSensitivity("Normals Sensitivity", Float) = 0.1

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
            float _OutlineThickness;
            float _ColorSensitivity;
            float _DepthSensitivity;
            float _NormalsSensitivity;

            uniform float4 _MainTex_TexelSize;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST_2)
            UNITY_INSTANCING_BUFFER_END(Props)

            UNITY_DECLARE_TEX2DARRAY(_PortalDepthNormalsTexture);//UNITY_DECLARE_TEX2DARRAY(_CameraDepthNormalsTexture);
            UNITY_DECLARE_TEX2DARRAY(_MainTex);

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

            float4 outline(float3 uv)
            {
                //read depthnormal
                float3 color = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv);
                float4 depthNormal = UNITY_SAMPLE_TEX2DARRAY(_PortalDepthNormalsTexture, uv);

                //decode depthnormal
                float3 normal;
                float depth;
                DecodeDepthNormal(depthNormal, depth, normal);
                depth = depth * _ProjectionParams.z;

                // Get uv positions
                float halfScaleFloor = floor(_OutlineThickness * 0.5);
                float halfScaleCeil = ceil(_OutlineThickness * 0.5);
                float2 Texel = (1.0) / float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);

                float2 uvSamples[4];
                float depthSamples[4];
                float3 normalSamples[4], colorSamples[4];

                uvSamples[0] = uv - float2(Texel.x, Texel.y) * halfScaleFloor;
                uvSamples[1] = uv + float2(Texel.x, Texel.y) * halfScaleCeil;
                uvSamples[2] = uv + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
                uvSamples[3] = uv + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);

                for (int i = 0; i < 4; i++)
                {
                    float3 uvSample = float3(uvSamples[i], uv.z);
                    colorSamples[i] = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvSample);
                    float4 enc = UNITY_SAMPLE_TEX2DARRAY(_PortalDepthNormalsTexture, uvSample);
                    DecodeDepthNormal(enc, depthSamples[i], normalSamples[i]);
                    depthSamples[i] = depthSamples[i] * _ProjectionParams.z;
                }

                // Depth
                float edgeDifference = abs(depth - depthSamples[0]) + abs(depth - depthSamples[1]) + abs(depth - depthSamples[2]) + abs(depth - depthSamples[3]);
                float depthThreshold = (1.0 / _DepthSensitivity);// * depthSamples[0];
                float edgeDepth = edgeDifference > depthThreshold ? 1 : 0;

                // Normals
                float3 normalFiniteDifference0 = normalSamples[1] - normalSamples[0];
                float3 normalFiniteDifference1 = normalSamples[3] - normalSamples[2];
                float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
                edgeNormal = edgeNormal > (1.0 / _NormalsSensitivity) ? 1 : 0;

                // Color
                float3 colorFiniteDifference0 = colorSamples[1] - colorSamples[0];
                float3 colorFiniteDifference1 = colorSamples[3] - colorSamples[2];
                float edgeColor = sqrt(dot(colorFiniteDifference0, colorFiniteDifference0) + dot(colorFiniteDifference1, colorFiniteDifference1));
                edgeColor = edgeColor > (1.0 / _ColorSensitivity) ? 1 : 0;

                float edge = max(edgeDepth, max(edgeNormal, edgeColor));

                return _Color.rgba * edge;
            }

            fixed4 frag(v2f i) : SV_TARGET
            {
                // Allow single pass instancing
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                float4 st = _MainTex_ST_2 * unity_StereoEyeIndex + _MainTex_ST * (1 - unity_StereoEyeIndex);
                
                return outline(float3(tilingAndOffset(screenUV, st), unity_StereoEyeIndex));
            }
            ENDCG
        }
    }
}