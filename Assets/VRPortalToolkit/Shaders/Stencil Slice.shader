Shader "VRPortalToolkit/Stencil Slice"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        _SliceNormal("Slice Plane Normal", Vector) = (0,0,0,0)
        _SliceCentre("Slice Plane Centre", Vector) = (0,0,0,0)

		_StencilID("Stencil ID", Float) = 0
		_ClippingCentre("Clipping Plane Centre", Vector) = (0,0,0,0)
		_ClippingNormal("Clipping Plane Normal", Vector) = (0,0,0,0)
    }

    // Depth Pass First
    SubShader
    {
        Tags
        {
            "RenderType" = "Stencil Slice"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }
        
        LOD 200
		
        Stencil
		{
			Ref [_StencilID]
			Comp Equal
			ReadMask 255
		}

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float4 color : COlOR;
        };

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;

        float3 _SliceNormal;
        float3 _SliceCentre;
        float3 _ClippingNormal;
        float3 _ClippingCentre;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            clip(dot(_SliceCentre - IN.worldPos, _SliceNormal));
            clip(dot(_ClippingCentre - IN.worldPos, _ClippingNormal));
            
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color * IN.color;
            o.Albedo = c.rgb;

            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
