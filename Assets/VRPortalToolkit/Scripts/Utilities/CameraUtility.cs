using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using VRPortalToolkit.Portables;
using VRPortalToolkit.Rendering.Universal;

namespace VRPortalToolkit.Utilities
{
    /// <summary>
    /// Provides utility methods for camera-related operations in portal rendering.
    /// </summary>
    public static class CameraUtility
    {
        /// <summary>
        /// Determines if a renderer is visible from a camera.
        /// </summary>
        /// <param name="renderer">The renderer to check visibility for.</param>
        /// <param name="camera">The camera to check visibility from.</param>
        /// <returns>True if the renderer is visible from the camera.</returns>
        public static bool VisibleFromCamera(this Renderer renderer, Camera camera)
            => renderer.VisibleFromCameraPlanes(GeometryUtility.CalculateFrustumPlanes(camera));

        /// <summary>
        /// Determines if a renderer is visible from a set of camera planes.
        /// </summary>
        /// <param name="renderer">The renderer to check visibility for.</param>
        /// <param name="planes">The planes to check visibility against.</param>
        /// <returns>True if the renderer is visible from the planes.</returns>
        public static bool VisibleFromCameraPlanes(this Renderer renderer, Plane[] planes)
            => GeometryUtility.TestPlanesAABB(planes, renderer.bounds);

        /// <summary>
        /// Determines if bounds are visible from a camera.
        /// </summary>
        /// <param name="bounds">The bounds to check visibility for.</param>
        /// <param name="camera">The camera to check visibility from.</param>
        /// <returns>True if the bounds are visible from the camera.</returns>
        public static bool VisibleFromCamera(this Bounds bounds, Camera camera)
            => bounds.VisibleFromCameraPlanes(GeometryUtility.CalculateFrustumPlanes(camera));

        /// <summary>
        /// Determines if bounds are visible from a set of camera planes.
        /// </summary>
        /// <param name="bounds">The bounds to check visibility for.</param>
        /// <param name="planes">The planes to check visibility against.</param>
        /// <returns>True if the bounds are visible from the planes.</returns>
        public static bool VisibleFromCameraPlanes(this Bounds bounds, Plane[] planes)
            => GeometryUtility.TestPlanesAABB(planes, bounds);

        /// <summary>
        /// Gets the stereo offset for a specified eye.
        /// </summary>
        /// <param name="camera">The camera to get the stereo offset for.</param>
        /// <param name="eye">The eye to get the stereo offset for.</param>
        /// <returns>The stereo offset vector.</returns>
        public static Vector3 GetStereoOffset(this Camera camera, Camera.MonoOrStereoscopicEye eye)
        {
            Vector3 offset = camera.transform.InverseTransformPoint(camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f), eye));

            // TODO: Shouldn't need to fix this
            if (eye == Camera.MonoOrStereoscopicEye.Right)
            { if (offset.x < 0) offset.x = -offset.x; }
            else if (offset.x > 0) offset.x = -offset.x;

