using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering
{
    /// <summary>
    /// Base class for portal renderers. Handles registration and provides the interface for rendering portals.
    /// </summary>
    public abstract class PortalRendererBase : MonoBehaviour, IPortalRenderer
    {
        /// <inheritdoc/>
        int IPortalRenderer.Layer => gameObject.layer;

        /// <inheritdoc/>
        public virtual PortalRendererSettings Overrides => default;

        /// <inheritdoc/>
        public abstract IPortal Portal { get; }
        
        protected virtual void OnEnable()
        {
            PortalRendering.RegisterPortalRenderer(this);
        }

        protected virtual void OnDisable()
        {
            PortalRendering.UnregisterPortalRenderer(this);
        }

        /// <inheritdoc/>
        public abstract bool TryGetWindow(PortalRenderNode renderNode, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 proj, out ViewWindow innerWindow);

        /// <inheritdoc/>
        public virtual bool TryGetClippingPlane(PortalRenderNode renderNode, out Vector3 clippingPlaneCentre, out Vector3 clippingPlaneNormal)
        {
            clippingPlaneCentre = clippingPlaneNormal = Vector3.zero;
            return false;
        }

        /// <inheritdoc/>
        public virtual void PreCull(PortalRenderNode renderNode) {}

        /// <inheritdoc/>
        public virtual void PostCull(PortalRenderNode renderNode) { }

        /// <inheritdoc/>
        public abstract void Render(PortalRenderNode renderNode, CommandBuffer commandBuffer, Material material, MaterialPropertyBlock properties = null);

        /// <inheritdoc/>
        public abstract void RenderDefault(PortalRenderNode renderNode, CommandBuffer commandBuffer);

        /// <inheritdoc/>
        public virtual void PostRender(PortalRenderNode renderNode) {}
    }
}