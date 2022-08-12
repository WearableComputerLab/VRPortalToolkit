using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Misc.EditorHelpers
{
    [CustomPropertyDrawer(typeof(GroupObjectAttribute))]
    public class GroupObjectPropertyDrawer : PropertyDrawer
    {
        private GUIStyle _foldoutStyle;
        protected GUIStyle foldoutStyle
        {
            get
            {
                if (_foldoutStyle == null)
                    _foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader);

                return _foldoutStyle;
            }
        }

        private GUIStyle _labelStyle;
        protected GUIStyle labelStyle
        {
            get
            {
                if (_labelStyle == null)
                    _labelStyle = new GUIStyle(EditorStyles.label);

                return _labelStyle;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GroupObjectAttribute groupAttribute = attribute as GroupObjectAttribute;

            // Draw header if required
            IEnumerator<SerializedProperty> properties = property.GetChildren().GetEnumerator();

            float currentY = position.y, currentHeight;

            if (groupAttribute.groupMask.HasFlag(GroupObjectMask.SpaceBefore))
                currentY += EditorGUIUtility.singleLineHeight * 0.5f;

            // Draw each field
            GUIStyle style = groupAttribute.groupMask.HasFlag(GroupObjectMask.Foldout) ? foldoutStyle : labelStyle;
            style.fontStyle = groupAttribute.groupMask.HasFlag(GroupObjectMask.BoldHeader) ? FontStyle.Bold : FontStyle.Normal;
            style.fontSize = groupAttribute.groupMask.HasFlag(GroupObjectMask.LargeHeader) ? EditorStyles.largeLabel.fontSize : EditorStyles.label.fontSize;

            string labelText = label.text;

            bool firstAsHeader = ((groupAttribute.groupMask & (GroupObjectMask.FirstAsHeader | GroupObjectMask.FirstAddNameToLabel | GroupObjectMask.FirstAddNameToLabel))) != 0,
                unmodifiedFirst = ((groupAttribute.groupMask & (GroupObjectMask.BoldHeader | GroupObjectMask.LargeHeader))) == 0;

            if (groupAttribute.groupMask.HasFlag(GroupObjectMask.Foldout))
            {
                if (groupAttribute.groupMask.HasFlag(GroupObjectMask.FirstAsHeader) && properties.MoveNext())
                {
                    if (groupAttribute.groupMask.HasFlag(GroupObjectMask.FirstReplaceLabel))
                        label.text = $"{labelText} {properties.Current.displayName}";
                    else if (groupAttribute.groupMask.HasFlag(GroupObjectMask.FirstAsHeader))
                        label.text = properties.Current.displayName;

                    currentHeight = EditorGUI.GetPropertyHeight(properties.Current);
                    property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(new Rect(position.x, currentY, position.width, currentHeight), property.isExpanded, label, style);

                    SortedEditor.PropertyField(new Rect(position.x + EditorGUIUtility.labelWidth + EditorGUIUtility.standardVerticalSpacing, currentY,
                        position.width - EditorGUIUtility.labelWidth - EditorGUIUtility.standardVerticalSpacing, currentHeight), properties.Current, GUIContent.none);
                    EditorGUI.EndFoldoutHeaderGroup();
                }
                else
                {
                    currentHeight = EditorGUIUtility.singleLineHeight;
                    property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(new Rect(position.x, currentY, position.width, currentHeight), property.isExpanded, label);
                    EditorGUI.EndFoldoutHeaderGroup();
                }
            }
            else
            {
                if (groupAttribute.groupMask.HasFlag(GroupObjectMask.FirstAsHeader) && properties.MoveNext())
                {
                    if (groupAttribute.groupMask.HasFlag(GroupObjectMask.FirstAddNameToLabel))
                    {
                        label.text = $"{labelText} {properties.Current.displayName}";
                        unmodifiedFirst = false;
                    }
                    else if (groupAttribute.groupMask.HasFlag(GroupObjectMask.FirstReplaceLabel))
                        unmodifiedFirst = false;

                    // No purpose styling if there are no styles
                    if (unmodifiedFirst)
                    {
                        currentHeight = EditorGUI.GetPropertyHeight(properties.Current);
                        SortedEditor.PropertyField(new Rect(position.x, currentY, position.width, currentHeight), properties.Current, label);
                    }
                    else
                    {
                        currentHeight = EditorGUI.GetPropertyHeight(properties.Current);
                        SortedEditor.PropertyField(new Rect(position.x + EditorGUIUtility.labelWidth + EditorGUIUtility.standardVerticalSpacing, currentY,
                            position.width - EditorGUIUtility.labelWidth - EditorGUIUtility.standardVerticalSpacing, currentHeight), properties.Current, GUIContent.none);
                        EditorGUILayout.LabelField(label, style);
                    }
                }
                else
                {
                    currentHeight = EditorGUIUtility.singleLineHeight;
                    EditorGUILayout.LabelField(label, style);
                }
            }

            currentY += currentHeight + EditorGUIUtility.standardVerticalSpacing;

            if (!groupAttribute.groupMask.HasFlag(GroupObjectMask.Foldout) || property.isExpanded)
            {
                if (groupAttribute.groupMask.HasFlag(GroupObjectMask.IndentFields))
                    EditorGUI.indentLevel++;

                while (properties.MoveNext())
                {
                    currentHeight = EditorGUI.GetPropertyHeight(properties.Current);
                    EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, currentHeight), properties.Current, label, true);
                    currentY += currentHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                if (groupAttribute.groupMask.HasFlag(GroupObjectMask.IndentFields))
                    EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            GroupObjectAttribute groupAttribute = attribute as GroupObjectAttribute;
            float height = -EditorGUIUtility.standardVerticalSpacing;

            if (groupAttribute.groupMask.HasFlag(GroupObjectMask.SpaceBefore))
                height += EditorGUIUtility.singleLineHeight * 0.5f;

            if (!groupAttribute.groupMask.HasFlag(GroupObjectMask.FirstAsHeader))
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (!groupAttribute.groupMask.HasFlag(GroupObjectMask.Foldout) || property.isExpanded)
            {
                foreach (SerializedProperty child in property.GetChildren())
                    height += SortedEditor.GetPropertyHeight(child, null) + EditorGUIUtility.standardVerticalSpacing;
            }

            if (groupAttribute.groupMask.HasFlag(GroupObjectMask.SpaceAfter))
                height += EditorGUIUtility.singleLineHeight * 0.5f;

            return height;
        }
    }
}
