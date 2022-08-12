using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering
{
    public abstract class PortalRenderer : MonoBehaviour
    {
        public static List<PortalRenderer> allRenderers { get; } = new List<PortalRenderer>();

        public virtual IEnumerable<PortalRenderer> visiblePortals => allRenderers;

        public delegate void PortalRenderCallback(Camera camera, PortalRenderNode renderNode);

        // TODO: May need a way to solve these calbacks, they are a little strange for how portals behave
        public static PortalRenderCallback onPreRender;
        public static PortalRenderCallback onPostRender;

        [SerializeField] private Portal _portal;
        /// <summary>The portal this is required to render.</summary>
        public Portal portal
        {
            get =>_portal;
            set => _portal = value;
        }
        public void ClearPortal() => portal = null;

        /*[SerializeField] private Texture _texture;
        public Texture texture {
            get => _texture;
            set => _texture = value;
        }

        [SerializeField] private Color _color = Color.white;
        public Color color {
            get => _color;
            set => _color = value;
        }*/

        protected virtual void Reset()
        {
            _portal = GetComponentInChildren<Portal>(true);

            if (!_portal) _portal = GetComponentInParent<Portal>();
        }

        protected virtual void OnEnable()
        {
            if (!allRenderers.Contains(this))
                allRenderers.Add(this);
        }

        protected virtual void OnDisable()
        {
            allRenderers.Remove(this);
        }

        public abstract bool TryGetWindow(Camera camera, out ViewWindow innerWindow);

        public abstract bool TryGetWindow(Matrix4x4 localToWorld, Matrix4x4 view, Matrix4x4 proj, out ViewWindow innerWindow);

        public virtual bool TryGetClippingPlane(Camera camera, PortalRenderNode renderNode, out Vector3 clippingPlaneCentre, out Vector3 clippingPlaneNormal)
        {
            clippingPlaneCentre = clippingPlaneNormal = Vector3.zero;
            return false;
        }

        public virtual void PreCull(Camera camera, PortalRenderNode renderNode) {}

        public virtual void PostCull(Camera camera, PortalRenderNode renderNode) { }

        public abstract void Render(Camera camera, PortalRenderNode renderNode, Material material, MaterialPropertyBlock properties = null);

        public abstract void Render(Camera camera, PortalRenderNode renderNode, CommandBuffer commandBuffer, Material material, MaterialPropertyBlock properties = null);

        public abstract void RenderDefault(Camera camera, PortalRenderNode renderNode);

        public abstract void RenderDefault(Camera camera, PortalRenderNode renderNode, CommandBuffer commandBuffer);

        public virtual void PostRender(Camera camera, PortalRenderNode renderNode) {}
    }
}