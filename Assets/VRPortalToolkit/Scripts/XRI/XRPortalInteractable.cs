using EzySlice;
using Misc.EditorHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Physics;
using static UnityEngine.XR.Interaction.Toolkit.XRInteractionUpdateOrder;

/**
 * What's left for the demo:
 * - Ball physics clones cause transitions not to work
 * - Render clones sometimes jump forward (as in get teleported twice through the portal)
 * - XRPortalRayInteractor does no work for teleports cursor (because of the forward direction thing), also doesn't work through portals
 */

// TODO: Add a timer of swap that flips the anchor when you put a portal over your head
// TODO: Add bool for if you can hold both sides of a portal at the same time

namespace VRPortalToolkit.XRI
{
    public class XRPortalInteractable : XRGrabInteractable
    {
        private readonly WaitForEndOfFrame _WaitForEndOfFrame = new WaitForEndOfFrame();

        const float k_DeltaTimeThreshold = 0.001f;

        [SerializeField] private Portal _portal;
        public Portal portal { get => _portal; set => _portal = value; }

        [SerializeField] private XRPortalInteractable _connected;
        public XRPortalInteractable connected { get => _connected; set => _connected = value; }

        [SerializeField] private Transform _groundLevel;
        public Transform groundLevel { get => _groundLevel; set => _groundLevel = value; }

        [SerializeField] private LinkedMovement _linkedMovement = LinkedMovement.Anchored;
        public LinkedMovement linkedMovement {
            get => _linkedMovement;
            set {
                if (_linkedMovement != value)
                {
                    _linkedMovement = value;
                    StoreAnchor();
                }
            }
        }

        public enum LinkedMovement
        {
            None = 0,
            Relative = 1,
            Anchored = 2,
        }

        // TODO: Levelness is implemented rather naively here (there isn't much of a way for heigh to be updated
        // (Though I suppose you would just update container as its ground level
        [SerializeField] private Levelness _levelness = Levelness.LevelElevation | Levelness.LevelOrientation;
        public Levelness levelness { get => _levelness; set => _levelness = value; }

        [Flags]
        public enum Levelness
        {
            None = 0,
            LevelElevation = 1 << 1,
            LevelOrientation = 1 << 2,
        }

        [SerializeField] private bool _useSnapDistanceThreshold = true;
        public bool useSnapDistanceThreshold { get => _useSnapDistanceThreshold; set => _useSnapDistanceThreshold = value; }

        [ShowIf(nameof(_useSnapDistanceThreshold))]
        [SerializeField] private float _snapDistanceThreshold = 0.1f;
        public float snapDistanceThreshold { get => _snapDistanceThreshold; set => _snapDistanceThreshold = value; }

        [SerializeField] private bool _useSnapAngleThreshold = true;
        public bool useSnapAngleThreshold { get => _useSnapAngleThreshold; set => _useSnapAngleThreshold = value; }

        [ShowIf(nameof(_useSnapAngleThreshold))]
        [SerializeField] private float _snapAngleThreshold = 3f;
        public float snapAngleThreshold { get => _snapAngleThreshold; set => _snapAngleThreshold = value; }

        // TODO: Currently do not support scaled movement
        /*[ShowIf(nameof(_linkedMovement))]
        [SerializeField] private AnimationCurve _translationCurve = AnimationCurve.Constant(0f, 1f, 1f);
        public AnimationCurve translationCurve { get => _translationCurve; set => _translationCurve = value; }

        [ShowIf(nameof(_linkedMovement))]
        [SerializeField] private AnimationCurve _rotationCurve = AnimationCurve.Constant(0f, 1f, 1f);
        public AnimationCurve rotationCurve { get => _rotationCurve; set => _rotationCurve = value; }*/

        private PortalRelativePosition _interactorPositioning;
        private Transform _interactorOrigin;
        private Vector3 _forward;
        private Rigidbody _rigidbody;

        private Matrix4x4 _connectedAnchor;
        private Matrix4x4 _anchor;

        private float _lastGrabTime;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = GetComponent<Rigidbody>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            StoreAnchor();

            PortalPhysics.AddPostTeleportListener(transform, OnPostTeleport);
        }

        protected override void OnDisable()
        {
            base.OnEnable();

            PortalPhysics.RemovePostTeleportListener(transform, OnPostTeleport);
        }

