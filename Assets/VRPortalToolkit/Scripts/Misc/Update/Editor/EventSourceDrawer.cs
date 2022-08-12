using Misc.EditorHelpers;
using Misc.Events;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Update
{
    [CustomPropertyDrawer(typeof(EventSource))]
    public class EventSourceDrawer : PropertyDrawer
    {
        // TODO: I think it would be better to use GenericMenu, instead of popupList
        // Popup list has to populate every time
        // Only issue is that it would be more diffcult to catch when it changes

        protected List<KeyValuePair<Object, string>> popupList = new List<KeyValuePair<Object, string>>();
        protected string[] popupText;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sourceObject = property.FindPropertyRelative(nameof(EventSource.SourceObject)),
                eventName = property.FindPropertyRelative(nameof(EventSource.EventName));

            Rect objectPos = new Rect(position.x, position.y, position.width * 0.34f, position.height - EditorGUIUtility.standardVerticalSpacing),
                eventPos = new Rect(objectPos.x + position.width * 0.34f + 6f, position.y, position.width * 0.66f - 12f, position.height);

            // Event context menu
            if (EditorUtils.HandleContextEvent(eventPos))
                EditorUtils.DoPropertyContextMenu(eventName); // TODO: Would be nice if this could auto set the object property

            // Object context menu
            if (EditorUtils.HandleContextEvent(objectPos))
                EditorUtils.DoPropertyDropDown(new Rect(objectPos.xMin, objectPos.yMax, 0f, 0f), sourceObject);

            // Check object
            EditorGUI.BeginChangeCheck();

            Object currentObject = EditorGUI.ObjectField(objectPos, sourceObject.objectReferenceValue, typeof(Object), true);

            if (EditorGUI.EndChangeCheck()) sourceObject.objectReferenceValue = currentObject;

            // Create new popup
            popupList.Clear();

            GameObject gameObject = (currentObject is Component) ? ((Component)currentObject).gameObject : (currentObject as GameObject);

            if (gameObject)
            {
                AddToList(gameObject, popupList);

                Component[] components = gameObject.GetComponents<Component>();
                Component component;
                System.Type type;

                for (int i = 0; i < components.Length; i++)
                {
                    component = components[i];

                    if (component == null) continue;

                    AddToList(component, popupList);

                    // Type must be unique
                    type = component.GetType();
                    for (int j = i + 1; j < components.Length; j++)
                        if (type == components[j].GetType()) components[j] = null;
                }
            }
            else if (currentObject)
                AddToList(currentObject, popupList);

            // Convert the list to an array
            if (popupText == null || popupText.Length != popupList.Count + 1)
                popupText = new string[popupList.Count + 1];

            KeyValuePair<Object, string> pair;

            int popupIndex = 0;
            popupText[0] = "No Event";
            for (int i = 0, j = 1; i < popupList.Count; i = j++)
            {
                pair = popupList[i];
                popupText[j] = pair.Key.GetType().Name + "/" + pair.Value;

                if (pair.Key == currentObject && pair.Value == eventName.stringValue)
                    popupIndex = j;
            }

            // Check event
            if (popupList.Count != 0)
            {
                EditorGUI.BeginChangeCheck();

                int newIndex = EditorGUI.Popup(eventPos, popupIndex, popupText);

                if (EditorGUI.EndChangeCheck())
                {
                    if (newIndex == 0)
                        eventName.stringValue = null;
                    else
                    {
                        pair = popupList[newIndex - 1];

                        sourceObject.objectReferenceValue = pair.Key;
                        eventName.stringValue = pair.Value;
                    }
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Popup(eventPos, popupIndex, popupText);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.EndProperty();
        }

        protected virtual void AddToList(Object value, List<KeyValuePair<Object, string>> list)
        {
            System.Type type = value.GetType();

            while (type != null)
            {
                foreach (FieldInfo field in type.GetFields())
                {
                    if (field.IsPublic && TypeIsValid(field.FieldType))
                        list.Add(new KeyValuePair<Object, string>(value, field.Name));
                }

                foreach (PropertyInfo property in type.GetProperties())
                {
                    if (TypeIsValid(property.PropertyType))
                    {
                        MethodInfo getMethod = property.GetGetMethod();

                        if (getMethod != null && getMethod.IsPublic)
                            list.Add(new KeyValuePair<Object, string>(value, property.Name));
                    }
                }

                type = type.BaseType;
            }
        }

        protected virtual bool TypeIsValid(System.Type type)
        {
            return type.IsSubclassOf(typeof(UnityEventBase)) || type.IsSubclassOf(typeof(System.Delegate)) || type.IsSubclassOf(typeof(SerializableEventBase));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
