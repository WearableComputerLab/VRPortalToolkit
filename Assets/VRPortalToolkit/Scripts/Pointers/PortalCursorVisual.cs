using UnityEngine;

namespace VRPortalToolkit
{
    public interface IPortalCursorRenderable
    {
        /*int cursorPortalsCount { get; }

        IPortal GetCursorPortal(int portalRayIndex);*/

        bool TryGetCursor(out Pose cursorPose, out bool isValidTarget);
    }

    public class PortalCursorVisual : MonoBehaviour
    {
        [SerializeField] private GameObject _validCursor;
        public GameObject validCursor
        {
            get => _validCursor;
            set
            {
                _validCursor = value;

                if (Application.isPlaying)
                    SetupCursor(ref _validCursor);
            }
        }

        [SerializeField] private GameObject _invalidCursor;
        public GameObject invalidCursor
        {
            get => _invalidCursor;
            set
            {
                _invalidCursor = value;

                if (Application.isPlaying)
                    SetupCursor(ref _invalidCursor);
            }
        }

        /*[SerializeField] private ScaleMode _scaleMode;

        public enum ScaleMode
        {
            Ignore = 0,
            Reset = 1,
            Apply = 2,
        }*/

        private IPortalCursorRenderable _cursorRenderable;
        public IPortalCursorRenderable cursorRenderable => _cursorRenderable;

        protected virtual void Awake()
        {
            SetupCursor(ref _validCursor);
            SetupCursor(ref _invalidCursor);

            _cursorRenderable = GetComponent<IPortalCursorRenderable>();
            if (_cursorRenderable == null) Debug.LogError("IPortalCursorRenderable not found!");
        }

        protected virtual void OnEnable()
        {
            // Nothing to do
        }

        protected virtual void OnDisable()
        {
            if (_validCursor) _validCursor.SetActive(false);
            if (_invalidCursor) _invalidCursor.SetActive(false);
        }

        protected virtual void LateUpdate()
        {
            if (_cursorRenderable != null && _cursorRenderable.TryGetCursor(out Pose cursorPose, out bool isValidTarget))
            {
                if (isValidTarget)
                    UpdateCursor(_validCursor, _invalidCursor, cursorPose);
                else
                    UpdateCursor(_invalidCursor, _validCursor, cursorPose);
            }
            else
            {
                if (_validCursor) _validCursor.SetActive(false);
                if (_invalidCursor) _invalidCursor.SetActive(false);
            }
        }

        private static void SetupCursor(ref GameObject cursor)
        {
            if (cursor == null) return;

            // Instantiate if the reticle is a Prefab asset rather than a scene GameObject
            if (!cursor.scene.IsValid())
                cursor = Instantiate(cursor);

            cursor.SetActive(false);
        }

        private void UpdateCursor(GameObject validCursor, GameObject invalidCursor, Pose cursorPose)
        {
            if (validCursor)
            {
                validCursor.transform.SetPositionAndRotation(cursorPose.position, cursorPose.rotation);
                validCursor.SetActive(true);
            }

            if (invalidCursor) invalidCursor.SetActive(false);
        }

        /*private IEnumerable<IPortal> GetCursorPortals()
        {
            if (_cursorRenderable != null)
            {
                for (int i = 0; i < _cursorRenderable.cursorPortalsCount; i++)
                    yield return _cursorRenderable.GetCursorPortal(i);
            }
        }*/
    }
}
