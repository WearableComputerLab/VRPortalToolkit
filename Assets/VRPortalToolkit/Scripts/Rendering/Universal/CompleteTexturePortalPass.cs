using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    public class CompleteTexturePortalPass : PortalRenderPass
    {
        public CompleteTexturePortalPass(PortalRenderFeature feature) : base(feature) { }

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

                feature.currentGroup.SetViewAndProjectionMatrices(cmd);
                feature.currentGroup.RestoreState(cmd, ref renderingData);

                feature.currentGroup.colorTexture = null;

                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
