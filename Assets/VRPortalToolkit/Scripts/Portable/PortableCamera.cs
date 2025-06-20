using Misc.EditorHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Portables
{
    /// <summary>
    /// Component that manages a camera's behavior when passing through portals, including automatic layer mask updates.
    /// </summary>
    public class PortableCamera : MonoBehaviour
    {
        private Camera _camera;
        /// <summary>
        /// Reference to the attached Camera component.
        /// </summary>
        public new Camera camera
        {
            get
            {
                if (_camera == null)
                    _camera = GetComponent<Camera>();

                return _camera;
            }
        }

        [SerializeField, Tooltip("The transform that is tracked to determine when the camera passes through portals.")]
        private Transform _source;
        /// <summary>
        /// The transform that is tracked to determine when the camera passes through portals.
        /// </summary>
        public Transform source
        {
            get => _source;
            set
            {
                if (_source != value)
                {
                    if (isActiveAndEnabled && Application.isPlaying)
                    {
                        AddTeleportListener(_source);
                        Validate.UpdateField(this, nameof(_source), _source = value);
                        RemoveTeleportListener(_source);
                    }
                    else
                        Validate.UpdateField(this, nameof(_source), _source = value);
                }
            }
        }

        protected virtual void Reset()
        {
            source = transform;
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_source), nameof(source));
        }

        protected virtual void OnEnable()
        {
            AddTeleportListener(_source);
        }

        protected virtual void OnDisable()
        {
            RemoveTeleportListener(_source);
        }

        /// <summary>
        /// Adds a teleport listener to the given source transform.
        /// </summary>
        /// <param name="source">The transform to add the teleport listener to.</param>
        protected virtual void AddTeleportListener(Transform source)
        {
            if (source) PortalPhysics.AddPostTeleportListener(source, OnPostTeleport);
        }

        /// <summary>
        /// Removes the teleport listener from the given source transform.
        /// </summary>
        /// <param name="source">The transform to remove the teleport listener from.</param>
        protected virtual void RemoveTeleportListener(Transform source)
        {
            if (source) PortalPhysics.RemovePostTeleportListener(source, OnPostTeleport);
        }

        /// <summary>
        /// Called after the source has teleported through a portal.
        /// Updates the camera's culling mask based on the portal's layer configuration.
        /// </summary>
        /// <param name="args">Information about the teleportation event.</param>
        protected virtual void OnPostTeleport(Teleportation args)
        {
            if (args.fromPortal && args.fromPortal.usesLayers && camera)
                camera.cullingMask = args.fromPortal.ModifyLayerMask(camera.cullingMask);
        }
    }
}
