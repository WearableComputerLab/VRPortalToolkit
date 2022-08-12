using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    public static partial class PortalCloning
    {
        public static bool UpdateTag(GameObject original) => UpdateTag(original.transform);

        public static bool UpdateTag(Component clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                UpdateTag(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateTag<TComponent>(PortalCloneInfo<TComponent> cloneInfo) where TComponent : Component
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

        public static bool UpdateLayer<TComponent>(GameObject original) => UpdateLayer(original.transform);

        public static bool UpdateLayer(Component clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                UpdateLayer(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateLayer<TComponent>(PortalCloneInfo<TComponent> cloneInfo) where TComponent : Component
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

        public static bool UpdateActive(GameObject original) => UpdateActiveAndEnabled(original.transform);

        public static bool UpdateActiveAndEnabled(Component clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                UpdateActiveAndEnabled(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateActiveAndEnabled<TComponent>(PortalCloneInfo<TComponent> cloneInfo) where TComponent : Component
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

        public static bool UpdateEnabled(Component clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                UpdateEnabled(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateEnabled<TComponent>(PortalCloneInfo<TComponent> cloneInfo) where TComponent : Component
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

        public static bool UpdateTransformWorld(Transform clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Transform> cloneInfo))
            {
                UpdateTransformWorld(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateTransformWorld(PortalCloneInfo<Transform> cloneInfo)
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

        public static bool UpdateTransformLocal(Transform clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Transform> cloneInfo))
            {
                UpdateTransformLocal(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateTransformLocal(PortalCloneInfo<Transform> cloneInfo)
        {
            Transform original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                clone.localPosition = original.localPosition;
                clone.localRotation = original.localRotation;
                clone.localScale = original.localScale;
            }
        }

        /*public static bool UpdateParent(Transform clone)
        {
            if (TryGetOriginal(clone, out Transform original))
            {
                if (TryGetClone(original.parent, out Transform cloneParent))
                {
                    if (clone.parent != cloneParent)
                        clone.SetParent(cloneParent, false);
                }
                else if (clone.parent != original.parent)
                {
                    //if (transform.IsChildOf(original.transform))
                    //clone.SetParent(transform, false);
                    //else
                    clone.SetParent(original.parent, false);
                }

                return true;
            }

            return false;
        }*/

        /*public bool UpdateChildren(Transform clone)
        {
            if (TryGetOriginal(clone, out Transform original))
            {
                Transform originalChild, cloneChild;

                for (int i = 0; i < original.childCount; i++)
                {
                    originalChild = original.GetChild(i);

                    if (TryGetClone(originalChild, out cloneChild))
                    {
                        if (cloneChild.parent != clone)
                            cloneChild.SetParent(clone, false);
                    }
                }

                return true;
            }

            return false;
        }*/

        // TODO: Would be cool to have an UpdateSerialized
        // And an UpdateSerializedRelative

        // TODO Also these dont also update fields of the derrived class
        // Also updating the fields wont trigger the properties (for my Validate stuff)

        public static bool UpdateFields(Component clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                UpdateFields(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateFields(PortalCloneInfo<Component> cloneInfo)
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

        /*public virtual bool UpdateFieldsRelative(Component clone)
        {
            if (TryGetOriginal(clone, out Component original))
            {
                object asObject;
                System.Type type = original.GetType();

                if (type == clone.GetType())
                {
                    foreach (var property in type.GetFields())
                    {
                        asObject = property.GetValue(original);

                        if (asObject is Component asComponent && TryGetClone(asComponent, out Component cloneComponent))
                            property.SetValue(clone, cloneComponent);
                        else
                            property.SetValue(clone, asObject);
                    }

                    return true;
                }
            }

            return true;
        }*/

        public static bool UpdateProperties(Component clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Component> cloneInfo))
            {
                UpdateProperties(cloneInfo);
                return true;
            }    

            return false;
        }

        public static void UpdateProperties(PortalCloneInfo<Component> cloneInfo)
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

        /*public virtual bool UpdatePropertiesRelative(Component original)
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            System.Type type;
            object asObject;
            Component asComponent;
            bool ignoreMaterial;

            if (TryGetClone(original, out Component clone))
            {
                type = original.GetType();

                if (type == clone.GetType())
                {
                    ignoreMaterial = (original is Renderer) || (original is Collider);

                    do
                    {
                        foreach (var property in type.GetProperties())
                        {
                            if (!property.CanWrite || !property.CanRead) continue;

                            if (ignoreMaterial && (property.Name == "material" || property.Name == "materials"))
                                continue;

                            asObject = property.GetValue(original);
                            asComponent = asObject as Component;

                            if (asComponent && TryGetClone(asComponent, out Component cloneComponent))
                                property.SetValue(clone, cloneComponent);
                            else
                                property.SetValue(clone, asObject);
                        }

                        type = type.BaseType;

                    } while (type != null);

                    return true;
                }
            }

            return false;
        }*/

        /*private void UpdateRenderer(Renderer original, Renderer clone)
        {
            // Use this to prevent the use of instancing materials unneccessarily
            clone.motionVectorGenerationMode = original.motionVectorGenerationMode;
            clone.renderingLayerMask = original.renderingLayerMask;
            clone.rendererPriority = original.rendererPriority;
            clone.rayTracingMode = original.rayTracingMode;
            clone.sortingLayerID = original.sortingLayerID;
            clone.sortingOrder = original.sortingOrder;
            clone.allowOcclusionWhenDynamic = original.allowOcclusionWhenDynamic;
            clone.lightProbeProxyVolumeOverride = original.lightProbeProxyVolumeOverride;
            clone.probeAnchor = original.probeAnchor;
            clone.lightmapIndex = original.lightmapIndex;
            clone.realtimeLightmapIndex = original.realtimeLightmapIndex;
            clone.lightmapScaleOffset = original.lightmapScaleOffset;
            clone.realtimeLightmapScaleOffset = original.realtimeLightmapScaleOffset;
            clone.reflectionProbeUsage = original.reflectionProbeUsage;
            clone.lightProbeUsage = original.lightProbeUsage;
            clone.sharedMaterials = original.sharedMaterials;
            clone.staticShadowCaster = original.staticShadowCaster;
            clone.enabled = original.enabled;
            clone.shadowCastingMode = original.shadowCastingMode;
            clone.receiveShadows = original.receiveShadows;
            clone.forceRenderingOff = original.forceRenderingOff;
        }

        private void UpdateCollider(Collider original, Collider clone)
        {
            // Use this to prevent the use of instancing materials unneccessarily
            clone.isTrigger = original.isTrigger;
            clone.contactOffset = original.contactOffset;
            clone.sharedMaterial = original.sharedMaterial;
            clone.enabled = original.enabled;
        }*/
    }
}
