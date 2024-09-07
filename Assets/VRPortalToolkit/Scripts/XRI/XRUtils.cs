using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using static UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable;

namespace VRPortalToolkit.XRI
{
    public static class XRUtils
    {
        private static object[] _args1 = new object[1];

        private static FieldInfo _targetPoseField;
        private static void UpdateTargetPoseField()
        {
            if (_targetPoseField == null)
            {
                _targetPoseField = typeof(XRGrabInteractable).GetField("m_TargetPose", BindingFlags.NonPublic | BindingFlags.Instance);

                if (_targetPoseField == null)
                    Debug.LogError("\"m_TargetPose\" field could not be found!");
            }
        }
        public static Pose GetTargetPose(XRGrabInteractable interactable)
        {
            UpdateTargetPoseField();

            if (_targetPoseField != null)
                return (Pose)_targetPoseField.GetValue(interactable);

            return default;
        }
        public static void SetTargetPose(XRGrabInteractable interactable, Pose pose)
        {
            UpdateTargetPoseField();

            if (_targetPoseField != null)
                _targetPoseField.SetValue(interactable, pose);
        }

        public static void PerformKinematicUpdate(XRGrabInteractable interactable, Rigidbody rigidbody, Pose targetPose)
        {
            if (rigidbody)
            {
                if (interactable.trackPosition)
                {
                    var position = interactable.attachPointCompatibilityMode == AttachPointCompatibilityMode.Default
                        ? targetPose.position
                        : targetPose.position - rigidbody.worldCenterOfMass + rigidbody.position;
                    rigidbody.MovePosition(position);
                }

                if (interactable.trackRotation)
                    rigidbody.MoveRotation(targetPose.rotation);
            }
        }

        private static MethodInfo _onTeleportedMethod;
        public static void OnTeleported(XRGrabInteractable interactable, Pose pose)
        {
            if (_onTeleportedMethod == null)
            {
                _onTeleportedMethod = typeof(XRGrabInteractable).GetMethod("OnTeleported", BindingFlags.NonPublic | BindingFlags.Instance);

                if (_onTeleportedMethod == null)
                    Debug.LogError("\"OnTeleported\" method could not be found!");
            }

            if (_onTeleportedMethod != null)
            {
                _args1[0] = pose;
                _onTeleportedMethod.Invoke(interactable, _args1);
            }
        }

        #region XRRayInteractor

        private static FieldInfo _raycastHitsField;
        public static RaycastHit[] GetRaycastHits(XRRayInteractor interactor)
        {
            if (_raycastHitsField == null)
            {
                _raycastHitsField = typeof(XRRayInteractor).GetField("m_RaycastHits", BindingFlags.NonPublic | BindingFlags.Instance);

                if (_raycastHitsField == null)
                    Debug.LogError("\"m_RaycastHits\" field could not be found!");
            }

            if (_raycastHitsField != null)
                return (RaycastHit[])_raycastHitsField.GetValue(interactor);

            return default;
        }
        //m_RaycastHitsCount


        private static FieldInfo _raycastHitsCountField;
        private static void UpdateRaycastHitsCountField()
        {
            if (_raycastHitsCountField == null)
            {
                _raycastHitsCountField = typeof(XRRayInteractor).GetField("m_RaycastHitsCount", BindingFlags.NonPublic | BindingFlags.Instance);

                if (_raycastHitsCountField == null)
                    Debug.LogError("\"m_RaycastHitsCount\" field could not be found!");
            }
        }
        public static int GetRaycastHitsCount(XRRayInteractor interactor)
        {
            UpdateRaycastHitsCountField();

            if (_raycastHitsCountField != null)
                return (int)_raycastHitsCountField.GetValue(interactor);

            return -1;
        }
        public static void SetRaycastHitsCount(XRRayInteractor interactor, int count)
        {
            UpdateRaycastHitsCountField();

            if (_raycastHitsCountField != null)
                _raycastHitsCountField.SetValue(interactor, count);
        }
        #endregion

        private static MethodInfo _getSmoothedVelocityValueMethod;
        private static void UpdateGetSmoothedVelocityValueMethod()
        {
            if (_getSmoothedVelocityValueMethod == null)
            {
                _getSmoothedVelocityValueMethod = typeof(XRGrabInteractable).GetMethod("GetSmoothedVelocityValue", BindingFlags.NonPublic | BindingFlags.Instance);

                if (_getSmoothedVelocityValueMethod == null)
                    Debug.LogError("\"GetSmoothedVelocityValue\" method could not be found!");
            }
        }


        private static FieldInfo _throwSmoothingVelocityFramesField;
        public static Vector3 GetThrowingVelocity(XRGrabInteractable interactable)
        {
            if (_throwSmoothingVelocityFramesField == null)
            {
                _throwSmoothingVelocityFramesField = typeof(XRGrabInteractable).GetField("m_ThrowSmoothingVelocityFrames", BindingFlags.NonPublic | BindingFlags.Instance);

                if (_throwSmoothingVelocityFramesField == null)
                    Debug.LogError("\"m_ThrowSmoothingVelocityFrames\" field could not be found!");
            }

            UpdateGetSmoothedVelocityValueMethod();

            if (_throwSmoothingVelocityFramesField != null && _getSmoothedVelocityValueMethod != null)
            {
                _args1[0] = _throwSmoothingVelocityFramesField.GetValue(interactable);
                return (Vector3)_getSmoothedVelocityValueMethod.Invoke(interactable, _args1);
            }

            return Vector3.zero;
        }

        private static FieldInfo _throwSmoothingAngularVelocityFramesField;
        public static Vector3 GetThrowingAngularVelocity(XRGrabInteractable interactable)
        {
            if (_throwSmoothingAngularVelocityFramesField == null)
            {
                _throwSmoothingAngularVelocityFramesField = typeof(XRGrabInteractable).GetField("m_ThrowSmoothingAngularVelocityFrames", BindingFlags.NonPublic | BindingFlags.Instance);

                if (_throwSmoothingAngularVelocityFramesField == null)
                    Debug.LogError("\"m_ThrowSmoothingAngularVelocityFrames\" field could not be found!");
            }

            UpdateGetSmoothedVelocityValueMethod();

            if (_throwSmoothingAngularVelocityFramesField != null && _getSmoothedVelocityValueMethod != null)
            {
                _args1[0] = _throwSmoothingAngularVelocityFramesField.GetValue(interactable);
                return (Vector3)_getSmoothedVelocityValueMethod.Invoke(interactable, _args1);
            }

            return Vector3.zero;
        }
    }
}
