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
    // TODO: I could almost allow for internal passes within the portal system...
    // I mean I could probably just use a scriptable renderer that I hide, right?
    // I'd probably just need to ignore its Execute etc and call my own to mess with the order of things
    // I can also modify the renderData as I see fit before passing it out
    // This may end up being difficult though (can't change render targets and what not

    public abstract class PortalRenderPass : ScriptableRenderPass
    {
        protected static ForwardLights forwardLights;

        public PortalRenderPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base()
        {
            this.renderPassEvent = renderPassEvent;

            profilingSampler = new ProfilingSampler(GetType().Name);

            if (forwardLights == null) forwardLights = new ForwardLights();
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (PortalPassStack.Current != null && PortalPassStack.Current.colorTexture)
                ConfigureTarget(PortalPassStack.Current.colorTarget);
        }
    }
}
