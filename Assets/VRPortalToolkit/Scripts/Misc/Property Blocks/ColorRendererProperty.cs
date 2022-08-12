using Misc.EditorHelpers;
using UnityEngine;

namespace Misc.PropertyBlocks
{
    [ExecuteInEditMode]
    public class ColorRendererProperty : RendererProperty<Color>
    {
        [SerializeField] public Type _propertyType = Type.Color;
        public Type propertyType {
            get => _propertyType;
            set {
                if (_propertyType != value)
                {
                    RemoveFromRenderer();
                    Validate.UpdateField(this, nameof(_propertyType), _propertyType = value);
                    AddToRenderer();
                }
            }
        }

        public enum Type
        {
            Color = 0,
            Texture = 1
        }

        protected Texture2D texture;

        protected override void Reset()
        {
            base.Reset();
            propertyName = "_Color";
            propertyValue = Color.white;
        }

        protected override void AddToMaterialBlock(Material sharedMaterial, MaterialPropertyBlock materialPropertyBlock)
        {
            switch (_propertyType)
            {
                case Type.Texture:
                    materialPropertyBlock.SetTexture(propertyName, GetTexture());
                    break;

                default:
                    materialPropertyBlock.SetColor(propertyName, propertyValue);
                    break;
            };
        }

        // TODO: This doesnt really remove it, I'm not sure if can be removed
        protected override void RemoveFromMaterialBlock(Material sharedMaterial, MaterialPropertyBlock materialPropertyBlock)
        {
            switch (_propertyType)
            {
                case Type.Texture:
                    materialPropertyBlock.SetTexture(propertyName, sharedMaterial.GetTexture(propertyName));
                    break;

                default:
                    materialPropertyBlock.SetColor(propertyName, sharedMaterial.GetColor(propertyName));
                    break;
            };
        }

        protected override void AddToMaterial(Material sharedMaterial, Material material)
        {
            switch (_propertyType)
            {
                case Type.Texture:
                    material.SetTexture(propertyName, GetTexture());
                    break;

                default:
                    material.SetColor(propertyName, propertyValue);
                    break;
            };
        }

        protected override void RemoveFromMaterial(Material sharedMaterial, Material material)
        {
            switch (_propertyType)
            {
                case Type.Texture:
                    material.SetTexture(propertyName, sharedMaterial.GetTexture(propertyName));
                    break;

                default:
                    material.SetColor(propertyName, sharedMaterial.GetColor(propertyName));
                    break;
            };
        }

        protected override void AddToSharedMaterial(Material sharedMaterial)
        {
            switch (_propertyType)
            {
                case Type.Texture:
                    sharedMaterial.SetTexture(propertyName, GetTexture());
                    break;

                default:
                    sharedMaterial.SetColor(propertyName, propertyValue);
                    break;
            };
        }

        protected override void RemoveFromSharedMaterial(Material sharedMaterial)
        {
            if (sharedMaterial.shader)
            {
                switch (_propertyType)
                {
                    case Type.Texture:
                        sharedMaterial.SetTexture(propertyName, Resources.Load(sharedMaterial.shader.GetPropertyTextureDefaultName(sharedMaterial.shader.FindPropertyIndex(propertyName))) as Texture);
                        break;

                    default:
                        sharedMaterial.SetColor(propertyName, sharedMaterial.shader.GetPropertyDefaultVectorValue(sharedMaterial.shader.FindPropertyIndex(propertyName)));
                        break;
                };
            }
        }

        protected virtual Texture GetTexture()
        {
            if (texture == null) texture = new Texture2D(1, 1);

            texture.SetPixels(new Color[] { propertyValue });
            texture.Apply();

            return texture;
        }
    }
}