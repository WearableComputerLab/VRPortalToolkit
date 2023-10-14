using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    public class DrawObjectsInPortalPass : PortalRenderPass
    {
        private DrawingSettings _drawingSettings;
        public DrawingSettings drawingSettings { get => _drawingSettings; set => _drawingSettings = value; }

        private FilteringSettings _filteringSettings;
        public FilteringSettings filteringSettings { get => _filteringSettings; set => _filteringSettings = value; }

        private Material _overrideMaterial;
        public Material overrideMaterial { get => _overrideMaterial; set => _overrideMaterial = value; }

        public DrawObjectsInPortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            
            //using (new ProfilingScope(cmd, new ProfilingSampler(nameof(DrawOpaqueObjectsInPortalPass))))
            {
                PortalPassStack.Current.SetViewAndProjectionMatrices(cmd);
                context.ExecuteCommandBuffer(cmd);
                context.DrawRenderers(renderingData.cullResults, ref _drawingSettings, ref _filteringSettings, ref PortalPassStack.Current.stateBlock);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
