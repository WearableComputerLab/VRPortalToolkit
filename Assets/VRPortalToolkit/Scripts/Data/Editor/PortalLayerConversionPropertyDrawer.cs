using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRPortalToolkit
{
    [CustomPropertyDrawer(typeof(PortalLayerConversion), true)]
    public class PortalLayerConversionPropertyDrawer : PropertyDrawer
    {
        protected virtual SerializedProperty GetOutsideProperty(SerializedProperty property) => property.FindPropertyRelative("outside");

        protected virtual SerializedProperty GetBetweenProperty(SerializedProperty property) => property.FindPropertyRelative("between");

        protected virtual SerializedProperty GetInsideProperty(SerializedProperty property) => property.FindPropertyRelative("inside");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty outsideProperty = GetOutsideProperty(property);
            SerializedProperty betweenProperty = GetBetweenProperty(property);
            SerializedProperty insideProperty = GetInsideProperty(property);

            if (outsideProperty != null && betweenProperty != null && insideProperty != null)
            {
                float width = (position.width - EditorGUIUtility.singleLineHeight * 2f) / 3f;

                DrawOutsideProperty(new Rect(position.x, position.y, width, position.height), outsideProperty);

                EditorGUI.LabelField(new Rect(position.x + width, position.y, EditorGUIUtility.singleLineHeight, position.height - 4f), ">>", EditorStyles.label);

                DrawBetweenProperty(new Rect(position.x + width + EditorGUIUtility.singleLineHeight, position.y, width, position.height), betweenProperty);

                EditorGUI.LabelField(new Rect(position.x + width * 2f + EditorGUIUtility.singleLineHeight, position.y, EditorGUIUtility.singleLineHeight, position.height - 4f), ">>", EditorStyles.label);

                DrawInsideProperty(new Rect(position.x + (width + EditorGUIUtility.singleLineHeight) * 2f, position.y, width, position.height), insideProperty);
            }
            else
                EditorGUI.PropertyField(position, property, label);
        }

        protected virtual void DrawOutsideProperty(Rect position, SerializedProperty property)
            => property.intValue = EditorGUI.LayerField(position, property.intValue);

        protected virtual void DrawBetweenProperty(Rect position, SerializedProperty property)
            => property.intValue = EditorGUI.LayerField(position, property.intValue);

        protected virtual void DrawInsideProperty(Rect position, SerializedProperty property)
            => property.intValue = EditorGUI.LayerField(position, property.intValue);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty outsideProperty = GetOutsideProperty(property);
            SerializedProperty betweenProperty = GetBetweenProperty(property);
            SerializedProperty insideProperty = GetInsideProperty(property);

            if (outsideProperty != null && betweenProperty != null && insideProperty != null)
                return Mathf.Max(GetFromHeight(outsideProperty), GetBetweenHeight(betweenProperty), GetToHeight(outsideProperty));
            else
                return EditorGUI.GetPropertyHeight(property, label);
        }

        protected virtual float GetFromHeight(SerializedProperty property) => EditorGUI.GetPropertyHeight(property, true);

        protected virtual float GetBetweenHeight(SerializedProperty property) => EditorGUI.GetPropertyHeight(property, true);

        protected virtual float GetToHeight(SerializedProperty property) => EditorGUI.GetPropertyHeight(property, true);
    }
}
