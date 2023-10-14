using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using VRPortalToolkit.Data;
using VRPortalToolkit.Utilities;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace VRPortalToolkit.Rendering
{
    /*[RequireComponent(typeof(MeshFilter)), ExecuteInEditMode]
    public class PortalMeshRenderer : BasePortalRenderer
    {
        [SerializeField] private PortalMeshRenderer _connectedRenderer;
        public PortalMeshRenderer connectedRenderer {
            get => _connectedRenderer;
            set => _connectedRenderer = value;
        }

        private MeshFilter _filter;
        /// <summary>The filter for the mesh.</summary>
        public MeshFilter filter => _filter ? _filter : _filter = GetComponent<MeshFilter>();

        [SerializeField] private Transform _facingDirection;
        public Transform facingDirection {
            get => _facingDirection;
            set => _facingDirection = value;
        }

        [SerializeField] private ClippingPlane _clippingPlane;
        public ClippingPlane clippingPlane {
            get => _clippingPlane;
            set => _clippingPlane = value;
        }

        [SerializeField] private CullMode _cullMode;
        public CullMode cullMode {
            get => _cullMode;
            set => _cullMode = value;
        }

        [SerializeField] private Material _defaultMaterial;
        public Material defaultMaterial {
            get => _defaultMaterial;
            set => _defaultMaterial = value;
        }

        public UnityAction<PortalRenderNode> preCull;
        public UnityAction<PortalRenderNode> postCull;
        public UnityAction<PortalRenderNode> postRender;

        public virtual void OnDrawGizmos()
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

        public override bool TryGetWindow(PortalRenderNode renderNode, Matrix4x4 localToWorld, Matrix4x4 view, Matrix4x4 proj, out ViewWindow innerWindow)
        {
            if (!isActiveAndEnabled || !filter || !_filter.sharedMesh
                || (facingDirection && Vector3.Dot((Vector3)localToWorld.GetColumn(3) - facingDirection.position, facingDirection.forward) < 0f))
            {
                innerWindow = default;
                return false;
            }

            innerWindow = ViewWindow.GetWindow(view, proj, _filter.sharedMesh.bounds, transform.localToWorldMatrix);
            return true;
        }

        public override void PreCull(PortalRenderNode renderNode)
        {
            preCull?.Invoke(renderNode);
        }

        public override void PostCull(PortalRenderNode renderNode)
        {
            postCull?.Invoke(renderNode);
        }

        public override void Render(PortalRenderNode renderNode, CommandBuffer commandBuffer, Material material, MaterialPropertyBlock properties = null)
        {
            if (isActiveAndEnabled)
            {
                Matrix4x4 localToWorld = transform.localToWorldMatrix;

                if (filter && _filter.sharedMesh)
                    for (int i = 0; i < _filter.sharedMesh.subMeshCount; i++)
                        commandBuffer.DrawMesh(_filter.sharedMesh, localToWorld, material, i, -1, properties);
            }
        }

        public override void RenderDefault(PortalRenderNode renderNode, CommandBuffer commandBuffer)
        {
            if (isActiveAndEnabled && defaultMaterial)
                Render(renderNode, commandBuffer, defaultMaterial);
        }

        public override void PostRender(PortalRenderNode renderNode)
        {
            postRender?.Invoke(renderNode);
        }

        public override bool TryGetClippingPlane(PortalRenderNode renderNode, out Vector3 clippingPlaneCentre, out Vector3 clippingPlaneNormal)
            => clippingPlane.TryGetClippingPlane(transform, renderNode.localToWorldMatrix.GetColumn(3), out clippingPlaneCentre, out clippingPlaneNormal);
    }*/


    [RequireComponent(typeof(MeshFilter)), ExecuteInEditMode]
    public class PortalMeshRenderer : PortalRendererBase
    {
        [SerializeField] private PortalMeshRenderer _connectedRenderer;
        public PortalMeshRenderer connectedRenderer {
            get => _connectedRenderer;
            set => _connectedRenderer = value;
        }

        private MeshFilter _filter;
        /// <summary>The filter for the mesh.</summary>
        public MeshFilter filter => _filter ? _filter : _filter = GetComponent<MeshFilter>();

        [SerializeField] private Transform _clippingPlane;
        public Transform clippingPlane {
            get => _clippingPlane;
            set => _clippingPlane = value;
        }


        [SerializeField] private ClippingMode _clippingMode;
        public ClippingMode clippingMode {
            get => _clippingMode;
            set => _clippingMode = value;
        }

        public enum ClippingMode
        {
            None = 0,
            OneSided = 1,
            DoubleSided = 2,
        }

        [SerializeField] private CullMode _cullMode = CullMode.Back;
        public CullMode cullMode {
            get => _cullMode;
            set => _cullMode = value;
        }

        [SerializeField] private float _clippingOffset = 0.001f;
        public float clippingOffset {
            get => _clippingOffset;
            set => _clippingOffset = value;
        }

        [SerializeField] private Material _defaultMaterial;
        public Material defaultMaterial {
            get => _defaultMaterial;
            set => _defaultMaterial = value;
        }

        public UnityAction<PortalRenderNode> preCull;
        public UnityAction<PortalRenderNode> postCull;
        public UnityAction<PortalRenderNode> postRender;

        protected override void Reset()
        {
            base.Reset();
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

        public override void PreCull(PortalRenderNode renderNode)
        {
            preCull?.Invoke(renderNode);
        }

        public override void PostCull(PortalRenderNode renderNode)
        {
            postCull?.Invoke(renderNode);
        }

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

        public override void RenderDefault(PortalRenderNode renderNode, CommandBuffer commandBuffer)
        {
            if (isActiveAndEnabled && defaultMaterial)
                Render(renderNode, commandBuffer, defaultMaterial);
        }

        public override void PostRender(PortalRenderNode renderNode)
        {
            postRender?.Invoke(renderNode);
        }

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