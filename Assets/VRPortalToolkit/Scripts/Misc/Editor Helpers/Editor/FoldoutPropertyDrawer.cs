using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Misc.EditorHelpers
{
    [CustomPropertyDrawer(typeof(FoldoutAttribute))]
    public class FoldoutPropertyDrawer : PropertyDrawer
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

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            FoldoutAttribute foldoutAttribute = attribute as FoldoutAttribute;

            if (foldoutAttribute != null)
            {
                GUIStyle style = foldoutStyle;
                style.fontStyle = foldoutAttribute.options.HasFlag(FoldoutOptions.Bold) ? FontStyle.Bold : FontStyle.Normal;
                style.fontSize = foldoutAttribute.options.HasFlag(FoldoutOptions.Large) ? EditorStyles.largeLabel.fontSize : EditorStyles.label.fontSize;

                Rect labelPosition = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);

                if (EditorUtils.HandleContextEvent(position))
                    EditorUtils.DoPropertyContextMenu(property);

                property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(labelPosition, property.isExpanded, label, style);

                this.NextPropertyField(new Rect(position.x + EditorGUIUtility.labelWidth + EditorGUIUtility.standardVerticalSpacing, position.y,
                    position.width - EditorGUIUtility.labelWidth - EditorGUIUtility.standardVerticalSpacing, position.height), property, GUIContent.none);

                EditorGUI.EndFoldoutHeaderGroup();
            }
            else
                this.NextPropertyField(position, property, label);
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return this.GetNextPropertyHeight(property);
        }
    }
}