using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using VRPortalToolkit.Rendering.Universal;
using VRPortalToolkit.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace VRPortalToolkit
{
    /// <summary>
    /// Render pass that begins the portal rendering process and initializes the portal pass stack.
    /// </summary>
    public class PortalCameraSetupPass : PortalRenderPass
    {
        public bool clearDepth { get; set; }

        /// <summary>
        /// Initializes a new instance of the BeginPortalPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public PortalCameraSetupPass(RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRendering) : base()
        {
            this.renderPassEvent = renderPassEvent;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();

            if (!clearDepth)
            {
                cameraData.clearDepth = false;
                cameraData.renderType = CameraRenderType.Overlay;
                cameraData.camera.clearFlags = CameraClearFlags.Nothing;
            }
        }
    }
}