            return offset;
        }

        /// <summary>
        /// Gets a fixed stereo projection matrix for a specified eye.
        /// </summary>
        /// <param name="camera">The camera to get the projection matrix for.</param>
        /// <param name="eye">The eye to get the projection matrix for.</param>
        /// <returns>The fixed stereo projection matrix.</returns>
        public static Matrix4x4 GetStereoProjectionMatrixFixed(this Camera camera, Camera.StereoscopicEye eye)
        {
            Matrix4x4 projectionMatrix = camera.GetStereoProjectionMatrix(eye);

            if (eye == Camera.StereoscopicEye.Right)
            { if (projectionMatrix.m02 < 0) projectionMatrix.m02 = -projectionMatrix.m02; }
            else if (projectionMatrix.m02 > 0) projectionMatrix.m02 = -projectionMatrix.m02;

            return projectionMatrix;
        }

        /// <summary>
        /// Gets the stereo camera properties for a specified eye.
        /// </summary>
        /// <param name="camera">The camera to get the stereo properties for.</param>
        /// <param name="eye">The eye to get the stereo properties for.</param>
        /// <param name="offset">Output parameter for the stereo offset.</param>
        /// <param name="projectionMatrix">Output parameter for the projection matrix.</param>
        public static void GetStereoCamera(this Camera camera, Camera.MonoOrStereoscopicEye eye, out Vector3 offset, out Matrix4x4 projectionMatrix)
        {
            switch (eye)
            {
                case Camera.MonoOrStereoscopicEye.Left:
                    offset = camera.GetStereoOffset(eye);
                    projectionMatrix = camera.GetStereoProjectionMatrixFixed(Camera.StereoscopicEye.Left);
                    break;

                case Camera.MonoOrStereoscopicEye.Right:
                    offset = camera.GetStereoOffset(eye);
                    projectionMatrix = camera.GetStereoProjectionMatrixFixed(Camera.StereoscopicEye.Right);
                    break;

                default:
                    offset = Vector3.zero;
                    projectionMatrix = camera.projectionMatrix;
                    break;
            }
        }

        /// <summary>
        /// Calculates an oblique projection matrix based on a clipping plane.
        /// </summary>
        /// <param name="view">The view matrix.</param>
        /// <param name="proj">The projection matrix.</param>
        /// <param name="clippingPlaneCentre">The center of the clipping plane in world space.</param>
        /// <param name="clippingPlaneNormal">The normal of the clipping plane in world space.</param>
        /// <returns>The oblique projection matrix.</returns>
        public static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 view, Matrix4x4 proj, Vector3 clippingPlaneCentre, Vector3 clippingPlaneNormal)
        {
            Plane clippingPlane = new Plane(-clippingPlaneNormal, clippingPlaneCentre);
            Vector4 clipPlane = new Vector4(clippingPlane.normal.x, clippingPlane.normal.y, clippingPlane.normal.z, clippingPlane.distance);
            Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(view)) * clipPlane;

            Matrix4x4 obliqueProj = CalculateObliqueMatrix(proj, clipPlaneCameraSpace);

            if (obliqueProj[14] <= -0.001f)
                return obliqueProj;

            return proj;
        }

        private static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 projection, Vector4 clipPlane)
        {
            Vector4 q = projection.inverse * new Vector4(Mathf.Sign(clipPlane.x), Mathf.Sign(clipPlane.y), 1.0f, 1.0f);
            Vector4 c = clipPlane * (2.0F / (Vector4.Dot(clipPlane, q)));

            projection[2] = c.x - projection[3];
            projection[6] = c.y - projection[7];
            projection[10] = c.z - projection[11];
            projection[14] = c.w - projection[15];

            return projection;
        }

        /// <summary>
        /// Determines if a plane intersects with the camera view.
        /// </summary>
        /// <param name="camera">The camera to check intersection with.</param>
        /// <param name="centre">The center of the plane.</param>
        /// <param name="normal">The normal of the plane.</param>
        /// <param name="viewPosition">Output parameter for the intersection position in viewport space.</param>
        /// <param name="viewDirecion">Output parameter for the intersection direction in viewport space.</param>
        /// <param name="eye">The eye to use for stereo cameras.</param>
        /// <returns>True if the plane intersects with the camera view.</returns>
        public static bool PlaneIntersection(this Camera camera, Vector3 centre, Vector3 normal, out Vector2 viewPosition, out Vector2 viewDirecion, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono)
        {
            if (camera)
            {
                if (PlaneIntersection(camera.transform.position + camera.transform.forward * camera.nearClipPlane, camera.transform.forward, centre, normal, out Vector3 position, out Vector3 direction))
                {
                    viewPosition = camera.WorldToViewportPoint(position, eye);
                    viewDirecion = (Vector2)camera.WorldToViewportPoint(position + direction, eye) - viewPosition;

                    if (viewDirecion != Vector2.zero)
                    {
                        viewDirecion = viewDirecion.normalized;
                        return true;
                    }
                }
            }

            viewPosition = new Vector2(0.5f, 0.5f);
            viewDirecion = Vector2.right;
            return false;
        }

        /// <summary>
        /// Determines the intersection line between two planes.
        /// </summary>
        /// <param name="planeA">The first plane.</param>
        /// <param name="planeB">The second plane.</param>
        /// <param name="linePoint">Output parameter for a point on the intersection line.</param>
        /// <param name="lineDirection">Output parameter for the direction of the intersection line.</param>
        /// <returns>True if the planes intersect.</returns>
        public static bool PlaneIntersection(Plane planeA, Plane planeB, out Vector3 linePoint, out Vector3 lineDirection) =>
            PlaneIntersection(planeA.distance * planeA.normal, planeA.normal, planeB.distance * planeB.normal, planeB.normal, out linePoint, out lineDirection);

        /// <summary>
        /// Determines the intersection line between two planes defined by centers and normals.
        /// </summary>
        /// <param name="centreA">The center of the first plane.</param>
        /// <param name="normalA">The normal of the first plane.</param>
        /// <param name="centreB">The center of the second plane.</param>
        /// <param name="normalB">The normal of the second plane.</param>
        /// <param name="linePoint">Output parameter for a point on the intersection line.</param>
        /// <param name="lineDirection">Output parameter for the direction of the intersection line.</param>
        /// <returns>True if the planes intersect.</returns>
        public static bool PlaneIntersection(Vector3 centreA, Vector3 normalA, Vector3 centreB, Vector3 normalB, out Vector3 linePoint, out Vector3 lineDirection)
        {
            lineDirection = Vector3.Cross(normalA, normalB);

            if (lineDirection.magnitude < Mathf.Epsilon)
            {
                lineDirection = default;
                linePoint = default;
                return false;
            }

            linePoint = Vector3.zero;
            float d1 = -Vector3.Dot(normalA, centreA);
            float d2 = -Vector3.Dot(normalB, centreB);
            float det = 1.0f / Vector3.Dot(lineDirection, lineDirection);
            linePoint = (Vector3.Cross(normalB, lineDirection) * d1 +
                         Vector3.Cross(lineDirection, normalA) * d2) * det;

            return true;
        }

        /// <summary>
        /// Calculates a scissor matrix for a projection matrix and rectangle.
        /// </summary>
        /// <param name="proj">The projection matrix.</param>
        /// <param name="rect">The rectangle to use for scissoring.</param>
        /// <returns>The scissor matrix.</returns>
        public static Matrix4x4 CalculateScissorMatrix(this Matrix4x4 proj, Rect rect)
        {
            if (rect.x < 0)
            {
                rect.width += rect.x;
                rect.x = 0;
            }

            if (rect.y < 0)
            {
                rect.height += rect.y;
                rect.y = 0;
            }

            rect.width = Mathf.Min(1 - rect.x, rect.width);
            rect.height = Mathf.Min(1 - rect.y, rect.height);

            Matrix4x4 m1 = Matrix4x4.TRS(new Vector3((1 / rect.width - 1), (1 / rect.height - 1), 0), Quaternion.identity, new Vector3(1 / rect.width, 1 / rect.height, 1)),
                m2 = Matrix4x4.TRS(new Vector3(-rect.x * 2 / rect.width, -rect.y * 2 / rect.height, 0), Quaternion.identity, Vector3.one);

            return m2 * m1 * proj;
        }

        /// <summary>
        /// Calculates a scissor matrix for a camera and rectangle.
        /// </summary>
        /// <param name="camera">The camera to calculate the scissor matrix for.</param>
        /// <param name="rect">The rectangle to use for scissoring.</param>
        /// <returns>The scissor matrix.</returns>
        public static Matrix4x4 CalculateScissorMatrix(this Camera camera, Rect rect)
        {
            Matrix4x4 m1 = Matrix4x4.TRS(new Vector3((1 / rect.width - 1), (1 / rect.height - 1), 0), Quaternion.identity, new Vector3(1 / rect.width, 1 / rect.height, 1)),
                m2 = Matrix4x4.TRS(new Vector3(-rect.x * 2 / rect.width, -rect.y * 2 / rect.height, 0), Quaternion.identity, Vector3.one);

            return m2 * m1 * camera.projectionMatrix;
        }

        /// <summary>
        /// Converts a world point to viewport space using view and projection matrices.
        /// </summary>
        /// <param name="view">The view matrix.</param>
        /// <param name="proj">The projection matrix.</param>
        /// <param name="point">The world point to convert.</param>
        /// <returns>The point in viewport space.</returns>
        public static Vector3 WorldToViewportPoint(in Matrix4x4 view, in Matrix4x4 proj, Vector3 point)
        {
            Matrix4x4 VP = proj * view;

            Vector4 point4 = new Vector4(point.x, point.y, point.z, 1.0f);
            Vector4 result4 = VP * point4;

            Vector3 result = result4;

            result /= -result4.w;

            result.x = -result.x / 2 + 0.5f;
            result.y = -result.y / 2 + 0.5f;

            result.z = result4.w;

            return result;
        }

        /// <summary>
        /// Converts a viewport point to world space using view and projection matrices.
        /// </summary>
        /// <param name="view">The view matrix.</param>
        /// <param name="proj">The projection matrix.</param>
        /// <param name="point">The viewport point to convert.</param>
        /// <returns>The point in world space.</returns>
        public static Vector3 ViewportToWorldPoint(Matrix4x4 view, Matrix4x4 proj, Vector3 point)
        {
            Vector4 clipPos = new Vector4(point.x * 2 - 1, point.y * 2 - 1, point.z, 1f);

            Vector4 worldPos = proj.inverse * clipPos;

            worldPos = view.inverse.MultiplyPoint(worldPos);

            return worldPos;
        }

        /// <summary>
        /// Sets stereo view and projection matrices in a command buffer.
        /// </summary>
        /// <param name="commandBuffer">The command buffer to set the matrices in.</param>
        /// <param name="leftView">The left eye view matrix.</param>
        /// <param name="leftProj">The left eye projection matrix.</param>
        /// <param name="rightView">The right eye view matrix.</param>
        /// <param name="rightProj">The right eye projection matrix.</param>
        public static void SetStereoViewProjectionMatrices(this CommandBuffer commandBuffer, Matrix4x4 leftView, Matrix4x4 leftProj, Matrix4x4 rightView, Matrix4x4 rightProj)
        {
            if (stereoConstraints == null) stereoConstraints = new StereoConstants();

            stereoConstraints.viewMatrix[0] = leftView;
            stereoConstraints.projMatrix[0] = leftProj;
            stereoConstraints.viewMatrix[1] = rightView;
            stereoConstraints.projMatrix[1] = rightProj;

            for (int i = 0; i < 2; i++)
            {
                stereoConstraints.gpuProjectionMatrix[i] = GL.GetGPUProjectionMatrix(stereoConstraints.projMatrix[i], true);
                stereoConstraints.viewProjMatrix[i] = stereoConstraints.gpuProjectionMatrix[i] * stereoConstraints.viewMatrix[i];
                stereoConstraints.invViewMatrix[i] = Matrix4x4.Inverse(stereoConstraints.viewMatrix[i]);
                stereoConstraints.invGpuProjMatrix[i] = Matrix4x4.Inverse(stereoConstraints.gpuProjectionMatrix[i]);
                stereoConstraints.invViewProjMatrix[i] = Matrix4x4.Inverse(stereoConstraints.viewProjMatrix[i]);
                stereoConstraints.invProjMatrix[i] = Matrix4x4.Inverse(stereoConstraints.projMatrix[i]);
                stereoConstraints.worldSpaceCameraPos[i] = stereoConstraints.invViewMatrix[i].GetColumn(3);
            }

            commandBuffer.SetGlobalMatrixArray(UNITY_STEREO_MATRIX_V, stereoConstraints.viewMatrix);
            commandBuffer.SetGlobalMatrixArray(UNITY_STEREO_MATRIX_P, stereoConstraints.gpuProjectionMatrix);
            commandBuffer.SetGlobalMatrixArray(UNITY_STEREO_MATRIX_VP, stereoConstraints.viewProjMatrix);

            commandBuffer.SetGlobalMatrixArray(UNITY_STEREO_CAMERA_PROJECTION, stereoConstraints.projMatrix);

            commandBuffer.SetGlobalMatrixArray(UNITY_STEREO_MATRIX_IV, stereoConstraints.invViewMatrix);
            commandBuffer.SetGlobalMatrixArray(UNITY_STEREO_MATRIX_IP, stereoConstraints.invGpuProjMatrix);
            commandBuffer.SetGlobalMatrixArray(UNITY_STEREO_MATRIX_IVP, stereoConstraints.invViewProjMatrix);

            commandBuffer.SetGlobalMatrixArray(UNITY_STEREO_CAMERA_INV_PROJECTION, stereoConstraints.invProjMatrix);

            commandBuffer.SetGlobalVectorArray(UNITY_STEREO_VECTOR_CAMPOS, stereoConstraints.worldSpaceCameraPos);
        }

        /// <summary>
        /// Enables single-pass stereo rendering in a command buffer.
        /// </summary>
        /// <param name="cmd">The command buffer to enable single-pass in.</param>
        public static void StartSinglePass(CommandBuffer cmd)
        {
            if (SystemInfo.supportsMultiview)
            {
                cmd.EnableShaderKeyword("STEREO_MULTIVIEW_ON");
                cmd.SetGlobalVectorArray("unity_StereoEyeIndices", stereoEyeIndices);
            }
            else
            {
                cmd.EnableShaderKeyword("STEREO_INSTANCING_ON");
                cmd.SetInstanceMultiplier(2);
            }
        }

        /// <summary>
        /// Disables single-pass stereo rendering in a command buffer.
        /// </summary>
        /// <param name="cmd">The command buffer to disable single-pass in.</param>
        public static void StopSinglePass(CommandBuffer cmd)
        {
            if (SystemInfo.supportsMultiview)
            {
                cmd.DisableShaderKeyword("STEREO_MULTIVIEW_ON");
            }
            else
            {
                cmd.DisableShaderKeyword("STEREO_INSTANCING_ON");
                cmd.SetInstanceMultiplier(1);
            }
        }

        /// <summary>
        /// Shader property IDs for stereo matrices and camera properties.
        /// </summary>
        public static readonly int UNITY_STEREO_MATRIX_V = Shader.PropertyToID("unity_StereoMatrixV");
        public static readonly int UNITY_STEREO_MATRIX_IV = Shader.PropertyToID("unity_StereoMatrixInvV");
        public static readonly int UNITY_STEREO_MATRIX_P = Shader.PropertyToID("unity_StereoMatrixP");
        public static readonly int UNITY_STEREO_MATRIX_IP = Shader.PropertyToID("unity_StereoMatrixInvP");
        public static readonly int UNITY_STEREO_MATRIX_VP = Shader.PropertyToID("unity_StereoMatrixVP");
        public static readonly int UNITY_STEREO_MATRIX_IVP = Shader.PropertyToID("unity_StereoMatrixInvVP");
        public static readonly int UNITY_STEREO_CAMERA_PROJECTION = Shader.PropertyToID("unity_StereoCameraProjection");
        public static readonly int UNITY_STEREO_CAMERA_INV_PROJECTION = Shader.PropertyToID("unity_StereoCameraInvProjection");
        public static readonly int UNITY_STEREO_VECTOR_CAMPOS = Shader.PropertyToID("unity_StereoWorldSpaceCameraPos");

        private static StereoConstants stereoConstraints;

        private class StereoConstants
        {
            public readonly Matrix4x4[] viewMatrix = new Matrix4x4[2];
            public readonly Matrix4x4[] gpuProjectionMatrix = new Matrix4x4[2];
            public readonly Matrix4x4[] projMatrix = new Matrix4x4[2];
            public readonly Matrix4x4[] viewProjMatrix = new Matrix4x4[2];
            public readonly Matrix4x4[] invViewMatrix = new Matrix4x4[2];
            public readonly Matrix4x4[] invGpuProjMatrix = new Matrix4x4[2];
            public readonly Matrix4x4[] invViewProjMatrix = new Matrix4x4[2];
            public readonly Matrix4x4[] invProjMatrix = new Matrix4x4[2];
            public readonly Vector4[] worldSpaceCameraPos = new Vector4[2];
        };

        private static Vector4[] stereoEyeIndices = new Vector4[2] { Vector4.zero, Vector4.one };
    }
}