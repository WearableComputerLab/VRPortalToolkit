using System;
using UnityEngine;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering
{
    [Serializable]
    public class ClippingPlane
    {
        public enum Mode : sbyte
        {
            None = 0,
            FaceCamera = 1,
            Directional = 2,
            Transform = 3
        }

        [SerializeField] private Mode _mode = Mode.Directional;
        public Mode mode
        {
            get => _mode;
            set => _mode = value;
        }

        [SerializeField] private Direction _directions;
        public Direction directions
        {
            get => _directions;
            set => _directions = value;
        }

        [SerializeField] private float _offset;
        public float offset
        {
            get => _offset;
            set => _offset = value;
        }

        [SerializeField] private Transform _origin;
        public Transform origin
        {
            get => _origin;
            set => _origin = value;
        }

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