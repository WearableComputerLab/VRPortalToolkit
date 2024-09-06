using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using static UnityEngine.Camera;

namespace VRPortalToolkit.XRI
{
    [DefaultExecutionOrder(2)]
    public class XRAdaptivePortalRestriction : MonoBehaviour, IAdaptivePortalProcessor
    {
        private readonly static WaitForFixedUpdate _WaitForFixedUpdate = new WaitForFixedUpdate();

        [SerializeField] private XRAdaptivePortalRestriction _connected;
        public XRAdaptivePortalRestriction connected
        {
            get => _connected;
            set => _connected = value;
        }

        [SerializeField] private XRPortalInteractable _portalInteractable;
        public XRPortalInteractable portalInteractable
        {
            get => _portalInteractable;
            set => _portalInteractable = value;
        }

        [SerializeField] private bool _restrictWhileSelected = true;
        public bool restrictWhileSelected
        {
            get => _restrictWhileSelected;
            set => _restrictWhileSelected = value;
        }

        [SerializeField] private float _distance = 0.01f;
        public float distance
        {
            get => _distance;
            set => _distance = value;
        }

        int IAdaptivePortalProcessor.Order => 10;

        protected readonly TriggerHandler<Camera> triggerHandler = new TriggerHandler<Camera>();
        protected readonly HashSet<Collider> _stayedColliders = new HashSet<Collider>();
        private IEnumerator _waitFixedUpdateLoop;
        private bool _isPrimary = false;

        protected virtual void Awake()
        {
            _waitFixedUpdateLoop = WaitFixedUpdateLoop();
        }

        protected virtual void OnEnable()
        {
            StartCoroutine(_waitFixedUpdateLoop);
        }

        protected virtual void OnDisable()
        {
            StopCoroutine(_waitFixedUpdateLoop);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            AddContainer(other);
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            if (!triggerHandler.HasCollider(other))
                AddContainer(other);

            _stayedColliders.Add(other);
        }

        private void AddContainer(Collider other)
        {
            if (!other) return;

            XROrigin origin = other.GetComponentInParent<XROrigin>();

            if (origin && origin.Camera && (origin.Camera.transform == other.transform || origin.Camera.transform.IsChildOf(other.transform)))
                triggerHandler.Add(other, origin.Camera);
            else
                triggerHandler.Add(other, null);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            triggerHandler.RemoveCollider(other);
        }

        private IEnumerator WaitFixedUpdateLoop()
        {
            while (true)
            {
                yield return _WaitForFixedUpdate;

                triggerHandler.UpdateColliders(_stayedColliders);
                _stayedColliders.Clear();
            }
        }

        void IAdaptivePortalProcessor.Process(ref AdaptivePortalTransform apTransform)
        {
            // Plane = offset, Offset is secondary goal

            if (!isActiveAndEnabled) return;

            bool shouldRun = ShouldRun(), connectedShouldRun = _connected && _connected.ShouldRun();

            // Only one can run
            if (shouldRun == connectedShouldRun) return;

            if (shouldRun)
            {
                Plane plane = new Plane(_portalInteractable.transform.forward, _portalInteractable.transform.TransformPoint(new Vector3(0f, 0f, apTransform.entryDepth)));

                float distance = 0f;

                foreach (Camera camera in triggerHandler.Values)
                {
                    Vector3 cameraCorner = camera.ViewportToWorldPoint(new Vector3(1f, 1f, camera.nearClipPlane), camera.stereoEnabled ? MonoOrStereoscopicEye.Right : MonoOrStereoscopicEye.Mono);
                    float cameraSize = Vector3.Distance(camera.transform.position, cameraCorner);
                    distance = Mathf.Min(distance, plane.GetDistanceToPoint(camera.transform.position - plane.normal * (cameraSize + _distance)));
                }

                apTransform.entryDepth += distance;
                apTransform.exitDepth += distance;
            }
        }

        private bool ShouldRun()
        {
            if (isActiveAndEnabled && _portalInteractable)
            {
                if (!_restrictWhileSelected && _portalInteractable.isSelected)
                    return false;

                if (triggerHandler.HasValue(null))
                    return triggerHandler.Count > 1;

                return triggerHandler.Count > 0;
            }

            return false;
        }

        /*private void SetOffset(float z)
        {
            if (_offset)
            {
                Vector3 localPos = _offset.localPosition;
                _offset.localPosition = new Vector3(localPos.x, localPos.y, z);
            }
        }*/
    }
}
