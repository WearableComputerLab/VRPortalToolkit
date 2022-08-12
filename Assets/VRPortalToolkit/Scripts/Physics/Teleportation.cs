using Misc;
using System;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Portables;

namespace VRPortalToolkit.Physics
{
    public delegate void TeleportAction(Teleportation args);

    public sealed class Teleportation : IEquatable<Teleportation>
    {
        private static ObjectPool<Teleportation> _pool = new ObjectPool<Teleportation>(() => new Teleportation(), null, null, null);

        internal static Teleportation Get() => _pool.Get();

        internal static Teleportation Get(Component source, Transform target, Transform transform, Portal fromPortal)
        {
            Teleportation teleport = _pool.Get();
            teleport.Set(source, target, transform, fromPortal);
            return teleport;
        }

        internal static Teleportation Get(Teleportation other)
        {
            Teleportation teleport = _pool.Get();
            teleport.Set(other);
            return teleport;
        }

        internal static void Release(Teleportation teleport)
        {
            if (teleport != null)
            {
                teleport.Clear();
                _pool.Release(teleport);
            }
        }

        private Component _source;
        public Component source => _source;

        private Transform _target;
        public Transform target => _target;

        private Transform _transform;
        public Transform transform => _transform;

        private Portal _fromPortal;
        public Portal fromPortal => _fromPortal;

        public Teleportation() { }

        public Teleportation(Component source, Transform target, Transform transform, Portal fromPortal)
            => Set(source, target, transform, fromPortal);

        public Teleportation(Teleportation other) => Set(other);

        internal void Clear() => Set(null, null, null, null);

        internal void Set(Teleportation other)
        {
            if (other != null)
                Set(other.source, other.target, other.transform, other.fromPortal);
            else
                Clear();
        }

        internal void Set(Component source, Transform target, Transform transform, Portal fromPortal)
        {
            _source = source;
            _target = target;
            _transform = transform;
            _fromPortal = fromPortal;
        }

        public override bool Equals(object other)
        {
            return other is Teleportation teleport && Equals(teleport); 
        }

        public bool Equals(Teleportation other)
        {
            return other != null && _source == other._source && _target == other._target
                && _transform == other._transform && _fromPortal == other._fromPortal;
        }

        public override int GetHashCode()
        {
            int hashCode = -1416235152;
            hashCode = hashCode * -1521134295 + EqualityComparer<Component>.Default.GetHashCode(_source);
            hashCode = hashCode * -1521134295 + EqualityComparer<Transform>.Default.GetHashCode(_target);
            hashCode = hashCode * -1521134295 + EqualityComparer<Transform>.Default.GetHashCode(_transform);
            hashCode = hashCode * -1521134295 + EqualityComparer<Portal>.Default.GetHashCode(_fromPortal);
            return hashCode;
        }

        public static bool operator ==(Teleportation lhs, Teleportation rhs)
        {
            if (lhs is null) return rhs is null;

            return lhs.Equals(rhs);
        }

        public static bool operator !=(Teleportation lhs, Teleportation rhs) => !(lhs == rhs);
    }
}