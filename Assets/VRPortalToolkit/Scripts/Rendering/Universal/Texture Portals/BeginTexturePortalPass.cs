using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering.Universal
{
    public class BeginTexturePortalPass : PortalRenderPass
    {
        public PortalPassNode portalPassNode { get; set; }

        public float Resolution { get; set; } = 1f;

        private static readonly Plane[] _planes = new Plane[6];

        public BeginTexturePortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderPortalsBuffer buffer = RenderPortalsBuffer.GetBuffer(portalPassNode.renderNode);

            cameraTextureDescriptor.msaaSamples = 1;

            cameraTextureDescriptor.dimension = TextureDimension.Tex2DArray;
            cameraTextureDescriptor.width = Mathf.Max(1, (int)(cameraTextureDescriptor.width * Resolution));
            cameraTextureDescriptor.height = Mathf.Max(1, (int)(cameraTextureDescriptor.height * Resolution));

            buffer.UpdateTexture(cameraTextureDescriptor);

            portalPassNode.colorTexture = buffer.texture;

            ConfigureTarget(portalPassNode.colorTarget);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (portalPassNode == null || portalPassNode.renderNode == null || portalPassNode.renderNode.parent == null)
            {
                Debug.LogError(nameof(BeginTexturePortalPass) + "' passGroup is invalid!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                Camera camera = renderingData.cameraData.camera;

                // Pass Group
                PortalPassStack.Push(portalPassNode);
                PortalRenderNode renderNode = PortalPassStack.Current.renderNode;

                context.Submit(); // Needs to be submitted so global shaders are updated (for shadows and lighting etc.), I think
                PortalPassStack.Parent.StoreState(ref renderingData);

                // Pre Render events
                PortalRendering.onPreRender?.Invoke(renderNode);
                foreach (IPortalRenderer renderer in renderNode.renderers)
                    renderer?.PreCull(renderNode);

                // 
                float width = renderingData.cameraData.cameraTargetDescriptor.width,
                    height = renderingData.cameraData.cameraTargetDescriptor.height;

                Rect rect = renderNode.cullingWindow.GetRect();
                portalPassNode.viewport = new Rect(rect.x * width, rect.y * height, rect.width * width, rect.height * height);
                
                // Setup current pass group
                cmd.SetGlobalVector(PropertyID.WorldSpaceCameraPos, (Vector3)renderNode.localToWorldMatrix.GetColumn(3));
                cmd.ClearRenderTarget(true, true, camera.backgroundColor);

                if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
                {
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
                    if (portalPassNode.mainLightShadowCasterPass != null)
                        portalPassNode.mainLightShadowCasterPass.enabled = portalPassNode.mainLightShadowCasterPass.Setup(ref renderingData);

                    if (portalPassNode.additionalLightsShadowCasterPass != null)
                        portalPassNode.additionalLightsShadowCasterPass.enabled = portalPassNode.additionalLightsShadowCasterPass.Setup(ref renderingData);
                }
                else
                {
                    if (portalPassNode.mainLightShadowCasterPass != null)
                        portalPassNode.mainLightShadowCasterPass.enabled = false;

                    if (portalPassNode.additionalLightsShadowCasterPass != null)
                        portalPassNode.additionalLightsShadowCasterPass.enabled = false;
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