using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit
{
    [DefaultExecutionOrder(300)]
    public class PortalProximityDisabler : MonoBehaviour
    {
        [SerializeField] private Portal _portal;
        public Portal portal
        {
            get => _portal;
            set => _portal = value;
        }

        [SerializeField] private PortalRendererBase _portalRenderer;
        public PortalRendererBase portalRenderer
        {
            get => _portalRenderer;
            set => _portalRenderer = value;
        }

        [SerializeField] private List<Collider> _colliders;
        public List<Collider> colliders => _colliders;

        [SerializeField] private bool _useDistanceThreshold = true;
        public bool useDistanceThreshold { get => _useDistanceThreshold; set => _useDistanceThreshold = value; }

        [ShowIf(nameof(_useDistanceThreshold))]
        [SerializeField] private float _distanceThreshold = 0.01f;
        public float distanceThreshold { get => _distanceThreshold; set => _distanceThreshold = value; }

        [SerializeField] private bool _useAngleThreshold = true;
        public bool useAngleThreshold { get => _useAngleThreshold; set => _useAngleThreshold = value; }

        [ShowIf(nameof(_useAngleThreshold))]
        [SerializeField] private float _angleThreshold = 1f;
        public float angleThreshold { get => _angleThreshold; set => _angleThreshold = value; }

        public void Reset()
        {
            _portal = GetComponentInChildren<Portal>();
            _portalRenderer = GetComponentInChildren<PortalRendererBase>();
            GetComponentsInChildren(_colliders);
        }

        public void LateUpdate()
        {
            bool state = GetState();

            if (_portalRenderer && _portalRenderer.enabled != state)
                _portalRenderer.enabled = state;

            foreach (Collider collider in _colliders)
                if (collider && collider.enabled != state)
                    collider.enabled = state;
        }

        private bool GetState()
        {
            if (_portal && _useDistanceThreshold || _useAngleThreshold)
            {
                if (_useDistanceThreshold)
                {
                    if (Vector3.Distance(transform.position, _portal.ModifyPoint(transform.position)) > _distanceThreshold)
                        return true;
                }

                if (_useAngleThreshold)
                {
                    if (Quaternion.Angle(transform.rotation, _portal.ModifyRotation(transform.rotation)) > _angleThreshold)
                        return true;
                }

                return false;
            }

            return true;
        }
    }
}
