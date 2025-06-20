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
    /// Render pass that begins the portal rendering process and initializes the portal pass stack.
    /// </summary>
    public class BeginPortalPass : PortalRenderPass
    {
        /// <summary>
        /// The portal pass node associated with this pass.
        /// </summary>
        public PortalPassNode portalPassNode { get; set; }

        /// <summary>
        /// Initializes a new instance of the BeginPortalPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public BeginPortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            PortalPassStack.Clear();
            PortalPassStack.Push(portalPassNode);
        }
    }
}
