using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering.Universal
{
    // TODO: Could reuse culling results between recursive portals
    // Might need to combine the culling matrices, which I'm not sure is possible/practical
    public class BeginStencilPortalPass : PortalRenderPass
    {
        public PortalPassGroup passGroup;

        private static MaterialPropertyBlock propertyBlock;

        public BeginStencilPortalPass(PortalRenderFeature feature) : base(feature)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (passGroup == null || passGroup.renderNode == null || passGroup.renderNode.parent == null)
            {
                Debug.LogError(nameof(BeginStencilPortalPass) + "' passGroup is invalid!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                Camera camera = renderingData.cameraData.camera, renderCamera = feature.renderCamera;

                // Pass Group
                passGroup.parent = feature.currentGroup;
                PortalRenderNode renderNode = passGroup.renderNode;

                passGroup.parent.SetViewAndProjectionMatrices(cmd);

                // Masking
                cmd.SetGlobalInt(PropertyID.PortalStencilRef, renderNode.depth - 1);

                if (feature.portalIncrease)
                    renderNode.renderer.Render(camera, renderNode, cmd, feature.portalIncrease);

                cmd.SetGlobalInt(PropertyID.PortalStencilRef, renderNode.depth);

                if (feature.portalClearDepth)
                    renderNode.renderer.Render(camera, renderNode, cmd, feature.portalClearDepth);

                context.Submit(); // Needs to be submitted so global shaders are updated (for shadows and lighting etc.), would like to remove this some time down the line
                passGroup.parent.StoreState(ref renderingData);

                // Pre Render events
                PortalRenderer.onPreRender?.Invoke(camera, renderNode);
                renderNode.renderer.PreCull(camera, renderNode);

                // 
                float width = renderingData.cameraData.cameraTargetDescriptor.width,
                    height = renderingData.cameraData.cameraTargetDescriptor.height;

                Rect rect = renderNode.cullingWindow.GetRect();
                passGroup.viewport = new Rect(rect.x * width, rect.y * height, rect.width * width, rect.height * height);

                renderCamera.cullingMask = renderNode.cullingMask;
                renderCamera.projectionMatrix = renderNode.projectionMatrix;
                renderCamera.worldToCameraMatrix = renderNode.worldToCameraMatrix;

                // Setup current pass group
                feature.currentGroup = passGroup;
                cmd.SetGlobalVector(PropertyID.WorldSpaceCameraPos, (Vector3)renderNode.localToWorldMatrix.GetColumn(3));

                if (renderCamera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
                {
                    cullingParameters.maximumVisibleLights = UniversalRenderPipeline.maxVisibleAdditionalLights + 1;
                    cullingParameters.shadowDistance = renderingData.cameraData.maxShadowDistance;
                    cullingParameters.origin = renderNode.localToWorldMatrix.GetColumn(3);
                    if (renderNode.isStereo) cullingParameters.cullingOptions &= ~CullingOptions.Stereo;
                    renderingData.cullResults = context.Cull(ref cullingParameters);

                    renderingData.lightData.visibleLights = renderingData.cullResults.visibleLights;
                    renderingData.lightData.additionalLightsCount = renderingData.cullResults.visibleLights.Length;

                    // Update lights
                    if (passGroup.mainLightShadowCasterPass != null)
                        passGroup.mainLightShadowCasterPass.enabled = passGroup.mainLightShadowCasterPass.Setup(ref renderingData);

                    if (passGroup.additionalLightsShadowCasterPass != null)
                        passGroup.additionalLightsShadowCasterPass.enabled = passGroup.additionalLightsShadowCasterPass.Setup(ref renderingData);
                }
                else
                {
                    if (passGroup.mainLightShadowCasterPass != null)
                        passGroup.mainLightShadowCasterPass.enabled = false;

                    if (passGroup.additionalLightsShadowCasterPass != null)
                        passGroup.additionalLightsShadowCasterPass.enabled = false;
                }

                // Post Cull events
                renderNode.renderer.PostCull(camera, renderNode);

                forwardLights.Setup(context, ref renderingData);
                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
