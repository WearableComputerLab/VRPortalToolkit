using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering
{
    public abstract class PortalRendererBase : MonoBehaviour, IPortalRenderer
    {
        int IPortalRenderer.Layer => gameObject.layer;

        public virtual PortalRendererSettings Overrides => default;

        public abstract IPortal Portal { get; }

        protected virtual void OnEnable()
        {
            PortalRendering.RegisterPortalRenderer(this);
        }

        protected virtual void OnDisable()
        {
            PortalRendering.UnregisterPortalRenderer(this);
        }

        public abstract bool TryGetWindow(PortalRenderNode renderNode, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 proj, out ViewWindow innerWindow);

        public virtual bool TryGetClippingPlane(PortalRenderNode renderNode, out Vector3 clippingPlaneCentre, out Vector3 clippingPlaneNormal)
        {
            clippingPlaneCentre = clippingPlaneNormal = Vector3.zero;
            return false;
        }

        public virtual void PreCull(PortalRenderNode renderNode) {}

        public virtual void PostCull(PortalRenderNode renderNode) { }

        public abstract void Render(PortalRenderNode renderNode, CommandBuffer commandBuffer, Material material, MaterialPropertyBlock properties = null);

        public abstract void RenderDefault(PortalRenderNode renderNode, CommandBuffer commandBuffer);

        public virtual void PostRender(PortalRenderNode renderNode) {}
    }
}