        public override bool IsSelectableBy(IXRSelectInteractor interactor)
        {
            if (base.IsSelectableBy(interactor))
            {
                if (IsSelected(interactor)) return true;

                // When portals are too  close, only allow the most recently used to be grabbed
                if (IsWithinSnapThreshold() && _connected && (_connected.isSelected || _connected._lastGrabTime > _lastGrabTime))
                    return false;

                // Do not allow a portal to be grabbed through itself
                IEnumerable<Portal> from = null, to = null;

                PortalRelativePosition positioning = interactor.transform.GetComponentInParent<PortalRelativePosition>();
                if (positioning) from = positioning.GetPortalsToOrigin();

                if (interactor is IXRPortableInteractor portableInteractor)
                    to = portableInteractor.GetPortalsToInteractable(this);

                foreach (Portal _ in from.Difference(to))
                    return false;

                return true;
            }

            return false;
        }

        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            PortalPhysics.UnregisterPortable(transform);

            Transform interactor = args.interactorObject.transform;
            _interactorPositioning = interactor.GetComponentInParent<PortalRelativePosition>();

            // Drop because we'll end up grabbing again
            if (_connected && _connected.isSelected && interactorsSelecting.Count == 1)
                _connected.SetupRigidbodyDrop(_connected._rigidbody);

            if (_interactorPositioning)
            {
                // Do teleportations
                Pose interactorPose = new Pose(interactor.position, interactor.rotation);
                _interactorPositioning.GetPortalsToOrigin().ModifyTransform(interactor);

                // Do the actual hard work
                base.OnSelectEntering(args);

                // Restore
                interactor.transform.SetPositionAndRotation(interactorPose.position, interactorPose.rotation);

                _interactorOrigin = _interactorPositioning.origin;
                AddOriginListener();
            }
            else
                base.OnSelectEntering(args);

            if (_connected && !_connected.isSelected)
            {
                if (_connected._rigidbody)
                    _connected.SetupRigidbodyGrab(_connected._rigidbody);

                if (IsWithinSnapThreshold())
                    _connected.transform.SetPositionAndRotation(transform.position, Quaternion.LookRotation(-transform.forward, transform.up));

                _connected.StoreAnchor();
            }

