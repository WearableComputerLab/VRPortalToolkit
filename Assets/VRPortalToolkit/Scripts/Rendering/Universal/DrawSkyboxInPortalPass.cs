using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    public class DrawSkyboxInPortalPass : PortalRenderPass
    {
        public DrawSkyboxInPortalPass(PortalRenderFeature feature) : base(feature) { }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                Camera renderCamera = feature.renderCamera;
                PortalRenderNode renderNode = feature.currentGroup.renderNode;

                // Need to clear the viewport for this to work
                cmd.SetViewport(new Rect(0f, 0f, renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height));

                if (renderingData.cameraData.xrRendering)
                {
                    // Setup Legacy XR buffer states
                    if (XRGraphics.stereoRenderingMode != XRGraphics.StereoRenderingMode.MultiPass)
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
