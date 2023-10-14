using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using VRPortalToolkit.Rendering.Universal;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// This is for the very specific use case where one eye of a stereo camera has passed through a portal
    /// </summary>
    public class BeginUndoStencilPortalPass : PortalRenderPass
    {
        public Material increaseMaterial { get; set; }

        public Material clearDepthMaterial { get; set; }

        public PortalPassNode passNode { get; set; }

        private static MaterialPropertyBlock propertyBlock;

        public BeginUndoStencilPortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        }

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
                // TODO: This should be important
                PortalPassNode parent = PortalPassStack.Parent;

                // Pass Group
                PortalPassStack.Push(passNode);
                PortalRenderNode renderNode = passNode.renderNode;

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

                // Restore using root portal pass node
                if (parent != null) parent.RestoreState(cmd, ref renderingData);

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
