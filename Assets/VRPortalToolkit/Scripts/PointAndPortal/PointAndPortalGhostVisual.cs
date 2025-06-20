using UnityEngine;

namespace VRPortalToolkit.PointAndPortal
{
    /// <summary>
    /// Manages ghost visualizations for prediciting the portal's position during Point & Portal.
    /// Shows different visual representations for valid and invalid teleport targets.
    /// </summary>
    public class PointAndPortalGhostVisual : MonoBehaviour
    {
        [SerializeField, Tooltip("The GameObject to display when the teleport target is valid.")]
        private GameObject _validGhost;
        /// <summary>
        /// The GameObject to display when the teleport target is valid.
        /// </summary>
        public GameObject validGhost
        {
            get => _validGhost;
            set
            {
                _validGhost = value;

                if (Application.isPlaying)
                    SetupGhost(ref _validGhost);
            }
        }

        [SerializeField, Tooltip("The GameObject to display when the teleport target is invalid.")]
        private GameObject _invalidGhost;
        /// <summary>
        /// The GameObject to display when the teleport target is invalid.
        /// </summary>
        public GameObject invalidGhost
        {
            get => _invalidGhost;
            set
            {
                _invalidGhost = value;

                if (Application.isPlaying)
                    SetupGhost(ref _invalidGhost);
            }
        }

        /*[SerializeField] private ScaleMode _scaleMode;

        public enum ScaleMode
        {
            Ignore = 0,
            Reset = 1,
            Apply = 2,
        }*/

        private IPointAndPortal _pointAndPortal;
        /// <summary>
        /// Reference to the point-and-portal system.
        /// </summary>
        public IPointAndPortal pointAndPortal => _pointAndPortal;

        protected virtual void Awake()
        {
            SetupGhost(ref _validGhost);
            SetupGhost(ref _invalidGhost);

            _pointAndPortal = GetComponent<IPointAndPortal>();
            if (_pointAndPortal == null) Debug.LogError("IPointAndPortal not found!");
        }

        protected virtual void OnEnable()
        {
            // Nothing to do
        }

        protected virtual void OnDisable()
        {
            if (_validGhost) _validGhost.SetActive(false);
            if (_invalidGhost) _invalidGhost.SetActive(false);
        }

        protected virtual void LateUpdate()
        {
            if (_pointAndPortal != null && _pointAndPortal.TryGetTeleportConnectedPose(out Pose connectPose, out bool isValidTarget))
            {
                if (isValidTarget)
                    UpdateGhost(_validGhost, _invalidGhost, connectPose);
                else
                    UpdateGhost(_invalidGhost, _validGhost, connectPose);
            }
            else
            {
                if (_validGhost) _validGhost.SetActive(false);
                if (_invalidGhost) _invalidGhost.SetActive(false);
            }
        }

        /// <summary>
        /// Sets up a ghost GameObject, instantiating it if it's a prefab.
        /// </summary>
        /// <param name="ghost">Reference to the ghost GameObject.</param>
        private static void SetupGhost(ref GameObject ghost)
        {
            if (ghost == null) return;

            // Instantiate if the reticle is a Prefab asset rather than a scene GameObject
            if (!ghost.scene.IsValid())
                ghost = Instantiate(ghost);

            ghost.SetActive(false);
        }

        /// <summary>
        /// Updates the visible ghost and its transform to match the target pose.
        /// </summary>
        /// <param name="visibleGhost">The ghost to make visible.</param>
        /// <param name="hiddenGhost">The ghost to hide.</param>
        /// <param name="ghostPose">The pose to apply to the visible ghost.</param>
        private void UpdateGhost(GameObject visibleGhost, GameObject hiddenGhost, Pose ghostPose)
        {
            if (visibleGhost)
            {
                visibleGhost.transform.SetPositionAndRotation(ghostPose.position, ghostPose.rotation);
                visibleGhost.SetActive(true);
            }

            if (hiddenGhost) hiddenGhost.SetActive(false);
        }
    }
}
