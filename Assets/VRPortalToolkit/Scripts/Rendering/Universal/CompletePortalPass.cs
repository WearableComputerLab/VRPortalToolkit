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
    public class CompletePortalPass : PortalRenderPass
    {
        public CompletePortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            PortalPassStack.Clear();
        }
    }
}
