using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VRPortalToolkit.Data;
using VRPortalToolkit.Rendering.Universal;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering
{
    internal class PortalCameraTransitionRenderer : IPortalRenderer
    {
        private static readonly Vector2[] _corners = new Vector2[4] {
            new Vector2(0f,0f), new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(1f,0f)
        };

        private static readonly Vector3[] _vertices = new Vector3[5];

        private static readonly int[] _triangles = new int[] {
            0, 1, 2, 0, 2, 1, 
            0, 2, 3, 0, 3, 2,
            0, 3, 4, 0, 4, 3
        };

        private readonly Mesh[] _meshes = new Mesh[2];

        public Camera camera { get; set; }

        public IPortalCameraTransition transition { get; set; }

        public int Layer => transition.layer;

        public IPortal Portal => transition.portal;

        public PortalRendererSettings Overrides => default;

        public void PreCull(PortalRenderNode renderNode) { }

        public void PostCull(PortalRenderNode renderNode) { }

        public void Render(PortalRenderNode renderNode, CommandBuffer commandBuffer, Material material, MaterialPropertyBlock properties = null)
        {
            if (renderNode.depth > 1 || transition == null) return;

            transition.GetTransitionPlane(out Vector3 centre, out Vector3 normal);

            if (!renderNode.isStereo)
            {
                if (TryGetMesh(centre, normal, renderNode.parent.worldToCameraMatrix, renderNode.parent.projectionMatrix, ref _meshes[0]))
                    commandBuffer.DrawMesh(_meshes[0], Matrix4x4.identity, material, 0, -1, properties);
            }
            else
            {
                if (TryGetMesh(centre, normal, renderNode.parent.GetStereoViewMatrix(0), renderNode.parent.GetStereoProjectionMatrix(0), ref _meshes[0]))
                    commandBuffer.DrawMesh(_meshes[0], Matrix4x4.identity, material, 0, -1, properties);

                if (TryGetMesh(centre, normal, renderNode.parent.GetStereoViewMatrix(1), renderNode.parent.GetStereoProjectionMatrix(1), ref _meshes[1]))
                    commandBuffer.DrawMesh(_meshes[1], Matrix4x4.identity, material, 0, -1, properties);
            }
        }

        public void RenderDefault(PortalRenderNode renderNode, CommandBuffer commandBuffer) { } // Intentionally blank

        public void PostRender(PortalRenderNode renderNode) { }

        public bool TryGetWindow(PortalRenderNode renderNode, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 proj, out ViewWindow innerWindow)
        {
            // We only draw the transition on top of the original render
            if (Portal == null || camera != renderNode.camera || renderNode.depth > 0)
            {
                innerWindow = default;
                return false;
            }

            innerWindow = new ViewWindow(1f, 1f, 0f);
            return true;
        }

        public bool TryGetClippingPlane(PortalRenderNode renderNode, out Vector3 clippingPlaneCentre, out Vector3 clippingPlaneNormal)
        {
            // TODO: This is causing issues?
            /*if (transition != null)
            {
                transition.GetTransitionPlane(out clippingPlaneCentre, out clippingPlaneNormal);
                return true;
            }*/

            clippingPlaneCentre = clippingPlaneNormal = default;
            return false;
        }

        private static bool TryGetMesh(Vector3 transitionCentre, Vector3 transitionNormal, in Matrix4x4 view, in Matrix4x4 projection, ref Mesh mesh)
        {
            // TODO: Should not have to use the rendercamera
            PortalRenderFeature.renderCamera.worldToCameraMatrix = view;
            PortalRenderFeature.renderCamera.projectionMatrix = projection;
            PortalRenderFeature.renderCamera.nearClipPlane = -projection.m23 * 0.5001f;

            if (PlaneIntersection(transitionCentre, transitionNormal, out Vector2 viewPosition, out Vector2 viewDirection))
            {
                int count = 0;

                bool previous = !IsLeft(viewPosition, viewDirection, _corners[3]);

                // TODO: Add intersection
                for (int i = 0; i < 4; i++)
                {
                    Vector2 corner = _corners[i];
                    bool current = !IsLeft(viewPosition, viewDirection, corner);

                    if (previous != current)
                    {
                        if (TryIntersects(_corners[(i + 3) % 4], corner, viewPosition, viewDirection, out Vector2 intersection))
                            _vertices[count++] = PortalRenderFeature.renderCamera.ViewportToWorldPoint(new Vector3(intersection.x, intersection.y, PortalRenderFeature.renderCamera.nearClipPlane));
                    }

                    if (current)
                        _vertices[count++] = PortalRenderFeature.renderCamera.ViewportToWorldPoint(new Vector3(corner.x, corner.y, PortalRenderFeature.renderCamera.nearClipPlane));

                    previous = current;
                }

                if (count > 2)
                {
                    GetMesh(ref mesh, count);
                    return true;
                }
            }
            else
            {
                // If they dont intersect, we may need to draw over the entire view
                Plane portalPlane = new Plane(transitionNormal, transitionCentre);

                if (!portalPlane.GetSide(PortalRenderFeature.renderCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, PortalRenderFeature.renderCamera.nearClipPlane))))
                {
                    for (int i = 0; i < 4; i++)
                        _vertices[i] = PortalRenderFeature.renderCamera.ViewportToWorldPoint(new Vector3(_corners[i].x, _corners[i].y, PortalRenderFeature.renderCamera.nearClipPlane));

                    GetMesh(ref mesh, 4);
                    return true;
                }
            }

            mesh = null;
            return false;

        }

        private static bool TryIntersects(Vector2 lineStart, Vector2 lineEnd, Vector2 rayOrigin, Vector2 rayDirection, out Vector2 intersection)
        {
            //http://stackoverflow.com/questions/3838329/how-can-i-check-if-two-segments-intersect
            // Simplified line intersection

            Vector2 rayEnd = rayOrigin + rayDirection;

            float lineSlope = GetSlope(lineStart, lineEnd);
            float raySlope = GetSlope(rayOrigin, rayOrigin + rayDirection);

            // Parallel
            if (lineSlope == raySlope)
            {
                intersection = default;
                return false;
            }
            else if (lineSlope == float.MaxValue)
            {
                intersection = VerticalIntersection(lineStart.x, rayOrigin, rayEnd);
                return ((lineStart.y > intersection.y) && (intersection.y > lineEnd.y))
                    || ((lineEnd.y > intersection.y) && (intersection.y > lineStart.y));

            }
            else if (raySlope == float.MaxValue)
            {
                intersection = VerticalIntersection(rayOrigin.x, lineStart, lineEnd);
                return ((lineStart.y > intersection.y) && (intersection.y > lineEnd.y))
                    || ((lineEnd.y > intersection.y) && (intersection.y > lineStart.y));
            }

            float lineYIntercept = lineStart.y - (lineSlope * lineStart.x);
            float rayYIntercept = rayOrigin.y - (raySlope * rayOrigin.x);

            float t = (rayYIntercept - lineYIntercept) / (lineSlope - raySlope);

            //Out of bound
            if (t <= Mathf.Min(lineStart.x, lineEnd.x) || (t >= Mathf.Max(lineStart.x, lineEnd.x)))
            {
                intersection = default;
                return false;
            }

            // Not too sure why this part is neccessary, but it is
            if (lineStart.x < lineEnd.x)
                intersection = lineStart + (lineEnd - lineStart) * t;
            else
                intersection = lineEnd + (lineStart - lineEnd) * t;
            return true;
        }

        private static Vector2 VerticalIntersection(float x, Vector2 otherStart, Vector2 otherEnd)
        {
            /* this is vertical */
            float yIntersection = (((otherEnd.y - otherStart.y) * (x - otherStart.x))
                / (otherEnd.x - otherStart.x)) + otherStart.y;

            return new Vector2(x, yIntersection);
        }

        private static float GetSlope(Vector2 start, Vector2 end)
        {
            float dif = start.x - end.x;

            // Avoids dividing by 0
            if (dif != 0) return (start.y - end.y) / dif;

            // Segment is vertical
            return float.MaxValue;
        }

        private static void GetMesh(ref Mesh mesh, int vertexCount)
        {
            if (!mesh) mesh = new Mesh();

            mesh.Clear();
            mesh.SetVertices(_vertices, 0, vertexCount);
            mesh.SetIndices(_triangles, 0, ((vertexCount - 2) * 3) * 2, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
        }

        private static bool IsLeft(Vector2 linePoint, Vector2 lineDirection, Vector2 point)
        {
            return lineDirection.x * (point.y - linePoint.y) - lineDirection.y * (point.x - linePoint.x) > 0;
        }

        private static bool PlaneIntersection(Vector3 portalPosition, Vector3 portalNormal, out Vector2 viewPosition, out Vector2 viewDirecion)
        {
            Vector3 a = PortalRenderFeature.renderCamera.ViewportToWorldPoint(new Vector3(0f, 0f, PortalRenderFeature.renderCamera.nearClipPlane)),
                b = PortalRenderFeature.renderCamera.ViewportToWorldPoint(new Vector3(0f, 1f, PortalRenderFeature.renderCamera.nearClipPlane)),
                c = PortalRenderFeature.renderCamera.ViewportToWorldPoint(new Vector3(1f, 0f, PortalRenderFeature.renderCamera.nearClipPlane));

            Plane cameraPlane = new Plane(a, b, c), portalPlane = new Plane(portalNormal, portalPosition);

            if (CameraUtility.PlaneIntersection(cameraPlane, portalPlane, out Vector3 position, out Vector3 direction))
            {
                viewPosition = PortalRenderFeature.renderCamera.WorldToViewportPoint(position);
                viewDirecion = (Vector2)PortalRenderFeature.renderCamera.WorldToViewportPoint(position + direction) - viewPosition;

                if (viewDirecion != Vector2.zero)
                {
                    viewDirecion = viewDirecion.normalized;
                    return true;
                }
            }

            viewPosition = new Vector2(0.5f, 0.5f);
            viewDirecion = Vector2.right;
            return false;
        }
    }
}
