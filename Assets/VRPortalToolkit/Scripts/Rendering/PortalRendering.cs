using Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
//using UnityEngine.Rendering;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering
{
    public interface IPortalCameraTransition
    {
        int layer { get; }

        IPortal portal { get; }

        void GetTransitionPlane(out Vector3 planeCentre, out Vector3 planeNormal);
    }

    [Serializable]
    public struct PortalRendererSettings : IEquatable<PortalRendererSettings>
    {
        public Material portalStereo;

        public Material portalIncrease;

        public Material portalDecrease;

        public Material portalClearDepth;

        public Material portalDepthOnly;

        public bool depthNormalTexture;

        public bool Equals(PortalRendererSettings other) =>
             depthNormalTexture == other.depthNormalTexture && portalStereo == other.portalStereo && portalIncrease == other.portalIncrease &&
            portalDecrease == other.portalDecrease && portalClearDepth == other.portalClearDepth && portalDepthOnly == other.portalDepthOnly;
    }

    public interface IPortalRenderer
    {
        int Layer { get; }

        IPortal Portal { get; }

        PortalRendererSettings Overrides { get; }

        bool TryGetWindow(PortalRenderNode renderNode, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 proj, out ViewWindow innerWindow);

        bool TryGetClippingPlane(PortalRenderNode renderNode, out Vector3 clippingPlaneCentre, out Vector3 clippingPlaneNormal);

        void PreCull(PortalRenderNode renderNode);

        void PostCull(PortalRenderNode renderNode);

        void Render(PortalRenderNode renderNode, CommandBuffer commandBuffer, Material material, MaterialPropertyBlock properties = null);

        void RenderDefault(PortalRenderNode renderNode, CommandBuffer commandBuffer);

        void PostRender(PortalRenderNode renderNode);
    }


    public delegate void PortalRenderCallback(PortalRenderNode renderNode);

    public static class PortalRendering
    {
        // TODO: May need a way to solve these calbacks, they are a little strange for how portals behave
        public static PortalRenderCallback onPreRender;
        public static PortalRenderCallback onPostRender;

        private readonly static List<IPortalRenderer> _allRenderers = new List<IPortalRenderer>();
        private readonly static List<PortalCameraTransitionRenderer> _transitionRenderers = new List<PortalCameraTransitionRenderer>();
        private readonly static Dictionary<Camera, PortalCameraTransitionRenderer> _transitionByCamera = new Dictionary<Camera, PortalCameraTransitionRenderer>();
        private readonly static Misc.ObjectPool<PortalCameraTransitionRenderer> _transitionPool =
            new Misc.ObjectPool<PortalCameraTransitionRenderer>(() => new PortalCameraTransitionRenderer(), null);

        public static IEnumerable<IPortalRenderer> GetAllPortalRenderers()
        {
            foreach (PortalCameraTransitionRenderer renderer in _transitionRenderers)
                yield return renderer;

            foreach (IPortalRenderer renderer in _allRenderers)
                yield return renderer;
        }

        public static IEnumerable<IPortalRenderer> GetVisiblePortalRenderers(IPortal portal)
        {
            IPortal connected = portal.connected;

            foreach (IPortalRenderer renderer in GetAllPortalRenderers())
            {
                if (renderer.Portal != connected)
                    yield return renderer;
            }
        }

        public static IEnumerable<IPortalRenderer> GetVisiblePortalRenderers(this IPortalRenderer renderer) =>
            GetVisiblePortalRenderers(renderer.Portal);

        public static void RegisterPortalRenderer(IPortalRenderer renderer)
        {
            _allRenderers.Add(renderer);
        }

        public static void UnregisterPortalRenderer(IPortalRenderer renderer)
        {
            _allRenderers.Remove(renderer);
        }

        public static void RegisterCameraTranstion(Camera camera, IPortalCameraTransition transition)
        {
            if (transition == null || camera == null) return;

            PortalCameraTransitionRenderer renderer = _transitionPool.Get();

            renderer.camera = camera;
            renderer.transition = transition;

            _transitionRenderers.Add(renderer);

            if (!_transitionByCamera.ContainsKey(camera))
                _transitionByCamera[camera] = renderer;
        }

        public static void UnregisterCameraTranstion(Camera camera, IPortalCameraTransition transition)
        {
            if (transition == null || camera == null) return;

            int index = _transitionRenderers.FindIndex((i) => i.camera == camera && i.transition == transition);

            if (index != -1)
            {
                PortalCameraTransitionRenderer renderer = _transitionRenderers[index];
                _transitionRenderers.RemoveAt(index);
                renderer.camera = null;
                renderer.transition = null;

                if (_transitionByCamera.TryGetValue(camera, out PortalCameraTransitionRenderer other) && renderer == other)
                {
                    renderer = _transitionRenderers.Find((i) => i.camera == camera && i.transition == transition);
                    
                    if (renderer != null)
                        _transitionByCamera[camera] = renderer;
                    else
                        _transitionByCamera.Remove(camera);
                }
            }
        }

        public static bool TryGetTransition(Camera camera, out IPortal portal, out Vector3 transitionCentre, out Vector3 transitionNormal)
        {
            if (_transitionByCamera.TryGetValue(camera, out PortalCameraTransitionRenderer renderer) && renderer.Portal != null)
            {
                portal = renderer.Portal;
                renderer.transition.GetTransitionPlane(out transitionCentre, out transitionNormal);
                return true;
            }

            portal = null;
            transitionCentre = transitionNormal = default;
            return false;
        }
    }
}
