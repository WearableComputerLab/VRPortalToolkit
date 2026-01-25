using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Render pass that draws objects within a portal view.
    /// </summary>
    public class DrawObjectsInPortalPass : PortalRenderPass
    {
        private DrawingSettings _drawingSettings;
        /// <summary>
        /// The drawing settings to use for rendering objects.
        /// </summary>
        public DrawingSettings drawingSettings { get => _drawingSettings; set => _drawingSettings = value; }

        private FilteringSettings _filteringSettings;
        /// <summary>
        /// The filtering settings to use for determining which objects to render.
        /// </summary>
        public FilteringSettings filteringSettings { get => _filteringSettings; set => _filteringSettings = value; }

        private Material _overrideMaterial;
        /// <summary>
        /// Optional material to use for overriding the appearance of all rendered objects.
        /// </summary>
        public Material overrideMaterial { get => _overrideMaterial; set => _overrideMaterial = value; }

        /// <summary>
        /// Initializes a new instance of the DrawObjectsInPortalPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public DrawObjectsInPortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        /// <inheritdoc/>
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
