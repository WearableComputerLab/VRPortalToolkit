using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using VRPortalToolkit.Portables;
using VRPortalToolkit.Rendering.Universal;

namespace VRPortalToolkit.Utilities
{
    public static class CameraUtility
    {

        // http://wiki.unity3d.com/index.php/IsVisibleFrom
        public static bool VisibleFromCamera(this Renderer renderer, Camera camera)
            => renderer.VisibleFromCameraPlanes(GeometryUtility.CalculateFrustumPlanes(camera));

        public static bool VisibleFromCameraPlanes(this Renderer renderer, Plane[] planes)
            => GeometryUtility.TestPlanesAABB(planes, renderer.bounds);

        public static bool VisibleFromCamera(this Bounds bounds, Camera camera)
            => bounds.VisibleFromCameraPlanes(GeometryUtility.CalculateFrustumPlanes(camera));

        public static bool VisibleFromCameraPlanes(this Bounds bounds, Plane[] planes)
            => GeometryUtility.TestPlanesAABB(planes, bounds);

        public static Vector3 GetStereoOffset(this Camera camera, Camera.MonoOrStereoscopicEye eye)
        {
            Vector3 offset = camera.transform.InverseTransformPoint(camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f), eye));

            // TODO: Shouldn't need to fix this
            if (eye == Camera.MonoOrStereoscopicEye.Right)
            { if (offset.x < 0) offset.x = -offset.x; }
            else if (offset.x > 0) offset.x = -offset.x;

