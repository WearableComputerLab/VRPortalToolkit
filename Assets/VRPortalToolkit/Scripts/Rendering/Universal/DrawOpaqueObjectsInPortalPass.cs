using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    public class DrawOpaqueObjectsInPortalPass : PortalRenderPass
    {
        public DrawOpaqueObjectsInPortalPass(PortalRenderFeature feature) : base(feature) { }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            
            //using (new ProfilingScope(cmd, new ProfilingSampler(nameof(DrawOpaqueObjectsInPortalPass))))
            {
                feature.currentGroup.SetViewAndProjectionMatrices(cmd);
                context.ExecuteCommandBuffer(cmd);

                context.DrawRenderers(renderingData.cullResults, ref feature.opaqueDrawingSettings, ref feature.opaqueFilteringSettings, ref feature.currentGroup.stateBlock);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
