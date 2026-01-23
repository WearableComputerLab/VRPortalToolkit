using Misc.EditorHelpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit
{
    /// <summary>
    /// Represents a portal in the VRPortalToolkit system. Handles teleportation, layer/tag changes, and anchor management.
    /// </summary>
    public class Portal : MonoBehaviour, IPortal
    {
        private Matrix4x4 _previousWorldToLocalMatrix;
        internal Matrix4x4 previousWorldToLocalMatrix {
            get => _previousWorldToLocalMatrix;
            set => _previousWorldToLocalMatrix = value;
        }

        [Tooltip("The portal connected to this portal.")]
        [SerializeField] private Portal _connectedPortal;
        /// <summary>
        /// The portal connected to this portal. Setting this property will automatically update the connection on both portals.
        /// </summary>
        public Portal connected {
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
        /// <inheritdoc/>
        IPortal IPortal.connected => _connectedPortal;

        [Header("Local World")]
        [Tooltip("The anchor transform representing the local space of this portal.")]
        [SerializeField] private Transform _localAnchor;
        /// <summary>
        /// The anchor transform representing the local space of this portal.
        /// </summary>
        public Transform localAnchor {
            get => _localAnchor;
            set => _localAnchor = value;
        }

        [Tooltip("The set of local layers used for layer mapping when teleporting through this portal.")]
        [SerializeField] private LayerMask[] _localLayers;
        /// <summary>
        /// The set of local layers used for layer mapping when teleporting through this portal.
        /// </summary>
        public LayerMask[] localLayers {
            get => _localLayers;
            set => _localLayers = value;
        }

        [Tooltip("The set of local tags used for tag mapping when teleporting through this portal.")]
        [SerializeField] public string[] _localTags;
        /// <summary>
        /// The set of local tags used for tag mapping when teleporting through this portal.
        /// </summary>
        public string[] localTags {
            get => _localTags;
        }

        /// <summary>
        /// Event invoked before teleportation occurs through this portal.
        /// </summary>
        public TeleportAction preTeleport;
        /// <summary>
        /// Event invoked after teleportation occurs through this portal.
        /// </summary>
        public TeleportAction postTeleport;

        protected virtual void Reset()
        {
            localAnchor = transform;
        }

        protected virtual void OnValidate()
        {
            if (Application.isPlaying) Validate.FieldWithProperty(this, nameof(_connectedPortal), nameof(connected));
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
        public virtual Matrix4x4 teleportMatrix => _connectedPortal ? _connectedPortal._localAnchor.localToWorldMatrix * _localAnchor.worldToLocalMatrix : Matrix4x4.identity;

        private Rigidbody _rigidbody;
        /// <summary>
        /// The Rigidbody attached to this portal's GameObject, if any.
        /// </summary>
        public new Rigidbody rigidbody {
            get => _rigidbody ? _rigidbody : _rigidbody = transform.GetComponent<Rigidbody>();
        }

        private List<Collider> _colliders = new List<Collider>();
        private ReadOnlyCollection<Collider> _readOnlyColliders;
        /// <summary>
        /// The colliders associated with this portal.
        /// </summary>
        public IReadOnlyCollection<Collider> colliders => _readOnlyColliders;

        #region Unity Functions

        protected virtual void Awake()
        {
            GetComponentsInChildren(true, _colliders);
            _readOnlyColliders = new ReadOnlyCollection<Collider>(_colliders);
        }

        protected virtual void OnEnable()
        {
            PortalPhysics.RegisterPortal(this);
            PortalPhysics.AddPostTeleportListener(transform, OnPostTeleport);
        }

        protected virtual void OnDisable()
        {
            PortalPhysics.UnregisterPortal(this);
            PortalPhysics.RemovePostTeleportListener(transform, OnPostTeleport);
        }

        #endregion

        /// <summary>
        /// Called after teleportation occurs through this portal.
        /// </summary>
        /// <param name="teleportation">The teleportation data.</param>
        private void OnPostTeleport(Teleportation teleportation)
        {
            _previousWorldToLocalMatrix = transform.worldToLocalMatrix;
        }

        #region Physics Casting Functions

        /// <summary>
        /// Indicates whether the connected portal was active before casting.
        /// </summary>
        protected bool previousConnectedActive;
        /// <summary>
        /// The previous world-to-local matrix of the connected portal.
        /// </summary>
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

        /// <summary>
        /// Teleports the given transform through this portal.
        /// </summary>
        /// <param name="transform">The transform to teleport.</param>
        /// <param name="applyToChildren">Whether to apply changes to child objects.</param>
        public void Teleport(Transform transform, bool applyToChildren = true)
        {
            if (transform)
                PortalPhysics.ForceTeleport(transform, () => TeleportLogic(transform, transform.GetComponent<Rigidbody>(), applyToChildren), this, this);
        }

        /// <summary>
        /// Teleports the given rigidbody through this portal.
        /// </summary>
        /// <param name="rigidbody">The rigidbody to teleport.</param>
        /// <param name="applyToChildren">Whether to apply changes to child objects.</param>
        public void Teleport(Rigidbody rigidbody, bool applyToChildren = true)
        {
            if (rigidbody)
                PortalPhysics.ForceTeleport(rigidbody.transform, () => TeleportLogic(rigidbody.transform, rigidbody, applyToChildren), this, this);
        }

        /// <summary>
        /// Handles the teleportation logic for the given transform and rigidbody.
        /// </summary>
        /// <param name="transform">The transform to teleport.</param>
        /// <param name="rigidbody">The rigidbody to teleport.</param>
        /// <param name="applyToChildren">Whether to apply changes to child objects.</param>
        protected virtual void TeleportLogic(Transform transform, Rigidbody rigidbody, bool applyToChildren)
        {
            if (usesTeleport)
            {
                Matrix4x4 matrix = this.ModifyMatrix(transform.localToWorldMatrix);

                transform.position = matrix.GetColumn(3);
                transform.rotation = matrix.rotation;
                transform.localScale = matrix.lossyScale;

                if (rigidbody && !rigidbody.isKinematic)
                {
                    rigidbody.linearVelocity = this.ModifyVector(rigidbody.linearVelocity);
                    rigidbody.angularVelocity = this.ModifyVector(rigidbody.angularVelocity);
                }
            }

            if (applyToChildren)
            {
                foreach (Transform child in transform)
                {
                    if (usesTag)
                        child.tag = this.ModifyTag(child.tag);

                    if (usesLayers)
                        child.gameObject.layer = this.ModifyLayer(child.gameObject.layer);
                }
            }
            else
            {
                if (usesTag)
                    transform.tag = this.ModifyTag(transform.tag);

                if (usesLayers)
                    transform.gameObject.layer = this.ModifyLayer(transform.gameObject.layer);
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

        /// <inheritdoc/>
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

        #endregion
    }
}