using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Render pass that completes the rendering process for a texture-based portal.
    /// </summary>
    public class CompleteTexturePortalPass : PortalRenderPass
    {
        /// <summary>
        /// Initializes a new instance of the CompleteTexturePortalPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public CompleteTexturePortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                Camera camera = renderingData.cameraData.camera;

                PortalPassNode passNode = PortalPassStack.Pop();
                PortalRenderNode renderNode = passNode.renderNode;

                // Release shadow textures
                if (passNode.mainLightShadowCasterPass != null)
                    passNode.mainLightShadowCasterPass.OnPortalCleanup(cmd);

                if (passNode.additionalLightsShadowCasterPass != null)
                    passNode.additionalLightsShadowCasterPass.OnPortalCleanup(cmd);

                // Trigger Post Render
                foreach (IPortalRenderer renderer in renderNode.renderers)
                    renderer?.PostRender(renderNode);
                PortalRendering.onPostRender?.Invoke(renderNode);

                PortalPassGroupPool.Release(passNode);

                PortalPassStack.Current.RestoreState(cmd, ref renderingData);
                PortalPassStack.Current.SetViewAndProjectionMatrices(cmd);

                //feature.currentGroup.colorTexture = null;

                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
