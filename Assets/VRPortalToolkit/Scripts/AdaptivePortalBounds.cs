using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit
{
    /// <summary>
    /// Processes and maintains the bounds of an adaptive portal based on tracked objects and their positions.
    /// </summary>
    public class AdaptivePortalBounds : MonoBehaviour, IAdaptivePortalProcessor
    {
        /// <summary>
        /// The portal associated with these bounds.
        /// </summary>
        [Tooltip("The portal associated with these bounds.")]
        [SerializeField] private Portal _portal;
        /// <summary>
        /// Gets or sets the portal associated with these bounds.
        /// </summary>
        public Portal portal
        {
            get => _portal;
            set
            {
                if (_portal != value)
                {
                    if (Application.isPlaying)
                    {
                        RemovePortalListener();
                        _portal = value;
                        AddPortalListener();
                    }
                    else
                        _portal = value;
                }
            }
        }

        /// <summary>
        /// Padding to apply around the calculated bounds.
        /// </summary>
        [Tooltip("Padding to apply around the calculated bounds.")]
        [SerializeField] private Vector2 _padding = new Vector2(0.1f, 0.1f);
        /// <summary>
        /// Gets or sets the padding to apply around the calculated bounds.
        /// </summary>
        public Vector2 padding
        {
            get => _padding;
            set => _padding = value;
        }

        /// <summary>
        /// Gets the order of the adaptive portal processor.
        /// </summary>
        int IAdaptivePortalProcessor.Order => 0;

        private readonly List<PortalRelativePosition> _positionings = new List<PortalRelativePosition>();

        protected virtual void Reset()
        {
            _portal = GetComponentInChildren<Portal>();
        }

        protected virtual void OnEnable()
        {
            AddPortalListener();
        }

        protected virtual void LateUpdate()
        {
            _positionings.RemoveAll(IsInvalid);
        }

        protected virtual void OnDisable()
        {
            RemovePortalListener();
        }

        private bool IsInvalid(PortalRelativePosition positioning)
        {
            if (positioning)
            {
                if (!positioning.GetPortalsFromSource().Contains(_portal))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a listener to the portal's postTeleport event.
        /// </summary>
        private void AddPortalListener()
        {
            if (_portal != null) _portal.postTeleport += OnPortalPostTeleport;
        }

        /// <summary>
        /// Removes the listener from the portal's postTeleport event.
        /// </summary>
        private void RemovePortalListener()
        {
            if (_portal != null) _portal.postTeleport -= OnPortalPostTeleport;
        }

        /// <summary>
        /// Handles the portal's postTeleport event.
        /// </summary>
        /// <param name="teleportation">The teleportation data.</param>
        private void OnPortalPostTeleport(Teleportation teleportation)
        {
            if (teleportation.target && teleportation.target.gameObject.TryGetComponent(out PortalRelativePosition positioning))
                _positionings.Add(positioning);
        }

        /// <summary>
        /// Processes the adaptive portal transform based on the tracked positionings.
        /// </summary>
        /// <param name="apTransform">The adaptive portal transform to process.</param>
        void IAdaptivePortalProcessor.Process(ref AdaptivePortalTransform apTransform)
        {
            if (!isActiveAndEnabled) return;

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue),
                    max = new Vector2(float.MinValue, float.MinValue);

            foreach (PortalRelativePosition positioning in _positionings)
            {
                if (positioning && positioning.origin && TryGetPortalIndex(positioning, out int index))
                {
                    Vector3 startPos = positioning.origin.position, endPos = positioning.transform.position;

                    for (int i = 0; i < index; i++)
                        positioning.GetPortalFromSource(i)?.ModifyPoint(ref startPos);

                    for (int i = 0; i < positioning.portalCount - index; i++)
                        positioning.GetPortalToSource(i)?.ModifyPoint(ref endPos);

                    Plane plane = new Plane(transform.forward, transform.position);

                    Ray ray = new Ray(startPos, endPos - startPos);

                    Debug.DrawLine(startPos, endPos, Color.black);

                    if (plane.Raycast(ray, out float enter) && enter < Vector3.Distance(startPos, endPos))
                    {
                        Vector2 pos = transform.InverseTransformPoint(ray.GetPoint(enter));

                        min = Vector2.Min(min, pos - _padding);
                        max = Vector2.Max(max, pos + _padding);
                    }
                }
            }

            if (min.x <= max.x && min.y <= max.y)
                apTransform.AddMinMax(min, max);
        }

        /// <summary>
        /// Tries to get the index of the portal in the positioning's portal list.
        /// </summary>
        /// <param name="positioning">The positioning to check.</param>
        /// <param name="index">The index of the portal if found; otherwise, -1.</param>
        /// <returns>True if the portal index was found; otherwise, false.</returns>
        private bool TryGetPortalIndex(PortalRelativePosition positioning, out int index)
        {
            for (int i = 0; i < positioning.portalCount; i++)
            {
                Portal portal = positioning.GetPortalFromSource(i);
                if (portal == _portal)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }
    }
}
