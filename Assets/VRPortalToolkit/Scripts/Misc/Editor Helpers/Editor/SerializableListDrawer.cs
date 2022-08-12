using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Misc.Tasks.WaitTask;

namespace Misc.EditorHelpers
{
    [CustomPropertyDrawer(typeof(SerializedList), true)]
    public class SerializableListDrawer : PropertyDrawer
    {
        protected virtual SerializedProperty GetListProperty(SerializedProperty property) => property.FindPropertyRelative("list");

        protected virtual SerializedList GetList(SerializedProperty property)
        {
            if (fieldInfo != null)
            {
                object parent = property.GetParentObject();

                if (parent != null) return fieldInfo.GetValue(parent) as SerializedList;
            }

            return null;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty listProperty = GetListProperty(property);

            bool shouldDisable = false;

            foreach (Object obj in property.serializedObject.targetObjects)
            {
                if (listProperty.TryGetObject(obj, out SerializedList list) && list.IsReadOnly)
                    shouldDisable = true;
            }

            EditorGUI.BeginDisabledGroup(shouldDisable);

            EditorGUI.BeginChangeCheck();

            if (listProperty != null)
                EditorGUI.PropertyField(position, listProperty, label, true);
            else
                this.NextPropertyField(position, property, label);

            if (EditorGUI.EndChangeCheck())
            {
                foreach (Object obj in property.serializedObject.targetObjects)
                {
                    if (listProperty.TryGetObject(obj, out SerializedList list))
                        list.Validate();
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty listProperty = GetListProperty(property);

            if (listProperty != null)
                return EditorGUI.GetPropertyHeight(listProperty, label, true);

            return this.GetNextPropertyHeight(property, label);
        }
    }
}
