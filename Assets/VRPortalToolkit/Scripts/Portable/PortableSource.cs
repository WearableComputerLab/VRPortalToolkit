using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VRPortalToolkit;
using VRPortalToolkit.Physics;
using VRPortalToolkit.Portables;


public class PortableSource : MonoBehaviour
{
    private Portable _portable;
    public Portable portable
    {
        get
        {
            if (!_portable)
            {
                _portable = GetComponent<Portable>();
                if (_portable) _portable.enabled = !_source;
            }

            return _portable;
        }
    }

    [SerializeField] private Transform _source;
    public Transform source
    {
        get => _source;
        set {
            if (_source != value)
            {
                if (isActiveAndEnabled && Application.isPlaying)
                {
                    RemoveSourceListener(_source);
                    Validate.UpdateField(this, nameof(_source), _source = value);
                    AddSourceListener(_source);
                }
                else
                    Validate.UpdateField(this, nameof(_source), _source = value);
            }
        }
    }

    public UnityEvent<Portal> failed;

    protected virtual void AddSourceListener(Transform source)
    {
        if (portable) _portable.enabled = false;

        if (source) PortalPhysics.AddPostTeleportListener(source, SourcePostTeleport);
    }

    protected virtual void RemoveSourceListener(Transform source)
    {
        if (source) PortalPhysics.RemovePostTeleportListener(source, SourcePostTeleport);
        
        if (portable) _portable.enabled = true;
    }

    protected virtual void OnValidate()
    {
        Validate.FieldWithProperty(this, nameof(_source), nameof(source));
    }

    protected virtual void OnEnable()
    {
        AddSourceListener(source);
    }

    protected virtual void OnDisable()
    {
        RemoveSourceListener(source);
    }

    protected virtual void SourcePostTeleport(Teleportation args)
    {
        if (args.target != transform && args.fromPortal)
        {
            if (!portable || _portable.IsValid(args.fromPortal))
                PortalPhysics.Teleport(transform, args.fromPortal);
            else
                if (failed != null) failed.Invoke(args.fromPortal);
        }
    }
}
