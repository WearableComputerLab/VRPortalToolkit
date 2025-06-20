using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    /// <summary>
    /// Multi-object extensions for the PortalCloning class.
    /// Provides utilities for cloning multiple components and GameObjects through portals simultaneously.
    /// </summary>
    public static partial class PortalCloning
    {
        /// <summary>
        /// Adds multiple component clone mappings at once.
        /// </summary>
        /// <typeparam name="TComponent">The type of components being cloned.</typeparam>
        /// <param name="cloneInfos">The collection of clone infos to add.</param>
        public static void AddClones<TComponent>(IEnumerable<PortalCloneInfo<TComponent>> cloneInfos) where TComponent : Component
        {
            foreach (PortalCloneInfo<TComponent> cloneInfo in cloneInfos)
                AddClone(cloneInfo);
        }

        /// <summary>
        /// Adds multiple component clone mappings at once.
        /// </summary>
        /// <param name="cloneInfos">The collection of clone infos to add.</param>
        public static void AddClones(IEnumerable<PortalCloneInfo<Component>> cloneInfos)
        {
            foreach (PortalCloneInfo<Component> cloneInfo in cloneInfos)
                AddClone(cloneInfo);
        }

        /// <summary>
        /// Adds clone mappings between all components of two GameObjects.
        /// </summary>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clone">The clone GameObject.</param>
        public static void AddClones(GameObject original, GameObject clone)
            => AddClones<Component>(original, clone, (Portal[])null, null);

        /// <summary>
        /// Adds clone mappings between all components of two GameObjects with a single portal.
        /// </summary>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clone">The clone GameObject.</param>
        /// <param name="originalToClone">The portal used for cloning from original to clone.</param>
        public static void AddClones(GameObject original, GameObject clone, Portal originalToClone)
            => AddClones<Component>(original, clone, new Portal[] { originalToClone }, null);

        /// <summary>
        /// Adds clone mappings between all components of two GameObjects with multiple portals.
        /// </summary>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clone">The clone GameObject.</param>
        /// <param name="originalToClone">The array of portals used for cloning from original to clone.</param>
        public static void AddClones(GameObject original, GameObject clone, Portal[] originalToClone)
            => AddClones<Component>(original, clone, originalToClone, null);

        /// <summary>
        /// Adds clone mappings between specific component types of two GameObjects.
        /// </summary>
        /// <typeparam name="TComponent">The type of components to clone.</typeparam>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clone">The clone GameObject.</param>
        /// <param name="list">Optional list to store the created clone infos.</param>
        public static void AddClones<TComponent>(GameObject original, GameObject clone, List<PortalCloneInfo<TComponent>> list = null) where TComponent : Component
            => AddClones(original, clone, (Portal[])null, list);

        /// <summary>
        /// Adds clone mappings between specific component types of two GameObjects with a single portal.
        /// </summary>
        /// <typeparam name="TComponent">The type of components to clone.</typeparam>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clone">The clone GameObject.</param>
        /// <param name="originalToClone">The portal used for cloning from original to clone.</param>
        /// <param name="list">Optional list to store the created clone infos.</param>
        public static void AddClones<TComponent>(GameObject original, GameObject clone, Portal originalToClone, List<PortalCloneInfo<TComponent>> list = null) where TComponent : Component
            => AddClones(original, clone, new Portal[] { originalToClone }, list);

        /// <summary>
        /// Adds clone mappings between specific component types of two GameObjects with multiple portals.
        /// </summary>
        /// <typeparam name="TComponent">The type of components to clone.</typeparam>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clone">The clone GameObject.</param>
        /// <param name="originalToClone">The array of portals used for cloning from original to clone.</param>
        /// <param name="list">Optional list to store the created clone infos.</param>
        public static void AddClones<TComponent>(GameObject original, GameObject clone, Portal[] originalToClone, List<PortalCloneInfo<TComponent>> list = null) where TComponent : Component
        {
            if (original && clone)
            {
                List<TComponent> originalList = new List<TComponent>(), cloneList = new List<TComponent>();

                AddClonesRecursive(original.transform, clone.transform, originalToClone, originalList, cloneList, list);
            }
        }

        private static void AddClonesRecursive<TComponent>(Transform original, Transform clone, Portal[] originalToClone, List<TComponent> originalList, List<TComponent> cloneList, List<PortalCloneInfo<TComponent>> list) where TComponent : Component
        {
            original.GetComponents(originalList);
            clone.GetComponents(cloneList);

            // First try to find a type match, keeping in mind the structure of the two gameobjects may be slightly different
            for (int i = 0; i < originalList.Count; i++)
            {
                TComponent originalComponent = originalList[i];

                for (int j = 0; j < cloneList.Count; j++)
                {
                    TComponent cloneComponent = cloneList[j];

                    if (originalComponent.GetType().IsAssignableFrom(cloneComponent.GetType()))
                    {
                        AddClone(originalComponent, cloneComponent, originalToClone);

                        if (list != null)
                            list.Add(new PortalCloneInfo<TComponent>(originalComponent, cloneComponent, originalToClone));

                        cloneList.RemoveAt(j);
                        break;
                    }
                }
            }

            int childCount = Mathf.Min(original.childCount, clone.childCount);

            for (int i = 0; i < childCount; i++)
                AddClonesRecursive(original.GetChild(i), clone.GetChild(i), originalToClone, originalList, cloneList, list);
        }

        /// <summary>
        /// Creates and adds components to a clone GameObject to match the original GameObject.
        /// </summary>
        /// <typeparam name="TComponent">The type of components to clone.</typeparam>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clone">The clone GameObject, which will have components added to it.</param>
        /// <param name="list">Optional list to store the created clone infos.</param>
        public static void CreateClones<TComponent>(GameObject original, GameObject clone, List<PortalCloneInfo<TComponent>> list = null) where TComponent : Component
            => CreateClones(original, clone, (Portal[])null, list);

        /// <summary>
        /// Creates and adds components to a clone GameObject to match the original GameObject with a single portal.
        /// </summary>
        /// <typeparam name="TComponent">The type of components to clone.</typeparam>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clone">The clone GameObject, which will have components added to it.</param>
        /// <param name="originalToClone">The portal used for cloning from original to clone.</param>
        /// <param name="list">Optional list to store the created clone infos.</param>
        public static void CreateClones<TComponent>(GameObject original, GameObject clone, Portal originalToClone, List<PortalCloneInfo<TComponent>> list = null) where TComponent : Component
            => CreateClones(original, clone, new Portal[] { originalToClone }, list);

        /// <summary>
        /// Creates and adds components to a clone GameObject to match the original GameObject with multiple portals.
        /// </summary>
        /// <typeparam name="TComponent">The type of components to clone.</typeparam>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clone">The clone GameObject, which will have components added to it.</param>
        /// <param name="originalToClone">The array of portals used for cloning from original to clone.</param>
        /// <param name="list">Optional list to store the created clone infos.</param>
        public static void CreateClones<TComponent>(GameObject original, GameObject clone, Portal[] originalToClone, List<PortalCloneInfo<TComponent>> list = null) where TComponent : Component
        {
            if (original)
            {
                Dictionary<Transform, Transform> cloneByOriginal = new Dictionary<Transform, Transform>();
                FindCloneTransforms(original.transform, clone.transform, cloneByOriginal);

                TComponent[] components = original.GetComponentsInChildren<TComponent>(true);

                foreach (TComponent originalComponent in components)
                {
                    Transform originalTransform = originalComponent.transform;

                    if (cloneByOriginal.TryGetValue(originalTransform, out Transform cloneTransform))
                    {
                        AddComponent(cloneTransform.gameObject, originalComponent, originalToClone, list);
                        continue;
                    }

                    cloneTransform = new GameObject(originalComponent.gameObject.name).transform;
                    cloneByOriginal.Add(originalTransform, cloneTransform);
                    UpdateTransformLocal(new PortalCloneInfo<Transform>(originalTransform, cloneTransform, originalToClone));

                    AddComponent(cloneTransform.gameObject, originalComponent, originalToClone, list);
                    CloneHierarchy(originalTransform, cloneTransform, originalToClone, cloneByOriginal);
                }
            }
        }

        /// <summary>
        /// Creates or finds parent transforms to recreate the hierarchy of an original transform in a clone.
        /// </summary>
        /// <param name="original">The original transform.</param>
        /// <param name="clone">The clone transform.</param>
        /// <param name="originalToClone">The array of portals used for cloning.</param>
        /// <param name="cloneByOriginal">Dictionary mapping original transforms to their clones.</param>
        private static void CloneHierarchy(Transform original, Transform clone, Portal[] originalToClone, Dictionary<Transform, Transform> cloneByOriginal)
        {
            if (!original || !clone || cloneByOriginal == null) return;

            while (original.parent)
            {
                if (cloneByOriginal.TryGetValue(original.parent, out Transform cloneParent))
                {
                    InsertSiblings(originalToClone, cloneByOriginal, original, cloneParent);
                    clone.SetParent(cloneParent, false);
                    break;
                }

                cloneParent = new GameObject(original.parent.name).transform;
                InsertSiblings(originalToClone, cloneByOriginal, original, cloneParent);

                clone.SetParent(cloneParent, false);
                cloneByOriginal.Add(original.parent, cloneParent);
                UpdateTransformLocal(new PortalCloneInfo<Transform>(original.parent, cloneParent, originalToClone));

                original = original.parent;
                clone = cloneParent;
            }
        }

        /// <summary>
        /// Inserts sibling GameObjects to maintain hierarchy integrity between original and clone.
        /// </summary>
        /// <param name="originalToClone">The array of portals used for cloning.</param>
        /// <param name="cloneByOriginal">Dictionary mapping original transforms to their clones.</param>
        /// <param name="originalTransform">The original transform whose siblings need to be inserted.</param>
        /// <param name="cloneParent">The parent transform of the clone where siblings should be inserted.</param>
        private static void InsertSiblings(Portal[] originalToClone, Dictionary<Transform, Transform> cloneByOriginal, Transform originalTransform, Transform cloneParent)
        {
            // Need to insert some children to make sure the hierarchy is duplicated correctly
            int siblingIndex = originalTransform.GetSiblingIndex();
            while (siblingIndex > cloneParent.childCount)
            {
                Transform originalSibling = originalTransform.parent.GetChild(cloneParent.childCount),
                    cloneSibling = new GameObject(originalSibling.name).transform;

                cloneSibling.SetParent(cloneParent, false);
                cloneByOriginal.Add(originalSibling, cloneSibling);
                UpdateTransformLocal(new PortalCloneInfo<Transform>(originalSibling, cloneSibling, originalToClone));
            }
        }

        /// <summary>
        /// Adds a component to a clone GameObject based on an original component.
        /// </summary>
        /// <typeparam name="TComponent">The type of component to clone.</typeparam>
        /// <param name="clone">The clone GameObject to add the component to.</param>
        /// <param name="originalComponent">The original component to clone.</param>
        /// <param name="originalToClone">The array of portals used for cloning.</param>
        /// <param name="list">Optional list to store the created clone info.</param>
        private static void AddComponent<TComponent>(GameObject clone, TComponent originalComponent, Portal[] originalToClone, List<PortalCloneInfo<TComponent>> list) where TComponent : Component
        {
            System.Type type = originalComponent.GetType();

            TComponent cloneComponent;

            if (type != typeof(Transform))
                cloneComponent = (TComponent)clone.AddComponent(type);
            else
                cloneComponent = clone.transform as TComponent;

            AddClone(originalComponent, cloneComponent, originalToClone);

            if (list != null)
                list.Add(new PortalCloneInfo<TComponent>(originalComponent, cloneComponent, originalToClone));
        }

        /// <summary>
        /// Finds and maps corresponding transforms between an original and clone GameObject hierarchy.
        /// </summary>
        /// <param name="original">The original transform.</param>
        /// <param name="clone">The clone transform.</param>
        /// <param name="cloneByOriginal">Dictionary to store the mappings between original and clone transforms.</param>
        private static void FindCloneTransforms(Transform original, Transform clone, Dictionary<Transform, Transform> cloneByOriginal)
        {
            cloneByOriginal.Add(original, clone);

            int childCount = Mathf.Min(original.childCount, clone.childCount);

            for (int i = 0; i < childCount; i++)
                FindCloneTransforms(original.GetChild(i), clone.GetChild(i), cloneByOriginal);
        }

        /// <summary>
        /// Replaces the portals associated with a list of clone infos with a single new portal.
        /// </summary>
        /// <typeparam name="TComponent">The type of components in the clone infos.</typeparam>
        /// <param name="list">The list of clone infos to update.</param>
        /// <param name="originalToClone">The new portal to use.</param>
        public static void ReplacePortals<TComponent>(List<PortalCloneInfo<TComponent>> list, Portal originalToClone) where TComponent : Component
            => ReplacePortals(list, new Portal[] { originalToClone });

        /// <summary>
        /// Replaces the portals associated with a list of clone infos with new portals.
        /// </summary>
        /// <typeparam name="TComponent">The type of components in the clone infos.</typeparam>
        /// <param name="list">The list of clone infos to update.</param>
        /// <param name="originalToClone">The new array of portals to use.</param>
        public static void ReplacePortals<TComponent>(List<PortalCloneInfo<TComponent>> list, Portal[] originalToClone) where TComponent : Component
        {
            for (int i = 0; i < list.Count; i++)
            {
                PortalCloneInfo<TComponent> info = list[i];
                AddClone(list[i] = new PortalCloneInfo<TComponent>(info.original, info.clone, originalToClone));
            }
        }
    }
}
