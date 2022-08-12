using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Misc.EditorHelpers
{
    [CustomPropertyDrawer(typeof(GroupAttribute))]
    public class GroupPropertyDrawer : PropertyDrawer
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

        protected static Dictionary<GroupKey, GroupValue> groupsByKey = new Dictionary<GroupKey, GroupValue>();

        protected struct GroupKey
        {
            public SerializedObject serializedObject;
            public string groupName;

            public GroupKey(SerializedObject serializedObject, string groupName)
            {
                this.serializedObject = serializedObject;
                this.groupName = groupName;
            }
        }

        protected class GroupValue
        {
            public bool isExpanded = false;
            public string headPropertyPath;
            public GroupAttribute groupAttribute;

            public GroupValue(GroupAttribute groupAttribute, string headPropertyPath)
            {
                this.groupAttribute = groupAttribute;
                this.headPropertyPath = headPropertyPath;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GroupValue group = GetOrCreateGroup(attribute as GroupAttribute, property);

            if (group.headPropertyPath == property.propertyPath)
            {
                GUIStyle style = foldoutStyle;
                style.fontStyle = group.groupAttribute.groupMask.HasFlag(GroupMask.BoldHeader) ? FontStyle.Bold : FontStyle.Normal;
                style.fontSize = group.groupAttribute.groupMask.HasFlag(GroupMask.LargeHeader) ? EditorStyles.largeLabel.fontSize : EditorStyles.label.fontSize;
                
                label.text = group.groupAttribute.groupName;

                if (group.groupAttribute.groupMask.HasFlag(GroupMask.FirstAsHeader))
                {
                    Rect labelPosition = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);

                    if (EditorUtils.HandleContextEvent(position))
                        EditorUtils.DoPropertyContextMenu(property);

                    
                    group.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(labelPosition, group.isExpanded, label, style);

                    this.NextPropertyField(new Rect(position.x + EditorGUIUtility.labelWidth + EditorGUIUtility.standardVerticalSpacing, position.y,
                        position.width - EditorGUIUtility.labelWidth - EditorGUIUtility.standardVerticalSpacing, position.height), property, GUIContent.none);

                    EditorGUI.EndFoldoutHeaderGroup();
                }
                else
                {
                    group.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                        group.isExpanded, group.groupAttribute.groupName, style);
                    EditorGUI.EndFoldoutHeaderGroup();

                    DrawProperty(Rect.MinMaxRect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.xMax, position.yMax), property, label, group);
                }
            }
            else
                DrawProperty(position, property, label, group);
        }

        protected virtual void DrawProperty(Rect position, SerializedProperty property, GUIContent label, GroupValue group)
        {
            if (group.isExpanded)
            {
                if (group.groupAttribute.groupMask.HasFlag(GroupMask.IndentFields))
                {
                    EditorGUI.indentLevel++;
                    this.NextPropertyField(position, property, label);
                    EditorGUI.indentLevel--;
                }
                else
                    EditorGUI.PropertyField(position, property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            GroupValue group = GetOrCreateGroup(attribute as GroupAttribute, property);

            if (group.headPropertyPath == property.propertyPath)
            {
                if (group.groupAttribute.groupMask.HasFlag(GroupMask.FirstAsHeader))
                    return this.GetNextPropertyHeight(property);
                else
                {
                    if (group.isExpanded)
                        return this.GetNextPropertyHeight(property) + EditorGUIUtility.singleLineHeight;
                    
                    return EditorGUIUtility.singleLineHeight;
                }
            }

            if (group.isExpanded)
                return this.GetNextPropertyHeight(property);

            return 0f;
        }

        protected static GroupValue GetOrCreateGroup(GroupAttribute groupAttribute, SerializedProperty property)
        {
            GroupKey key = new GroupKey(property.serializedObject, groupAttribute.groupName);

            if (!groupsByKey.TryGetValue(key, out GroupValue group))
                groupsByKey[key] = group = new GroupValue(groupAttribute, property.propertyPath);

            return group;
        }
    }
}
