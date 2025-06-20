using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Render pass that controls shadow settings during portal rendering.
    /// </summary>
    public class ShadowSettingsInPortalPass : PortalRenderPass
    {
        /// <summary>
        /// Whether shadows should be supported in this pass.
        /// </summary>
        public bool supportsShadows { get; set; }

        /// <summary>
        /// Initializes a new instance of the ShadowSettingsInPortalPass class.
        /// </summary>
        /// <param name="supportsShadows">Whether to enable or disable shadows in this pass.</param>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public ShadowSettingsInPortalPass(bool supportsShadows, RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent)
        {
            this.supportsShadows = supportsShadows;
        }

        /// <inheritdoc/>
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
