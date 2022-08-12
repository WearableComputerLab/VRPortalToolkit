using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    public static partial class PortalCloning
    {
        private static Dictionary<Component, PortalCloneInfo<Component>> _cloneInfos = new Dictionary<Component, PortalCloneInfo<Component>>();
        private static Dictionary<Component, Component[]> _clonesByOriginal = new Dictionary<Component, Component[]>();

        #region Component Cloning

        public static void AddClone(Component original, Component clone, Portal[] originalToClone = null)
            => AddClone(new PortalCloneInfo<Component>(original, clone, originalToClone));

        public static void AddClone<TComponent>(PortalCloneInfo<TComponent> cloneInfo) where TComponent : Component
        {
            if (cloneInfo.TryAs(out PortalCloneInfo<Component> cloneInfo2))
                AddClone(cloneInfo2);
        }

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

        public static void AddClone(Component original, Component clone, Portal portalToClone)
            => AddClone(original, clone, portalToClone != null ? new Portal[] { portalToClone } : null);

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

        public static T GetOriginal<T>(T clone) where T : Component
        {
            GetOriginal(ref clone);
            return clone;
        }

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

        public static bool TryGetClones<T>(T original, out T[] clones) where T : Component
        {
            if (original != null)
            {
                if (_clonesByOriginal.TryGetValue(original, out Component[] componentClones))
                {
                    int actualLength = 0;

                    foreach (Component clone in componentClones)
                        if (clone is T) actualLength++;

                    clones = new T[actualLength];

                    int index = 0;

                    foreach (Component clone in componentClones)
                        if (clone is T asT) clones[index++] = asT;
                }
            }

            clones = null;
            return false;
        }

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
                        info.TryAs(out PortalCloneInfo<T> infoT);
                        infos[index++] = infoT;
                    }
                }
            }

            infos = null;
            return false;
        }

        public static bool IsClone(Component clone) => _cloneInfos.ContainsKey(clone);
        public static bool HasClones(Component original) => _clonesByOriginal.ContainsKey(original);

        #endregion

        #region GameObject Cloning

        public static void AddClone(GameObject original, GameObject clone, Portal[] originalToClone = null)
        {
            if (original != null && clone != null)
                AddClone(original.transform, clone.transform, originalToClone);
        }

        public static void AddClone(GameObject original, GameObject clone, Portal portalToClone)
        {
            if (original != null && clone != null)
                AddClone(original.transform, clone.transform, portalToClone);
        }

        public static void RemoveClone(GameObject clone)
        {
            if (clone != null) RemoveClone(clone.transform);
        }

        public static GameObject GetOriginal(GameObject clone)
        {
            GetOriginal(ref clone);
            return clone;
        }

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
                }
            }

            clones = null;
            return false;
        }

        public static bool IsClone(GameObject clone) => clone != null ? IsClone(clone.transform) : false;
        public static bool HasClones(GameObject original) => original != null ? HasClones(original.transform) : false;

        #endregion

        private static bool Add(ref Component[] original, Component component)
        {
            if (original == null) original = new Component[] { component };

            foreach (Component found in original)
                if (found == component) return false;

            Component[] newArray = new Component[original.Length + 1];

            Array.Copy(original, newArray, original.Length);
            newArray[newArray.Length] = component;

            original = newArray;
            return false;
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
