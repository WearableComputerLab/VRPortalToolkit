using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using VRPortalToolkit.Rendering.Universal;
using VRPortalToolkit.Rendering;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering
{
    public class PortalDepthNormalsPass : PortalRenderPass
    {
        public static readonly int PortalDepthNormalsTexture = Shader.PropertyToID("_PortalDepthNormalsTexture");

        private static Material depthNormalsMaterial;

        private DrawingSettings _drawingSettings;
        public DrawingSettings drawingSettings { get => _drawingSettings; set => _drawingSettings = value; }

        private FilteringSettings _filteringSettings;
        public FilteringSettings filteringSettings { get => _filteringSettings; set => _filteringSettings = value; }

        //public PortalRenderer portalRenderer { get; set; }

        private ShaderTagId _shaderTagId = new ShaderTagId("DepthOnly");

        private RenderTexture _depthNormalsTexture;

        public PortalDepthNormalsPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent)
        {
            if (!depthNormalsMaterial)
                depthNormalsMaterial = CoreUtils.CreateEngineMaterial("Hidden/Internal-DepthNormalsTexture");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            //RenderTextureDescriptor descriptor = new RenderTextureDescriptor(cameraTextureDescriptor.width, cameraTextureDescriptor.height, RenderTextureFormat.ARGB32, 32);
            //descriptor.dimension = TextureDimension.Tex2DArray;

            cameraTextureDescriptor.msaaSamples = 1;
            cameraTextureDescriptor.depthBufferBits = 32;
            cameraTextureDescriptor.dimension = TextureDimension.Tex2DArray;
            cameraTextureDescriptor.colorFormat = RenderTextureFormat.ARGB32;

            _depthNormalsTexture = RenderTexture.GetTemporary(cameraTextureDescriptor);
            ConfigureTarget(new RenderTargetIdentifier(_depthNormalsTexture, 0, CubemapFace.Unknown, -1));
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            // TODO: This shouldn't be neccessary, but it is and I don't know why
            /*cmd.SetViewProjectionMatrices(PortalPassStack.Parent.renderNode.worldToCameraMatrix, PortalPassStack.Current.renderNode.projectionMatrix);

            if (PortalPassStack.Current.renderNode.isStereo)
            {
                cmd.SetStereoViewProjectionMatrices(PortalPassStack.Parent.renderNode.GetStereoViewMatrix(0), PortalPassStack.Current.renderNode.GetStereoProjectionMatrix(0),
                    PortalPassStack.Parent.renderNode.GetStereoViewMatrix(1), PortalPassStack.Current.renderNode.GetStereoProjectionMatrix(1));
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (portalRenderer.enabled)
                portalRenderer.Render(renderingData.cameraData.camera, PortalPassStack.Current.renderNode, cmd, depthNormalsMaterial);
            else
            {
                portalRenderer.enabled = true;
                portalRenderer.Render(renderingData.cameraData.camera, PortalPassStack.Current.renderNode, cmd, depthNormalsMaterial);
                portalRenderer.enabled = false;
            }*/

            PortalPassStack.Current.SetViewAndProjectionMatrices(cmd, true);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            DrawingSettings drawSettings = _drawingSettings;

            drawSettings.sortingSettings = new SortingSettings(renderingData.cameraData.camera)
            { criteria = renderingData.cameraData.defaultOpaqueSortFlags };

            drawSettings.SetShaderPassName(0, _shaderTagId);
            drawSettings.perObjectData = PerObjectData.None;

            drawSettings.overrideMaterial = depthNormalsMaterial;

            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings, ref PortalPassStack.Current.stateBlock);

            cmd.SetGlobalTexture(PortalDepthNormalsTexture, _depthNormalsTexture);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (_depthNormalsTexture)
            {
                RenderTexture.ReleaseTemporary(_depthNormalsTexture);
                _depthNormalsTexture = null;
            }
        }
    }
}
