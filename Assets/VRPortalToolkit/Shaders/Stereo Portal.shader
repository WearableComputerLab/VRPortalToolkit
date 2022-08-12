Shader "VRPortalToolkit/Stereo Portal"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", 2D) = "white" {}
        _SecondaryTex("Secondary Texture", 2D) = "white" {}
        [MainColor] _Color("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _SecondaryTex;
	        float4 _SecondaryTex_ST;

            float4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;

                // Allow single pass instancing
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;

                // Allow single pass instancing
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            v2f vert (appdata v)
            {
                v2f o;

                // Allow single pass instancing
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Allow single pass instancing
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                fixed4 l = tex2D(_MainTex, TRANSFORM_TEX(screenUV, _MainTex));
                fixed4 r = tex2D(_SecondaryTex, TRANSFORM_TEX(screenUV, _SecondaryTex));

                return (r * unity_StereoEyeIndex + l * (1 - unity_StereoEyeIndex)) * _Color.rgba;
            }
            ENDCG
        }
    }
    Fallback "Standard" // for shadows
}
