using System;
using System.Collections;
using System.Collections.Generic;
using Misc.EditorHelpers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit
{
    /// <summary>
    /// Controls the visibility of an object based on its relation to specific portals during rendering.
    /// </summary>
    /// <remarks>
    /// This component allows objects to be shown or hidden conditionally based on whether they are
    /// being viewed directly or through specific portals. This is useful for creating effects where
    /// objects appear or disappear when seen through portals.
    /// </remarks>
    [ExecuteAlways]
    public class ShowByPortals : MonoBehaviour
    {
        [SerializeField, Tooltip("Whether the object is currently visible.")]
        private bool _showing = true;
        /// <summary>
        /// Gets or sets whether the object is currently showing.
        /// </summary>
        /// <remarks>
        /// When this value changes, the corresponding show or hide events will be invoked.
        /// </remarks>
        public bool showing
        {
            get => _showing;
            set
            {
                if (_showing != value)
                {
                    Validate.UpdateField(this, nameof(_showing), _showing = value);

                    if (_showing) show?.Invoke();
                    else hide?.Invoke();
                }
            }
        }

        [SerializeField, Tooltip("The portals to check against when determining visibility.")]
        private List<Portal> _portals = new List<Portal>();
        /// <summary>
        /// Gets or sets the list of portals to check when determining visibility.
        /// </summary>
        public List<Portal> portals {
            get => _portals;
            set => _portals = value;
        }

        [SerializeField, Tooltip("The inclusion rules that determine when this object should be visible.")]
        private Include _includes = Include.LastMatchesAnyPortal;
        /// <summary>
        /// Gets or sets the inclusion rules that determine when this object should be visible.
        /// </summary>
        public Include includes {
            get => _includes;
            set => _includes = value;
        }

        /// <summary>
        /// Flags that determine the conditions under which the object should be visible.
        /// </summary>
        [System.Flags]
        public enum Include
        {
            /// <summary>No inclusion rules are active.</summary>
            None = 0,
            /// <summary>Show when not viewed through any portal.</summary>
            NoPortal = 1 << 0,
            /// <summary>Show when the first portal in the render path matches any portal in the list.</summary>
            FirstMatchesAnyPortal = 1 << 1,
            /// <summary>Show when the last portal in the render path matches any portal in the list.</summary>
            LastMatchesAnyPortal = 1 << 2,
            /// <summary>Show when any portal in the render path matches any portal in the list.</summary>
            AnyMatchesAnyPortal = 1 << 3,
            //StartMatchesPortalsAsPath = 1 << 4,
            //EndMatchesPortalsAsPath = 1 << 5,
            //AnywhereMatchesPortalsAsPath = 1 << 6,
            //ExactlyMatchesPortalsAsPath = 1 << 7
        }

        [SerializeField, Tooltip("When true, the visibility rules are inverted.")]
        private bool _inverted = false;
        /// <summary>
        /// Gets or sets whether the visibility rules should be inverted.
        /// </summary>
        /// <remarks>
        /// When true, the object will be hidden when the inclusion rules are satisfied
        /// and shown when they are not.
        /// </remarks>
        public bool inverted {
            get => _inverted;
            set => _inverted = value;
        }

        /// <summary>
        /// Event invoked when the object becomes visible.
        /// </summary>
        public UnityEvent show = new UnityEvent();
        
        /// <summary>
        /// Event invoked when the object becomes hidden.
        /// </summary>
        public UnityEvent hide = new UnityEvent();

        //protected List<Portal> renderPath = new List<Portal>();

        //protected bool previousEnabled;

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_showing), nameof(showing));
        }

        protected virtual void OnEnable()
        {
            Camera.onPreCull += OnCameraPreCull;
            //Camera.onPostRender += OnCameraPostRender;

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            //RenderPipelineManager.endCameraRendering += OnEndCameraRendering;

            PortalRendering.onPreRender += OnPortalPreCull;
            PortalRendering.onPostRender += OnPortalPostRender;

            if (_showing) show?.Invoke();
            else hide?.Invoke();
        }

        protected virtual void OnDisable()
        {
            Camera.onPreCull -= OnCameraPreCull;
            //Camera.onPostRender -= OnCameraPostRender;

            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            //RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            PortalRendering.onPreRender -= OnPortalPreCull;
            PortalRendering.onPostRender -= OnPortalPostRender;
        }

        /// <summary>
        /// Called when a camera begins rendering in the scriptable render pipeline.
        /// </summary>
        /// <param name="_">The render context (unused).</param>
        /// <param name="camera">The camera that is rendering.</param>
        protected virtual void OnBeginCameraRendering(ScriptableRenderContext _, Camera camera) => CheckNonPortal(camera);

        /// <summary>
        /// Called just before a camera culls the scene.
        /// </summary>
        /// <param name="camera">The camera that is about to render.</param>
        protected virtual void OnCameraPreCull(Camera camera) => CheckNonPortal(camera);

        /// <summary>
        /// Checks visibility rules for non-portal rendering.
        /// </summary>
        /// <param name="camera">The camera that is rendering.</param>
        protected virtual void CheckNonPortal(Camera camera)
        {
            if (_includes.HasFlag(Include.NoPortal))
                showing = !_inverted;
            else
                showing = _inverted;
        }

        /// <summary>
        /// Called just before a portal is rendered.
        /// </summary>
        /// <param name="renderNode">The portal render node that is about to be rendered.</param>
        protected virtual void OnPortalPreCull(PortalRenderNode renderNode) => CheckPortal(renderNode);

        /// <summary>
        /// Called after a portal has been rendered.
        /// </summary>
        /// <param name="renderNode">The portal render node that was rendered.</param>
        protected virtual void OnPortalPostRender(PortalRenderNode renderNode)
        {
            if (renderNode.parent.portal != null)
                CheckPortal(renderNode.parent);
            else
                CheckNonPortal(renderNode.camera);
        }

        /// <summary>
        /// Checks the visibility rules against the current portal render node.
        /// </summary>
        /// <param name="renderNode">The portal render node to check against.</param>
        protected virtual void CheckPortal(PortalRenderNode renderNode)
        {
            // FirstMatchesAnyPortal
            if (_includes.HasFlag(Include.FirstMatchesAnyPortal))
            {
                PortalRenderNode firstNode = renderNode;

                while (firstNode.parent != null && firstNode.parent.portal != null)
                    firstNode = firstNode.parent;

                Portal first = firstNode.portal as Portal;

                foreach (Portal portal in portals)
                {
                    if (portal == first)
                    {
                        showing = !_inverted;
                        return;
                    }
                }
            }

            // LastMatchesAnyPortal
            if (_includes.HasFlag(Include.LastMatchesAnyPortal))
            {
                Portal last = renderNode.portal as Portal;

                foreach (Portal portal in portals)
                {
                    if (portal == last)
                    {
                        showing = !_inverted;
                        return;
                    }
                }
            }

            // AnyMatchesAnyPortal
            if (_includes.HasFlag(Include.AnyMatchesAnyPortal))
            {
                PortalRenderNode currentNode = renderNode;
                Portal current;

                do
                {
                    current = currentNode.portal as Portal;

                    foreach (Portal portal in portals)
                    {
                        if (portal == current)
                        {
                            showing = !_inverted;
                            return;
                        }
                    }

                } while (currentNode != null && currentNode.portal != null);
            }

            showing = _inverted;
        }
    }
}
