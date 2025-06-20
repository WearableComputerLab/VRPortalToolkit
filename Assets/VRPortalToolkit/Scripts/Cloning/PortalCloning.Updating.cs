using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    /// <summary>
    /// Component and GameObject property updating utilities for the PortalCloning system.
    /// Provides methods to synchronize various properties between original and cloned objects.
    /// </summary>
    public static partial class PortalCloning
    {
        /// <summary>
        /// Updates the tag of a GameObject to match its original.
        /// </summary>
        /// <param name="original">The original GameObject.</param>
        /// <returns>True if the tag was successfully updated, false otherwise.</returns>
        public static bool UpdateTag(GameObject original) => UpdateTag(original.transform);

        /// <summary>
        /// Updates the tag of a cloned Component's GameObject to match its original.
        /// </summary>
        /// <param name="clone">The cloned Component.</param>
        /// <returns>True if the tag was successfully updated, false otherwise.</returns>
        public static bool UpdateTag(Component clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                UpdateTag(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the tag of a cloned Component's GameObject to match its original using the provided clone info.
        /// Applies any portal-specific tag modifications as needed.
        /// </summary>
        /// <typeparam name="TComponent">The type of component.</typeparam>
        /// <param name="cloneInfo">The clone information containing the original and clone Component.</param>
        public static void UpdateTag<TComponent>(this PortalCloneInfo<TComponent> cloneInfo) where TComponent : Component
        {
            Component original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                string tag = original.tag;

                for (int i = 0; i < cloneInfo.PortalCount; i++)
                    cloneInfo.GetOriginalToClonePortal(i)?.ModifyTag(ref tag);

                clone.tag = tag;
            }
        }

        /// <summary>
        /// Updates the layer of a GameObject to match its original.
        /// </summary>
        /// <param name="original">The original GameObject.</param>
        /// <returns>True if the layer was successfully updated, false otherwise.</returns>
        public static bool UpdateLayer<TComponent>(GameObject original) => UpdateLayer(original.transform);

        /// <summary>
        /// Updates the layer of a cloned Component's GameObject to match its original.
        /// </summary>
        /// <param name="clone">The cloned Component.</param>
        /// <returns>True if the layer was successfully updated, false otherwise.</returns>
        public static bool UpdateLayer(Component clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                UpdateLayer(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the layer of a cloned Component's GameObject to match its original using the provided clone info.
        /// Applies any portal-specific layer modifications as needed.
        /// </summary>
        /// <typeparam name="TComponent">The type of component.</typeparam>
        /// <param name="cloneInfo">The clone information containing the original and clone Component.</param>
        public static void UpdateLayer<TComponent>(this PortalCloneInfo<TComponent> cloneInfo) where TComponent : Component
        {
            Component original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                int layer = original.gameObject.layer;

                for (int i = 0; i < cloneInfo.PortalCount; i++)
                    cloneInfo.GetOriginalToClonePortal(i)?.ModifyLayer(ref layer);

                clone.gameObject.layer = layer;
            }
        }

        /// <summary>
        /// Updates the active state of a GameObject to match its original.
        /// </summary>
        /// <param name="original">The original GameObject.</param>
        /// <returns>True if the active state was successfully updated, false otherwise.</returns>
        public static bool UpdateActive(GameObject original) => UpdateActiveAndEnabled(original.transform);

        /// <summary>
        /// Updates the active and enabled states of a cloned Component to match its original.
        /// </summary>
        /// <param name="clone">The cloned Component.</param>
        /// <returns>True if the states were successfully updated, false otherwise.</returns>
        public static bool UpdateActiveAndEnabled(Component clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                UpdateActiveAndEnabled(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the active and enabled states of a cloned Component to match its original using the provided clone info.
        /// Handles different types of components appropriately (Transform, Behaviour, Renderer).
        /// </summary>
        /// <typeparam name="TComponent">The type of component.</typeparam>
        /// <param name="cloneInfo">The clone information containing the original and clone Component.</param>
        public static void UpdateActiveAndEnabled<TComponent>(this PortalCloneInfo<TComponent> cloneInfo) where TComponent : Component
        {
            Component original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                if (original is Transform originalT && clone is Transform cloneT)
                {
                    if (cloneT.gameObject.activeSelf != originalT.gameObject.activeSelf)
                        cloneT.gameObject.SetActive(originalT.gameObject.activeSelf);
                }

                if (original is Behaviour originalB && clone is Behaviour cloneB)
                    cloneB.enabled = originalB.enabled;

                if (original is Renderer originalR && clone is Renderer cloneR)
                    cloneR.enabled = originalR.enabled;
            }
        }

        /// <summary>
        /// Updates the enabled state of a cloned Component to match its original.
        /// </summary>
        /// <param name="clone">The cloned Component.</param>
        /// <returns>True if the enabled state was successfully updated, false otherwise.</returns>
        public static bool UpdateEnabled(Component clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                UpdateEnabled(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the enabled state of a cloned Component to match its original using the provided clone info.
        /// Handles different types of components appropriately (Behaviour, Renderer).
        /// </summary>
        /// <typeparam name="TComponent">The type of component.</typeparam>
        /// <param name="cloneInfo">The clone information containing the original and clone Component.</param>
        public static void UpdateEnabled<TComponent>(this PortalCloneInfo<TComponent> cloneInfo) where TComponent : Component
        {
            Component original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                if (original is Behaviour originalB && clone is Behaviour cloneB)
                    cloneB.enabled = originalB.enabled;

                if (original is Renderer originalR && clone is Renderer cloneR)
                    cloneR.enabled = originalR.enabled;
            }
        }

        /// <summary>
        /// Updates the world transform of a cloned Transform to match its original, with portal transformations applied.
        /// </summary>
        /// <param name="clone">The cloned Transform.</param>
        /// <returns>True if the transform was successfully updated, false otherwise.</returns>
        public static bool UpdateTransformWorld(Transform clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Transform> cloneInfo))
            {
                UpdateTransformWorld(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the world transform of a cloned Transform to match its original using the provided clone info.
        /// Applies portal transformations to properly position the clone relative to portals.
        /// </summary>
        /// <param name="cloneInfo">The clone information containing the original and clone Transform.</param>
        public static void UpdateTransformWorld(this PortalCloneInfo<Transform> cloneInfo)
        {
            Transform original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                Matrix4x4 localToWorld = Matrix4x4.TRS(original.position, original.rotation, original.localScale);

                for (int i = 0; i < cloneInfo.PortalCount; i++)
                    cloneInfo.GetOriginalToClonePortal(i)?.ModifyMatrix(ref localToWorld);

                clone.position = localToWorld.GetColumn(3);
                clone.rotation = localToWorld.rotation;
                clone.localScale = localToWorld.lossyScale;
            }
        }

        /// <summary>
        /// Updates the local transform of a cloned Transform to match its original.
        /// </summary>
        /// <param name="clone">The cloned Transform.</param>
        /// <returns>True if the transform was successfully updated, false otherwise.</returns>
        public static bool UpdateTransformLocal(Transform clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Transform> cloneInfo))
            {
                UpdateTransformLocal(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the local transform of a cloned Transform to match its original using the provided clone info.
        /// </summary>
        /// <param name="cloneInfo">The clone information containing the original and clone Transform.</param>
        public static void UpdateTransformLocal(this PortalCloneInfo<Transform> cloneInfo)
        {
            Transform original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                clone.localPosition = original.localPosition;
                clone.localRotation = original.localRotation;
                clone.localScale = original.localScale;
            }
        }

        /// <summary>
        /// Updates the fields of a cloned Component to match its original.
        /// </summary>
        /// <param name="clone">The cloned Component.</param>
        /// <returns>True if the fields were successfully updated, false otherwise.</returns>
        public static bool UpdateFields(Component clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                UpdateFields(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the fields of a cloned Component to match its original using the provided clone info.
        /// Uses reflection to copy all public instance fields.
        /// </summary>
        /// <param name="cloneInfo">The clone information containing the original and clone Component.</param>
        public static void UpdateFields(this PortalCloneInfo<Component> cloneInfo)
        {
            Component original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                System.Type type = original.GetType();
                
                if (type.IsAssignableFrom(clone.GetType()))
                {
                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) // TODO: Does this get base types?
                        field.SetValue(clone, field.GetValue(original));
                }
            }
        }

        /// <summary>
        /// Updates the properties of a cloned Component to match its original.
        /// </summary>
        /// <param name="clone">The cloned Component.</param>
        /// <returns>True if the properties were successfully updated, false otherwise.</returns>
        public static bool UpdateProperties(Component clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                UpdateProperties(cloneInfo);
                return true;
            }    

            return false;
        }

        /// <summary>
        /// Updates the properties of a cloned Component to match its original using the provided clone info.
        /// Uses reflection to copy all readable and writable properties, with special handling for material-related properties.
        /// </summary>
        /// <param name="cloneInfo">The clone information containing the original and clone Component.</param>
        public static void UpdateProperties(this PortalCloneInfo<Component> cloneInfo)
        {
            Component original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                System.Type type = original.GetType();

                if (type.IsAssignableFrom(clone.GetType()))
                {
                    bool ignoreMaterial = (original is Renderer) || (original is Collider);

                    do
                    {
                        foreach (var property in type.GetProperties())
                        {
                            if (!property.CanWrite || !property.CanRead)
                                continue;

                            if (ignoreMaterial && (property.Name == "material" || property.Name == "materials"))
                                continue;

                            property.SetValue(clone, property.GetValue(original));
                        }

                        type = type.BaseType;

                    } while (type != null);
                }
            }
        }
    }
}
