using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Render pass that draws the skybox for portal rendering.
    /// </summary>
    public class DrawSkyboxInPortalPass : PortalRenderPass
    {
        /// <summary>
        /// Initializes a new instance of the DrawSkyboxInPortalPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public DrawSkyboxInPortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.clearFlags != CameraClearFlags.Skybox)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                // TODO: Should not use portal render feature
                Camera renderCamera = PortalRenderFeature.renderCamera;
                PortalRenderNode renderNode = PortalPassStack.Current.renderNode;

                // Need to clear the viewport for this to work
                cmd.SetViewport(new Rect(0f, 0f, renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height));

                if (renderingData.cameraData.xrRendering)
                {
                    // Setup Legacy XR buffer states
                    if (XRSettings.stereoRenderingMode != XRSettings.StereoRenderingMode.MultiPass)
                    {
                        // TODO: Doesnt work for stencils

                        // Setup legacy skybox stereo buffer
                        renderCamera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, renderingData.cameraData.GetProjectionMatrix(0));
                        renderCamera.SetStereoViewMatrix(Camera.StereoscopicEye.Left, renderNode.GetStereoViewMatrix(0));
                        renderCamera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, renderingData.cameraData.GetProjectionMatrix(1));
                        renderCamera.SetStereoViewMatrix(Camera.StereoscopicEye.Right, renderNode.GetStereoViewMatrix(1));

                        cmd.SetSinglePassStereo(SystemInfo.supportsMultiview ? SinglePassStereoMode.Multiview : SinglePassStereoMode.Instancing);
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();

                        // Calling into built-in skybox pass
                        context.DrawSkybox(renderCamera);

                        // Disable Legacy XR path
                        cmd.SetSinglePassStereo(SinglePassStereoMode.None);
                        context.ExecuteCommandBuffer(cmd);
                    }
                    else
                    {
                        context.ExecuteCommandBuffer(cmd);

                        renderCamera.projectionMatrix = renderingData.cameraData.GetProjectionMatrix(0);
                        renderCamera.worldToCameraMatrix = renderNode.worldToCameraMatrix;

                        context.DrawSkybox(renderCamera);
                        context.Submit(); // TODO: Not entirely sure if this is necessary
                    }
                }
                else
                {
                    context.ExecuteCommandBuffer(cmd);

                    renderCamera.projectionMatrix = renderingData.cameraData.camera.projectionMatrix;
                    renderCamera.worldToCameraMatrix = renderNode.worldToCameraMatrix;

                    context.DrawSkybox(renderCamera);
                }
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