            _lastGrabTime = Time.time;
        }

        public virtual bool IsWithinSnapThreshold()
        {
            if (!_connected) return false;

            if (_useSnapDistanceThreshold)
            {
                return Vector3.Distance(transform.position, connected.transform.position) < _snapDistanceThreshold
                && (!_useSnapAngleThreshold || Quaternion.Angle(transform.rotation, Quaternion.LookRotation(-connected.transform.forward, connected.transform.up)) < _snapAngleThreshold);
            }
            return _useSnapAngleThreshold && Quaternion.Angle(transform.rotation, Quaternion.LookRotation(-connected.transform.forward, connected.transform.up)) < _snapAngleThreshold;
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
        }

        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);

            if (_connected && _connected._rigidbody && !_connected.isSelected)
                _connected.SetupRigidbodyDrop(_connected._rigidbody);

            if (_connected && _connected.isSelected)
            {
                SetupRigidbodyGrab(_rigidbody);
                StoreAnchor();
            }

            RemoveOriginListener();
            _interactorPositioning = null;

            _lastGrabTime = Time.time;
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
        }

        public override void ProcessInteractable(UpdatePhase updatePhase)
        {
            if (_interactorOrigin != _interactorPositioning?.origin)
            {
                RemoveOriginListener();
                _interactorOrigin = _interactorPositioning?.origin;
                AddOriginListener();
            }

            if (isSelected)
            {
                Matrix4x4 previous = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

                if (_interactorPositioning)
                {
                    Transform interactor = interactorsSelecting[0].transform;

                    // Do teleportations
                    Pose interactorPose = new Pose(interactor.position, interactor.rotation);
                    _interactorPositioning.GetPortalsToOrigin().ModifyTransform(interactor);

                    // Do the actual hard work
                    base.ProcessInteractable(updatePhase);

                    // Restore
                    interactor.transform.SetPositionAndRotation(interactorPose.position, interactorPose.rotation);
                }
                else
                    base.ProcessInteractable(updatePhase);

                // Get target to world
                Pose targetPose = XRUtils.GetTargetPose(this);

                _connected?.ProcessLinkedMovement(updatePhase, previous, Matrix4x4.TRS(targetPose.position, targetPose.rotation, transform.localScale));
            }
            else
                base.ProcessInteractable(updatePhase);


        }
        protected virtual void ProcessLinkedMovement(UpdatePhase updatePhase, in Matrix4x4 connectedPrevious, in Matrix4x4 connectedTarget)
        {
            if (!isSelected)
            {
                switch (updatePhase)
                {
                    case UpdatePhase.Fixed:
                        if (movementType == MovementType.VelocityTracking)
                        {
                            GetLinkedTargetPose(connectedPrevious, connectedTarget, out Pose targetPose, out Vector3 targetScale);
                            PerformVelocityTrackingUpdate(Time.deltaTime, targetPose);
                            transform.localScale = targetScale;
                        }
                        else if (movementType == MovementType.Kinematic)
                        {
                            GetLinkedTargetPose(connectedPrevious, connectedTarget, out Pose targetPose, out Vector3 targetScale);
                            PerformKinematicUpdate(targetPose);
                            transform.localScale = targetScale;
                        }
                        break;

                    case UpdatePhase.Dynamic:
                    case UpdatePhase.OnBeforeRender:
                        if (movementType == MovementType.Instantaneous)
                        {
                            GetLinkedTargetPose(connectedPrevious, connectedTarget, out Pose targetPose, out Vector3 targetScale);
                            transform.SetPositionAndRotation(targetPose.position, targetPose.rotation);
                            transform.localScale = targetScale;
                        }
                        break;
                }
            }
        }

        private void GetLinkedTargetPose(Matrix4x4 connectedPrevious, in Matrix4x4 connectedTarget, out Pose pose, out Vector3 scale)
        {
            if (_linkedMovement != LinkedMovement.None)
            {
                if (_linkedMovement == LinkedMovement.Relative)
                {
                    // Update the anchor to be the latest previous
                    _anchor = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
                    // TODO: This assumes that the anchor is 180
                    _connectedAnchor = Matrix4x4.TRS(connectedPrevious.GetColumn(3),
                        Quaternion.LookRotation(connectedPrevious.MultiplyVector(Vector3.back), connectedPrevious.MultiplyVector(Vector3.up)), 
                        connectedPrevious.lossyScale);
                }

                Matrix4x4 target = (_anchor * _connectedAnchor.inverse) * connectedTarget; // Brackets its the teleport matrix

                // TODO: This is where scaled movement would need to kick in

                pose = new Pose(target.GetColumn(3), Quaternion.LookRotation(target.MultiplyVector(Vector3.back), target.MultiplyVector(Vector3.up)));
                scale = target.lossyScale;
            }
            else
            {
                pose = new Pose(transform.position, transform.rotation);
                scale = transform.localScale;
            }

            Pose connectedTargetPose = new Pose(connectedTarget.GetColumn(3), connectedTarget.rotation);

            if (_levelness.HasFlag(Levelness.LevelOrientation))
                LevelOrientation(connectedTargetPose, ref pose);

            if (_levelness.HasFlag(Levelness.LevelElevation))
                LevelElevation(connectedTargetPose, ref pose);
        }

        private void StoreAnchor()
        {
            _anchor = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

            // TODO: This assumes that the anchor is 180
            if (_connected)//Quaternion.AngleAxis(180f, _connected.transform.up) * 
                _connectedAnchor = Matrix4x4.TRS(_connected.transform.position,
                    Quaternion.LookRotation(-_connected.transform.forward, _connected.transform.up),
                    _connected.transform.localScale);
        }

        protected virtual void LevelOrientation(Pose connectedTargetPose, ref Pose targetPose)
        {
            // Store forward
            _forward = GetForward(targetPose.forward);

            // Do rotation
            Quaternion connectedRot = _connected && _connected._groundLevel
                ? Quaternion.Inverse(_connected._groundLevel.transform.rotation) * connectedTargetPose.rotation
                : connectedTargetPose.rotation;

            targetPose.rotation = _groundLevel ? _groundLevel.rotation * connectedRot : connectedRot;

            // TODO: This assumes that the anchor is 180
            targetPose.rotation = Quaternion.LookRotation(-targetPose.forward, targetPose.up);

            // Reapply forward
            Vector3 newForward = GetForward(targetPose.forward), up = GetUp();
            targetPose.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(newForward, _forward, up), up) * targetPose.rotation;
        }

        // TODO: Does not use scale (well atleast this objects scale)
        protected virtual void LevelElevation(Pose connectedTargetPose, ref Pose targetPose)
        {
            if (_connected)
            {
                Vector3 connectedPos = _connected && _connected._groundLevel
                    ? _connected._groundLevel.InverseTransformPoint(connectedTargetPose.position)
                    : connectedTargetPose.position;

                if (_groundLevel)
                {
                    Vector3 originalPos = _groundLevel.InverseTransformPoint(targetPose.position);

                    targetPose.position = _groundLevel.TransformPoint(new Vector3(originalPos.x, connectedPos.y, originalPos.z));
                }
                else
                    targetPose.position = new Vector3(targetPose.position.x, connectedPos.y, targetPose.position.z);
            }
        }

        private Vector3 GetUp() => _groundLevel ? _groundLevel.up : Vector3.up;

        private Vector3 GetForward(Vector3 forward)
        {
            forward = Vector3.ProjectOnPlane(forward, GetUp());

            // Incase forward is not valid, use the previous valid one
            if (forward.magnitude == 0)
                return _forward;

            return forward;
        }

        private void PerformKinematicUpdate(Pose targetPose)
        {
            var position = attachPointCompatibilityMode == AttachPointCompatibilityMode.Default
                ? targetPose.position
                : targetPose.position - _rigidbody.worldCenterOfMass + _rigidbody.position;

            _rigidbody.MovePosition(position);
            _rigidbody.MoveRotation(targetPose.rotation);
        }

        private void PerformVelocityTrackingUpdate(float deltaTime, Pose targetPose)
        {
            // Skip velocity calculations if Time.deltaTime is too low due to a frame-timing issue on Quest
            if (deltaTime < k_DeltaTimeThreshold)
                return;

            // Do velocity tracking
            // Scale initialized velocity by prediction factor
            _rigidbody.velocity *= (1f - velocityDamping);
            var positionDelta = attachPointCompatibilityMode == AttachPointCompatibilityMode.Default
                ? targetPose.position - transform.position
                : targetPose.position - _rigidbody.worldCenterOfMass;
            var velocity = positionDelta / deltaTime;
            _rigidbody.velocity += (velocity * velocityScale);

            // Do angular velocity tracking
            // Scale initialized velocity by prediction factor
            _rigidbody.angularVelocity *= (1f - angularVelocityDamping);
            var rotationDelta = targetPose.rotation * Quaternion.Inverse(transform.rotation);
            rotationDelta.ToAngleAxis(out var angleInDegrees, out var rotationAxis);
            if (angleInDegrees > 180f)
                angleInDegrees -= 360f;

            if (Mathf.Abs(angleInDegrees) > Mathf.Epsilon)
            {
                var angularVelocity = (rotationAxis * (angleInDegrees * Mathf.Deg2Rad)) / deltaTime;
                _rigidbody.angularVelocity += (angularVelocity * angularVelocityScale);
            }
        }

        private Pose _originPreTeleportLocalPose;
        protected virtual void OnOriginPreTeleport(Teleportation teleportation)
        {
            // Not sure if there is anything that should be done here
            _originPreTeleportLocalPose = teleportation.transform.InverseTransformPose(new Pose(transform.position, transform.rotation));
        }

        /// <summary>
        /// After the origin teleports through our portal, we swap the portals controller
        /// </summary>
        protected virtual void OnOriginPostTeleport(Teleportation teleportation)
        {
            if (isSelected)
            {
                if (teleportation.fromPortal == portal)
                {
                    // Handles putting your head through a portal you are holding
                    // TODO: The swap over needs to make sure that the attach point remains persistent, otherwise it drifts with each swap
                    IXRSelectInteractor interactor = interactorsSelecting[0];
                    interactionManager.SelectExit(interactor, this);
                    if (_connected) interactionManager.SelectEnter(interactor, _connected);
                }
                else
                {
                    // Handles not moving the connected portal when the user teleports themself
                    IXRSelectInteractor interactor = interactorsSelecting[0];
                    interactionManager.SelectExit(interactor, this);

                    Pose newPose = teleportation.transform.TransformPose(_originPreTeleportLocalPose);
                    transform.SetPositionAndRotation(newPose.position, newPose.rotation);

                    interactionManager.SelectEnter(interactor, this);
                }
            }
        }

        private IEnumerator WaitToGrab(IXRSelectInteractor interactor)
        {
            yield return _WaitForEndOfFrame;

        }

        private void AddOriginListener()
        {
            if (_interactorOrigin != null)
            {
                PortalPhysics.AddPreTeleportListener(_interactorOrigin, OnOriginPreTeleport);
                PortalPhysics.AddPostTeleportListener(_interactorOrigin, OnOriginPostTeleport);
            }
        }

        private void RemoveOriginListener()
        {
            if (_interactorOrigin != null)
            {
                PortalPhysics.RemovePreTeleportListener(_interactorOrigin, OnOriginPreTeleport);
                PortalPhysics.RemovePostTeleportListener(_interactorOrigin, OnOriginPostTeleport);
            }
        }

        private void OnPostTeleport(Teleportation teleportation)
        {
            StoreAnchor();
        }
    }
}
