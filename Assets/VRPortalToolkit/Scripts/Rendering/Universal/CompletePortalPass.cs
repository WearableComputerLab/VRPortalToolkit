using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using VRPortalToolkit.Rendering.Universal;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit
{
    /// <summary>
    /// Render pass that completes the portal rendering process and cleans up resources.
    /// </summary>
    public class CompletePortalPass : PortalRenderPass
    {
        /// <summary>
        /// Initializes a new instance of the CompletePortalPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public CompletePortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            PortalPassStack.Clear();
        }
    }
}
