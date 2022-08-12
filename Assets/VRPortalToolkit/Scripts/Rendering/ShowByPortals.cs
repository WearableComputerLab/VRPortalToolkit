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

            PortalRenderer.onPreRender += OnPortalPreCull;
            PortalRenderer.onPostRender += OnPortalPostRender;

            if (_showing) show?.Invoke();
            else hide?.Invoke();
        }

        protected virtual void OnDisable()
        {
            Camera.onPreCull -= OnCameraPreCull;
            //Camera.onPostRender -= OnCameraPostRender;

            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            //RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            PortalRenderer.onPreRender -= OnPortalPreCull;
            PortalRenderer.onPostRender -= OnPortalPostRender;
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

        protected virtual void OnPortalPreCull(Camera camera, PortalRenderNode renderNode) => CheckPortal(camera, renderNode);

        protected virtual void OnPortalPostRender(Camera camera, PortalRenderNode renderNode)
        {
            if (renderNode.parent.renderer)
                CheckPortal(camera, renderNode.parent);
            else
                CheckNonPortal(camera);
        }

        protected virtual void CheckPortal(Camera camera, PortalRenderNode renderNode)
        {
            //previousEnabled = showing;

            //bool portalsUpdated = false, valid;

            // FirstMatchesAnyPortal
            if (_includes.HasFlag(Include.FirstMatchesAnyPortal))
            {
                PortalRenderNode firstNode = renderNode;

                while (firstNode.parent != null && firstNode.parent.renderer)
                    firstNode = firstNode.parent;

                Portal first = firstNode.renderer.portal;

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
                Portal last = renderNode.renderer.portal;

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
                    current = currentNode.renderer.portal;

                    foreach (Portal portal in portals)
                    {
                        if (portal == current)
                        {
                            showing = !_inverted;
                            return;
                        }
                    }

                } while (currentNode != null && currentNode.renderer);
            }

            // StartMatchesPortalsAsPath
            /*if (_includes.HasFlag(Include.StartMatchesPortalsAsPath))
            {
                TryUpdatePortalpath(renderNode, ref portalsUpdated);

                valid = true;

                for (int i = 0; i < portals.Count && i < renderPath.Count; j++)
                {
                    if (portals[i] != renderPath[i])
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    _renderer.enabled = !_inverted;
                    return;
                }
            }

            // EndMatchesPortalsAsPath
            if (_includes.HasFlag(Include.EndMatchesPortalsAsPath))
            {
                TryUpdatePortalpath(renderNode, ref portalsUpdated);

                valid = true;

                for (int i = 0, j = 1; j < portals.Count && j < portals.Count; i++, j++)
                {
                    if (portals[i] != renderPath[j])
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    _renderer.enabled = !_inverted;
                    return;
                }
            }

            // ExactlyMatchesPortalsAsPath
            if (_includes.HasFlag(Include.ExactlyMatchesPortalsAsPath))
            {
                TryUpdatePortalpath(renderNode, ref portalsUpdated);

                if (portals.Count == renderPath.Count)
                {
                    valid = true;

                    for (int i = 0; i < portals.Count; i++)
                    {
                        if (portals[i] != renderPath[i])
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                    {
                        _renderer.enabled = !_inverted;
                        return;
                    }
                }
            }*/

            showing = _inverted;
        }

        /*protected virtual void TryUpdatePortalpath(PortalRenderNode renderNode, ref bool portalsUpdated)
        {
            if (!portalsUpdated)
            {
                UpdateRenderPath(renderNode);
                portalsUpdated = true;
            }
        }

        private void UpdateRenderPath(PortalRenderNode renderNode)
        {
            PortalRenderNode current = renderNode;

            renderPath.Clear();

            do
            {
                // This is added cause its faster than insert I think
                renderPath.Add(current.renderer.portal);
                current = current.parent;
            } while (renderNode.parent != null && renderNode.parent.renderer);

            renderPath.Reverse();
        }*/
    }
}
