using Misc.EditorHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.PropertyBlocks
{
    public abstract class RendererProperty : MonoBehaviour
    {
        [SerializeField] public Renderer _target;
        public Renderer target {
            get => _target;
            set {
                if (_target != value)
                {
                    if (isActiveAndEnabled)
                    {
                        RemoveFromRenderer();
                        Validate.UpdateField(this, nameof(_target), _target = value);
                        AddToRenderer();
                    }
                    else
                        Validate.UpdateField(this, nameof(_target), _target = value);
                }
            }
        }

        [SerializeField] public Mode _targetMode = Mode.PropertyBlock;
        public Mode targetMode {
            get => _targetMode;
            set {
                if (_targetMode != value)
                {
                    if (isActiveAndEnabled)
                    {
                        RemoveFromRenderer();
                        Validate.UpdateField(this, nameof(_targetMode), _targetMode = value);
                        AddToRenderer();
                    }
                    else
                        Validate.UpdateField(this, nameof(_targetMode), _targetMode = value);
                }
            }
        }

        public enum Mode
        {
            Material = 0,
            SharedMaterial = 1,
            PropertyBlock = 2
        }

        [SerializeField] public int _materialIndex = -1;
        public int materialIndex {
            get => _materialIndex;
            set {
                if (_materialIndex != value)
                {
                    if (isActiveAndEnabled)
                    {
                        RemoveFromRenderer();
                        Validate.UpdateField(this, nameof(_materialIndex), _materialIndex = value);
                        AddToRenderer();
                    }
                    else
                        Validate.UpdateField(this, nameof(_materialIndex), _materialIndex = value);
                }
            }
        }

        private MaterialPropertyBlock _propertyBlock;

        protected virtual void Reset()
        {
            target = GetComponent<Renderer>();
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_targetMode), nameof(targetMode));
            Validate.FieldWithProperty(this, nameof(_target), nameof(target));
            Validate.FieldWithProperty(this, nameof(_materialIndex), nameof(materialIndex));
        }

        protected virtual void OnEnable() => AddToRenderer();

        protected virtual void OnDisable() => RemoveFromRenderer();

        protected abstract void AddToMaterialBlock(Material sharedMaterial, MaterialPropertyBlock materialPropertyBlock);

        protected abstract void RemoveFromMaterialBlock(Material sharedMaterial, MaterialPropertyBlock materialPropertyBlock);

        protected abstract void AddToMaterial(Material sharedMaterial, Material material);

        protected abstract void RemoveFromMaterial(Material sharedMaterial, Material material);

        protected abstract void AddToSharedMaterial(Material sharedMaterial);

        protected abstract void RemoveFromSharedMaterial(Material sharedMaterial);

        protected virtual void AddToRenderer()
        {
            if (target)
            {
                Material[] sharedMaterials = target.sharedMaterials;
                Material material;

                switch (_targetMode)
                {
                    case Mode.PropertyBlock:
                    {
                        if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();

                        if (materialIndex < 0)
                        {
                            for (int i = 0; i < sharedMaterials.Length; i++)
                            {
                                material = sharedMaterials[i];

                                if (PropertyIsValid(material))
                                    AddToPropertyBlock(material, i);
                            }
                        }
                        else if (sharedMaterials.Length > 0 && materialIndex < sharedMaterials.Length)
                        {
                            material = sharedMaterials[materialIndex];

                            if (PropertyIsValid(material))
                                AddToPropertyBlock(material, materialIndex);
                        }

                        break;
                    }

                    case Mode.SharedMaterial:
                        if (Application.isPlaying)
                        {
                            if (materialIndex < 0)
                            {
                                for (int i = 0; i < sharedMaterials.Length; i++)
                                {
                                    material = sharedMaterials[i];

                                    if (PropertyIsValid(material))
                                        AddToSharedMaterial(material);
                                }
                            }
                            else if (sharedMaterials.Length > 0 && materialIndex < sharedMaterials.Length)
                            {
                                material = sharedMaterials[materialIndex];

                                if (PropertyIsValid(material))
                                    AddToSharedMaterial(material);
                            }
                        }
                        break;

                    default:
                        if (Application.isPlaying)
                        {
                            Material[] materials = target.materials;

                            if (materialIndex < 0)
                            {
                                for (int i = 0; i < sharedMaterials.Length; i++)
                                {
                                    material = materials[i];

                                    if (PropertyIsValid(material))
                                        AddToMaterial(sharedMaterials[i], material);
                                }
                            }
                            else if (sharedMaterials.Length > 0 && materialIndex < sharedMaterials.Length)
                            {
                                material = materials[materialIndex];

                                if (PropertyIsValid(material))
                                    AddToMaterial(sharedMaterials[materialIndex], material);
                            }
                        }

                        break;
                }
            }
        }

        protected virtual void AddToPropertyBlock(Material sharedMaterial, int i)
        {
            _propertyBlock.Clear();
            target.GetPropertyBlock(_propertyBlock, i);

            AddToMaterialBlock(sharedMaterial, _propertyBlock);

            if (!_propertyBlock.isEmpty)
                target.SetPropertyBlock(_propertyBlock, i);
            else
                target.SetPropertyBlock(null, i);
        }

        protected virtual void RemoveFromRenderer()
        {
            if (target && target.HasPropertyBlock())
            {
                Material[] sharedMaterials = target.sharedMaterials;
                Material material;

                switch (_targetMode)
                {
                    case Mode.PropertyBlock:
                    {
                        if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();

                        if (materialIndex < 0)
                        {
                            for (int i = 0; i < sharedMaterials.Length; i++)
                            {
                                material = sharedMaterials[i];

                                if (PropertyIsValid(material))
                                    RemoveFromPropertyBlock(material, i);
                            }
                        }
                        else if (sharedMaterials.Length > 0 && materialIndex < sharedMaterials.Length)
                        {
                            material = sharedMaterials[materialIndex];

                            if (PropertyIsValid(material))
                                RemoveFromPropertyBlock(material, materialIndex);
                        }

                        break;
                    }

                    case Mode.SharedMaterial:
                        if (Application.isPlaying)
                        {
                            if (materialIndex < 0)
                            {
                                for (int i = 0; i < sharedMaterials.Length; i++)
                                {
                                    material = sharedMaterials[i];

                                    if (PropertyIsValid(material))
                                        RemoveFromSharedMaterial(material);
                                }
                            }
                            else if (sharedMaterials.Length > 0 && materialIndex < sharedMaterials.Length)
                            {
                                material = sharedMaterials[materialIndex];

                                if (PropertyIsValid(material))
                                    RemoveFromSharedMaterial(material);
                            }
                        }
                        break;

                    default:
                        if (Application.isPlaying)
                        {
                            Material[] materials = target.materials;

                            if (materialIndex < 0)
                            {
                                for (int i = 0; i < sharedMaterials.Length; i++)
                                {
                                    material = materials[i];

                                    if (PropertyIsValid(material))
                                        RemoveFromMaterial(sharedMaterials[i], material);
                                }
                            }
                            else if (sharedMaterials.Length > 0 && materialIndex < sharedMaterials.Length)
                            {
                                material = materials[materialIndex];

                                if (PropertyIsValid(material))
                                    RemoveFromMaterial(sharedMaterials[materialIndex], material);
                            }
                        }

                        break;
                }
            }
        }

        protected virtual bool PropertyIsValid(Material sharedMaterial) => true;

        protected virtual void RemoveFromPropertyBlock(Material sharedMaterial, int i)
        {
            _propertyBlock.Clear();
            target.GetPropertyBlock(_propertyBlock, i);

            RemoveFromMaterialBlock(sharedMaterial, _propertyBlock);

            if (!_propertyBlock.isEmpty)
                target.SetPropertyBlock(_propertyBlock, i);
            else
                target.SetPropertyBlock(null, i);
        }
    }

    public abstract class RendererProperty<T> : RendererProperty
    {
        [SerializeField] public string _propertyName = "_Property";
        public string propertyName {
            get => _propertyName;
            set {
                if (_propertyName != value)
                {
                    RemoveFromRenderer();
                    Validate.UpdateField(this, nameof(_propertyName), _propertyName = value);
                    AddToRenderer();
                }
            }
        }

        [SerializeField] public T _propertyValue;
        public T propertyValue {
            get => _propertyValue;
            set {
                if (!EqualityComparer<T>.Default.Equals(_propertyValue, value))
                {
                    RemoveFromRenderer();
                    Validate.UpdateField(this, nameof(_propertyValue), _propertyValue = value);
                    AddToRenderer();
                }
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            Validate.FieldWithProperty(this, nameof(_propertyName), nameof(propertyName));
            Validate.FieldWithProperty(this, nameof(_propertyValue), nameof(propertyValue));
        }
        protected override bool PropertyIsValid(Material sharedMaterial) => sharedMaterial.HasProperty(_propertyName);

        protected override void AddToRenderer()
        {
            if (!string.IsNullOrWhiteSpace(_propertyName)) base.AddToRenderer();
        }

        protected override void RemoveFromRenderer()
        {
            if (!string.IsNullOrWhiteSpace(_propertyName)) base.RemoveFromRenderer();
        }
    }
}
