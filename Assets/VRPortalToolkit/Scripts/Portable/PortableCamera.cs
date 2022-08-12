using Misc.EditorHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Portables
{
    public class PortableCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        public new Camera camera {
            get => _camera;
            set => _camera = value;
        }
        public void ClearCamera() => camera = null;

        [SerializeField] private Transform _source;
        public Transform source {
            get => _source;
            set {
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
        public void ClearSource() => source = null;

        protected virtual void Reset()
        {
            camera = GetComponent<Camera>();
            source = transform;
        }
        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_source), nameof(source));
        }

        protected virtual void AddTeleportListener(Transform source)
        {
            if (source) PortalPhysics.AddPostTeleportListener(source, OnPostTeleport);
        }
        protected virtual void RemoveTeleportListener(Transform source)
        {
            if (source) PortalPhysics.RemovePostTeleportListener(source, OnPostTeleport);
        }

        protected virtual void OnPostTeleport(Teleportation args)
        {
            if (args.fromPortal && args.fromPortal.usesLayers && camera)
                camera.cullingMask = args.fromPortal.ModifyLayerMask(camera.cullingMask);
        }
    }
}
