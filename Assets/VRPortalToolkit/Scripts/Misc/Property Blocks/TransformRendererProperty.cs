using Misc.EditorHelpers;
using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.PropertyBlocks
{
    [ExecuteInEditMode]
    public class TransformRendererProperty : RendererProperty<Transform>
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.LateUpdate);
        public UpdateMask UpdateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] public Type _propertyType = Type.LocalToWorldMatrix;
        public Type propertyType {
            get => _propertyType;
            set {
                if (_propertyType != value)
                {
                    if (isActiveAndEnabled)
                    {
                        RemoveFromRenderer();
                        Validate.UpdateField(this, nameof(_propertyType), _propertyType = value);
                        AddToRenderer();
                    }
                    else
                        Validate.UpdateField(this, nameof(_propertyType), _propertyType = value );
                }
            }
        }

        public enum Type
        {
            LocalToWorldMatrix,
            LocalPosition,
            LocalPositionX,
            LocalPositionY,
            LocalPositionZ,
            LocalRotation,
            LocalEulerAngles,
            LocalEulerAnglesX,
            LocalEulerAnglesY,
            LocalEulerAnglesZ,
            LocalScale,
            WorldToLocalMatrix,
            Position,
            PositionX,
            PositionY,
            PositionZ,
            Rotation,
            EulerAngles,
            EulerAnglesX,
            EulerAnglesY,
            EulerAnglesZ,
            LossyScale,
        }

        protected override void Reset()
        {
            base.Reset();
            propertyValue = transform;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            Validate.FieldWithProperty(this, nameof(_propertyType), nameof(propertyType));
        }

        protected virtual void Awake()
        {
            updater.updateMask = _updateMask;
            updater.onInvoke = ForceApply;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            updater.enabled = true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            updater.enabled = false;
        }


        public virtual void Apply()
        {
            if (isActiveAndEnabled && Application.isPlaying && !updater.isUpdating) ForceApply();
        }

        public virtual void ForceApply()
        {
            AddToRenderer();
        }

        protected override void AddToMaterialBlock(Material sharedMaterial, MaterialPropertyBlock materialPropertyBlock)
        {
            switch (_propertyType)
            {
                case Type.LocalPosition:
                    materialPropertyBlock.SetVector(propertyName, propertyValue.localPosition);
                    break;
                case Type.LocalPositionX:
                    materialPropertyBlock.SetFloat(propertyName, propertyValue.localPosition.x);
                    break;
                case Type.LocalPositionY:
                    materialPropertyBlock.SetFloat(propertyName, propertyValue.localPosition.y);
                    break;
                case Type.LocalPositionZ:
                    materialPropertyBlock.SetFloat(propertyName, propertyValue.localPosition.z);
                    break;
                case Type.LocalRotation:
                {
                    Quaternion rotation = propertyValue.localRotation;
                    materialPropertyBlock.SetVector(propertyName, new Vector4(rotation.x, rotation.y, rotation.z, rotation.w));
                }
                break;
                case Type.LocalEulerAngles:
                    materialPropertyBlock.SetVector(propertyName, propertyValue.localEulerAngles);
                    break;
                case Type.LocalEulerAnglesX:
                    materialPropertyBlock.SetFloat(propertyName, propertyValue.localEulerAngles.x);
                    break;
                case Type.LocalEulerAnglesY:
                    materialPropertyBlock.SetFloat(propertyName, propertyValue.localEulerAngles.y);
                    break;
                case Type.LocalEulerAnglesZ:
                    materialPropertyBlock.SetFloat(propertyName, propertyValue.localEulerAngles.z);
                    break;
                case Type.LocalScale:
                    materialPropertyBlock.SetVector(propertyName, propertyValue.localScale);
                    break;
                case Type.WorldToLocalMatrix:
                    materialPropertyBlock.SetMatrix(propertyName, propertyValue.worldToLocalMatrix);
                    break;
                case Type.Position:
                    materialPropertyBlock.SetVector(propertyName, propertyValue.position);
                    break;
                case Type.PositionX:
                    materialPropertyBlock.SetFloat(propertyName, propertyValue.position.x);
                    break;
                case Type.PositionY:
                    materialPropertyBlock.SetFloat(propertyName, propertyValue.position.y);
                    break;
                case Type.PositionZ:
                    materialPropertyBlock.SetFloat(propertyName, propertyValue.position.z);
                    break;
                case Type.Rotation:
                {
                    Quaternion rotation = propertyValue.rotation;
                    materialPropertyBlock.SetVector(propertyName, new Vector4(rotation.x, rotation.y, rotation.z, rotation.w));
                }
                break;
                case Type.EulerAngles:
                    materialPropertyBlock.SetVector(propertyName, propertyValue.eulerAngles);
                    break;
                case Type.EulerAnglesX:
                    materialPropertyBlock.SetFloat(propertyName, propertyValue.eulerAngles.x);
                    break;
                case Type.EulerAnglesY:
                    materialPropertyBlock.SetFloat(propertyName, propertyValue.eulerAngles.y);
                    break;
                case Type.EulerAnglesZ:
                    materialPropertyBlock.SetFloat(propertyName, propertyValue.eulerAngles.z);
                    break;
                case Type.LossyScale:
                    materialPropertyBlock.SetVector(propertyName, propertyValue.lossyScale);
                    break;
                default:
                    materialPropertyBlock.SetMatrix(propertyName, propertyValue.localToWorldMatrix);
                    break;
            };
        }

        // TODO: This doesnt really remove it, I'm not sure if can be removed
        protected override void RemoveFromMaterialBlock(Material sharedMaterial, MaterialPropertyBlock materialPropertyBlock)
        {
            switch (_propertyType)
            {
                case Type.LocalPosition:
                case Type.LocalRotation:
                case Type.LocalEulerAngles:
                case Type.LocalScale:
                case Type.Position:
                case Type.Rotation:
                case Type.EulerAngles:
                case Type.LossyScale:
                    materialPropertyBlock.SetVector(propertyName, sharedMaterial.GetVector(propertyName));
                    break;
                case Type.LocalPositionX:
                case Type.LocalPositionY:
                case Type.LocalPositionZ:
                case Type.LocalEulerAnglesX:
                case Type.LocalEulerAnglesY:
                case Type.LocalEulerAnglesZ:
                case Type.PositionX:
                case Type.PositionY:
                case Type.PositionZ:
                case Type.EulerAnglesX:
                case Type.EulerAnglesY:
                case Type.EulerAnglesZ:
                    materialPropertyBlock.SetFloat(propertyName, sharedMaterial.GetFloat(propertyName));
                    break;
                case Type.WorldToLocalMatrix:
                default:
                    materialPropertyBlock.SetMatrix(propertyName, sharedMaterial.GetMatrix(propertyName));
                    break;
            };
        }

        protected override void AddToMaterial(Material sharedMaterial, Material material)
        {
            switch (_propertyType)
            {
                case Type.LocalPosition:
                    material.SetVector(propertyName, propertyValue.localPosition);
                    break;
                case Type.LocalPositionX:
                    material.SetFloat(propertyName, propertyValue.localPosition.x);
                    break;
                case Type.LocalPositionY:
                    material.SetFloat(propertyName, propertyValue.localPosition.y);
                    break;
                case Type.LocalPositionZ:
                    material.SetFloat(propertyName, propertyValue.localPosition.z);
                    break;
                case Type.LocalRotation:
                {
                    Quaternion rotation = propertyValue.localRotation;
                    material.SetVector(propertyName, new Vector4(rotation.x, rotation.y, rotation.z, rotation.w));
                }
                break;
                case Type.LocalEulerAngles:
                    material.SetVector(propertyName, propertyValue.localEulerAngles);
                    break;
                case Type.LocalEulerAnglesX:
                    material.SetFloat(propertyName, propertyValue.localEulerAngles.x);
                    break;
                case Type.LocalEulerAnglesY:
                    material.SetFloat(propertyName, propertyValue.localEulerAngles.y);
                    break;
                case Type.LocalEulerAnglesZ:
                    material.SetFloat(propertyName, propertyValue.localEulerAngles.z);
                    break;
                case Type.LocalScale:
                    material.SetVector(propertyName, propertyValue.localScale);
                    break;
                case Type.WorldToLocalMatrix:
                    material.SetMatrix(propertyName, propertyValue.worldToLocalMatrix);
                    break;
                case Type.Position:
                    material.SetVector(propertyName, propertyValue.position);
                    break;
                case Type.PositionX:
                    material.SetFloat(propertyName, propertyValue.position.x);
                    break;
                case Type.PositionY:
                    material.SetFloat(propertyName, propertyValue.position.y);
                    break;
                case Type.PositionZ:
                    material.SetFloat(propertyName, propertyValue.position.z);
                    break;
                case Type.Rotation:
                {
                    Quaternion rotation = propertyValue.rotation;
                    material.SetVector(propertyName, new Vector4(rotation.x, rotation.y, rotation.z, rotation.w));
                }
                break;
                case Type.EulerAngles:
                    material.SetVector(propertyName, propertyValue.eulerAngles);
                    break;
                case Type.EulerAnglesX:
                    material.SetFloat(propertyName, propertyValue.eulerAngles.x);
                    break;
                case Type.EulerAnglesY:
                    material.SetFloat(propertyName, propertyValue.eulerAngles.y);
                    break;
                case Type.EulerAnglesZ:
                    material.SetFloat(propertyName, propertyValue.eulerAngles.z);
                    break;
                case Type.LossyScale:
                    material.SetVector(propertyName, propertyValue.lossyScale);
                    break;
                default:
                    material.SetMatrix(propertyName, propertyValue.localToWorldMatrix);
                    break;
            };
        }

        protected override void RemoveFromMaterial(Material sharedMaterial, Material material)
        {
            switch (_propertyType)
            {
                case Type.LocalPosition:
                case Type.LocalRotation:
                case Type.LocalEulerAngles:
                case Type.LocalScale:
                case Type.Position:
                case Type.Rotation:
                case Type.EulerAngles:
                case Type.LossyScale:
                    material.SetVector(propertyName, sharedMaterial.GetVector(propertyName));
                    break;
                case Type.LocalPositionX:
                case Type.LocalPositionY:
                case Type.LocalPositionZ:
                case Type.LocalEulerAnglesX:
                case Type.LocalEulerAnglesY:
                case Type.LocalEulerAnglesZ:
                case Type.PositionX:
                case Type.PositionY:
                case Type.PositionZ:
                case Type.EulerAnglesX:
                case Type.EulerAnglesY:
                case Type.EulerAnglesZ:
                    material.SetFloat(propertyName, sharedMaterial.GetFloat(propertyName));
                    break;
                case Type.WorldToLocalMatrix:
                default:
                    material.SetMatrix(propertyName, sharedMaterial.GetMatrix(propertyName));
                    break;
            };
        }

        protected override void AddToSharedMaterial(Material sharedMaterial)
        {
            switch (_propertyType)
            {
                case Type.LocalPosition:
                    sharedMaterial.SetVector(propertyName, propertyValue.localPosition);
                    break;
                case Type.LocalPositionX:
                    sharedMaterial.SetFloat(propertyName, propertyValue.localPosition.x);
                    break;
                case Type.LocalPositionY:
                    sharedMaterial.SetFloat(propertyName, propertyValue.localPosition.y);
                    break;
                case Type.LocalPositionZ:
                    sharedMaterial.SetFloat(propertyName, propertyValue.localPosition.z);
                    break;
                case Type.LocalRotation:
                {
                    Quaternion rotation = propertyValue.localRotation;
                    sharedMaterial.SetVector(propertyName, new Vector4(rotation.x, rotation.y, rotation.z, rotation.w));
                }
                break;
                case Type.LocalEulerAngles:
                    sharedMaterial.SetVector(propertyName, propertyValue.localEulerAngles);
                    break;
                case Type.LocalEulerAnglesX:
                    sharedMaterial.SetFloat(propertyName, propertyValue.localEulerAngles.x);
                    break;
                case Type.LocalEulerAnglesY:
                    sharedMaterial.SetFloat(propertyName, propertyValue.localEulerAngles.y);
                    break;
                case Type.LocalEulerAnglesZ:
                    sharedMaterial.SetFloat(propertyName, propertyValue.localEulerAngles.z);
                    break;
                case Type.LocalScale:
                    sharedMaterial.SetVector(propertyName, propertyValue.localScale);
                    break;
                case Type.WorldToLocalMatrix:
                    sharedMaterial.SetMatrix(propertyName, propertyValue.worldToLocalMatrix);
                    break;
                case Type.Position:
                    sharedMaterial.SetVector(propertyName, propertyValue.position);
                    break;
                case Type.PositionX:
                    sharedMaterial.SetFloat(propertyName, propertyValue.position.x);
                    break;
                case Type.PositionY:
                    sharedMaterial.SetFloat(propertyName, propertyValue.position.y);
                    break;
                case Type.PositionZ:
                    sharedMaterial.SetFloat(propertyName, propertyValue.position.z);
                    break;
                case Type.Rotation:
                {
                    Quaternion rotation = propertyValue.rotation;
                    sharedMaterial.SetVector(propertyName, new Vector4(rotation.x, rotation.y, rotation.z, rotation.w));
                }
                break;
                case Type.EulerAngles:
                    sharedMaterial.SetVector(propertyName, propertyValue.eulerAngles);
                    break;
                case Type.EulerAnglesX:
                    sharedMaterial.SetFloat(propertyName, propertyValue.eulerAngles.x);
                    break;
                case Type.EulerAnglesY:
                    sharedMaterial.SetFloat(propertyName, propertyValue.eulerAngles.y);
                    break;
                case Type.EulerAnglesZ:
                    sharedMaterial.SetFloat(propertyName, propertyValue.eulerAngles.z);
                    break;
                case Type.LossyScale:
                    sharedMaterial.SetVector(propertyName, propertyValue.lossyScale);
                    break;
                default:
                    sharedMaterial.SetMatrix(propertyName, propertyValue.localToWorldMatrix);
                    break;
            };
        }

        protected override void RemoveFromSharedMaterial(Material sharedMaterial)
        {
            if (sharedMaterial.shader)
            {
                switch (_propertyType)
                {
                    default:
                        sharedMaterial.SetColor(propertyName, sharedMaterial.shader.GetPropertyDefaultVectorValue(sharedMaterial.shader.FindPropertyIndex(propertyName)));
                        break;
                };
                switch (_propertyType)
                {
                    case Type.LocalPosition:
                    case Type.LocalRotation:
                    case Type.LocalEulerAngles:
                    case Type.LocalScale:
                    case Type.Position:
                    case Type.Rotation:
                    case Type.EulerAngles:
                    case Type.LossyScale:
                        sharedMaterial.SetVector(propertyName, sharedMaterial.shader.GetPropertyDefaultVectorValue(sharedMaterial.shader.FindPropertyIndex(propertyName)));
                        break;
                    case Type.LocalPositionX:
                    case Type.LocalPositionY:
                    case Type.LocalPositionZ:
                    case Type.LocalEulerAnglesX:
                    case Type.LocalEulerAnglesY:
                    case Type.LocalEulerAnglesZ:
                    case Type.PositionX:
                    case Type.PositionY:
                    case Type.PositionZ:
                    case Type.EulerAnglesX:
                    case Type.EulerAnglesY:
                    case Type.EulerAnglesZ:
                        sharedMaterial.SetFloat(propertyName, sharedMaterial.shader.GetPropertyDefaultFloatValue(sharedMaterial.shader.FindPropertyIndex(propertyName)));
                        break;
                    case Type.WorldToLocalMatrix:
                    default:
                        sharedMaterial.SetMatrix(propertyName, Matrix4x4.identity);
                        break;
                };
            }
        }

        /*[CalledBeforeChangeOf(nameof(Type))]
        protected virtual void OnBeforeTypeChange() => RemoveFromRenderer();

        [CalledAfterChangeOf(nameof(Type))]
        protected virtual void OnAfterTypeChange() => AddToRenderer();*/
    }
}