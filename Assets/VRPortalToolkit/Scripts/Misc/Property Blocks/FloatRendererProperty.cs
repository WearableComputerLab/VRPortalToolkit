using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.PropertyBlocks
{
    [ExecuteInEditMode]
    public class FloatRendererProperty : RendererProperty<float>
    {
        protected override void AddToMaterialBlock(Material sharedMaterial, MaterialPropertyBlock materialPropertyBlock)
        {
            materialPropertyBlock.SetFloat(propertyName, propertyValue);
        }

        // TODO: This doesnt really remove it, I'm not sure if can be removed
        protected override void RemoveFromMaterialBlock(Material sharedMaterial, MaterialPropertyBlock materialPropertyBlock)
        {
            materialPropertyBlock.SetFloat(propertyName, sharedMaterial.GetFloat(propertyName));
        }

        protected override void AddToMaterial(Material sharedMaterial, Material material)
        {
            material.SetFloat(propertyName, propertyValue);
        }

        protected override void RemoveFromMaterial(Material sharedMaterial, Material material)
        {
            material.SetFloat(propertyName, sharedMaterial.GetFloat(propertyName));
        }

        protected override void AddToSharedMaterial(Material sharedMaterial)
        {
            sharedMaterial.SetFloat(propertyName, propertyValue);
        }

        protected override void RemoveFromSharedMaterial(Material sharedMaterial)
        {
            sharedMaterial.SetFloat(propertyName, sharedMaterial.shader.GetPropertyDefaultFloatValue(sharedMaterial.shader.FindPropertyIndex(propertyName)));
        }
    }
}