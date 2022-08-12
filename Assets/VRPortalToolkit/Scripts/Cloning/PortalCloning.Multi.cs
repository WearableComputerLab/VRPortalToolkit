using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    public static partial class PortalCloning
    {
        public static void AddClones<TComponent>(IEnumerable<PortalCloneInfo<TComponent>> cloneInfos) where TComponent : Component
        {
            foreach (PortalCloneInfo<TComponent> cloneInfo in cloneInfos)
                AddClone(cloneInfo);
        }

        public static void AddClones(IEnumerable<PortalCloneInfo<Component>> cloneInfos)
        {
            foreach (PortalCloneInfo<Component> cloneInfo in cloneInfos)
                AddClone(cloneInfo);
        }

        public static void AddClones(GameObject original, GameObject clone)
            => AddClones<Component>(original, clone, (Portal[])null, null);

        public static void AddClones(GameObject original, GameObject clone, Portal originalToClone)
            => AddClones<Component>(original, clone, new Portal[] { originalToClone }, null);

        public static void AddClones(GameObject original, GameObject clone, Portal[] originalToClone)
            => AddClones<Component>(original, clone, originalToClone, null);

        public static void AddClones<TComponent>(GameObject original, GameObject clone, List<PortalCloneInfo<TComponent>> list = null) where TComponent : Component
            => AddClones(original, clone, (Portal[])null, list);

        public static void AddClones<TComponent>(GameObject original, GameObject clone, Portal originalToClone, List<PortalCloneInfo<TComponent>> list = null) where TComponent : Component
            => AddClones(original, clone, new Portal[] { originalToClone }, list);

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

        public static void CreateClones<TComponent>(GameObject original, GameObject clone, List<PortalCloneInfo<TComponent>> list = null) where TComponent : Component
            => CreateClones(original, clone, (Portal[])null, list);

        public static void CreateClones<TComponent>(GameObject original, GameObject clone, Portal originalToClone, List<PortalCloneInfo<TComponent>> list = null) where TComponent : Component
            => CreateClones(original, clone, new Portal[] { originalToClone }, list);

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

                    while (originalTransform.parent)
                    {
                        if (cloneByOriginal.TryGetValue(originalTransform.parent, out Transform cloneParent))
                        {
                            cloneTransform.SetParent(cloneParent, false);
                            break;
                        }

                        cloneParent = new GameObject(originalTransform.parent.name).transform;
                        cloneTransform.SetParent(cloneParent, false);
                        cloneByOriginal.Add(originalTransform.parent, cloneParent);
                        UpdateTransformLocal(new PortalCloneInfo<Transform>(originalTransform.parent, cloneParent, originalToClone));

                        originalTransform = originalTransform.parent;
                        cloneTransform = cloneParent;
                    }
                }
            }
        }

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

        private static void FindCloneTransforms(Transform original, Transform clone, Dictionary<Transform, Transform> cloneByOriginal)
        {
            cloneByOriginal.Add(original, clone);

            int childCount = Mathf.Min(original.childCount, clone.childCount);

            for (int i = 0; i < childCount; i++)
                FindCloneTransforms(original.GetChild(i), clone.GetChild(i), cloneByOriginal);
        }

        public static void ReplacePortals<TComponent>(List<PortalCloneInfo<TComponent>> list, Portal originalToClone) where TComponent : Component
            => ReplacePortals(list, new Portal[] { originalToClone });

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
