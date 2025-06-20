using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    /// <summary>
    /// Provides utility methods for cloning components and GameObjects through portals.
    /// When an object is near a portal, a clone should be used to mimic the object on the other side of the protal.
    /// </summary>
    public static partial class PortalCloning
    {
        private static Dictionary<Component, PortalCloneInfo<Component>> _cloneInfos = new Dictionary<Component, PortalCloneInfo<Component>>();
        private static Dictionary<Component, Component[]> _clonesByOriginal = new Dictionary<Component, Component[]>();

        #region Component Cloning

        /// <summary>
        /// Adds a clone mapping between an original component and its clone.
        /// </summary>
        /// <param name="original">The original component.</param>
        /// <param name="clone">The clone component.</param>
        /// <param name="originalToClone">The array of portals used for cloning from original to clone.</param>
        public static void AddClone(Component original, Component clone, Portal[] originalToClone = null)
            => AddClone(new PortalCloneInfo<Component>(original, clone, originalToClone));

        /// <summary>
        /// Adds a clone mapping using a PortalCloneInfo structure.
        /// </summary>
        /// <typeparam name="TComponent">The type of component being cloned.</typeparam>
        /// <param name="cloneInfo">The clone info to add.</param>
        public static void AddClone<TComponent>(PortalCloneInfo<TComponent> cloneInfo) where TComponent : Component
        {
            if (cloneInfo.TryAs(out PortalCloneInfo<Component> cloneInfo2))
                AddClone(cloneInfo2);
        }

        /// <summary>
        /// Adds a clone mapping using a PortalCloneInfo structure for a Component.
        /// </summary>
        /// <param name="cloneInfo">The clone info to add.</param>
        public static void AddClone(PortalCloneInfo<Component> cloneInfo)
        {
            Component original = cloneInfo.original, clone = cloneInfo.clone;

            if (original != null && clone != null)
            {
                // Remove original just in case
                if (_cloneInfos.TryGetValue(clone, out PortalCloneInfo<Component> previousCloneInfo) && previousCloneInfo.original != original)
                {
                    if (_clonesByOriginal.TryGetValue(previousCloneInfo.original, out Component[] originalClones))
                    {
                        if (Remove(ref originalClones, clone))
                        {
                            if (originalClones == null) _clonesByOriginal.Remove(previousCloneInfo.original);
                            else _clonesByOriginal[previousCloneInfo.original] = originalClones;
                        }
                    }
                }
                _cloneInfos[clone] = cloneInfo;
                _clonesByOriginal.TryGetValue(original, out Component[] clones);

                if (Add(ref clones, clone)) _clonesByOriginal[original] = clones;
            }
        }

        /// <summary>
        /// Adds a clone mapping between an original component and its clone with a single portal.
        /// </summary>
        /// <param name="original">The original component.</param>
        /// <param name="clone">The clone component.</param>
        /// <param name="portalToClone">The portal used for cloning from original to clone.</param>
        public static void AddClone(Component original, Component clone, Portal portalToClone)
            => AddClone(original, clone, portalToClone != null ? new Portal[] { portalToClone } : null);

        /// <summary>
        /// Removes a clone mapping for a specific clone component.
        /// </summary>
        /// <param name="clone">The clone component to remove mapping for.</param>
        public static void RemoveClone(Component clone)
        {
            if (_cloneInfos.TryGetValue(clone, out PortalCloneInfo<Component> originalCloneInfo))
            {
                _cloneInfos.Remove(clone);

                if (_clonesByOriginal.TryGetValue(originalCloneInfo.original, out Component[] clones))
                {
                    if (Remove(ref clones, clone))
                    {
                        if (clones == null) _clonesByOriginal.Remove(originalCloneInfo.original);
                        else _clonesByOriginal[originalCloneInfo.original] = clones;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the original component for a given clone.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="clone">The clone component.</param>
        /// <returns>The original component, or the clone itself if no mapping exists.</returns>
        public static T GetOriginal<T>(T clone) where T : Component
        {
            GetOriginal(ref clone);
            return clone;
        }

        /// <summary>
        /// Gets the original component for a given clone by reference.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="clone">The clone component reference, replaced with the original if found.</param>
        /// <returns>True if the original was found, false otherwise.</returns>
        public static bool GetOriginal<T>(ref T clone) where T : Component
        {
            if (clone == null) return false;

            if (_cloneInfos.TryGetValue(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                if (cloneInfo.original is T original)
                {
                    clone = original;
                    return true;
                }
            }

            return clone;
        }

        /// <summary>
        /// Tries to get the clone info for a specific clone component.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="clone">The clone component.</param>
        /// <param name="info">The output clone info if found.</param>
        /// <returns>True if the clone info was found, false otherwise.</returns>
        public static bool TryGetCloneInfo<T>(T clone, out PortalCloneInfo<T> info) where T : Component
        {
            if (clone != null)
            {
                if (_cloneInfos.TryGetValue(clone, out PortalCloneInfo<Component> cloneInfo) && cloneInfo.TryAs(out info))
                    return true;
            }

            info = default(PortalCloneInfo<T>);
            return false;
        }

        /// <summary>
        /// Tries to get the original component for a specific clone component.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="clone">The clone component.</param>
        /// <param name="original">The output original component if found.</param>
        /// <returns>True if the original was found, false otherwise.</returns>
        public static bool TryGetOriginal<T>(T clone, out T original) where T : Component
        {
            if (clone != null)
            {
                if (_cloneInfos.TryGetValue(clone, out PortalCloneInfo<Component> cloneInfo) && cloneInfo.original is T asT)
                {
                    original = asT;
                    return true;
                }
            }

            original = null;
            return false;
        }

        /// <summary>
        /// Tries to get all clones for a specific original component.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="original">The original component.</param>
        /// <param name="clones">The output array of clones if found.</param>
        /// <returns>True if clones were found, false otherwise.</returns>
        public static bool TryGetClones<T>(T original, out T[] clones) where T : Component
        {
            if (original != null && _clonesByOriginal.TryGetValue(original, out Component[] componentClones))
            {
                int actualLength = 0;

                foreach (Component clone in componentClones)
                    if (clone is T) actualLength++;

                clones = new T[actualLength];

                int index = 0;

                foreach (Component clone in componentClones)
                    if (clone is T asT) clones[index++] = asT;

                return true;
            }

            clones = null;
            return false;
        }

        /// <summary>
        /// Gets all clones for a specific original component.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="original">The original component.</param>
        /// <returns>An enumerable of all clones for the original component.</returns>
        public static IEnumerable<T> GetClones<T>(T original) where T : Component
        {
            if (original != null && _clonesByOriginal.TryGetValue(original, out Component[] componentClones))
            {
                foreach (Component clone in componentClones)
                    if (clone is T cloneT) yield return cloneT;
            }
        }

        /// <summary>
        /// Tries to get all clone infos for a specific original component.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="original">The original component.</param>
        /// <param name="infos">The output array of clone infos if found.</param>
        /// <returns>True if clone infos were found, false otherwise.</returns>
        public static bool TryGetCloneInfos<T>(T original, out PortalCloneInfo<T>[] infos) where T : Component
        {
            if (original != null)
            {
                if (_clonesByOriginal.TryGetValue(original, out Component[] componentClones))
                {
                    int actualLength = 0;

                    foreach (Component clone in componentClones)
                        if (clone is T) actualLength++;

                    infos = new PortalCloneInfo<T>[actualLength];

                    int index = 0;

                    foreach (Component clone in componentClones)
                    {
                        _cloneInfos.TryGetValue(clone, out PortalCloneInfo<Component> info);

                        if (info.TryAs(out PortalCloneInfo<T> infoT))
                            infos[index++] = infoT;
                    }
                    
                    return true;
                }
            }

            infos = null;
            return false;
        }

        /// <summary>
        /// Gets all clone infos for a specific original component.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="original">The original component.</param>
        /// <returns>An enumerable of all clone infos for the original component.</returns>
        public static IEnumerable<PortalCloneInfo<T>> GetCloneInfos<T>(T original) where T : Component
        {
            if (original != null && _clonesByOriginal.TryGetValue(original, out Component[] componentClones))
            {
                foreach (Component clone in componentClones)
                {
                    _cloneInfos.TryGetValue(clone, out PortalCloneInfo<Component> info);

                    if (info.TryAs(out PortalCloneInfo<T> infoT))
                        yield return infoT;
                }
            }
        }

        /// <summary>
        /// Checks if a component is a clone.
        /// </summary>
        /// <param name="clone">The component to check.</param>
        /// <returns>True if the component is a clone, false otherwise.</returns>
        public static bool IsClone(Component clone) => _cloneInfos.ContainsKey(clone);
        
        /// <summary>
        /// Checks if a component has clones.
        /// </summary>
        /// <param name="original">The component to check.</param>
        /// <returns>True if the component has clones, false otherwise.</returns>
        public static bool HasClones(Component original) => _clonesByOriginal.ContainsKey(original);

        #endregion

        #region GameObject Cloning

        /// <summary>
        /// Adds a clone mapping between an original GameObject and its clone.
        /// </summary>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clone">The clone GameObject.</param>
        /// <param name="originalToClone">The array of portals used for cloning from original to clone.</param>
        public static void AddClone(GameObject original, GameObject clone, Portal[] originalToClone = null)
        {
            if (original != null && clone != null)
                AddClone(original.transform, clone.transform, originalToClone);
        }

        /// <summary>
        /// Adds a clone mapping between an original GameObject and its clone with a single portal.
        /// </summary>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clone">The clone GameObject.</param>
        /// <param name="portalToClone">The portal used for cloning from original to clone.</param>
        public static void AddClone(GameObject original, GameObject clone, Portal portalToClone)
        {
            if (original != null && clone != null)
                AddClone(original.transform, clone.transform, portalToClone);
        }

        /// <summary>
        /// Removes a clone mapping for a specific clone GameObject.
        /// </summary>
        /// <param name="clone">The clone GameObject to remove mapping for.</param>
        public static void RemoveClone(GameObject clone)
        {
            if (clone != null) RemoveClone(clone.transform);
        }

        /// <summary>
        /// Gets the original GameObject for a given clone.
        /// </summary>
        /// <param name="clone">The clone GameObject.</param>
        /// <returns>The original GameObject, or the clone itself if no mapping exists.</returns>
        public static GameObject GetOriginal(GameObject clone)
        {
            GetOriginal(ref clone);
            return clone;
        }

        /// <summary>
        /// Gets the original GameObject for a given clone by reference.
        /// </summary>
        /// <param name="clone">The clone GameObject reference, replaced with the original if found.</param>
        /// <returns>True if the original was found, false otherwise.</returns>
        public static bool GetOriginal(ref GameObject clone)
        {
            if (clone == null) return false;

            Transform transform = GetOriginal(clone.transform);

            if (transform)
            {
                clone = transform.gameObject;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get the original GameObject for a specific clone GameObject.
        /// </summary>
        /// <param name="clone">The clone GameObject.</param>
        /// <param name="original">The output original GameObject if found.</param>
        /// <returns>True if the original was found, false otherwise.</returns>
        public static bool TryGetOriginal(GameObject clone, out GameObject original)
        {
            if (clone != null)
            {
                if (TryGetOriginal(clone.transform, out Transform transform))
                {
                    original = transform.gameObject;
                    return true;
                }
            }

            original = null;
            return false;
        }

        /// <summary>
        /// Tries to get all clones for a specific original GameObject.
        /// </summary>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clones">The output array of clones if found.</param>
        /// <returns>True if clones were found, false otherwise.</returns>
        public static bool TryGetClones(GameObject original, out GameObject[] clones)
        {
            if (original != null)
            {
                if (_clonesByOriginal.TryGetValue(original.transform, out Component[] componentClones))
                {
                    int actualLength = 0;

                    foreach (Component clone in componentClones)
                        if (clone is Transform) actualLength++;

                    clones = new GameObject[actualLength];

                    int index = 0;

                    foreach (Component clone in componentClones)
                        if (clone is Transform transform) clones[index++] = transform.gameObject;
                    
                    return true;
                }
            }

            clones = null;
            return false;
        }

        /// <summary>
        /// Checks if a GameObject is a clone.
        /// </summary>
        /// <param name="clone">The GameObject to check.</param>
        /// <returns>True if the GameObject is a clone, false otherwise.</returns>
        public static bool IsClone(GameObject clone) => clone != null ? IsClone(clone.transform) : false;
        
        /// <summary>
        /// Checks if a GameObject has clones.
        /// </summary>
        /// <param name="original">The GameObject to check.</param>
        /// <returns>True if the GameObject has clones, false otherwise.</returns>
        public static bool HasClones(GameObject original) => original != null ? HasClones(original.transform) : false;

        #endregion

        private static bool Add(ref Component[] original, Component component)
        {
            if (original == null) original = new Component[] { component };

            foreach (Component found in original)
                if (found == component) return false;

            Component[] newArray = new Component[original.Length + 1];

            Array.Copy(original, newArray, original.Length);
            newArray[newArray.Length - 1] = component;

            original = newArray;
            return true;
        }

        private static bool Remove(ref Component[] original, Component component)
        {
            if (original == null) return false;

            foreach (Component found in original)
            {
                if (found == component)
                {
                    if (original.Length <= 1)
                    {
                        original = null;
                        return true;
                    }

                    Component[] newArray = new Component[original.Length - 1];

                    for (int i = 0, j = 0; i < original.Length; i++, j++)
                    {
                        Component originalComponent = original[i];

                        if (originalComponent != component)
                            newArray[j] = original[i];
                        else j--;
                    }

                    original = newArray;
                    return true;
                }
            }

            return false;
        }
    }
}
