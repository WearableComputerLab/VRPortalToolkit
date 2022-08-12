using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit
{
    // TODO: This should handle objects being destroyed
    // Also what is the hierachy changes
    // If the clone is destroyed, but their a still existing former children, what do we do there?

    public class CloneController
    {
        private GameObject _original;
        public GameObject original {
            get => _original;
            set {
                if (_original != value)
                {
                    ClearCache();
                    _original = value;
                }
            }
        }

        private GameObject _clone;
        public GameObject clone {
            get => _clone;
            set {
                if (_clone != value)
                {
                    if (_clone) Object.Destroy(clone);
                    ClearCache();

                    _clone = value;
                }
            }
        }

        public enum ComponentOverrideMode : sbyte
        {
            IgnoreExact = 0,
            IgnoreAssignable = 1,
            OnlyIncludeExact = 2,
            OnlyIncludeAssignable = 3
        }

        private ComponentOverrideMode _componentsOverrideMode;
        public ComponentOverrideMode componentsOverrideMode {
            get => _componentsOverrideMode;
            set => _componentsOverrideMode = value;
        }

        private System.Type[] _componentsOverride;
        public System.Type[] componentsOverride {
            get => _componentsOverride;
            set => _componentsOverride = value;
        }

        protected Dictionary<Component, Component> _cloneByOriginal;

        public int componentCount => _cloneByOriginal.Count;

        public CloneController(GameObject original = null, GameObject clone = null)
        {
            this.original = original;
            this.clone = clone;
        }

        #region Functions

        public virtual void DestroyClone()
        {
            if (_clone)
            {
                Object.Destroy(_clone);
                ClearCache();
            }
        }

        public virtual bool CreateCloneIfRequired()
        {
            if (!clone) return CreateOrReplaceClone();

            return false;
        }

        public virtual bool CreateOrReplaceClone()
        {
            DestroyClone();

            if (!original) return false;

            _clone = Object.Instantiate(original);

            foreach (Component cloneComponent in clone.GetComponentsInChildren<Component>(true))
                if (!isCloneable(cloneComponent)) Object.Destroy(cloneComponent);

            _clone.name = $"{original.name} (Clone)";

            _clone.transform.parent = original.transform.parent;

            return true;
        }

        public virtual void CreateCloneFromType(System.Type type)
            => CreateCloneFromTypes(new System.Type[1] { type });

        public virtual void CreateCloneFromTypes(params System.Type[] types)
            => CreateCloneFromTypes((IEnumerable<System.Type>)types);

        public virtual void CreateCloneFromTypes(IEnumerable<System.Type> types)
        {
            if (!original) return;
            // TODO: What if a component is dependent on another (and thus auto creates it)
            // TODO: For some reason, mesh filter doesnt seem to work

            clone = new GameObject($"{original.name} (Clone)");
            clone.SetActive(false);

            Transform originalTransform, cloneTransform, cloneParent;
            Component[] components;
            Component cloneComponent;

            if (_cloneByOriginal == null) _cloneByOriginal = new Dictionary<Component, Component>();
            else _cloneByOriginal.Clear();

            _cloneByOriginal.Add(original.transform, clone.transform);

            foreach (System.Type type in types)
            {
                components = original.GetComponentsInChildren(type, true);

                foreach (Component originalComponent in components)
                {
                    originalTransform = originalComponent.transform;

                    if (_cloneByOriginal.TryGetValue(originalComponent.transform, out cloneComponent))
                    {
                        AddComponent(cloneComponent.gameObject, originalComponent);
                        continue;
                    }

                    cloneTransform = new GameObject(originalComponent.gameObject.name).transform;
                    _cloneByOriginal.Add(originalComponent.transform, cloneTransform);
                    CopyTransform(originalComponent.transform, cloneTransform);

                    AddComponent(cloneTransform.gameObject, originalComponent);

                    while (originalTransform.parent)
                    {
                        if (_cloneByOriginal.TryGetValue(originalTransform.parent, out cloneComponent))
                        {
                            cloneTransform.SetParent((Transform)cloneComponent, false);
                            break;
                        }

                        cloneParent = new GameObject(originalTransform.parent.name).transform;
                        cloneTransform.SetParent(cloneParent, false);
                        _cloneByOriginal.Add(originalTransform.parent, cloneParent);
                        CopyTransform(originalTransform.parent, cloneParent);

                        originalTransform = originalTransform.parent;
                        cloneTransform = cloneParent;
                    }
                }
            }

            UpdatePropertiesRelative();
            UpdateEnabled();
            UpdateActive();

            foreach (System.Type type in types)
            {
                if (!isCloneable(type))
                {
                    _cloneByOriginal.Clear();
                    break;
                }
            }

            //original.SetActive(original.activeSelf);
        }

        private void AddComponent(GameObject cloneObject, Component original)
        {
            System.Type type = original.GetType();

            if (type != typeof(Transform))
            {
                Component clone = cloneObject.AddComponent(type);
                _cloneByOriginal.Add(original, clone);
            }
        }

        private void CopyTransform(Transform original, Transform clone)
        {
            clone.localPosition = original.localPosition;
            clone.localRotation = original.localRotation;
            clone.localScale = original.localScale;

            clone.gameObject.layer = original.gameObject.layer;
            clone.gameObject.SetActive(original.gameObject.activeSelf);
            clone.tag = original.tag;
        }

        public virtual void UpdateCache()
        {
            if (_cloneByOriginal == null) _cloneByOriginal = new Dictionary<Component, Component>();
            else _cloneByOriginal.Clear();

            if (!original || !clone) return;

            UpdateCache(original.transform, clone.transform);
        }

        protected List<Component> _originals = new List<Component>();
        protected List<Component> _clones = new List<Component>();

        protected virtual void UpdateCache(Transform original, Transform clone)
        {
            original.GetComponents(_originals);
            clone.GetComponents(_clones);

            Component originalComponent;
            System.Type type;
            int i;

            foreach (Component cloneComponent in _clones)
            {
                type = cloneComponent.GetType();

                for (i = 0; i < _originals.Count; i++)
                {
                    originalComponent = _originals[i];

                    if (originalComponent.GetType() == type)
                    {
                        _originals.RemoveAt(i);

                        if (isCloneable(originalComponent))
                            _cloneByOriginal[originalComponent] = cloneComponent;

                        break;
                    }
                }
            }

            for (i = 0; i < original.childCount && i < clone.childCount; i++)
                UpdateCache(original.GetChild(i), clone.GetChild(i));
        }

        public virtual bool isCloneable(Component component)
        {
            return component && isCloneable(component.GetType());
        }

        public virtual bool isCloneable(System.Type type)
        {
            switch (_componentsOverrideMode)
            {
                case ComponentOverrideMode.IgnoreExact:

                    if (_componentsOverride != null)
                    {
                        foreach (System.Type other in _componentsOverride)
                            if (type == other)
                                return false;
                    }
                    return true;

                case ComponentOverrideMode.IgnoreAssignable:
                    if (_componentsOverride != null)
                    {
                        foreach (System.Type other in _componentsOverride)
                            if (type.IsAssignableFrom(other))
                                return false;
                    }
                    return true;

                case ComponentOverrideMode.OnlyIncludeExact:
                    if (_componentsOverride != null)
                    {
                        foreach (System.Type other in _componentsOverride)
                            if (type != other)
                                return true;
                    }
                    return false;

                default: //ComponentOverrideMode.OnlyIncludeAssignable:
                    if (_componentsOverride != null)
                    {
                        foreach (System.Type other in _componentsOverride)
                            if (type.IsAssignableFrom(other))
                                return true;
                    }
                    return false;
            }
        }

        public virtual void ClearCache()
        {
            if (_cloneByOriginal != null)
                _cloneByOriginal.Clear();
        }
        public virtual bool IsClone(Component component)
        {
            Transform transform = component.transform;

            if (!clone) return false;

            if (transform.IsChildOf(clone.transform))
                return true;

            return false;
        }

        public virtual bool IsOriginal(Component component)
        {
            Transform transform = component.transform;

            if (transform.IsChildOf(component.transform))
                return true;

            return false;
        }

        public virtual bool IsCloneObject(GameObject gameObject) => IsClone(gameObject.transform);

        public virtual bool IsOriginalObject(GameObject gameObject) => IsOriginal(gameObject.transform);
        #endregion

        #region Get Original
        public virtual bool TryGetOriginal(Component clone, out Component original)
        {
            if (clone == null)
            {
                original = null;
                return false;
            }

            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            foreach (var pair in _cloneByOriginal)
            {
                if (pair.Value == clone)
                {
                    original = pair.Key;
                    return true;
                }
            }

            original = null;
            return false;
        }

        public Component GetOriginal(Component clone)
        {
            if (TryGetClone(clone, out Component original))
                return original;
            return null;
        }

        public virtual bool TryGetOriginal<T>(T clone, out T original) where T : Component
        {
            if (TryGetOriginal(clone, out Component cloneComponent))
            {
                if (cloneComponent is T)
                {
                    original = (T)cloneComponent;
                    return true;
                }
            }

            original = null;
            return false;
        }

        public T GetOriginal<T>(T clone) where T : Component
        {
            if (TryGetClone(clone, out T original))
                return original;
            return null;
        }

        public virtual bool TryGetOriginalObject(GameObject clone, out GameObject original)
        {
            if (TryGetOriginal(clone.transform, out Transform transform))
            {
                original = transform.gameObject;
                return true;
            }

            original = null;
            return false;
        }

        public GameObject GetOriginalObject(GameObject clone)
        {
            if (TryGetCloneObject(clone, out GameObject original))
                return original;
            return null;
        }

        public virtual IEnumerable<Component> GetOriginals()
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            foreach (var pair in _cloneByOriginal)
                yield return pair.Key;
        }

        public virtual IEnumerable<T> GetOriginals<T>() where T : Component
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            foreach (var pair in _cloneByOriginal)
            {
                if (pair.Key is T && pair.Value is T)
                    yield return (T)pair.Key;
            }
        }

        public virtual IEnumerable<GameObject> GetOriginalObjects()
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            foreach (var pair in _cloneByOriginal)
            {
                if (pair.Key is Transform && pair.Value is Transform)
                    yield return ((Transform)pair.Key).gameObject;
            }
        }
        #endregion

        #region Get Clones

        public virtual bool TryGetClone<T>(T original, out T clone) where T : Component
        {
            if (_cloneByOriginal.TryGetValue(original, out Component cloneComponent))
            {
                if (cloneComponent is T)
                {
                    clone = (T)cloneComponent;
                    return true;
                }
            }

            clone = null;
            return false;
        }

        public T GetClone<T>(T original) where T : Component
        {
            if (TryGetClone(original, out T clone))
                return clone;
            return null;
        }

        public virtual bool TryGetCloneObject(GameObject original, out GameObject clone)
        {
            if (TryGetClone(original.transform, out Transform transform))
            {
                clone = transform.gameObject;
                return true;
            }

            clone = null;
            return false;
        }

        public GameObject GetCloneObject(GameObject original)
        {
            if (TryGetCloneObject(original, out GameObject clone))
                return clone;
            return null;
        }

        public virtual IEnumerable<Component> GetClones()
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            foreach (var pair in _cloneByOriginal)
                yield return pair.Value;
        }

        public virtual IEnumerable<T> GetClones<T>() where T : Component
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            foreach (var pair in _cloneByOriginal)
            {
                if (pair.Key is T && pair.Value is T)
                    yield return (T)pair.Value;
            }
        }

        public virtual IEnumerable<GameObject> GetCloneObjects()
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            foreach (var pair in _cloneByOriginal)
            {
                if (pair.Key is Transform && pair.Value is Transform)
                    yield return ((Transform)pair.Value).gameObject;
            }
        }
        #endregion

        #region Get Pairs

        public virtual IEnumerable<KeyValuePair<Component, Component>> GetClonePairs()
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            foreach (var pair in _cloneByOriginal)
                yield return pair;
        }

        public virtual IEnumerable<KeyValuePair<T, T>> GetClonePairs<T>() where T : Component
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            foreach (var pair in _cloneByOriginal)
            {
                if (pair.Key is T && pair.Value is T)
                    yield return new KeyValuePair<T, T>((T)pair.Key, (T)pair.Value);
            }
        }

        public virtual IEnumerable<KeyValuePair<GameObject, GameObject>> GetCloneObjectPairs()
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            foreach (var pair in _cloneByOriginal)
            {
                if (pair.Key is Transform && pair.Value is Transform)
                    yield return new KeyValuePair<GameObject, GameObject>
                        (((Transform)pair.Key).gameObject, ((Transform)pair.Value).gameObject);
            }
        }
        #endregion

        #region Update Component

        public virtual bool UpdateTag(GameObject original) => UpdateTag(original.transform);

        public virtual bool UpdateTag(Transform original)
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            if (TryGetClone(original, out Transform clone))
            {
                clone.tag = original.tag;
                return true;
            }

            return false;
        }

        public virtual bool UpdateLayer(GameObject original) => UpdateLayer(original.transform);

        public virtual bool UpdateLayer(Transform original)
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            if (TryGetClone(original, out Transform clone))
            {
                clone.gameObject.layer = original.gameObject.layer;
                return true;
            }

            return false;
        }

        public virtual bool UpdateActive(GameObject original) => UpdateActiveAndEnabled(original.transform);

        public virtual bool UpdateActiveAndEnabled(Component original)
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            if (TryGetClone(original, out Component clone))
            {
                Behaviour originalB, cloneB;

                if ((originalB = original as Behaviour) && (cloneB = clone as Behaviour))
                {
                    cloneB.enabled = originalB.enabled;
                    return true;
                }

                Transform originalT, cloneT;
                if ((originalT = original as Transform) && (cloneT = clone as Transform))
                {
                    /*if (cloneT.gameObject == clone)
                        UpdateCloneActive();
                    else */
                    if (cloneT.gameObject.activeSelf != originalT.gameObject.activeSelf)
                        cloneT.gameObject.SetActive(originalT.gameObject.activeSelf);

                    return true;
                }

                Renderer originalR, cloneR;
                if ((originalR = original as Renderer) && (cloneR = clone as Renderer))
                {
                    cloneR.enabled = originalR.enabled;
                    return true;
                }
            }

            return false;
        }

        public virtual bool UpdateTransform(Transform original)
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            if (TryGetClone(original, out Transform clone))
            {
                clone.localPosition = original.localPosition;
                clone.localRotation = original.localRotation;
                clone.localScale = original.localScale;

                return true;
            }

            return false;
        }

        public virtual bool UpdateParent(Transform original)
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            if (TryGetClone(original, out Transform clone))
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
        }

        public bool UpdateChildren(Transform original)
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            if (TryGetClone(original, out Transform clone))
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
        }

        // TODO: Would be cool to have an UpdateSerialized
        // And an UpdateSerializedRelative

        // TODO Also these dont also update fields of the derrived class
        // Also updating the fields wont trigger the properties (for my Validate stuff)

        public virtual bool UpdateFields(Component original)
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            System.Type type;

            if (TryGetClone(original, out Component clone))
            {
                type = original.GetType();

                if (type == clone.GetType())
                {
                    foreach (var field in type.GetFields())
                        field.SetValue(clone, field.GetValue(original));

                    return true;
                }
            }

            return false;
        }

        public virtual bool UpdateFieldsRelative(Component original)
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            System.Type type;
            object asObject;

            if (TryGetClone(original, out Component clone))
            {
                type = original.GetType();

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
        }

        public virtual bool UpdateProperties(Component original)
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            System.Type type;
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
                            if (!property.CanWrite || !property.CanRead)
                                continue;

                            if (ignoreMaterial && (property.Name == "material" || property.Name == "materials"))
                                continue;

                            property.SetValue(clone, property.GetValue(original));
                        }

                        type = type.BaseType;

                    } while (type != null);

                    return true;
                }
            }

            return false;
        }

        public virtual bool UpdatePropertiesRelative(Component original)
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
        }

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
        #endregion

        #region Update Generic

        // TODO: Could be optimised if you know if T is a behaviour or renderer
        public virtual void UpdateEnabled<T>() where T : Component
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            foreach (var pair in _cloneByOriginal)
            {
                if (!pair.Key is T) continue;

                if (pair.Key is Behaviour originalB && pair.Value is Behaviour cloneB)
                    cloneB.enabled = originalB.enabled;
                else if (pair.Key is Renderer originalR && pair.Value is Renderer cloneR)
                    cloneR.enabled = originalR.enabled;
            }
        }

        public virtual void UpdateFields<T>() where T : Component
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            System.Type type;

            foreach (var pair in _cloneByOriginal)
            {
                if (!pair.Key is T) continue;

                type = pair.Key.GetType();

                if (type == pair.Value.GetType())
                {
                    foreach (var field in type.GetFields())
                        field.SetValue(pair.Value, field.GetValue(pair.Key));
                }
            }
        }
        public virtual void UpdateFieldsRelative<T>() where T : Component
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            System.Type type;
            object asObject;
            Component asComponent;

            foreach (var pair in _cloneByOriginal)
            {
                if (!(pair.Key is T)) continue;

                type = pair.Key.GetType();

                if (type == pair.Value.GetType())
                {
                    foreach (var field in type.GetFields())
                    {
                        asObject = field.GetValue(pair.Key);
                        asComponent = asObject as Component;

                        if (asComponent && TryGetClone(asComponent, out Component cloneComponent))
                            field.SetValue(pair.Value, cloneComponent);
                        else
                            field.SetValue(pair.Value, asObject);
                    }
                }
            }
        }

        public virtual void UpdateProperties<T>() where T : Component
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            System.Type type;
            bool ignoreMaterial;
            bool ignoreMesh;

            foreach (var pair in _cloneByOriginal)
            {
                if (!(pair.Key is T)) continue;

                type = pair.Key.GetType();

                if (type == pair.Value.GetType())
                {
                    ignoreMaterial = (pair.Key is Renderer) || (pair.Key is Collider);
                    ignoreMesh = (pair.Key is MeshFilter);

                    do
                    {
                        foreach (var property in type.GetProperties())
                        {
                            if (!property.CanWrite || !property.CanRead) continue;

                            if (ignoreMaterial && (property.Name == "material" || property.Name == "materials"))
                                continue;

                            property.SetValue(pair.Value, property.GetValue(pair.Key));
                        }

                        type = type.BaseType;

                    } while (type != null);
                }
            }
        }

        public virtual void UpdatePropertiesRelative<T>() where T : Component
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            System.Type type;
            object asObject;
            Component asComponent;
            bool ignoreMaterial;
            bool ignoreMesh;

            foreach (var pair in _cloneByOriginal)
            {
                if (!(pair.Key is T)) continue;

                ignoreMaterial = (pair.Key is Renderer) || (pair.Key is Collider);
                ignoreMesh = (pair.Key is MeshFilter);

                type = pair.Key.GetType();
                if (type == pair.Value.GetType())
                {
                    do
                    {
                        foreach (var property in type.GetProperties())
                        {
                            if (!property.CanWrite || !property.CanRead) continue;

                            if (ignoreMaterial && (property.Name == "material" || property.Name == "materials"))
                                continue;

                            if (ignoreMesh && (property.Name == "mesh"))
                                continue;

                            asObject = property.GetValue(pair.Key);
                            asComponent = asObject as Component;

                            if (asComponent && TryGetClone(asComponent, out Component cloneComponent))
                                property.SetValue(pair.Value, cloneComponent);
                            else
                                property.SetValue(pair.Value, asObject);
                        }

                        type = type.BaseType;

                    } while (type != null);
                }
            }
        }
        #endregion

        #region Update All

        public virtual void UpdateActiveAndEnabled()
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            Behaviour originalB, cloneB;
            Transform originalT, cloneT;
            Renderer originalR, cloneR;

            foreach (var pair in _cloneByOriginal)
            {
                if ((originalB = pair.Key as Behaviour) && (cloneB = pair.Value as Behaviour))
                    cloneB.enabled = originalB.enabled;
                else if ((originalT = pair.Key as Transform) && (cloneT = pair.Value as Transform))
                {
                    if (cloneT.gameObject.activeSelf != originalT.gameObject.activeSelf)
                        cloneT.gameObject.SetActive(originalT.gameObject.activeSelf);
                }
                else if ((originalR = pair.Key as Renderer) && (cloneR = pair.Value as Renderer))
                    cloneR.enabled = originalR.enabled;
            }
        }

        public virtual void UpdateEnabled() => UpdateEnabled<Component>();

        public virtual void UpdateActive()
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            Transform originalT, cloneT;

            foreach (var pair in _cloneByOriginal)
            {
                if ((originalT = pair.Key as Transform) && (cloneT = pair.Value as Transform))
                {
                    if (cloneT.gameObject.activeSelf != originalT.gameObject.activeSelf)
                        cloneT.gameObject.SetActive(originalT.gameObject.activeSelf);
                }
            }
        }

        public virtual void UpdateTags()
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            Transform originalT, cloneT;

            foreach (var pair in _cloneByOriginal)
            {
                if ((originalT = pair.Key as Transform) && (cloneT = pair.Value as Transform))
                    originalT.tag = cloneT.tag;
            }
        }

        public virtual void UpdateLayers()
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            Transform originalT, cloneT;

            foreach (var pair in _cloneByOriginal)
            {
                if ((originalT = pair.Key as Transform) && (cloneT = pair.Value as Transform))
                    originalT.gameObject.layer = cloneT.gameObject.layer;
            }
        }

        public virtual void UpdateTransforms()
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            Transform original, clone;

            foreach (var pair in _cloneByOriginal)
            {
                if ((original = pair.Key as Transform) && (clone = pair.Value as Transform))
                {
                    clone.localPosition = original.localPosition;
                    clone.localRotation = original.localRotation;
                    clone.localScale = original.localScale;
                }
            }
        }

        public virtual void UpdateHierachy()
        {
            if (_cloneByOriginal == null || _cloneByOriginal.Count == 0)
                UpdateCache();

            Transform original, clone;

            foreach (var pair in _cloneByOriginal)
            {
                if ((original = pair.Key as Transform) && (clone = pair.Value as Transform))
                {
                    if (TryGetClone(original.parent, out Transform cloneParent))
                    {
                        if (clone.parent != cloneParent)
                            clone.SetParent(cloneParent, false);
                    }
                    else if (clone.parent != original.parent)
                        clone.SetParent(original.parent, false);
                }
            }
        }

        public virtual void UpdateFields() => UpdateFields<Component>();

        public virtual void UpdateFieldsRelative() => UpdateFieldsRelative<Component>();

        public virtual void UpdateProperties() => UpdateProperties<Component>();

        public virtual void UpdatePropertiesRelative() => UpdatePropertiesRelative<Component>();

        #endregion
    }
}
