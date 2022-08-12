using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    public struct PortalCloneInfo<TComponent> : IEquatable<PortalCloneInfo<TComponent>> where TComponent : Component
    {
        public TComponent clone;
        public TComponent original;

        private Portal[] _originalToClone;

        public PortalCloneInfo(TComponent original, TComponent clone, Portal[] originalToClone)
        {
            this.original = original;
            this.clone = clone;
            _originalToClone = originalToClone;
        }

        public bool TryAs<T>(out PortalCloneInfo<T> asT) where T : Component
        {
            if (original is T originalT && clone is T otherT)
            {
                asT = new PortalCloneInfo<T>(originalT, otherT, _originalToClone);
                return true;
            }

            asT = default(PortalCloneInfo<T>);
            return false;
        }

        public PortalCloneInfo<T> As<T>() where T : Component
        {
            TryAs(out PortalCloneInfo<T> cloneInfo);
            return cloneInfo;
        }

        public int PortalCount => _originalToClone != null ? _originalToClone.Length : 0;

        public override bool Equals(object obj)
        {
            return obj is PortalCloneInfo<TComponent> info && Equals(info);
        }
        public bool Equals(PortalCloneInfo<TComponent> other) => Equals<TComponent>(other);

        public bool Equals<T>(PortalCloneInfo<T> other) where T : Component
        {
            return clone == other.clone && original == other.original && _originalToClone.Equals(other._originalToClone);
        }

        public Portal GetOriginalToClonePortal(int index) => _originalToClone[index];

        public Portal GetCloneToOriginalPortal(int index)
        {
            Portal other = _originalToClone[_originalToClone.Length - index - 1];

            if (other != null) return other.connectedPortal;

            return null;
        }

        public IEnumerable<Portal> GetOriginalToClonePortals()
        {
            for (int i = 0; i < PortalCount; i++)
                yield return GetOriginalToClonePortal(i);
        }

        public IEnumerable<Portal> GetCloneToOriginalPortals()
        {
            for (int i = 0; i < PortalCount; i++)
                yield return GetCloneToOriginalPortal(i);
        }

        public override int GetHashCode()
        {
            int hashCode = -1644474227;
            hashCode = hashCode * -1521134295 + EqualityComparer<TComponent>.Default.GetHashCode(clone);
            hashCode = hashCode * -1521134295 + EqualityComparer<TComponent>.Default.GetHashCode(original);
            hashCode = hashCode * -1521134295 + EqualityComparer<Portal[]>.Default.GetHashCode(_originalToClone);
            return hashCode;
        }

        public static implicit operator bool(PortalCloneInfo<TComponent> exists)
        {
            return exists.original && exists.clone;
        }
    }
}
