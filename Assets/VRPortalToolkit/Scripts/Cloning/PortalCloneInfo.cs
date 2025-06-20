using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    /// <summary>
    /// Represents a clone relationship between two components, with information about the portals between the clones.
    /// </summary>
    /// <typeparam name="TComponent">The type of component being cloned.</typeparam>
    public struct PortalCloneInfo<TComponent> : IEquatable<PortalCloneInfo<TComponent>> where TComponent : Component
    {
        /// <summary>
        /// The clone component.
        /// </summary>
        public TComponent clone;
        
        /// <summary>
        /// The original component.
        /// </summary>
        public TComponent original;

        private Portal[] _originalToClone;

        /// <summary>
        /// Creates a new portal clone information container.
        /// </summary>
        /// <param name="original">The original component.</param>
        /// <param name="clone">The clone component.</param>
        /// <param name="originalToClone">The portals used for cloning from original to clone.</param>
        public PortalCloneInfo(TComponent original, TComponent clone, Portal[] originalToClone)
        {
            this.original = original;
            this.clone = clone;
            _originalToClone = originalToClone;
        }

        /// <summary>
        /// Attempts to convert this clone info to a different component type.
        /// </summary>
        /// <typeparam name="T">The target component type.</typeparam>
        /// <param name="asT">The resulting converted clone info.</param>
        /// <returns>True if conversion was successful, false otherwise.</returns>
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

        /// <summary>
        /// Converts this clone info to a different component type. May return a default value if conversion fails.
        /// </summary>
        /// <typeparam name="T">The target component type.</typeparam>
        /// <returns>The converted clone info.</returns>
        public PortalCloneInfo<T> As<T>() where T : Component
        {
            TryAs(out PortalCloneInfo<T> cloneInfo);
            return cloneInfo;
        }

        /// <summary>
        /// Gets the number of portals in the chain from original to clone.
        /// </summary>
        public int PortalCount => _originalToClone != null ? _originalToClone.Length : 0;

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is PortalCloneInfo<TComponent> info && Equals(info);
        }
        
        /// <summary>
        /// Determines whether the specified clone info is equal to the current one.
        /// </summary>
        /// <param name="other">The clone info to compare with the current one.</param>
        /// <returns>True if the specified clone info is equal to the current one; otherwise, false.</returns>
        public bool Equals(PortalCloneInfo<TComponent> other) => Equals<TComponent>(other);

        /// <summary>
        /// Determines whether the specified clone info of a different type is equal to the current one.
        /// </summary>
        /// <typeparam name="T">The component type of the other clone info.</typeparam>
        /// <param name="other">The clone info to compare with the current one.</param>
        /// <returns>True if the specified clone info is equal to the current one; otherwise, false.</returns>
        public bool Equals<T>(PortalCloneInfo<T> other) where T : Component
        {
            return clone == other.clone && original == other.original && _originalToClone.Equals(other._originalToClone);
        }

        /// <summary>
        /// Gets the portal at the specified index in the chain from original to clone.
        /// </summary>
        /// <param name="index">The index of the portal to retrieve.</param>
        /// <returns>The portal at the specified index.</returns>
        public Portal GetOriginalToClonePortal(int index) => _originalToClone[index];

        /// <summary>
        /// Gets the portal at the specified index in the chain from clone to original (reverse direction).
        /// </summary>
        /// <param name="index">The index of the portal to retrieve.</param>
        /// <returns>The portal at the specified index.</returns>
        public Portal GetCloneToOriginalPortal(int index)
        {
            Portal other = _originalToClone[_originalToClone.Length - index - 1];

            if (other != null) return other.connected;

            return null;
        }

        /// <summary>
        /// Gets all portals in the chain from original to clone.
        /// </summary>
        /// <returns>An enumerable of portals from original to clone.</returns>
        public IEnumerable<Portal> GetOriginalToClonePortals()
        {
            for (int i = 0; i < PortalCount; i++)
                yield return GetOriginalToClonePortal(i);
        }

        /// <summary>
        /// Gets all portals in the chain from clone to original (reverse direction).
        /// </summary>
        /// <returns>An enumerable of portals from clone to original.</returns>
        public IEnumerable<Portal> GetCloneToOriginalPortals()
        {
            for (int i = 0; i < PortalCount; i++)
                yield return GetCloneToOriginalPortal(i);
        }

        /// <summary>
        /// Generates a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hashCode = -1644474227;
            hashCode = hashCode * -1521134295 + EqualityComparer<TComponent>.Default.GetHashCode(clone);
            hashCode = hashCode * -1521134295 + EqualityComparer<TComponent>.Default.GetHashCode(original);
            hashCode = hashCode * -1521134295 + EqualityComparer<Portal[]>.Default.GetHashCode(_originalToClone);
            return hashCode;
        }

        /// <summary>
        /// Implicitly converts a PortalCloneInfo to a boolean indicating if both original and clone are valid.
        /// </summary>
        /// <param name="exists">The PortalCloneInfo to convert.</param>
        /// <returns>True if both original and clone are not null; otherwise, false.</returns>
        public static implicit operator bool(PortalCloneInfo<TComponent> exists)
        {
            return exists.original && exists.clone;
        }
    }
}
