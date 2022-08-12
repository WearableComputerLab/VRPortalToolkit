using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    public class DrawTransparentObjectsInPortalPass : PortalRenderPass
    {
        public DrawTransparentObjectsInPortalPass(PortalRenderFeature feature) : base(feature) { }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                feature.currentGroup.SetViewAndProjectionMatrices(cmd);
                context.ExecuteCommandBuffer(cmd);
                
                context.DrawRenderers(renderingData.cullResults, ref feature.transparentDrawingSettings, ref feature.transparentFilteringSettings, ref feature.currentGroup.stateBlock);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