            return offset;
        }

        // TODO: Shouldn't need to fix this
        public static Matrix4x4 GetStereoProjectionMatrixFixed(this Camera camera, Camera.StereoscopicEye eye)
        {
            Matrix4x4 projectionMatrix = camera.GetStereoProjectionMatrix(eye);

            if (eye == Camera.StereoscopicEye.Right)
            { if (projectionMatrix.m02 < 0) projectionMatrix.m02 = -projectionMatrix.m02; }
            else if (projectionMatrix.m02 > 0) projectionMatrix.m02 = -projectionMatrix.m02;

            return projectionMatrix;
        }

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

        public static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 view, Matrix4x4 proj, Vector3 clippingPlaneCentre, Vector3 clippingPlaneNormal)
        {
            Plane clippingPlane = new Plane(-clippingPlaneNormal, clippingPlaneCentre);
            Vector4 clipPlane = new Vector4(clippingPlane.normal.x, clippingPlane.normal.y, clippingPlane.normal.z, clippingPlane.distance);
            Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(view)) * clipPlane;

            // Old
            // From: http://www.terathon.com/lengyel/Lengyel-Oblique.pdf
            //float dot = Mathf.Sign(Vector3.Dot(clippingPlaneNormal, clippingPlaneCentre - view.inverse.MultiplyPoint(Vector3.zero)));

            //Vector3 camSpacePos = view.MultiplyPoint(clippingPlaneCentre);
            //Vector3 camSpaceNormal = (view.MultiplyVector(clippingPlaneNormal) * dot);

            //float camSpaceDst = -Vector3.Dot(camSpacePos, camSpaceNormal); // TODO: + nearClipOffset;

            // Don't use oblique clip plane if very close to portal as it seems this can cause some visual artifacts
            //if (Mathf.Abs(camSpaceDst) > nearClipLimit)
            //{
            //Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);
            //CalculateObliqueMatrix(ref proj, clipPlaneCameraSpace);
            //return proj;
            //}*/

            Matrix4x4 obliqueProj = CalculateObliqueMatrix(proj, clipPlaneCameraSpace);

            if (obliqueProj[14] <= -0.001f)
                return obliqueProj;
            // For whatever reason, sometimes this comes back with a positive number, which flips the depth, so we have to fix it
            //proj[14] = Mathf.Min(-0.001f, proj[14]);

            return proj;
        }

        private static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 projection, Vector4 clipPlane)
        {
            // From: https://forum.unity.com/threads/problem-camera-calculateobliquematrix.252916/
            Vector4 q = projection.inverse * new Vector4(Mathf.Sign(clipPlane.x), Mathf.Sign(clipPlane.y), 1.0f, 1.0f);
            Vector4 c = clipPlane * (2.0F / (Vector4.Dot(clipPlane, q)));

            // third row = clip plane - fourth row
            projection[2] = c.x - projection[3];
            projection[6] = c.y - projection[7];
            projection[10] = c.z - projection[11];
            projection[14] = c.w - projection[15];

            return projection;
        }

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

        public static bool PlaneIntersection(Plane planeA, Plane planeB, out Vector3 linePoint, out Vector3 lineDirection) =>
            PlaneIntersection(planeA.distance * planeA.normal, planeA.normal, planeB.distance * planeB.normal, planeB.normal, out linePoint, out lineDirection);

        public static bool PlaneIntersection(Vector3 centreA, Vector3 normalA, Vector3 centreB, Vector3 normalB, out Vector3 linePoint, out Vector3 lineDirection)
        {
            lineDirection = Vector3.Cross(normalA, normalB);

            // Check if the planes are parallel or coincident
            if (lineDirection.magnitude < Mathf.Epsilon)
            {
                lineDirection = default;
                linePoint = default;
                return false;
            }

            // Calculate a point on the intersection line
            linePoint = Vector3.zero;
            float d1 = -Vector3.Dot(normalA, centreA);
            float d2 = -Vector3.Dot(normalB, centreB);
            float det = 1.0f / Vector3.Dot(lineDirection, lineDirection);
            linePoint = (Vector3.Cross(normalB, lineDirection) * d1 +
                         Vector3.Cross(lineDirection, normalA) * d2) * det;

            return true;
            /*lineDirection = Vector3.Cross(normalA, normalB);

            Vector3 ldir = Vector3.Cross(normalB, lineDirection);

            float numerator = Vector3.Dot(normalA, ldir);

            //Prevent divide by zero.
            if (Mathf.Abs(numerator) > float.Epsilon)
            {
                float t = Vector3.Dot(normalA, centreA - centreB) / numerator;
                linePoint = centreB + t * ldir;

                return true;
            }

            linePoint = Vector3.zero;
            lineDirection = Vector3.forward;
            return false;*/
        }

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

        public static Matrix4x4 CalculateScissorMatrix(this Camera camera, Rect rect)
        {
            /*if (rect.x < 0)
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
            rect.height = Mathf.Min(1 - rect.y, rect.height);*/

            Matrix4x4 m1 = Matrix4x4.TRS(new Vector3((1 / rect.width - 1), (1 / rect.height - 1), 0), Quaternion.identity, new Vector3(1 / rect.width, 1 / rect.height, 1)),
                m2 = Matrix4x4.TRS(new Vector3(-rect.x * 2 / rect.width, -rect.y * 2 / rect.height, 0), Quaternion.identity, Vector3.one);

            return m2 * m1 * camera.projectionMatrix;
        }

        public static Vector3 WorldToViewportPoint(in Matrix4x4 view, in Matrix4x4 proj, Vector3 point)
        {
            Matrix4x4 VP = proj * view;

            Vector4 point4 = new Vector4(point.x, point.y, point.z, 1.0f);  // turn into (x,y,z,1)
            Vector4 result4 = VP * point4;  // multiply 4 components

            Vector3 result = result4;  // store 3 components of the resulting 4 components

            // normalize by "-w"
            result /= -result4.w;

            // clip space => view space
            result.x = -result.x / 2 + 0.5f;
            result.y = -result.y / 2 + 0.5f;

            // "The z position is in world units from the camera."
            result.z = result4.w;

            return result;
        }
        public static Vector3 ViewportToWorldPoint(Matrix4x4 view, Matrix4x4 proj, Vector3 point)
        {
            // Calculate the homogenous clip-space coordinates
            Vector4 clipPos = new Vector4(point.x * 2 - 1, point.y * 2 - 1, point.z, 1f );

            Vector4 worldPos = proj.inverse * clipPos;

            worldPos = view.inverse.MultiplyPoint(worldPos);

            return worldPos;
        }

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

        public static void SetStereoViewProjectionMatrices(this CommandBuffer commandBuffer, Matrix4x4 leftView, Matrix4x4 leftProj, Matrix4x4 rightView, Matrix4x4 rightProj)
        {
            if (stereoConstraints == null) stereoConstraints = new StereoConstants();

            stereoConstraints.viewMatrix[0] = leftView;
            stereoConstraints.projMatrix[0] = leftProj;
            stereoConstraints.viewMatrix[1] = rightView;
            stereoConstraints.projMatrix[1] = rightProj;

            for (int i = 0; i < 2; i++)
            {
                stereoConstraints.gpuProjectionMatrix[i] = GL.GetGPUProjectionMatrix(stereoConstraints.projMatrix[i], true); // TODO: Need to figure out why this should be true
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

        private static Vector4[] stereoEyeIndices = new Vector4[2] { Vector4.zero, Vector4.one };

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
                cmd.SetInstanceMultiplier(2); // TODO: Technically this could be a number larger than 2, but dont have access to that number
            }
        }

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
    }
}