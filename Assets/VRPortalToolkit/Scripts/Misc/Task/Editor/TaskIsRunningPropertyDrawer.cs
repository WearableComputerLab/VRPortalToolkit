using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Misc.Tasks
{
    [CustomPropertyDrawer(typeof(TaskIsRunningAttribute))]
    public class TaskIsRunningPropertyDrawer : PropertyDrawer
    {
        private List<Task> tasks = new List<Task>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            tasks.Clear();

            foreach (Object targetObject in property.serializedObject.targetObjects)
                if (targetObject is Task task) tasks.Add(task);

            float widthUnit = (position.width - EditorGUIUtility.standardVerticalSpacing * 2f) / 3f;

            bool hasIsRunning = tasks.Exists(task => task.isRunning), hasIsNotRunning = tasks.Exists(task => !task.isRunning);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || !hasIsNotRunning);

            Rect buttonPosition = new Rect(position.x, position.y, widthUnit, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(buttonPosition, "Begin")) foreach (Task task in tasks) task.TryBegin();

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || !hasIsRunning);

            buttonPosition = new Rect(position.x + widthUnit + EditorGUIUtility.standardVerticalSpacing, position.y, widthUnit, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(buttonPosition, "Cancel")) foreach (Task task in tasks) task.TryCancel();

            buttonPosition = new Rect(position.x + (widthUnit + EditorGUIUtility.standardVerticalSpacing) * 2f, position.y, widthUnit, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(buttonPosition, "Complete")) foreach (Task task in tasks) task.TryComplete();

            EditorGUI.EndDisabledGroup();

            position = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 4f,
                position.width, position.height - EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing * 4f);

            label.text = property.displayName;
            EditorGUI.PropertyField(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.targetObject is Task)
                return EditorGUI.GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 4f;

            return EditorGUI.GetPropertyHeight(property, label);
        }
    }
}
