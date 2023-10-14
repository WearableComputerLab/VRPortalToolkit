using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    public class StoreFramePass : PortalRenderPass
    {
        private static Material _stereoBlit;

        public float resolution { get; set; } = 1f;

        //public PortalRenderNode rootRenderNode { get; set; }

        public StoreFramePass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent)
        {
            if (!_stereoBlit) _stereoBlit = CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/Blit");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (resolution > 0)
            {
                cameraTextureDescriptor.depthBufferBits = 0;
                cameraTextureDescriptor.msaaSamples = 1;

                cameraTextureDescriptor.dimension = TextureDimension.Tex2DArray;
                cameraTextureDescriptor.width = Mathf.Max(1, (int)(cameraTextureDescriptor.width * resolution));
                cameraTextureDescriptor.height = Mathf.Max(1, (int)(cameraTextureDescriptor.height * resolution));

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
                FrameBuffer.current.rootNode = PortalPassStack.Current.renderNode.root;

                RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;
                cmd.SetRenderTarget(FrameBuffer.current.identifier);

                if (PortalPassStack.Current.renderNode.isStereo)
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
