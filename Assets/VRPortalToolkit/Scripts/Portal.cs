 using Misc.EditorHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using VRPortalToolkit.Utilities;

// Seam appear on objects going through portals
// Transition head touching a portal no longer triggers teleport
// Transitions and slice portables can't find the normal on the first frame (causes a bunch of visual glitches)
// Portable ball needs a collider on the clones to prevent the ray from passing through on transitions
// Flicker when translating with XRPortalRayInteractor (probably disable portable to solve)
// Portals behave badly with XRPortalRayInteractor, seem to be pushed through there own portals :(

// Should updater not 

namespace VRPortalToolkit
{
    public class Portal : MonoBehaviour, IPortal
    {
        private Matrix4x4 _previousLocalToWorldMatrix;
        internal Matrix4x4 previousWorldToLocalMatrix {
            get => _previousLocalToWorldMatrix;
            set => _previousLocalToWorldMatrix = value;
        }

        [SerializeField] private Portal _connectedPortal;
        public Portal connectedPortal {
            get => _connectedPortal;
            set {
                if (_connectedPortal != value && value != this)
                {
                    if (_connectedPortal != null)
                        Validate.UpdateField(_connectedPortal, nameof(_connectedPortal), _connectedPortal._connectedPortal = null);

                    Validate.UpdateField(this, nameof(_connectedPortal), _connectedPortal = value);

                    if (_connectedPortal != null)
                        Validate.UpdateField(_connectedPortal, nameof(_connectedPortal), _connectedPortal._connectedPortal = this);
                }
            }
        }

        [Header("Local World")]
        [SerializeField] private Transform _localAnchor;
        public Transform localAnchor {
            get => _localAnchor;
            set => _localAnchor = value;
        }

        [SerializeField] private LayerMask[] _localLayers;
        public LayerMask[] localLayers {
            get => _localLayers;
            set => _localLayers = value;
        }

        [SerializeField] public string[] _localTags;
        public string[] localTags {
            get => _localTags;
        }

