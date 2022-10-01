using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit;
using VRPortalToolkit.Physics;

public class IgnoreParentTeleport : MonoBehaviour
{
    [Tooltip("If the transform should not move when the parent hierarchy teleports.")]
    [SerializeField] public bool _maintainTransform = true;
    public bool maintainTransform
    {
        get => _maintainTransform;
        set
        {
            if (_maintainTransform != value)
            {
                Validate.UpdateField(this, nameof(_maintainTransform), _maintainTransform = value);

                if (_maintainTransform && isActiveAndEnabled && Application.isPlaying)
                {
                    RemoveParentListener(previousParent);
                    previousParent = transform.parent;
                    AddParentListener(transform.parent);
                }
                else
                    RemoveParentListener(previousParent);
            }
        }
    }

    protected Matrix4x4 preTeleportMatrix;
    protected Transform previousParent;

    protected virtual void AddParentListener(Transform parent)
    {
        if (parent)
        {
            PortalPhysics.AddPostTeleportListener(parent, ParentPreTeleport);
            PortalPhysics.AddPostTeleportListener(parent, ParentPostTeleport);
        }
    }

    protected virtual void RemoveParentListener(Transform parent)
    {
        if (parent)
        {
            PortalPhysics.RemovePostTeleportListener(parent, ParentPreTeleport);
            PortalPhysics.RemovePostTeleportListener(parent, ParentPostTeleport);
        }
    }

    protected virtual void OnValidate()
    {
        Validate.FieldWithProperty(this, nameof(_maintainTransform), nameof(maintainTransform));
    }

    protected virtual void OnEnable()
    {
        if (maintainTransform)
        {
            AddParentListener(transform.parent);
            previousParent = transform.parent;
        }

        PortalPhysics.IgnoreParentTeleport(transform, true);
    }

    protected virtual void OnDisable()
    {
        RemoveParentListener(previousParent);

        PortalPhysics.IgnoreParentTeleport(transform, false);
    }

    protected virtual void FixedUpdate()
    {
        if (previousParent != transform.parent)
        {
            RemoveParentListener(previousParent);
            AddParentListener(transform.parent);
            previousParent = transform.parent;
        }
    }

    protected virtual void ParentPreTeleport(Teleportation args)
    {
        preTeleportMatrix = transform.localToWorldMatrix;
    }

    protected virtual void ParentPostTeleport(Teleportation args)
    {
        transform.SetPositionAndRotation(preTeleportMatrix.GetColumn(3), preTeleportMatrix.rotation);
    }
}
