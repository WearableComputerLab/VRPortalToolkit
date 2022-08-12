using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using VRPortalToolkit.Data;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering
{
    [RequireComponent(typeof(MeshFilter)), ExecuteInEditMode]
    public class PortalMeshRenderer : PortalRenderer
    {
        [SerializeField] private PortalMeshRenderer _connectedRenderer;
        /// <summary>The portal this is required to render.</summary>
        public PortalMeshRenderer connectedRenderer {
            get => _connectedRenderer;
            set => _connectedRenderer = value;
        }

        private MeshFilter _filter;
        /// <summary>The filter for the mesh.</summary>
        public MeshFilter filter => _filter ? _filter : _filter = GetComponent<MeshFilter>();

        [SerializeField] private OverrideMode _overrideRenderersMode;
        public OverrideMode overrideRenderersMode {
            get => _overrideRenderersMode;
            set => _overrideRenderersMode = value;
        }

        [SerializeField] private List<PortalRenderer> _overrideRenderers;
        public List<PortalRenderer> overrideRenderers {
            get => _overrideRenderers;
            set => _overrideRenderers = value;
        }

        public override IEnumerable<PortalRenderer> visiblePortals {
            get {
                switch (_overrideRenderersMode)
                {
                    case OverrideMode.Ignore:
                        foreach (PortalRenderer renderer in allRenderers)
                        {
                            if (renderer != _connectedRenderer && !_overrideRenderers.Contains(renderer))
                                yield return renderer;
                        }
                        break;

                    case OverrideMode.Replace:
                        foreach (PortalRenderer renderer in _overrideRenderers)
                        {
                            if (renderer != _connectedRenderer)
                                yield return renderer;
                        }
                        break;

                    default:
                        foreach (PortalRenderer renderer in allRenderers)
                        {
                            if (renderer != _connectedRenderer)
                                yield return renderer;
                        }
                        break;
                }
            }
        }

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

        [Header("Render Events")]
        public UnityEvent<Camera, PortalRenderNode> preCull;
        public UnityEvent<Camera, PortalRenderNode> postCull;
        public UnityEvent<Camera, PortalRenderNode> postRender;

        //protected MaterialPropertyBlock properties;

        /*protected static UnityEngine.Pool.ObjectPool<Mesh> meshPool = new UnityEngine.Pool.ObjectPool<Mesh>(
            () =>
            {
                Mesh mesh = new Mesh();
                mesh.vertices = screenvertices;
                mesh.triangles = new int[6] { 0, 1, 2, 0, 2, 3 };
                mesh.MarkDynamic();
                return mesh;
            });

        protected static Vector3[] screenvertices { get; } = new Vector3[4];

        protected List<Mesh> activeMeshes = new List<Mesh>(1);*/

        public virtual void OnDrawGizmos()
        {
            if (filter && _filter.sharedMesh)
            {
                Gizmos.matrix = transform.localToWorldMatrix;

                // Should be able to click on portals now
                if (portal && portal.connectedPortal)
                    Gizmos.color = Color.clear;
                else
                    Gizmos.color = Color.grey;

                Gizmos.DrawMesh(_filter.sharedMesh);
            }
        }

        public override bool TryGetWindow(Camera camera, out ViewWindow innerWindow) => TryGetWindow(camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, out innerWindow);

        public override bool TryGetWindow(Matrix4x4 localToWorld, Matrix4x4 view, Matrix4x4 proj, out ViewWindow innerWindow)
        {
            if (!isActiveAndEnabled || !filter || !_filter.sharedMesh
                || (facingDirection && Vector3.Dot((Vector3)localToWorld.GetColumn(3) - facingDirection.position, facingDirection.forward) < 0f))
            {
                innerWindow = default(ViewWindow);
                return false;
            }

            innerWindow = ViewWindow.GetWindow(view, proj, _filter.sharedMesh.bounds, transform.localToWorldMatrix);
            return true;
        }

        public override void PreCull(Camera camera, PortalRenderNode renderNode)
        {
            if (preCull != null) preCull.Invoke(camera, renderNode);
        }

        public override void PostCull(Camera camera, PortalRenderNode renderNode)
        {
            if (preCull != null) postCull.Invoke(camera, renderNode);
        }

        public override void Render(Camera camera, PortalRenderNode renderNode, Material material, MaterialPropertyBlock properties = null)
        {
            if (isActiveAndEnabled)
            {
                Matrix4x4 localToWorld = transform.localToWorldMatrix;

                if (filter && _filter.sharedMesh)
                    for (int i = 0; i < _filter.sharedMesh.subMeshCount; i++)
                        Graphics.DrawMesh(_filter.sharedMesh, localToWorld, material, gameObject.layer, camera, i, properties, false, false, false);
            }
        }

        public override void Render(Camera camera, PortalRenderNode renderNode, CommandBuffer commandBuffer, Material material, MaterialPropertyBlock properties = null)
        {
            if (isActiveAndEnabled)
            {
                Matrix4x4 localToWorld = transform.localToWorldMatrix;

                if (filter && _filter.sharedMesh)
                    for (int i = 0; i < _filter.sharedMesh.subMeshCount; i++)
                        commandBuffer.DrawMesh(_filter.sharedMesh, localToWorld, material, i, -1, properties);
            }
        }

        public override void RenderDefault(Camera camera, PortalRenderNode renderNode)
        {
            if (defaultMaterial) Render(camera, renderNode, defaultMaterial);
        }

        public override void RenderDefault(Camera camera, PortalRenderNode renderNode, CommandBuffer commandBuffer)
        {
            if (defaultMaterial) Render(camera, renderNode, commandBuffer, defaultMaterial);
        }

        //protected virtual bool GenerateCameraMesh(Camera camera, PortalRenderNode renderNode, out Mesh mesh)
        //{
        //    float distance = camera.nearClipPlane + 0.01f;

        //    if (PlaneIntersection(CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(0.5f, 0.5f, camera.nearClipPlane)),//camera.transform.position + camera.transform.forward * (camera.nearClipPlane),
        //    camera.transform.forward, transform.position, transform.forward, out Vector3 position, out Vector3 direction))
        //    {
        //        Vector2 viewPosition = CameraUtility.WorldToViewportPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, position),
        //            viewDirecion = (Vector2)CameraUtility.WorldToViewportPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, position + direction) - viewPosition;

        //        if (viewDirecion != Vector2.zero)
        //        {
        //            viewDirecion = viewDirecion.normalized;

        //            // Vertical line
        //            if (viewDirecion.x == 0f)
        //            {
        //                if (viewDirecion.y > 0f)
        //                {
        //                    screenvertices[0] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(viewPosition.x, 0f, distance));
        //                    screenvertices[1] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(viewPosition.x, 1f, distance));
        //                    screenvertices[2] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(1f, 1f, distance));
        //                    screenvertices[3] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(1f, 0f, distance));
        //                }
        //                else
        //                {
        //                    screenvertices[0] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(0f, 0f, distance));
        //                    screenvertices[1] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(0f, 1f, distance));
        //                    screenvertices[2] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(viewPosition.x, 1f, distance));
        //                    screenvertices[3] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(viewPosition.x, 0f, distance));
        //                }
        //            }
        //            else
        //            {
        //                float slope = viewDirecion.y / viewDirecion.x,
        //                    yIntersect0 = -viewPosition.x * slope + viewPosition.y,
        //                    yIntersect1 = (1f - viewPosition.x) * slope + viewPosition.y;

        //                screenvertices[0] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(0f, yIntersect0, distance));

        //                if (viewDirecion.x < 0f)
        //                {
        //                    screenvertices[1] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(0f, Mathf.Max(1f, yIntersect0), distance));
        //                    screenvertices[2] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(1f, Mathf.Max(1f, yIntersect1), distance));
        //                    screenvertices[3] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(1f, yIntersect1, distance));

        //                }
        //                else
        //                {
        //                    screenvertices[1] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(1f, yIntersect1, distance));
        //                    screenvertices[2] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(1f, Mathf.Min(0f, yIntersect1), distance));
        //                    screenvertices[3] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(0f, Mathf.Min(0f, yIntersect0), distance));
        //                }
        //            }

        //            mesh = meshPool.Get();
        //            mesh.vertices = screenvertices;
        //            activeMeshes.Add(mesh);

        //            return true;
        //        }
        //    }

        //    // Must be parallel

        //    if (CameraUtility.WorldToViewportPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, position).z <= camera.nearClipPlane)
        //    {
        //        // Before camera
        //        screenvertices[0] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(0f, 0f, distance));
        //        screenvertices[1] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(0f, 1f, distance));
        //        screenvertices[2] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(1f, 1f, distance));
        //        screenvertices[3] = CameraUtility.ViewportToWorldPoint(renderNode.worldToCameraMatrix, renderNode.projectionMatrix, new Vector3(1f, 0f, distance));

        //        mesh = meshPool.Get();
        //        mesh.vertices = screenvertices;
        //        activeMeshes.Add(mesh);

        //        return true;
        //    }

        //    // After camera
        //    mesh = null;
        //    return false;
        //}

        //private static bool PlaneIntersection(Vector3 centreA, Vector3 normalA, Vector3 centreB, Vector3 normalB, out Vector3 position, out Vector3 direction)
        //{
        //    direction = Vector3.Cross(normalA, normalB);

        //    Vector3 ldir = Vector3.Cross(normalB, direction);

        //    float numerator = Vector3.Dot(normalA, ldir);

        //    //Prevent divide by zero.
        //    if (Mathf.Abs(numerator) > float.Epsilon)
        //    {
        //        float t = Vector3.Dot(normalA, centreA - centreB) / numerator;
        //        position = centreB + t * ldir;

        //        return true;
        //    }

        //    position = Vector3.zero;
        //    direction = Vector3.forward;
        //    return false;
        //}

        public override void PostRender(Camera camera, PortalRenderNode renderNode)
        {
            if (postRender != null) postRender.Invoke(camera, renderNode);
            /*
            while (activeMeshes.Count > 0)
            {
                meshPool.Release(activeMeshes[0]);
                activeMeshes.RemoveAt(0);
            }*/
        }

        public override bool TryGetClippingPlane(Camera camera, PortalRenderNode renderNode, out Vector3 clippingPlaneCentre, out Vector3 clippingPlaneNormal)
            => clippingPlane.TryGetClippingPlane(transform, renderNode.localToWorldMatrix.GetColumn(3), out clippingPlaneCentre, out clippingPlaneNormal);
    }
}