using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Misc.EditorHelpers;
using System.Reflection;

namespace Misc.Events
{
    [CustomPropertyDrawer(typeof(SerializableEventBase), true)]
    public class SerializableEventDrawer : PropertyDrawer
    {
        private static Color _lightBackColor = new Color32(204, 204, 204, 255);
        private static Color _lightAltBackColor = new Color32(240, 240, 240, 255);
        private static Color _lightLineColor = new Color32(161, 161, 161, 255);

        private static Color _darkBackColor = new Color32(65, 65, 65, 255);
        private static Color _darkAltBackColor = new Color32(42, 42, 42, 255);
        private static Color _darkLineColor = new Color32(36, 36, 36, 255);

        public static Color backgroundColor = EditorGUIUtility.isProSkin ? _darkBackColor : _lightBackColor;
        public static Color altBackgroundColor = EditorGUIUtility.isProSkin ? _darkAltBackColor : _lightBackColor;
        public static Color lineColor = EditorGUIUtility.isProSkin ? _darkLineColor : _lightLineColor;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty listeners = property.FindPropertyRelative("_serializableListeners");

            SerializableEventBase serializableEvent = listeners.GetParentObject() as SerializableEventBase;

            if (listeners != null && serializableEvent != null)
            {
                string parameterTypes = "";

                for (int i = 0; i < serializableEvent.parameterCount; i++)
                {
                    if (i == 0)
                        parameterTypes += EventUtils.GetTypeName(serializableEvent.GetParameterType(i));
                    else
                        parameterTypes += $", {EventUtils.GetTypeName(serializableEvent.GetParameterType(i))}";
                }

                label.text += $" ({parameterTypes})";

                EditorGUI.BeginChangeCheck();

                HandleDropAndDrop(new Rect(position.xMin, position.yMin, position.width, EditorGUIUtility.singleLineHeight + 2f), listeners);

                FontStyle before = EditorStyles.foldoutHeader.fontStyle;

                EditorStyles.foldoutHeader.fontStyle = FontStyle.Normal;

                EditorGUI.PropertyField(new Rect(position.xMin, position.yMin + 1f, position.width, position.height - 2f), listeners, label, true);
                property.isExpanded = listeners.isExpanded;

                EditorStyles.foldoutHeader.fontStyle = before;//

                if (EditorGUI.EndChangeCheck()) EventUtils.Validate(property);
                
                if (listeners.isExpanded)
                {
                    EditorGUI.DrawRect(new Rect(position.xMin, position.yMin + EditorGUIUtility.singleLineHeight + 1f, position.width, 6f), backgroundColor);
                    EditorGUI.DrawRect(new Rect(position.xMin, position.yMin + EditorGUIUtility.singleLineHeight + 1f, 1f, 6f), lineColor);
                    EditorGUI.DrawRect(new Rect(position.xMax - 1f, position.yMin + EditorGUIUtility.singleLineHeight - 1f, 1f, 8f), lineColor);
                }

                // Outline top
                EditorGUI.DrawRect(new Rect(position.xMin - 14f, position.yMin, position.width + 14f, 1f), lineColor);
                EditorGUI.DrawRect(new Rect(position.xMin - 15f, position.yMin + 1f, 1f, EditorGUIUtility.singleLineHeight), lineColor);
                EditorGUI.DrawRect(new Rect(position.xMin - 14f, position.yMin + EditorGUIUtility.singleLineHeight + 1f, position.width + 14f, 1f), lineColor);

                // Fix count
                EditorGUI.DrawRect(new Rect(position.xMax - 46f, position.yMin + 1f, 43f, 1f), altBackgroundColor);
                EditorGUI.DrawRect(new Rect(position.xMax - 46f, position.yMin + EditorGUIUtility.singleLineHeight, 43f, 1f), altBackgroundColor);
            }
        }

        private void HandleDropAndDrop(Rect position, SerializedProperty listeners)
        {
            if (position.Contains(Event.current.mousePosition))
            {
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                        if (DragAndDrop.objectReferences != null) DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        else DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

                        break;

                    case EventType.DragPerform:
                        DragAndDrop.AcceptDrag();

                        if (DragAndDrop.objectReferences != null)
                        {
                            foreach (Object source in DragAndDrop.objectReferences)
                            {
                                listeners.InsertArrayElementAtIndex(listeners.arraySize);

                                SerializedProperty listener = listeners.GetArrayElementAtIndex(listeners.arraySize - 1);

                                listener.FindPropertyRelative("_targetObject").objectReferenceValue = source;
                                listener.FindPropertyRelative("_targetProcesses").ClearArray();
                                listener.FindPropertyRelative("_targetParameters").ClearArray();

                                listener.FindPropertyRelative("_data._objectValues").ClearArray();
                                listener.FindPropertyRelative("_data._stringValues").ClearArray();
                                listener.FindPropertyRelative("_data._boolValues").ClearArray();
                                listener.FindPropertyRelative("_data._intValues").ClearArray();
                                listener.FindPropertyRelative("_data._floatValues").ClearArray();
                            }

                            listeners.serializedObject.ApplyModifiedProperties();
                        }

                        Event.current.Use();
                        break;
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty listeners = property.FindPropertyRelative("_serializableListeners");

            if (listeners != null)
                return EditorGUI.GetPropertyHeight(listeners, label, true) + 2f + EditorGUIUtility.standardVerticalSpacing;

            return 0f;
        }
    }
}
