using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Misc.Update
{
    [CustomPropertyDrawer(typeof(StateRequest<>), true)]
    public class StateRequestDrawer : PropertyDrawer
    {
        protected static string[] Options = { "Deactive", "Active" };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            Rect sourcePos = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height - EditorGUIUtility.standardVerticalSpacing),
                statePos = new Rect(sourcePos.x + sourcePos.width + EditorGUIUtility.standardVerticalSpacing * 2f, position.y,
                    position.width - sourcePos.width - EditorGUIUtility.standardVerticalSpacing * 2f, position.height);

            EditorGUI.PropertyField(sourcePos, property.FindPropertyRelative("source"), GUIContent.none, true);

            SerializedProperty stateProperty = property.FindPropertyRelative("state");

            EditorGUI.BeginChangeCheck();
            int state = EditorGUI.Popup(statePos, stateProperty.boolValue ? 1 : 0, Options);
            if (EditorGUI.EndChangeCheck()) stateProperty.boolValue = state == 1;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}