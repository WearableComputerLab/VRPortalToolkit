using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Misc.Data
{
    [CustomPropertyDrawer(typeof(IntRange))]
    public class IntRangeDrawer : PropertyDrawer
    {
        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, GUIContent.none, property))
            {
                SerializedProperty minimum = property.FindPropertyRelative("min");
                SerializedProperty maximum = property.FindPropertyRelative("max");

                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

                float originalLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 30f;

                float fieldWidth = position.width * 0.5f;

                minimum.intValue = EditorGUI.IntField(new Rect(position.x, position.y, fieldWidth - 2, position.height), "Min", minimum.intValue);
                maximum.intValue = EditorGUI.IntField(new Rect(position.x + fieldWidth + 2, position.y, fieldWidth, position.height), "Max", maximum.intValue);

                EditorGUIUtility.labelWidth = originalLabelWidth;
            }
        }
    }
}