        protected virtual void Reset()
        {
            localAnchor = transform;
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_connectedPortal), nameof(connectedPortal));
        }

        /// <inheritdoc/>
        public virtual bool usesTeleport => _localAnchor && _connectedPortal && _connectedPortal._localAnchor && _localAnchor != _connectedPortal._localAnchor;

        /// <inheritdoc/>
        public virtual bool usesLayers => _localLayers != null && _localLayers.Length != 0
            && _connectedPortal && _connectedPortal._localLayers != null && _connectedPortal._localLayers.Length != 0;

        /// <inheritdoc/>
        public virtual bool usesTag => _localTags != null && _localTags.Length != 0
            && _connectedPortal && _connectedPortal._localTags != null && _connectedPortal._localTags.Length != 0;

        /// <inheritdoc/>
        public virtual Matrix4x4 teleportMatrix => _connectedPortal._localAnchor.localToWorldMatrix * _localAnchor.worldToLocalMatrix;

        private Rigidbody _rigidbody;
        public new Rigidbody rigidbody {
            get => _rigidbody ? _rigidbody : _rigidbody = transform.GetComponent<Rigidbody>();
        }

        private List<Collider> _colliders = new List<Collider>();
        private ReadOnlyCollection<Collider> _readOnlyColliders;
        public IReadOnlyCollection<Collider> colliders {
            get {
                if (_readOnlyColliders == null)
                {
                    gameObject.GetColliders(false, _colliders);
                    _readOnlyColliders = new ReadOnlyCollection<Collider>(_colliders);
                }

                return _readOnlyColliders;
            }
        }

        #region Unity Functions

        /// <summary>Add this to all portals.</summary>
        protected virtual void OnEnable()
        {
            PortalPhysics.RegisterPortal(this);
        }

        /// <summary>Remove this from all portals.</summary>
        protected virtual void OnDisable()
        {
            PortalPhysics.UnregisterPortal(this);
        }

        /*protected virtual void OnDrawGizmos()
        {
            if (teleportAnchor)
            {
                Gizmos.color = Color.HSVToRGB((GetHashCode() * 0.01f) % 1f, 1f, 1f);
                Gizmos.DrawLine(worldAnchor.position, teleportAnchor.position);
            }
        }*/

        #endregion

        #region Physics Casting Functions

        protected bool previousConnectedActive;
        protected Matrix4x4 previousConnectedMatrix;

        /// <inheritdoc/>
        public virtual void PreCast()
        {
            if (_connectedPortal)
            {
                previousConnectedActive = _connectedPortal.gameObject.activeInHierarchy;
                previousConnectedMatrix = _connectedPortal.previousWorldToLocalMatrix;
                _connectedPortal.gameObject.SetActive(false);
            }
        }

        /// <inheritdoc/>
        public virtual void PostCast()
        {
            if (_connectedPortal)
            {
                _connectedPortal.gameObject.SetActive(previousConnectedActive);
                _connectedPortal.previousWorldToLocalMatrix = previousConnectedMatrix;
            }
        }

        #endregion

        public void Teleport(Transform transform, bool applyToChildren = true)
        {
            if (transform)
                PortalPhysics.ForceTeleport(transform, () => TeleportLogic(transform, transform.GetComponent<Rigidbody>(), applyToChildren), this, this);
        }

        public void Teleport(Rigidbody rigidbody, bool applyToChildren = true)
        {
            if (rigidbody)
                PortalPhysics.ForceTeleport(rigidbody.transform, () => TeleportLogic(rigidbody.transform, rigidbody, applyToChildren), this, this);
        }

        protected virtual void TeleportLogic(Transform transform, Rigidbody rigidbody, bool applyToChildren)
        {
            if (usesTeleport)
            {
                Matrix4x4 matrix = ModifyMatrix(transform.localToWorldMatrix);

                transform.position = matrix.GetColumn(3);
                transform.rotation = matrix.rotation;
                transform.localScale = matrix.lossyScale;

                if (rigidbody)
                {
                    rigidbody.velocity = ModifyVector(rigidbody.velocity);
                    rigidbody.angularVelocity = ModifyVector(rigidbody.angularVelocity);
                }
            }

            if (applyToChildren)
            {
                foreach (Transform child in transform)
                {
                    if (usesTag)
                        child.tag = ModifyTag(child.tag);

                    if (usesLayers)
                        child.gameObject.layer = ModifyLayer(child.gameObject.layer);
                }
            }
            else
            {
                if (usesTag)
                    transform.tag = ModifyTag(transform.tag);

                if (usesLayers)
                    transform.gameObject.layer = ModifyLayer(transform.gameObject.layer);
            }
        }

        /// <inheritdoc/>
        public virtual bool ModifyLayerMask(ref int layerMask)
        {
            if (usesLayers)
            {
                int length = _localLayers.Length < _connectedPortal._localLayers.Length ? _localLayers.Length : _connectedPortal._localLayers.Length;

                int localLayer, newLayerMask = layerMask;
                for (int i = 0; i < length; i++)
                {
                    localLayer = _localLayers[i];

                    // Layer mask contains localLayer
                    if ((layerMask & localLayer) != 0)
                        newLayerMask = (newLayerMask & ~localLayer) | _connectedPortal._localLayers[i];
                }

                if (newLayerMask != layerMask)
                {
                    layerMask = newLayerMask;
                    return true;
                }
            }

            return false;
        }

        public virtual int ModifyLayerMask(int layer)
        {
            ModifyLayerMask(ref layer);
            return layer;
        }

        /// <inheritdoc/>
        public virtual bool ModifyLayer(ref int layer)
        {
            if (usesLayers)
            {
                int length = _localLayers.Length < _connectedPortal._localLayers.Length ? _localLayers.Length : _connectedPortal._localLayers.Length;

                for (int i = 0; i < length; i++)
                {
                    // Layer contains localLayer
                    if ((layer & _localLayers[i]) != 0)
                    {
                        layer = _connectedPortal._localLayers[i];
                        return true;
                    }
                }
            }

            return false;
        }

        public virtual int ModifyLayer(int layer)
        {
            ModifyLayer(ref layer);
            return layer;
        }

        /// <inheritdoc/>
        public virtual bool ModifyTag(ref string tag)
        {
            if (usesTag)
            {
                int length = _localTags.Length < _connectedPortal._localTags.Length ? _localTags.Length : _connectedPortal._localTags.Length;

                for (int i = 0; i < length; i++)
                {
                    if (tag == _localTags[i])
                    {
                        tag = _connectedPortal._localTags[i];
                        return true;
                    }
                }

            }

            return false;
        }

        public virtual string ModifyTag(string tag)
        {
            ModifyTag(ref tag);
            return tag;
        }

        #region Teleport Functions

        /// <inheritdoc/>
        public virtual bool ModifyMatrix(ref Matrix4x4 localToWorldMatrix)
        {
            if (usesTeleport)
            {
                localToWorldMatrix = teleportMatrix * localToWorldMatrix;
                return true;
            }

            return false;
        }

        public virtual Matrix4x4 ModifyMatrix(Matrix4x4 localToWorldMatrix)
        {
            ModifyMatrix(ref localToWorldMatrix);
            return localToWorldMatrix;
        }

        /// <inheritdoc/>
        public virtual bool ModifyPoint(ref Vector3 point)
        {
            if (usesTeleport)
            {
                point = _connectedPortal._localAnchor.TransformPoint(_localAnchor.InverseTransformPoint(point));
                return true;
            }

            return false;
        }

        public virtual Vector3 ModifyPoint(Vector3 point)
        {
            ModifyPoint(ref point);
            return point;
        }

        /// <inheritdoc/>
        public virtual bool ModifyDirection(ref Vector3 direction)
        {
            if (usesTeleport)
            {
                direction = _connectedPortal._localAnchor.TransformDirection(_localAnchor.InverseTransformDirection(direction));
                return true;
            }

            return false;
        }

        public virtual Vector3 ModifyDirection(Vector3 direction)
        {
            ModifyDirection(ref direction);
            return direction;
        }

        /// <inheritdoc/>
        public virtual bool ModifyVector(ref Vector3 vector)
        {
            if (usesTeleport)
            {
                vector = _connectedPortal._localAnchor.TransformVector(_localAnchor.InverseTransformVector(vector));
                return true;
            }

            return false;
        }

        public virtual Vector3 ModifyVector(Vector3 vector)
        {
            ModifyVector(ref vector);
            return vector;
        }

        /// <inheritdoc/>
        public virtual bool ModifyRotation(ref Quaternion rotation)
        {
            if (usesTeleport)
            {
                rotation = _connectedPortal._localAnchor.rotation * Quaternion.Inverse(_localAnchor.rotation) * rotation;
                return true;
            }

            return false;
        }
        public virtual Quaternion ModifyRotation(Quaternion rotation)
        {
            ModifyRotation(ref rotation);
            return rotation;
        }

        #endregion
    }
}