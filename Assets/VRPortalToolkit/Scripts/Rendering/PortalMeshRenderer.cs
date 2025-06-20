using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering
{
    /// <summary>
    /// Renders a portal using a mesh from a MeshFilter component.
    /// </summary>
    [RequireComponent(typeof(MeshFilter)), ExecuteInEditMode]
    public class PortalMeshRenderer : PortalRendererBase
    {
        [SerializeField] private Portal _portal;
        /// <summary>
        /// The portal this renderer is associated with.
        /// </summary>
        public Portal portal
        {
            get => _portal;
            set => _portal = value;
        }
        public override IPortal Portal => _portal;

        [SerializeField] private PortalMeshRenderer _connectedRenderer;
        /// <summary>
        /// The connected portal mesh renderer.
        /// </summary>
        public PortalMeshRenderer connectedRenderer {
            get => _connectedRenderer;
            set => _connectedRenderer = value;
        }

        private MeshFilter _filter;
        /// <summary>
        /// The filter for the portal mesh.
        /// </summary>
        public MeshFilter filter => _filter ? _filter : _filter = GetComponent<MeshFilter>();

        [SerializeField] private Transform _clippingPlane;
        /// <summary>
        /// The transform used for clipping plane calculations.
        /// </summary>
        public Transform clippingPlane {
            get => _clippingPlane;
            set => _clippingPlane = value;
        }

        [SerializeField] private ClippingMode _clippingMode;
        /// <summary>
        /// The clipping mode used for the portal.
        /// </summary>
        public ClippingMode clippingMode {
            get => _clippingMode;
            set => _clippingMode = value;
        }

        /// <summary>
        /// Defines how the portal mesh should be clipped.
        /// </summary>
        public enum ClippingMode
        {
            /// <summary>No clipping is applied.</summary>
            None = 0,
            
            /// <summary>Portal is only visible from one side.</summary>
            OneSided = 1,
            
            /// <summary>Portal is visible from both sides.</summary>
            DoubleSided = 2,
        }

        [SerializeField] private CullMode _cullMode = CullMode.Back;
        /// <summary>
        /// The culling mode used for the portal mesh.
        /// </summary>
        public CullMode cullMode {
            get => _cullMode;
            set => _cullMode = value;
        }

        [SerializeField] private float _clippingOffset = 0.001f;
        /// <summary>
        /// The offset used for the clipping plane to prevent z-fighting.
        /// </summary>
        public float clippingOffset {
            get => _clippingOffset;
            set => _clippingOffset = value;
        }

        [SerializeField] private Material _defaultMaterial;
        /// <summary>
        /// The default material used when rendering the portal.
        /// </summary>
        public Material defaultMaterial {
            get => _defaultMaterial;
            set => _defaultMaterial = value;
        }

        [SerializeField] private PortalRendererSettings _overrides;
        /// <summary>
        /// Override settings for portal rendering.
        /// </summary>
        public PortalRendererSettings overrides
        {
            get => _overrides;
            set => _overrides = value;
        }
        public override PortalRendererSettings Overrides => _overrides;

        /// <summary>
        /// Event triggered before the portal is culled.
        /// </summary>
        public UnityAction<PortalRenderNode> preCull;
        
        /// <summary>
        /// Event triggered after the portal is culled.
        /// </summary>
        public UnityAction<PortalRenderNode> postCull;
        
        /// <summary>
        /// Event triggered after the portal is rendered.
        /// </summary>
        public UnityAction<PortalRenderNode> postRender;

        protected virtual void Reset()
        {
            _portal = GetComponentInChildren<Portal>(true);

            if (!_portal) _portal = GetComponentInParent<Portal>();

            _clippingPlane = transform;
        }

        protected virtual void OnDrawGizmos()
        {
            if (filter && _filter.sharedMesh)
            {
                Gizmos.matrix = transform.localToWorldMatrix;

                // Should be able to click on portals now
                if (portal && portal.connected)
                    Gizmos.color = Color.clear;
                else
                    Gizmos.color = Color.grey;

                Gizmos.DrawMesh(_filter.sharedMesh);
            }
        }

        /// <inheritdoc/>
        public override bool TryGetWindow(PortalRenderNode renderNode, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 proj, out ViewWindow innerWindow)
        {
            if (!isActiveAndEnabled || !filter || !_filter.sharedMesh)
            {
                innerWindow = default;
                return false;
            }

            // If one side, it shouldn't display from certain sides
            if (_clippingPlane && _clippingMode == ClippingMode.OneSided && !IsOnFrontSide(cameraPosition))
            {
                innerWindow = default;
                return false;
            }

            if (_clippingPlane && _clippingMode == ClippingMode.DoubleSided && !IsOnFrontSide(cameraPosition))
                innerWindow = ViewWindow.GetWindow(view, proj, _filter.sharedMesh.bounds, transform.localToWorldMatrix); // TODO: Should flip by clipping plane
            else
                innerWindow = ViewWindow.GetWindow(view, proj, _filter.sharedMesh.bounds, transform.localToWorldMatrix);

            return true;
        }

        private bool IsOnFrontSide(Vector3 position)
        {
            return Vector3.Dot(position - _clippingPlane.position, _clippingPlane.forward) > 0f;
        }

        /// <inheritdoc/>
        public override void PreCull(PortalRenderNode renderNode)
        {
            preCull?.Invoke(renderNode);
        }

        /// <inheritdoc/>
        public override void PostCull(PortalRenderNode renderNode)
        {
            postCull?.Invoke(renderNode);
        }

        /// <inheritdoc/>
        public override void Render(PortalRenderNode renderNode, CommandBuffer commandBuffer, Material material, MaterialPropertyBlock properties = null)
        {
            if (isActiveAndEnabled)
            {
                commandBuffer.SetGlobalInt(PropertyID.PortalCullMode, (int)_cullMode);
                Matrix4x4 localToWorld = transform.localToWorldMatrix;

                // TODO: flip if required

                if (filter && _filter.sharedMesh)
                    for (int i = 0; i < _filter.sharedMesh.subMeshCount; i++)
                        commandBuffer.DrawMesh(_filter.sharedMesh, localToWorld, material, i, -1, properties);
            }
        }

        /// <inheritdoc/>
        public override void RenderDefault(PortalRenderNode renderNode, CommandBuffer commandBuffer)
        {
            if (isActiveAndEnabled && defaultMaterial)
                Render(renderNode, commandBuffer, defaultMaterial);
        }

        /// <inheritdoc/>
        public override void PostRender(PortalRenderNode renderNode)
        {
            postRender?.Invoke(renderNode);
        }

        /// <inheritdoc/>
        public override bool TryGetClippingPlane(PortalRenderNode renderNode, out Vector3 clippingPlaneCentre, out Vector3 clippingPlaneNormal)
        {
            if (_clippingPlane)
            {
                if (_clippingMode == ClippingMode.DoubleSided && !IsOnFrontSide(renderNode.localToWorldMatrix.GetColumn(3)))
                    clippingPlaneNormal = -_clippingPlane.forward;
                else
                    clippingPlaneNormal = _clippingPlane.forward;

                clippingPlaneCentre = _clippingPlane.transform.position + clippingPlaneNormal * _clippingOffset;
                return true;
            }

            clippingPlaneCentre = default;
            clippingPlaneNormal = default;
            return false;
        }
    }
}