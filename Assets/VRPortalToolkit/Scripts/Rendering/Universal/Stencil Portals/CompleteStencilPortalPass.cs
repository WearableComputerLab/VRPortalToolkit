using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering.Universal
{
    public class CompleteStencilPortalPass : PortalRenderPass
    {
        // TODO: This should not be necessary
        public Material clearDepthMaterial { get; set; }

        public Material decreaseMaterial { get; set; }

        public Material depthMaterial { get; set; }

        public CompleteStencilPortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

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

                //context.ExecuteCommandBuffer(cmd);
                //cmd.Clear();
                if (clearDepthMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer.Render(renderNode, cmd, clearDepthMaterial);
                }

                if (depthMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer.Render(renderNode, cmd, depthMaterial);
                }
                // Unmask
                if (decreaseMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer.Render(renderNode, cmd, decreaseMaterial);
                }

                cmd.SetGlobalInt(PropertyID.PortalStencilRef, renderNode.depth - 1);

                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
