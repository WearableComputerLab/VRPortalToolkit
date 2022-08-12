using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    public class StoreFramePass : PortalRenderPass
    {
        private float _resolution;
        private Material _stereoBlit;

        public StoreFramePass(PortalRenderFeature feature) : base(feature)
        {
            //renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            _stereoBlit = CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/Blit");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (FrameBuffer.current != null)
            {
                if (renderingData.cameraData.isPreviewCamera || renderingData.cameraData.isSceneViewCamera)
                    _resolution = feature.editorBufferResolution;
                else
                    _resolution = feature.bufferResolution;
            }
            else
            {
                Debug.LogError("Frame buffer not found!");
                _resolution = 0f;
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (_resolution > 0)
            {
                cameraTextureDescriptor.depthBufferBits = 0;
                cameraTextureDescriptor.msaaSamples = 1;

                cameraTextureDescriptor.dimension = TextureDimension.Tex2DArray;
                cameraTextureDescriptor.width = Mathf.Max(1, (int)(cameraTextureDescriptor.width * _resolution));
                cameraTextureDescriptor.height = Mathf.Max(1, (int)(cameraTextureDescriptor.height * _resolution));

                FrameBuffer.current.UpdateTexture(cameraTextureDescriptor);

                ConfigureTarget(FrameBuffer.current.identifier);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (FrameBuffer.current == null)
            {
                Debug.LogError("Frame buffer not found!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                FrameBuffer.current.rootNode = feature.currentGroup.renderNode.root;

                RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;
                cmd.SetRenderTarget(FrameBuffer.current.identifier);

                if (feature.currentGroup.renderNode.isStereo)
                {
                    cmd.SetGlobalTexture(PropertyID.SourceTex, source);

                    Vector4 scaleBias = new Vector4(1, 1, 0, 0);
                    Vector4 scaleBiasRt = new Vector4(1, 1, 0, 0);
                    cmd.SetGlobalVector(PropertyID.ScaleBias, scaleBias);
                    cmd.SetGlobalVector(PropertyID.ScaleBiasRt, scaleBiasRt);
                    cmd.DrawProcedural(Matrix4x4.identity, _stereoBlit, -1, MeshTopology.Quads, 4, 1, null);
                }
                else
                    cmd.Blit(source, BuiltinRenderTextureType.CurrentActive);

                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
