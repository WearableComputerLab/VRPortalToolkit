using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Rendering
{
    public static class PropertyID
    {
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int MainTex_ST = Shader.PropertyToID("_MainTex_ST");
        public static readonly int MainTex_ST_2 = Shader.PropertyToID("_MainTex_ST_2");

        public static readonly int PortalStencilRef = Shader.PropertyToID("_PortalStencilRef");
        public static readonly int StencilRef = Shader.PropertyToID("_StencilRef");
        public static readonly int StencilComp = Shader.PropertyToID("_StencilComp");
        public static readonly int StencilOp = Shader.PropertyToID("_StencilOp");
        public static readonly int StencilReadMask = Shader.PropertyToID("_StencilReadMask");
        public static readonly int StencilWriteMask = Shader.PropertyToID("_StencilWriteMask");

        public static readonly int CameraColorTexture = Shader.PropertyToID("_CameraColorTexture");

        public static readonly int WorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");

        public static readonly int MainLightPosition = Shader.PropertyToID("_MainLightPosition");
        public static readonly int MainLightColor = Shader.PropertyToID("_MainLightColor");
        public static readonly int MainLightOcclusionProbesChannel = Shader.PropertyToID("_MainLightOcclusionProbes");
        public static readonly int AdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");


        public static readonly int MainLightShadowmapTexture = Shader.PropertyToID("_MainLightShadowmapTexture");
        public static readonly int MainLightWorldToShadow = Shader.PropertyToID("_MainLightWorldToShadow");
        public static readonly int MainLightShadowParams = Shader.PropertyToID("_MainLightShadowParams");
        public static readonly int CascadeShadowSplitSpheres0 = Shader.PropertyToID("_CascadeShadowSplitSpheres0");
        public static readonly int CascadeShadowSplitSpheres1 = Shader.PropertyToID("_CascadeShadowSplitSpheres1");
        public static readonly int CascadeShadowSplitSpheres2 = Shader.PropertyToID("_CascadeShadowSplitSpheres2");
        public static readonly int CascadeShadowSplitSpheres3 = Shader.PropertyToID("_CascadeShadowSplitSpheres3");
        public static readonly int CascadeShadowSplitSphereRadii = Shader.PropertyToID("_CascadeShadowSplitSphereRadii");
        public static readonly int MainLightShadowOffset0 = Shader.PropertyToID("_MainLightShadowOffset0");
        public static readonly int MainLightShadowOffset1 = Shader.PropertyToID("_MainLightShadowOffset1");
        public static readonly int MainLightShadowOffset2 = Shader.PropertyToID("_MainLightShadowOffset2");
        public static readonly int MainLightShadowOffset3 = Shader.PropertyToID("_MainLightShadowOffset3");
        public static readonly int MainLightShadowmapSize = Shader.PropertyToID("_MainLightShadowmapSize");

        public static readonly int AdditionalLightsShadowmapTexture = Shader.PropertyToID("_AdditionalLightsShadowmapTexture");
        public static readonly int AdditionalLightsWorldToShadow = Shader.PropertyToID("_AdditionalLightWorldToShadow");
        public static readonly int AdditionalLightShadowParams = Shader.PropertyToID("_AdditionalLightShadowParams");
        public static readonly int AdditionalShadowOffset0 = Shader.PropertyToID("_AdditionalLightShadowOffset0");
        public static readonly int AdditionalShadowOffset1 = Shader.PropertyToID("_AdditionalLightShadowOffset1");
        public static readonly int AdditionalShadowOffset2 = Shader.PropertyToID("_AdditionalLightShadowOffset2");
        public static readonly int AdditionalShadowOffset3 = Shader.PropertyToID("_AdditionalLightShadowOffset3");
        public static readonly int AdditionalShadowmapSize = Shader.PropertyToID("_AdditionalLightShadowmapSize");


        //public static readonly int AdditionalLightsBufferId = Shader.PropertyToID("_AdditionalLightsBuffer");
        //public static readonly int AdditionalLightsIndicesId = Shader.PropertyToID("_AdditionalLightsIndices");

        public static readonly int AdditionalLightsPosition = Shader.PropertyToID("_AdditionalLightsPosition");
        public static readonly int AdditionalLightsColor = Shader.PropertyToID("_AdditionalLightsColor");
        public static readonly int AdditionalLightsAttenuation = Shader.PropertyToID("_AdditionalLightsAttenuation");
        public static readonly int AdditionalLightsSpotDir = Shader.PropertyToID("_AdditionalLightsSpotDir");
        public static readonly int AdditionalLightOcclusionProbeChannel = Shader.PropertyToID("_AdditionalLightsOcclusionProbes");


        public static readonly int SourceTex = Shader.PropertyToID("_SourceTex");
        public static readonly int ScaleBias = Shader.PropertyToID("_ScaleBias");
        public static readonly int ScaleBiasRt = Shader.PropertyToID("_ScaleBiasRt");
    }
}
