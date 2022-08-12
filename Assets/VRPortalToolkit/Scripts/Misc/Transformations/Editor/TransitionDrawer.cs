using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Misc.Reflection;
using Misc.Update;

namespace Misc.Transformations
{
    // TODO: Would be nice if lists didnt fill with the last value,
    // other than that, its working really well
    [CustomPropertyDrawer(typeof(Transition))]
    public class TransitionDrawer : PropertyDrawer
    {
        private GUIStyle _foldoutStyle;
        protected GUIStyle foldoutStyle {
            get {
                if (_foldoutStyle == null)
                {
                    _foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader);
                    _foldoutStyle.fontStyle = FontStyle.Normal;
                }

                return _foldoutStyle;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty modeProperty = property.FindPropertyRelative("_mode");
            TransitionMode mode = (TransitionMode)modeProperty.intValue;

            Rect current;
            string labelText = label.text;

            if (mode != TransitionMode.Instant)
            {
                // Draw field
                current = new Rect(position.x + EditorGUIUtility.labelWidth + EditorGUIUtility.standardVerticalSpacing, position.y,
                    position.width - EditorGUIUtility.labelWidth - EditorGUIUtility.standardVerticalSpacing, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(current, modeProperty, GUIContent.none, true);

                if (mode != (TransitionMode)modeProperty.intValue)
                {
                    property.isExpanded = true;
                    mode = (TransitionMode)modeProperty.intValue;
                }

                // Draw label as foldout
                // TODO: Should be able to use full label length but it doesnt work
                current = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - EditorGUIUtility.standardVerticalSpacing/*position.width*/, EditorGUIUtility.singleLineHeight);
                property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(current, property.isExpanded, labelText, foldoutStyle);
                EditorGUI.EndFoldoutHeaderGroup();

                // Draw children
                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    SerializedProperty unitProperty = property.FindPropertyRelative("_timeUnit");
                    current = new Rect(position.x, current.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(current, unitProperty, new GUIContent("Time Unit"), true);
                    TimeUnit unit = (TimeUnit)unitProperty.intValue;

                    if (unit != TimeUnit.None)
                    {
                        string timeUnit;

                        switch (unit)
                        {
                            case TimeUnit.Time:
                            case TimeUnit.TimeScaled:
                                timeUnit = "Steps Per Second";
                                break;
                            default:
                                timeUnit = "Steps Per Update";
                                break;
                        }

                        // TODO: if set to TimeUnit.One, use a slider from 0 to 1

                        SerializedProperty ammountProperty = property.FindPropertyRelative("_stepAmount");
                        current = new Rect(position.x, current.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(current, ammountProperty, new GUIContent(timeUnit), true);
                    }

                    if (mode == TransitionMode.Curve)
                    {
                        SerializedProperty curveProperty = property.FindPropertyRelative("_curve");
                        current = new Rect(position.x, current.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUI.GetPropertyHeight(curveProperty));
                        EditorGUI.PropertyField(current, curveProperty, new GUIContent("Curve"), true);
                    }

                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                property.isExpanded = false;

                // Draw field and label
                current = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(current, modeProperty, label, true);

                if (mode != (TransitionMode)modeProperty.intValue)
                    property.isExpanded = true;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            TransitionMode mode = (TransitionMode)property.FindPropertyRelative("_mode").intValue;

            float height = EditorGUIUtility.singleLineHeight;

            if (mode != TransitionMode.Instant)
            {
                if (property.isExpanded)
                {
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    if ((TimeUnit)property.FindPropertyRelative("_timeUnit").intValue != TimeUnit.None)
                        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    if (mode == TransitionMode.Curve)
                        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_curve")) + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            return height;
        }
    }
}
