using System;
using UnityEngine;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering
{
    /// <summary>
    /// Represents a clipping plane that can be used to clip rendering.
    /// </summary>
    [Serializable]
    public class ClippingPlane
    {
        /// <summary>
        /// The mode used to determine the clipping plane orientation.
        /// </summary>
        public enum Mode : sbyte
        {
            /// <summary>No clipping plane.</summary>
            None = 0,
            
            /// <summary>Clipping plane faces the camera.</summary>
            FaceCamera = 1,
            
            /// <summary>Clipping plane uses directional mode.</summary>
            Directional = 2,
            
            /// <summary>Clipping plane uses transform for orientation.</summary>
            Transform = 3
        }

        [SerializeField] private Mode _mode = Mode.Directional;
        /// <summary>
        /// The mode used to determine the clipping plane orientation.
        /// </summary>
        public Mode mode
        {
            get => _mode;
            set => _mode = value;
        }

        [SerializeField] private Direction _directions;
        /// <summary>
        /// The directions allowed for directional clipping mode.
        /// </summary>
        public Direction directions
        {
            get => _directions;
            set => _directions = value;
        }

        [SerializeField] private float _offset;
        /// <summary>
        /// The offset from the origin position.
        /// </summary>
        public float offset
        {
            get => _offset;
            set => _offset = value;
        }

        [SerializeField] private Transform _origin;
        /// <summary>
        /// The transform used as the origin of the clipping plane.
        /// </summary>
        public Transform origin
        {
            get => _origin;
            set => _origin = value;
        }

        /// <summary>
        /// Tries to get the clipping plane based on current settings.
        /// </summary>
        /// <param name="transform">The transform to use if origin is not set.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="clippingPlaneCentre">Output parameter for the center of the clipping plane.</param>
        /// <param name="clippingPlaneNormal">Output parameter for the normal of the clipping plane.</param>
        /// <returns>True if a valid clipping plane was found, false otherwise.</returns>
        public virtual bool TryGetClippingPlane(Transform transform, Vector3 cameraPosition, out Vector3 clippingPlaneCentre, out Vector3 clippingPlaneNormal)
        {
            Transform clippingPlane = _origin ? _origin : transform;

            if (clippingPlane)
            {
                switch (_mode)
                {
                    case Mode.Directional:

                        clippingPlaneNormal = GetDirection(cameraPosition, clippingPlane);
                        clippingPlaneCentre = clippingPlane.position + clippingPlaneNormal * _offset;

                        return true;

                    case Mode.FaceCamera:

                        clippingPlaneNormal = (cameraPosition - clippingPlane.position).normalized;
                        clippingPlaneCentre = clippingPlane.position + clippingPlaneNormal * _offset;

                        return true;

                    case Mode.Transform:

                        clippingPlaneNormal = (cameraPosition - clippingPlane.position).normalized;
                        clippingPlaneCentre = clippingPlane.position + clippingPlaneNormal * _offset;

                        return true;
                }
            }
            clippingPlaneCentre = clippingPlaneNormal = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Gets the direction vector for directional clipping mode.
        /// </summary>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="clippingPlane">The transform of the clipping plane.</param>
        /// <returns>The closest direction vector.</returns>
        protected virtual Vector3 GetDirection(Vector3 cameraPosition, Transform clippingPlane)
        {
            Vector3 direction = Vector3.zero, target = cameraPosition - clippingPlane.position;
            float distance = float.MaxValue;

            if (_directions.HasFlag(Direction.Left))
                ClosestDirection(-clippingPlane.right, target, ref direction, ref distance);
            if (_directions.HasFlag(Direction.Right))
                ClosestDirection(clippingPlane.right, target, ref direction, ref distance);
            if (_directions.HasFlag(Direction.Up))
                ClosestDirection(clippingPlane.up, target, ref direction, ref distance);
            if (_directions.HasFlag(Direction.Down))
                ClosestDirection(-clippingPlane.up, target, ref direction, ref distance);
            if (_directions.HasFlag(Direction.Back))
                ClosestDirection(-clippingPlane.forward, target, ref direction, ref distance);
            if (_directions.HasFlag(Direction.Forward))
                ClosestDirection(clippingPlane.forward, target, ref direction, ref distance);

            return direction;
        }

        /// <summary>
        /// Finds the closest direction to the target direction.
        /// </summary>
        /// <param name="direction">The direction to check.</param>
        /// <param name="targetDirection">The target direction.</param>
        /// <param name="bestDirection">Reference to the current best direction.</param>
        /// <param name="bestDistance">Reference to the current best distance.</param>
        protected virtual void ClosestDirection(Vector3 direction, Vector3 targetDirection, ref Vector3 bestDirection, ref float bestDistance)
        {
            float distance = Vector3.Angle(direction, targetDirection);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestDirection = direction;
            }
        }
    }
}