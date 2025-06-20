using Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering
{
    /// <summary>
    /// Interface for objects that handle camera transitions through portals.
    /// </summary>
    public interface IPortalCameraTransition
    {
        /// <summary>
        /// Gets the layer for this transition.
        /// </summary>
        int layer { get; }

        /// <summary>
        /// Gets the portal associated with this transition.
        /// </summary>
        IPortal portal { get; }

        /// <summary>
        /// Gets the transition plane information.
        /// </summary>
        /// <param name="planeCentre">Output parameter for the center of the transition plane.</param>
        /// <param name="planeNormal">Output parameter for the normal of the transition plane.</param>
        void GetTransitionPlane(out Vector3 planeCentre, out Vector3 planeNormal);
    }

    /// <summary>
    /// Settings for portal renderers that can be used to override default rendering behavior.
    /// </summary>
    [Serializable]
    public struct PortalRendererSettings : IEquatable<PortalRendererSettings>
    {
        /// <summary>
        /// Material used for stereo portal rendering.
        /// </summary>
        public Material portalStereo;

        /// <summary>
        /// Material used for increasing the stencil value.
        /// </summary>
        public Material portalIncrease;

        /// <summary>
        /// Material used for decreasing the stencil value.
        /// </summary>
        public Material portalDecrease;

        /// <summary>
        /// Material used for clearing the depth buffer.
        /// </summary>
        public Material portalClearDepth;

        /// <summary>
        /// Material used for depth-only rendering.
        /// </summary>
        public Material portalDepthOnly;

        /// <summary>
        /// Whether to generate a depth-normal texture for this portal.
        /// </summary>
        public bool depthNormalTexture;

        /// <summary>
        /// Compares this settings struct with another for equality.
        /// </summary>
        /// <param name="other">The other settings to compare with.</param>
        /// <returns>True if both settings are equal, false otherwise.</returns>
        public bool Equals(PortalRendererSettings other) =>
             depthNormalTexture == other.depthNormalTexture && portalStereo == other.portalStereo && portalIncrease == other.portalIncrease &&
            portalDecrease == other.portalDecrease && portalClearDepth == other.portalClearDepth && portalDepthOnly == other.portalDepthOnly;
    }

    /// <summary>
    /// Interface for objects that can render portals.
    /// </summary>
    public interface IPortalRenderer
    {
        /// <summary>
        /// Gets the layer for this portal renderer.
        /// </summary>
        int Layer { get; }

        /// <summary>
        /// Gets the portal associated with this renderer.
        /// </summary>
        IPortal Portal { get; }

        /// <summary>
        /// Gets override settings for portal rendering.
        /// </summary>
        PortalRendererSettings Overrides { get; }

        /// <summary>
        /// Tries to get the viewing window for this portal if it is visible.
        /// </summary>
        /// <param name="renderNode">The current render node.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="view">The view matrix.</param>
        /// <param name="proj">The projection matrix.</param>
        /// <param name="innerWindow">Output parameter for the calculated inner window.</param>
        /// <returns>True if a valid window was found, false otherwise.</returns>
        bool TryGetWindow(PortalRenderNode renderNode, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 proj, out ViewWindow innerWindow);

        /// <summary>
        /// Tries to get the clipping plane for this portal.
        /// </summary>
        /// <param name="renderNode">The current render node.</param>
        /// <param name="clippingPlaneCentre">Output parameter for the clipping plane center.</param>
        /// <param name="clippingPlaneNormal">Output parameter for the clipping plane normal.</param>
        /// <returns>True if a valid clipping plane was found, false otherwise.</returns>
        bool TryGetClippingPlane(PortalRenderNode renderNode, out Vector3 clippingPlaneCentre, out Vector3 clippingPlaneNormal);

        /// <summary>
        /// Called before the portal is culled.
        /// </summary>
        /// <param name="renderNode">The current render node.</param>
        void PreCull(PortalRenderNode renderNode);

        /// <summary>
        /// Called after the portal is culled.
        /// </summary>
        /// <param name="renderNode">The current render node.</param>
        void PostCull(PortalRenderNode renderNode);

        /// <summary>
        /// Renders the portal using the specified material.
        /// </summary>
        /// <param name="renderNode">The current render node.</param>
        /// <param name="commandBuffer">The command buffer to render into.</param>
        /// <param name="material">The material to use for rendering.</param>
        /// <param name="properties">Optional material property block to use.</param>
        void Render(PortalRenderNode renderNode, CommandBuffer commandBuffer, Material material, MaterialPropertyBlock properties = null);

        /// <summary>
        /// Renders the portal using its default rendering method.
        /// </summary>
        /// <param name="renderNode">The current render node.</param>
        /// <param name="commandBuffer">The command buffer to render into.</param>
        void RenderDefault(PortalRenderNode renderNode, CommandBuffer commandBuffer);

        /// <summary>
        /// Called after the portal is rendered.
        /// </summary>
        /// <param name="renderNode">The current render node.</param>
        void PostRender(PortalRenderNode renderNode);
    }

    /// <summary>
    /// Delegate for portal rendering callbacks.
    /// </summary>
    /// <param name="renderNode">The render node being processed.</param>
    public delegate void PortalRenderCallback(PortalRenderNode renderNode);

    /// <summary>
    /// Static class that manages portal rendering and transitions.
    /// </summary>
    public static class PortalRendering
    {
        /// <summary>
        /// Callback invoked before a portal is rendered.
        /// </summary>
        public static PortalRenderCallback onPreRender;
        
        /// <summary>
        /// Callback invoked after a portal is rendered.
        /// </summary>
        public static PortalRenderCallback onPostRender;

        private readonly static List<IPortalRenderer> _allRenderers = new List<IPortalRenderer>();
        private readonly static List<PortalCameraTransitionRenderer> _transitionRenderers = new List<PortalCameraTransitionRenderer>();
        private readonly static Dictionary<Camera, PortalCameraTransitionRenderer> _transitionByCamera = new Dictionary<Camera, PortalCameraTransitionRenderer>();
        private readonly static Misc.ObjectPool<PortalCameraTransitionRenderer> _transitionPool =
            new Misc.ObjectPool<PortalCameraTransitionRenderer>(() => new PortalCameraTransitionRenderer(), null);

        /// <summary>
        /// Gets all registered portal renderers.
        /// </summary>
        /// <returns>An enumerable of all portal renderers.</returns>
        public static IEnumerable<IPortalRenderer> GetAllPortalRenderers()
        {
            foreach (PortalCameraTransitionRenderer renderer in _transitionRenderers)
                yield return renderer;

            foreach (IPortalRenderer renderer in _allRenderers)
                yield return renderer;
        }

        /// <summary>
        /// Gets all portal renderers visible through the specified portal.
        /// </summary>
        /// <param name="portal">The portal to check visibility through.</param>
        /// <returns>An enumerable of visible portal renderers.</returns>
        public static IEnumerable<IPortalRenderer> GetVisiblePortalRenderers(IPortal portal)
        {
            IPortal connected = portal.connected;

            foreach (IPortalRenderer renderer in GetAllPortalRenderers())
            {
                if (renderer.Portal != connected)
                    yield return renderer;
            }
        }

        /// <summary>
        /// Gets all portal renderers visible through the portal associated with the specified renderer.
        /// </summary>
        /// <param name="renderer">The renderer whose portal to check visibility through.</param>
        /// <returns>An enumerable of visible portal renderers.</returns>
        public static IEnumerable<IPortalRenderer> GetVisiblePortalRenderers(this IPortalRenderer renderer) =>
            GetVisiblePortalRenderers(renderer.Portal);

        /// <summary>
        /// Registers a portal renderer.
        /// </summary>
        /// <param name="renderer">The portal renderer to register.</param>
        public static void RegisterPortalRenderer(IPortalRenderer renderer)
        {
            _allRenderers.Add(renderer);
        }

        /// <summary>
        /// Unregisters a portal renderer.
        /// </summary>
        /// <param name="renderer">The portal renderer to unregister.</param>
        public static void UnregisterPortalRenderer(IPortalRenderer renderer)
        {
            _allRenderers.Remove(renderer);
        }

        /// <summary>
        /// Registers a camera transition.
        /// </summary>
        /// <param name="camera">The camera for the transition.</param>
        /// <param name="transition">The transition to register.</param>
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

        /// <summary>
        /// Unregisters a camera transition.
        /// </summary>
        /// <param name="camera">The camera for the transition.</param>
        /// <param name="transition">The transition to unregister.</param>
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

        /// <summary>
        /// Tries to get the transition information for a camera.
        /// </summary>
        /// <param name="camera">The camera to check.</param>
        /// <param name="portal">Output parameter for the portal associated with the transition.</param>
        /// <param name="transitionCentre">Output parameter for the center of the transition plane.</param>
        /// <param name="transitionNormal">Output parameter for the normal of the transition plane.</param>
        /// <returns>True if a transition was found, false otherwise.</returns>
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
