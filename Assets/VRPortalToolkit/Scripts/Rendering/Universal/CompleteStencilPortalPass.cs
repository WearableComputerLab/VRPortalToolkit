using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering.Universal
{
    public class CompleteStencilPortalPass : PortalRenderPass
    {
        public CompleteStencilPortalPass(PortalRenderFeature feature) : base(feature) { }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                Camera camera = renderingData.cameraData.camera;

                PortalPassGroup passGroup = feature.currentGroup;
                PortalRenderNode renderNode = passGroup.renderNode;

                // Release shadow textures
                if (passGroup.mainLightShadowCasterPass != null)
                    passGroup.mainLightShadowCasterPass.OnPortalCleanup(cmd);

                if (passGroup.additionalLightsShadowCasterPass != null)
                    passGroup.additionalLightsShadowCasterPass.OnPortalCleanup(cmd);

                // Trigger Post Render
                renderNode.renderer.PostRender(camera, renderNode);
                PortalRenderer.onPostRender?.Invoke(camera, renderNode);

                feature.currentGroup = passGroup.parent;
                PortalPassGroupPool.Release(passGroup);

                feature.currentGroup.RestoreState(cmd, ref renderingData);
                feature.currentGroup.SetViewAndProjectionMatrices(cmd);

                //context.ExecuteCommandBuffer(cmd);
                //cmd.Clear();

                // Unmask
                if (feature.portalDecrease)
                    renderNode.renderer.Render(camera, renderNode, cmd, feature.portalDecrease);

                cmd.SetGlobalInt(PropertyID.PortalStencilRef, renderNode.depth - 1);
                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
