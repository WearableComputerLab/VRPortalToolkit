using Misc;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Misc.EditorHelpers
{
    [CustomPropertyDrawer(typeof(ElementAsPropertyAttribute))]
    public class ElementAsPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ElementAsPropertyAttribute propertyAttribute = attribute as ElementAsPropertyAttribute;

            if (propertyAttribute != null)
            {
                if (propertyAttribute.propertyName == "")
                    label = null;
                else if (propertyAttribute.propertyName != null)
                    label.text = propertyAttribute.propertyName;

                SerializedProperty newProperty = property.FindPropertyRelative(propertyAttribute.propertyName);

                if (newProperty != null)
                {
                    EditorGUI.PropertyField(position, newProperty, label);
                    return;
                }
            }

            EditorGUI.PropertyField(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ElementAsPropertyAttribute propertyAttribute = attribute as ElementAsPropertyAttribute;

            if (propertyAttribute != null)
            {
                if (propertyAttribute.propertyName == "")
                    label = null;
                else if (propertyAttribute.propertyName != null)
                    label.text = propertyAttribute.propertyName;

                SerializedProperty newProperty = property.FindPropertyRelative(propertyAttribute.propertyName);

                if (newProperty != null) return EditorGUI.GetPropertyHeight(newProperty, label);
            }

            return EditorGUI.GetPropertyHeight(property, label);
        }
    }
}
