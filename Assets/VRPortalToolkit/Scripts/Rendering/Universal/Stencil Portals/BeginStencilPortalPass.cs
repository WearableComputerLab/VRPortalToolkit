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
        public Material increaseMaterial { get; set; }

        public Material clearDepthMaterial { get; set; }

        public PortalPassNode passNode { get; set; }

        private static readonly Plane[] _planes = new Plane[6];

        public BeginStencilPortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (passNode == null || passNode.renderNode == null || passNode.renderNode.parent == null)
            {
                Debug.LogError(nameof(BeginStencilPortalPass) + "' passGroup is invalid!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                // Pass Group
                PortalPassStack.Push(passNode);
                PortalRenderNode renderNode = passNode.renderNode;

                Material increaseMaterial = renderNode.overrides.portalIncrease ? renderNode.overrides.portalIncrease : this.increaseMaterial,
                    clearDepthMaterial = renderNode.overrides.portalClearDepth ? renderNode.overrides.portalClearDepth : this.clearDepthMaterial;

                PortalPassStack.Parent.SetViewAndProjectionMatrices(cmd);

                // Masking
                cmd.SetGlobalInt(PropertyID.PortalStencilRef, renderNode.depth - 1);

                if (increaseMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer?.Render(renderNode, cmd, increaseMaterial);
                }

                cmd.SetGlobalInt(PropertyID.PortalStencilRef, renderNode.depth);

                if (clearDepthMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer?.Render(renderNode, cmd, clearDepthMaterial);
                }

                context.Submit(); // Needs to be submitted so global shaders are updated (for shadows and lighting etc.), would like to remove this some time down the line
                PortalPassStack.Parent.StoreState(ref renderingData);//

                // Pre Render events
                PortalRendering.onPreRender?.Invoke(renderNode);
                foreach (IPortalRenderer renderer in renderNode.renderers)
                    renderer?.PreCull(renderNode);

                // 
                float width = renderingData.cameraData.cameraTargetDescriptor.width,
                    height = renderingData.cameraData.cameraTargetDescriptor.height;

                Rect rect = renderNode.cullingWindow.GetRect();
                passNode.viewport = new Rect(rect.x * width, rect.y * height, rect.width * width, rect.height * height);

                // Setup current pass group
                cmd.SetGlobalVector(PropertyID.WorldSpaceCameraPos, (Vector3)renderNode.localToWorldMatrix.GetColumn(3));

                Camera renderCamera = PortalRenderFeature.renderCamera;
                renderCamera.cullingMask = renderNode.cullingMask;
                renderCamera.transform.position = renderNode.localToWorldMatrix.GetColumn(3);
                renderCamera.projectionMatrix = renderingData.cameraData.camera.projectionMatrix;
                renderCamera.worldToCameraMatrix = renderingData.cameraData.camera.worldToCameraMatrix * renderNode.connectedTeleportMatrix;
                /*renderCamera.cullingMask = renderNode.cullingMask;
                renderCamera.projectionMatrix = renderNode.projectionMatrix;
                renderCamera.worldToCameraMatrix = renderNode.worldToCameraMatrix;*/

                if (renderCamera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
                {
                    if (renderNode.isStereo) cullingParameters.cullingOptions &= ~CullingOptions.Stereo;

                    cullingParameters.cullingMask = (uint)renderNode.cullingMask;
                    cullingParameters.cullingMatrix = renderNode.projectionMatrix * renderNode.worldToCameraMatrix;

                    GeometryUtility.CalculateFrustumPlanes(cullingParameters.cullingMatrix, _planes);

                    for (int i = 0; i < _planes.Length; i++)
                        cullingParameters.SetCullingPlane(i, _planes[i]);

                    if (renderNode.isStereo) cullingParameters.cullingOptions &= ~CullingOptions.Stereo;

                    cullingParameters.maximumVisibleLights = UniversalRenderPipeline.maxVisibleAdditionalLights + 1;
                    cullingParameters.shadowDistance = renderingData.cameraData.maxShadowDistance;
                    cullingParameters.origin = renderNode.localToWorldMatrix.GetColumn(3);

                    renderingData.cullResults = context.Cull(ref cullingParameters);

                    renderingData.lightData.visibleLights = renderingData.cullResults.visibleLights;
                    renderingData.lightData.additionalLightsCount = renderingData.cullResults.visibleLights.Length;

                    // Update lights
                    if (passNode.mainLightShadowCasterPass != null)
                        passNode.mainLightShadowCasterPass.enabled = passNode.mainLightShadowCasterPass.Setup(ref renderingData);

                    if (passNode.additionalLightsShadowCasterPass != null)
                        passNode.additionalLightsShadowCasterPass.enabled = passNode.additionalLightsShadowCasterPass.Setup(ref renderingData);
                }
                else
                {
                    if (passNode.mainLightShadowCasterPass != null)
                        passNode.mainLightShadowCasterPass.enabled = false;

                    if (passNode.additionalLightsShadowCasterPass != null)
                        passNode.additionalLightsShadowCasterPass.enabled = false;
                }

                // Post Cull events
                foreach (IPortalRenderer renderer in renderNode.renderers)
                    renderer?.PostCull(renderNode);

                forwardLights.Setup(context, ref renderingData);
                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
