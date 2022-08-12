using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Misc.EditorHelpers
{
    [CustomPropertyDrawer(typeof(ExpandWithAttribute))]
    public class ExpandWithPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ExpandWithAttribute expandAttribute = attribute as ExpandWithAttribute;

            if (ShouldExpand(expandAttribute, property))
            {
                if (expandAttribute.indent)
                {
                    EditorGUI.indentLevel++;
                    this.NextPropertyField(position, property, label);
                    EditorGUI.indentLevel--;
                }
                else
                    this.NextPropertyField(position, property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (ShouldExpand(attribute as ExpandWithAttribute, property))
                return this.GetNextPropertyHeight(property);

            return 0f;
        }

        protected static bool ShouldExpand(ExpandWithAttribute expandAttribute, SerializedProperty property)
        {
            if (expandAttribute != null)
            {
                SerializedProperty parent = property.GetParent(), master;

                if (parent == null)
                    master = property.serializedObject.FindProperty(expandAttribute.fieldName);
                else
                    master = parent.FindPropertyRelative(expandAttribute.fieldName);

                if (master != null) return master.isExpanded;
            }

            return false;
        }
    }
}
