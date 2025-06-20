using UnityEngine;

namespace VRPortalToolkit
{
    /// <summary>
    /// Interface for components that can render a cursor at a portal ray's hit point.
    /// </summary>
    public interface IPortalCursorRenderable
    {
        /// <summary>
        /// Tries to get the current cursor position and orientation.
        /// </summary>
        /// <param name="cursorPose">Output parameter for the cursor pose (position and rotation).</param>
        /// <param name="isValidTarget">Output parameter indicating whether the cursor is over a valid target.</param>
        /// <returns>True if a cursor pose was found, false otherwise.</returns>
        bool TryGetCursor(out Pose cursorPose, out bool isValidTarget);
    }

    /// <summary>
    /// Manages cursor visuals for portal pointers, showing different cursors for valid and invalid targets.
    /// </summary>
    public class PortalCursorVisual : MonoBehaviour
    {
        [Tooltip("The cursor to show when pointing at a valid target.")]
        [SerializeField] private GameObject _validCursor;
        /// <summary>
        /// The cursor to show when pointing at a valid target.
        /// </summary>
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

        [Tooltip("The cursor to show when pointing at an invalid target.")]
        [SerializeField] private GameObject _invalidCursor;
        /// <summary>
        /// The cursor to show when pointing at an invalid target.
        /// </summary>
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

        private IPortalCursorRenderable _cursorRenderable;
        /// <summary>
        /// The IPortalCursorRenderable component that provides cursor data.
        /// </summary>
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

        /// <param name="cursor">Reference to the cursor GameObject to set up.</param>
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

    }
}
