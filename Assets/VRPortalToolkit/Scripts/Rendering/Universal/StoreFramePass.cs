using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Render pass that stores the current frame into a buffer for future portal rendering to create the "infinite" portal effect.
    /// </summary>
    public class StoreFramePass : ScriptableRenderPass
    {
        private static Material _stereoBlit;

        /// <summary>
        /// The resolution scale factor for the stored frame texture.
        /// </summary>
        public float resolution { get; set; } = 1f;

        /// <summary>
        /// Initializes a new instance of the StoreFramePass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public StoreFramePass(RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents) : base()
        {
            this.renderPassEvent = renderPassEvent;
            if (!_stereoBlit) _stereoBlit = CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/Blit");
        }

        private class PassData { }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            FrameBuffer.current.rootNode = PortalRenderStack.Current.root;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (FrameBuffer.current == null)
            {
                Debug.LogError("Frame buffer not found!");
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var cameraTextureDescriptor = cameraData.cameraTargetDescriptor;
            cameraTextureDescriptor.depthBufferBits = 0;
            cameraTextureDescriptor.msaaSamples = 1;

            //cameraTextureDescriptor.dimension = TextureDimension.Tex2DArray;
            cameraTextureDescriptor.width = Mathf.Max(1, (int)(cameraTextureDescriptor.width * resolution));
            cameraTextureDescriptor.height = Mathf.Max(1, (int)(cameraTextureDescriptor.height * resolution));

            FrameBuffer.current.UpdateTexture(cameraTextureDescriptor);
            var texture = renderGraph.ImportTexture(FrameBuffer.current.handle);

            const string passName = "StoreFramePass";
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }

            var blitParams = new RenderGraphUtils.BlitMaterialParameters(resourceData.activeColorTexture, texture, _stereoBlit, 0);
            renderGraph.AddBlitPass(blitParams, "BlitToFrameBufferPass");
            //renderGraph.AddCopyPass(resourceData.activeColorTexture, texture, passName:"CopyToFrameBufferPass");
        }

        /// <inheritdoc/>
        //[Obsolete]
        //public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        //{
        //    //if (resolution > 0)
        //    //{
        //    //    cameraTextureDescriptor.depthBufferBits = 0;
        //    //    cameraTextureDescriptor.msaaSamples = 1;

        //    //    cameraTextureDescriptor.dimension = TextureDimension.Tex2DArray;
        //    //    cameraTextureDescriptor.width = Mathf.Max(1, (int)(cameraTextureDescriptor.width * resolution));
        //    //    cameraTextureDescriptor.height = Mathf.Max(1, (int)(cameraTextureDescriptor.height * resolution));

        //    //    FrameBuffer.current.UpdateTexture(cameraTextureDescriptor);

        //    //    ConfigureTarget(FrameBuffer.current.identifier);
        //    //}
        //}

        ///// <inheritdoc/>
        //[Obsolete]
        //public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        //{
        //    //if (FrameBuffer.current == null)
        //    //{
        //    //    Debug.LogError("Frame buffer not found!");
        //    //    return;
        //    //}

        //    //CommandBuffer cmd = CommandBufferPool.Get();

        //    ////using (new ProfilingScope(cmd, profilingSampler))
        //    //{
        //    //    FrameBuffer.current.rootNode = PortalPassStack.Current.renderNode.root;

        //    //    RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;
        //    //    cmd.SetRenderTarget(FrameBuffer.current.identifier);

        //    //    if (PortalPassStack.Current.renderNode.isStereo)
        //    //    {
        //    //        cmd.SetGlobalTexture(PropertyID.SourceTex, source);

        //    //        Vector4 scaleBias = new Vector4(1, 1, 0, 0);
        //    //        Vector4 scaleBiasRt = new Vector4(1, 1, 0, 0);
        //    //        cmd.SetGlobalVector(PropertyID.ScaleBias, scaleBias);
        //    //        cmd.SetGlobalVector(PropertyID.ScaleBiasRt, scaleBiasRt);
        //    //        cmd.DrawProcedural(Matrix4x4.identity, _stereoBlit, -1, MeshTopology.Quads, 4, 1, null);
        //    //    }
        //    //    else
        //    //        cmd.Blit(source, BuiltinRenderTextureType.CurrentActive);

        //    //    context.ExecuteCommandBuffer(cmd);
        //    //}

        //    //CommandBufferPool.Release(cmd);
        //}
    }
}
