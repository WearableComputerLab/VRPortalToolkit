using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Examples
{
    public class OrbContainer : MonoBehaviour
    {
        private static readonly WaitForFixedUpdate _WaitForFixedUpdate = new WaitForFixedUpdate();
        //public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Transform[] _orbs;
        public Transform[] orbs
        {
            get => _orbs;
            set => _orbs = value;
        }

        private bool _isValid = false;
        public bool isValid => _isValid;

        protected readonly TriggerHandler<Transform> _triggerHandler = new TriggerHandler<Transform>();
        protected readonly HashSet<Collider> _stayedColliders = new HashSet<Collider>();
        private IEnumerator _waitFixedUpdateLoop;

        protected void Awake()
        {
            _waitFixedUpdateLoop = WaitFixedUpdateLoop();
        }

        protected void OnEnable()
        {
            _isValid = false;

            _triggerHandler.valueAdded += OnTriggerEnterContainer;
            _triggerHandler.valueRemoved += OnTriggerExitContainer;
            StartCoroutine(_waitFixedUpdateLoop);
        }

        protected void OnDisable()
        {
            _isValid = false;

            _triggerHandler.valueAdded -= OnTriggerEnterContainer;
            _triggerHandler.valueRemoved -= OnTriggerExitContainer;
            StopCoroutine(_waitFixedUpdateLoop);
        }

        protected void OnTriggerEnter(Collider other)
        {
            AddContainer(other);
        }

        protected void OnTriggerStay(Collider other)
        {
            if (!_triggerHandler.HasCollider(other))
                AddContainer(other);

            _stayedColliders.Add(other);
        }

        private void AddContainer(Collider other)
        {
            _triggerHandler.Add(other, other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform);
        }

        protected void OnTriggerExit(Collider other)
        {
            _triggerHandler.RemoveCollider(other);
        }

        private IEnumerator WaitFixedUpdateLoop()
        {
            while (true)
            {
                yield return _WaitForFixedUpdate;

                _triggerHandler.UpdateColliders(_stayedColliders);
                _stayedColliders.Clear();
            }
        }

        private void OnTriggerEnterContainer(Transform _) => UpdateState();

        private void OnTriggerExitContainer(Transform _) => UpdateState();

        private void UpdateState()
        {
            if (_orbs == null) return;

            foreach (Transform orb in _orbs)
            {
                if (!_triggerHandler.HasValue(orb))
                {
                    _isValid = false;
                    return;
                }
            }

            _isValid = true;
        }
    }
}
