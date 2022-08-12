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
        public PortalPassGroup passGroup;

        private float _resolution;

        public BeginTexturePortalPass(PortalRenderFeature feature) : base(feature) { }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (feature)
            {
                if (renderingData.cameraData.isPreviewCamera || renderingData.cameraData.isSceneViewCamera)
                    _resolution = feature.editorPortalResolution;
                else
                    _resolution = feature.portalResolution;
            }
            else
                _resolution = 1f;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderPortalsBuffer buffer = RenderPortalsBuffer.GetBuffer(passGroup.renderNode);

            cameraTextureDescriptor.msaaSamples = 1;

            cameraTextureDescriptor.dimension = TextureDimension.Tex2DArray;
            cameraTextureDescriptor.width = Mathf.Max(1, (int)(cameraTextureDescriptor.width * _resolution));
            cameraTextureDescriptor.height = Mathf.Max(1, (int)(cameraTextureDescriptor.height * _resolution));

            buffer.UpdateTexture(cameraTextureDescriptor);

            passGroup.colorTexture = buffer.texture;

            ConfigureTarget(passGroup.colorTarget);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (passGroup == null || passGroup.renderNode == null || passGroup.renderNode.parent == null)
            {
                Debug.LogError(nameof(BeginTexturePortalPass) + "' passGroup is invalid!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                Camera camera = renderingData.cameraData.camera, renderCamera = feature.renderCamera;

                // Pass Group
                passGroup.parent = feature.currentGroup;
                PortalRenderNode renderNode = passGroup.renderNode;

                context.Submit(); // Needs to be submitted so global shaders are updated (for shadows and lighting etc.), I think
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
                cmd.ClearRenderTarget(true, true, Color.clear);

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