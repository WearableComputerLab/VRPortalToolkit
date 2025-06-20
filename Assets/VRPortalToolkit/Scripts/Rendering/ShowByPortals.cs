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
    [ExecuteAlways]
    public class ShowByPortals : MonoBehaviour
    {
        [SerializeField] private bool _showing = true;
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

        [SerializeField] private List<Portal> _portals = new List<Portal>();
        public List<Portal> portals {
            get => _portals;
            set => _portals = value;
        }

        [SerializeField] private Include _includes = Include.LastMatchesAnyPortal;
        public Include includes {
            get => _includes;
            set => _includes = value;
        }

        [System.Flags]
        public enum Include
        {
            None = 0,
            NoPortal = 1 << 0,
            FirstMatchesAnyPortal = 1 << 1,
            LastMatchesAnyPortal = 1 << 2,
            AnyMatchesAnyPortal = 1 << 3,
            //StartMatchesPortalsAsPath = 1 << 4,
            //EndMatchesPortalsAsPath = 1 << 5,
            //AnywhereMatchesPortalsAsPath = 1 << 6,
            //ExactlyMatchesPortalsAsPath = 1 << 7
        }

        [SerializeField] private bool _inverted = false;
        public bool inverted {
            get => _inverted;
            set => _inverted = value;
        }

        public UnityEvent show = new UnityEvent();
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

        protected virtual void OnBeginCameraRendering(ScriptableRenderContext _, Camera camera) => CheckNonPortal(camera);

        //protected virtual void OnEndCameraRendering(ScriptableRenderContext _, Camera camera) => CheckNonPortal(camera);

        protected virtual void OnCameraPreCull(Camera camera) => CheckNonPortal(camera);

        //protected virtual void OnCameraPostRender(Camera camera) => CheckNonPortal(camera);

        protected virtual void CheckNonPortal(Camera camera)
        {
            if (_includes.HasFlag(Include.NoPortal))
                showing = !_inverted;
            else
                showing = _inverted;
        }

        protected virtual void OnPortalPreCull(PortalRenderNode renderNode) => CheckPortal(renderNode);

        protected virtual void OnPortalPostRender(PortalRenderNode renderNode)
        {
            if (renderNode.parent.portal != null)
                CheckPortal(renderNode.parent);
            else
                CheckNonPortal(renderNode.camera);
        }

        protected virtual void CheckPortal(PortalRenderNode renderNode)
        {
            //previousEnabled = showing;

            //bool portalsUpdated = false, valid;

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
