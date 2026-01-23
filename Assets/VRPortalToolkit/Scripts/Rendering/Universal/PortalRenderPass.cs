using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Base class for all portal rendering passes in the Universal Render Pipeline.
    /// Extends ScriptableRenderPass to provide common functionality for portal rendering.
    /// </summary>
    public abstract class PortalRenderPass : ScriptableRenderPass
    {
        /// <summary>
        /// Forward lighting system used for all portal render passes.
        /// </summary>
        protected static ForwardLights forwardLights;

        /// <summary>
        /// Initializes a new instance of the PortalRenderPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public PortalRenderPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base()
        {
            this.renderPassEvent = renderPassEvent;

            profilingSampler = new ProfilingSampler(GetType().Name);

            if (forwardLights == null) forwardLights = new ForwardLights();
        }

        [Obsolete]
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (PortalPassStack.Current != null && PortalPassStack.Current.colorTexture)
                ConfigureTarget(PortalPassStack.Current.colorTarget);
        }
    }
}
