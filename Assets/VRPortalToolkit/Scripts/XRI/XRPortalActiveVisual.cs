using UnityEngine;

namespace VRPortalToolkit.XRI
{
    [RequireComponent(typeof(XRPointAndPortal))]
    public class XRPortalActiveVisual : MonoBehaviour
    {
        [SerializeField] private GameObject _activeVisual;
        public GameObject activeVisual
        {
            get => _activeVisual;
            set
            {
                _activeVisual = value;

                if (Application.isPlaying)
                    SetupVisual(ref _activeVisual);
            }
        }

        [SerializeField] private GameObject _inactiveVisual;
        public GameObject inactiveVisual
        {
            get => _inactiveVisual;
            set
            {
                _inactiveVisual = value;

                if (Application.isPlaying)
                    SetupVisual(ref _inactiveVisual);
            }
        }

        private XRPointAndPortal _pointAndPortal;
        public XRPointAndPortal pointAndPortal => _pointAndPortal;

        protected virtual void Awake()
        {
            SetupVisual(ref _activeVisual);
            SetupVisual(ref _inactiveVisual);

            _pointAndPortal = GetComponent<XRPointAndPortal>();
        }

        protected virtual void OnDisable()
        {
            if (_activeVisual) _activeVisual.SetActive(false);
            if (_inactiveVisual) _inactiveVisual.SetActive(false);
        }

        protected virtual void LateUpdate()
        {
            if (_pointAndPortal && _pointAndPortal.isActivating)
                UpdateVisual(_activeVisual, _inactiveVisual);
            else
                UpdateVisual(_inactiveVisual, _activeVisual);
        }

        private static void SetupVisual(ref GameObject cursor)
        {
            if (cursor == null) return;

            // Instantiate if the reticle is a Prefab asset rather than a scene GameObject
            if (!cursor.scene.IsValid())
                cursor = Instantiate(cursor);

            cursor.SetActive(false);
        }

        private void UpdateVisual(GameObject validCursor, GameObject invalidCursor)
        {
            if (validCursor && !validCursor.activeSelf) validCursor.SetActive(true);
            if (invalidCursor && invalidCursor.activeSelf) invalidCursor.SetActive(false);
        }
    }
}
