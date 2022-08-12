using Misc.Events;
using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(SphereCollider))]
public class SphereColliderExpander : MonoBehaviour
{
    private SphereCollider _collider;
    public new SphereCollider collider => _collider ? _collider : _collider = GetComponent<SphereCollider>();

    [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.WaitForFixedUpdate);
    public UpdateMask updateMask => _updateMask;
    protected Updater updater = new Updater();

    [SerializeField] private List<Transform> _sources;
    public List<Transform> sources
    {
        get => _sources;
        set => _sources = value;
    }

    [SerializeField] private float _border;
    public float border
    {
        get => _border;
        set => _border = value;
    }

    [Header("Events")]
    public SerializableEvent preUpdate = new SerializableEvent();
    public SerializableEvent postUpdate = new SerializableEvent();

    protected virtual void Awake()
    {
        updater.onInvoke = ForceApply;
        updater.updateMask = _updateMask;
    }

    protected virtual void OnEnable()
    {
        updater.enabled = true;
    }

    protected virtual void OnDisable()
    {
        updater.enabled = false;
    }

    public virtual void Apply()
    {
        if (isActiveAndEnabled && Application.isPlaying && !updater.isUpdating) ForceApply();
    }

    public virtual void ForceApply()
    {
        preUpdate?.Invoke();

        if (collider && sources.Count > 0)
        {
            int count = 0;
            float radius = 0f, distance;
            Vector3 worldCentre = Vector3.zero, vector;

            foreach (Transform source in _sources)
            {
                if (source)
                {
                    worldCentre += source.transform.position;
                    count++;
                }
            }

            // Don't divide by zero
            if (count == 0) return;

            worldCentre /= _sources.Count;

            foreach (Transform source in _sources)
            {
                if (source)
                {
                    distance = Vector3.Distance(worldCentre, source.position);

                    if (distance > radius) radius = distance;
                }
            }

            _collider.center = transform.InverseTransformPoint(worldCentre);

            // Find the longest side of the transform
            if (transform.localScale.x > transform.localScale.z)
            {
                if (transform.localScale.x > transform.localScale.y)
                    vector = transform.right;
                else
                    vector = transform.up;
            }
            if (transform.localScale.y > transform.localScale.z)
                vector = transform.up;
            else
                vector = transform.forward;

            _collider.radius = transform.InverseTransformVector(vector * radius).magnitude + border;
        }

        postUpdate?.Invoke();
    }
}
