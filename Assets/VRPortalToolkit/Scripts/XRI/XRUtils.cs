using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using static UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable;

namespace VRPortalToolkit.XRI
{
    /// <summary>
    /// Utility class for accessing and manipulating XR interaction toolkit components.
    /// </summary>
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

        /// <summary>
        /// Gets the target pose from an XR grab interactable.
        /// </summary>
        /// <param name="interactable">The grab interactable.</param>
        /// <returns>The target pose.</returns>
        public static Pose GetTargetPose(XRGrabInteractable interactable)
        {
            UpdateTargetPoseField();

            if (_targetPoseField != null)
                return (Pose)_targetPoseField.GetValue(interactable);

            return default;
        }

        /// <summary>
        /// Sets the target pose for an XR grab interactable.
        /// </summary>
        /// <param name="interactable">The grab interactable.</param>
        /// <param name="pose">The pose to set.</param>
        public static void SetTargetPose(XRGrabInteractable interactable, Pose pose)
        {
            UpdateTargetPoseField();

            if (_targetPoseField != null)
                _targetPoseField.SetValue(interactable, pose);
        }

        /// <summary>
        /// Performs a kinematic update on a rigidbody based on the target pose.
        /// </summary>
        /// <param name="interactable">The grab interactable.</param>
        /// <param name="rigidbody">The rigidbody to update.</param>
        /// <param name="targetPose">The target pose.</param>
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

        /// <summary>
        /// Invokes the OnTeleported method on an XR grab interactable.
        /// </summary>
        /// <param name="interactable">The grab interactable.</param>
        /// <param name="pose">The teleportation pose.</param>
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

        /// <summary>
        /// Gets the raycast hits from an XR ray interactor.
        /// </summary>
        /// <param name="interactor">The ray interactor.</param>
        /// <returns>The array of raycast hits.</returns>
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

        /// <summary>
        /// Gets the count of raycast hits from an XR ray interactor.
        /// </summary>
        /// <param name="interactor">The ray interactor.</param>
        /// <returns>The count of raycast hits.</returns>
        public static int GetRaycastHitsCount(XRRayInteractor interactor)
        {
            UpdateRaycastHitsCountField();

            if (_raycastHitsCountField != null)
                return (int)_raycastHitsCountField.GetValue(interactor);

            return -1;
        }

        /// <summary>
        /// Sets the count of raycast hits for an XR ray interactor.
        /// </summary>
        /// <param name="interactor">The ray interactor.</param>
        /// <param name="count">The count to set.</param>
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

        /// <summary>
        /// Gets the throwing velocity of an XR grab interactable.
        /// </summary>
        /// <param name="interactable">The grab interactable.</param>
        /// <returns>The throwing velocity.</returns>
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

        /// <summary>
        /// Gets the throwing angular velocity of an XR grab interactable.
        /// </summary>
        /// <param name="interactable">The grab interactable.</param>
        /// <returns>The throwing angular velocity.</returns>
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
