using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Rendering
{
    /// <summary>
    /// Contains shader property IDs used throughout the portal rendering system.
    /// </summary>
    public static class PropertyID
    {
        /// <summary>ID for the clipping center property.</summary>
        public static readonly int ClippingCentre = Shader.PropertyToID("_ClippingCentre");
        
        /// <summary>ID for the clipping normal property.</summary>
        public static readonly int ClippingNormal = Shader.PropertyToID("_ClippingNormal");

        /// <summary>ID for the main texture property.</summary>
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");
        
        /// <summary>ID for the main texture ST property (scale/translation).</summary>
        public static readonly int MainTex_ST = Shader.PropertyToID("_MainTex_ST");
        
        /// <summary>ID for the secondary main texture ST property.</summary>
        public static readonly int MainTex_ST_2 = Shader.PropertyToID("_MainTex_ST_2");

        /// <summary>ID for the portal stencil reference property.</summary>
        public static readonly int PortalStencilRef = Shader.PropertyToID("_PortalStencilRef");
        
        /// <summary>ID for the stencil reference property.</summary>
        public static readonly int StencilRef = Shader.PropertyToID("_StencilRef");
        
        /// <summary>ID for the stencil comparison function property.</summary>
        public static readonly int StencilComp = Shader.PropertyToID("_StencilComp");
        
        /// <summary>ID for the stencil operation property.</summary>
        public static readonly int StencilOp = Shader.PropertyToID("_StencilOp");
        
        /// <summary>ID for the stencil read mask property.</summary>
        public static readonly int StencilReadMask = Shader.PropertyToID("_StencilReadMask");
        
        /// <summary>ID for the stencil write mask property.</summary>
        public static readonly int StencilWriteMask = Shader.PropertyToID("_StencilWriteMask");

        /// <summary>ID for the portal culling mode property.</summary>
        public static readonly int PortalCullMode = Shader.PropertyToID("_PortalCullMode");
        
        /// <summary>ID for the culling mode property.</summary>
        public static readonly int CullMode = Shader.PropertyToID("_CullMode");

        /// <summary>ID for the camera color texture property.</summary>
        public static readonly int CameraColorTexture = Shader.PropertyToID("_CameraColorTexture");

        /// <summary>ID for the world space camera position property.</summary>
        public static readonly int WorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");

        /// <summary>ID for the main light position property.</summary>
        public static readonly int MainLightPosition = Shader.PropertyToID("_MainLightPosition");
        
        /// <summary>ID for the main light color property.</summary>
        public static readonly int MainLightColor = Shader.PropertyToID("_MainLightColor");
        
        /// <summary>ID for the main light occlusion probes channel property.</summary>
        public static readonly int MainLightOcclusionProbesChannel = Shader.PropertyToID("_MainLightOcclusionProbes");
        
        /// <summary>ID for the additional lights count property.</summary>
        public static readonly int AdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");

        /// <summary>ID for the main light shadowmap texture property.</summary>
        public static readonly int MainLightShadowmapTexture = Shader.PropertyToID("_MainLightShadowmapTexture");
        
        /// <summary>ID for the main light world to shadow matrix property.</summary>
        public static readonly int MainLightWorldToShadow = Shader.PropertyToID("_MainLightWorldToShadow");
        
        /// <summary>ID for the main light shadow parameters property.</summary>
        public static readonly int MainLightShadowParams = Shader.PropertyToID("_MainLightShadowParams");
        
        /// <summary>ID for the first cascade shadow split sphere property.</summary>
        public static readonly int CascadeShadowSplitSpheres0 = Shader.PropertyToID("_CascadeShadowSplitSpheres0");
        
        /// <summary>ID for the second cascade shadow split sphere property.</summary>
        public static readonly int CascadeShadowSplitSpheres1 = Shader.PropertyToID("_CascadeShadowSplitSpheres1");
        
        /// <summary>ID for the third cascade shadow split sphere property.</summary>
        public static readonly int CascadeShadowSplitSpheres2 = Shader.PropertyToID("_CascadeShadowSplitSpheres2");
        
        /// <summary>ID for the fourth cascade shadow split sphere property.</summary>
        public static readonly int CascadeShadowSplitSpheres3 = Shader.PropertyToID("_CascadeShadowSplitSpheres3");
        
        /// <summary>ID for the cascade shadow split sphere radii property.</summary>
        public static readonly int CascadeShadowSplitSphereRadii = Shader.PropertyToID("_CascadeShadowSplitSphereRadii");
        
        /// <summary>ID for the first main light shadow offset property.</summary>
        public static readonly int MainLightShadowOffset0 = Shader.PropertyToID("_MainLightShadowOffset0");
        
        /// <summary>ID for the second main light shadow offset property.</summary>
        public static readonly int MainLightShadowOffset1 = Shader.PropertyToID("_MainLightShadowOffset1");
        
        /// <summary>ID for the third main light shadow offset property.</summary>
        public static readonly int MainLightShadowOffset2 = Shader.PropertyToID("_MainLightShadowOffset2");
        
        /// <summary>ID for the fourth main light shadow offset property.</summary>
        public static readonly int MainLightShadowOffset3 = Shader.PropertyToID("_MainLightShadowOffset3");
        
        /// <summary>ID for the main light shadowmap size property.</summary>
        public static readonly int MainLightShadowmapSize = Shader.PropertyToID("_MainLightShadowmapSize");

        /// <summary>ID for the additional lights shadowmap texture property.</summary>
        public static readonly int AdditionalLightsShadowmapTexture = Shader.PropertyToID("_AdditionalLightsShadowmapTexture");
        
        /// <summary>ID for the additional lights world to shadow matrix property.</summary>
        public static readonly int AdditionalLightsWorldToShadow = Shader.PropertyToID("_AdditionalLightWorldToShadow");
        
        /// <summary>ID for the additional light shadow parameters property.</summary>
        public static readonly int AdditionalLightShadowParams = Shader.PropertyToID("_AdditionalLightShadowParams");
        
        /// <summary>ID for the first additional light shadow offset property.</summary>
        public static readonly int AdditionalShadowOffset0 = Shader.PropertyToID("_AdditionalLightShadowOffset0");
        
        /// <summary>ID for the second additional light shadow offset property.</summary>
        public static readonly int AdditionalShadowOffset1 = Shader.PropertyToID("_AdditionalLightShadowOffset1");
        
        /// <summary>ID for the third additional light shadow offset property.</summary>
        public static readonly int AdditionalShadowOffset2 = Shader.PropertyToID("_AdditionalLightShadowOffset2");
        
        /// <summary>ID for the fourth additional light shadow offset property.</summary>
        public static readonly int AdditionalShadowOffset3 = Shader.PropertyToID("_AdditionalLightShadowOffset3");
        
        /// <summary>ID for the additional light shadowmap size property.</summary>
        public static readonly int AdditionalShadowmapSize = Shader.PropertyToID("_AdditionalLightShadowmapSize");

        //public static readonly int AdditionalLightsBufferId = Shader.PropertyToID("_AdditionalLightsBuffer");
        //public static readonly int AdditionalLightsIndicesId = Shader.PropertyToID("_AdditionalLightsIndices");

        /// <summary>ID for the additional lights position property.</summary>
        public static readonly int AdditionalLightsPosition = Shader.PropertyToID("_AdditionalLightsPosition");
        
        /// <summary>ID for the additional lights color property.</summary>
        public static readonly int AdditionalLightsColor = Shader.PropertyToID("_AdditionalLightsColor");
        
        /// <summary>ID for the additional lights attenuation property.</summary>
        public static readonly int AdditionalLightsAttenuation = Shader.PropertyToID("_AdditionalLightsAttenuation");
        
        /// <summary>ID for the additional lights spot direction property.</summary>
        public static readonly int AdditionalLightsSpotDir = Shader.PropertyToID("_AdditionalLightsSpotDir");
        
        /// <summary>ID for the additional light occlusion probe channel property.</summary>
        public static readonly int AdditionalLightOcclusionProbeChannel = Shader.PropertyToID("_AdditionalLightsOcclusionProbes");

        /// <summary>ID for the source texture property.</summary>
        public static readonly int SourceTex = Shader.PropertyToID("_SourceTex");
        
        /// <summary>ID for the scale bias property.</summary>
        public static readonly int ScaleBias = Shader.PropertyToID("_ScaleBias");
        
        /// <summary>ID for the render texture scale bias property.</summary>
        public static readonly int ScaleBiasRt = Shader.PropertyToID("_ScaleBiasRt");
    }
}
