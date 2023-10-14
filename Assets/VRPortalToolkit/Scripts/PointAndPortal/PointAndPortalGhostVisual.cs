using UnityEngine;

namespace VRPortalToolkit.PointAndPortal
{
    public class PointAndPortalGhostVisual : MonoBehaviour
    {

        [SerializeField] private GameObject _validGhost;
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

        [SerializeField] private GameObject _invalidGhost;
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

        private static void SetupGhost(ref GameObject ghost)
        {
            if (ghost == null) return;

            // Instantiate if the reticle is a Prefab asset rather than a scene GameObject
            if (!ghost.scene.IsValid())
                ghost = Instantiate(ghost);

            ghost.SetActive(false);
        }

        private void UpdateGhost(GameObject validGhost, GameObject invalidGhost, Pose ghostPose)
        {
            if (validGhost)
            {
                validGhost.transform.SetPositionAndRotation(ghostPose.position, ghostPose.rotation);
                validGhost.SetActive(true);
            }

            if (invalidGhost) invalidGhost.SetActive(false);
        }
    }
}
