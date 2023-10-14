using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    public class CompleteTexturePortalPass : PortalRenderPass
    {
        public CompleteTexturePortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

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
