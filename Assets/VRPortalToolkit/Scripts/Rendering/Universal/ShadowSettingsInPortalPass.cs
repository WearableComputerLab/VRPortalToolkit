using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{

    public class ShadowSettingsInPortalPass : PortalRenderPass
    {
        public bool supportsShadows { get; set; }

        public ShadowSettingsInPortalPass(PortalRenderFeature feature, bool supportsShadows) : base(feature)
        {
            this.supportsShadows = supportsShadows;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                if (supportsShadows)
                {
                    // Toggle light shadows enabled based on the renderer setting set in the constructor
                    if (SupportsMainLightShadows(ref renderingData))
                    {
                        CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, true);
                        CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, renderingData.shadowData.mainLightShadowCascadesCount > 0);
                    }

                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, SupportsAdditionalLightShadows(ref renderingData));
                }
                else
                {
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, false);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, false);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, false);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private bool SupportsMainLightShadows(ref RenderingData renderingData)
        {
            return renderingData.shadowData.supportsMainLightShadows && renderingData.lightData.mainLightIndex != -1 && Shader.GetGlobalTexture(PropertyID.MainLightShadowmapTexture) != null;
        }

        private bool SupportsAdditionalLightShadows(ref RenderingData renderingData)
        {
            return renderingData.shadowData.supportsAdditionalLightShadows && Shader.GetGlobalTexture(PropertyID.AdditionalLightsShadowmapTexture) != null;
        }
    }
}
