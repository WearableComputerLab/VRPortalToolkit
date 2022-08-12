using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Misc.EditorHelpers
{

    [CustomPropertyDrawer(typeof(ConversionAttribute), true)]
    public class ConversionPropertyDrawer : PropertyDrawer
    {
        protected virtual SerializedProperty GetFromProperty(SerializedProperty property, string from) => property.FindPropertyRelative(from);

        protected virtual SerializedProperty GetToProperty(SerializedProperty property, string to) => property.FindPropertyRelative(to);

        private void GetFromAndToProperties(SerializedProperty property, out SerializedProperty fromProperty, out SerializedProperty toProperty)
        {
            if (attribute is ConversionAttribute conversion)
            {
                fromProperty = GetFromProperty(property, conversion.from);
                toProperty = GetToProperty(property, conversion.to);
            }
            else
            {
                fromProperty = GetFromProperty(property, "from");
                toProperty = GetToProperty(property, "to");
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GetFromAndToProperties(property, out SerializedProperty fromProperty, out SerializedProperty toProperty);

            if (fromProperty != null && toProperty != null)
            {
                ConversionAttribute conversion = attribute as ConversionAttribute;

                float labelWidth = 0f;

                if (conversion == null || conversion.showLabel)
                {
                    labelWidth = EditorGUIUtility.labelWidth;
                    EditorGUI.LabelField(new Rect(position.x, position.y, labelWidth, position.height), label);
                }

                float width = (position.width - labelWidth - EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing) * 0.5f,
                    positionX = position.x + labelWidth + EditorGUIUtility.standardVerticalSpacing;

                DrawFromProperty(new Rect(positionX, position.y, width, position.height), fromProperty);

                EditorGUI.LabelField(new Rect(positionX + width, position.y, EditorGUIUtility.singleLineHeight, position.height - 4f),
                    conversion == null ? conversion.arrow : ">>", EditorStyles.label);

                DrawToProperty(new Rect(positionX + width + EditorGUIUtility.singleLineHeight, position.y, width, position.height), toProperty);
            }
            else
                this.NextPropertyField(position, property, label);
        }

        protected virtual void DrawFromProperty(Rect position, SerializedProperty property) => SortedEditor.PropertyField(position, property, GUIContent.none);

        protected virtual void DrawToProperty(Rect position, SerializedProperty property) => SortedEditor.PropertyField(position, property, GUIContent.none);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            GetFromAndToProperties(property, out SerializedProperty fromProperty, out SerializedProperty toProperty);

            if (fromProperty != null && toProperty != null)
                return Mathf.Max(GetFromHeight(fromProperty), GetToHeight(fromProperty));
            else
                return this.GetNextPropertyHeight(property, label);
        }

        protected virtual float GetFromHeight(SerializedProperty property) => EditorGUI.GetPropertyHeight(property, true);

        protected virtual float GetToHeight(SerializedProperty property) => EditorGUI.GetPropertyHeight(property, true);
    }
}
