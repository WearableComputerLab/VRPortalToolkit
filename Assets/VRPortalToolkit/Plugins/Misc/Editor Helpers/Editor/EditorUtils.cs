using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRPortalToolkit;

namespace Misc.EditorHelpers
{
    public static class EditorUtils
    {
        public static SerializedProperty GetParent(this SerializedProperty property)
        {
            string path = property.propertyPath;

            int i = path.LastIndexOf('.');

            if (i < 0) return null;

            return property.serializedObject.FindProperty(path.Substring(0, i));
        }

        public static SerializedProperty FindSiblingProperty(this SerializedProperty property, string path)
        {
            SerializedProperty parent = property.GetParent();

            if (parent == null) return property.serializedObject.FindProperty(path);

            return parent.FindPropertyRelative(path);
        }

        public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty property)
        {
            property = property.Copy();

            SerializedProperty nextElement = property.Copy();

            bool hasNextElement = nextElement.NextVisible(false);

            if (!hasNextElement) nextElement = null;

            property.NextVisible(true);

            while (true)
            {
                if ((SerializedProperty.EqualContents(property, nextElement)))
                    yield break;

                yield return property;

                bool hasNext = property.NextVisible(false);

                if (!hasNext) break;
            }
        }
        public static bool GetParentObject<T>(this SerializedProperty property, out T value)
            => GetParentObject(property, property.serializedObject.targetObject, out value);

        public static bool GetParentObject<T>(this SerializedProperty property, UnityEngine.Object targetObject, out T parent)
        {
            object asObject = GetParentObject(property);

            if (asObject is T)
            {
                parent = (T)asObject;
                return true;
            }

            parent = default(T);
            return false;
        }

        public static object GetParentObject(this SerializedProperty property)
            => TryGetObject(property, property.serializedObject.targetObject, -1);

        public static object GetParentObject(this SerializedProperty property, UnityEngine.Object targetObject)
            => TryGetObject(property, targetObject, -1);

        public static bool TryGetObject<T>(this SerializedProperty property, out T value)
            => TryGetObject(property, property.serializedObject.targetObject, out value);

        public static bool TryGetObject<T>(this SerializedProperty property, UnityEngine.Object targetObject, out T value)
        {
            object asObject = GetObject(property, targetObject);

            if (asObject is T)
            {
                value = (T)asObject;
                return true;
            }

            value = default(T);
            return false;
        }

        public static object GetObject(this SerializedProperty property)
            => TryGetObject(property, property.serializedObject.targetObject, 0);

        public static object GetObject(this SerializedProperty property, UnityEngine.Object targetObject)
            => TryGetObject(property, targetObject, 0);

        private static object TryGetObject(SerializedProperty property, UnityEngine.Object targetObject, int offset)
        {
            string path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = targetObject;

            string[] elements = path.Split('.');

            foreach (string element in elements.Take(elements.Length + offset))
            {
                if (element.Contains("["))
                {
                    string elementName = element.Substring(0, element.IndexOf("["));

                    int index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));

                    obj = TryGetValue(obj, elementName, index);
                }
                else
                    obj = GetValue(obj, element);
            }

            return obj;
        }

        private static object TryGetValue(object source, string name, int index)
        {
            IEnumerable enumerable = GetValue(source, name) as IEnumerable;

            if (enumerable != null)
            {
                IEnumerator enm = enumerable.GetEnumerator();

                while (index-- >= 0) enm.MoveNext();

                return enm.Current;
            }

            return GetValue(source, name);
        }

        private static object GetValue(object source, string name)
        {
            if (source != null)
            {
                Type type = source.GetType();

                while (type != null)
                {
                    FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                    if (field != null) return field.GetValue(source);

                    type = type.BaseType;
                }
            }

            return false;
        }


        public static SerializedProperty ForceGetArrayElementAtIndex(this SerializedProperty arrayProperty, int index)
        {
            if (arrayProperty == null) return null;

            if (arrayProperty.arraySize < index + 1)
            {
                arrayProperty.arraySize = index + 1;

                arrayProperty.serializedObject.ApplyModifiedProperties();
            }

            return arrayProperty.GetArrayElementAtIndex(index);
        }

        public static void SetArraySize(this SerializedProperty arrayProperty, int count)
        {
            if (arrayProperty == null) return;

            if (arrayProperty.arraySize != count)
            {
                arrayProperty.arraySize = count;

                arrayProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        public static void ModifyArraySize(this SerializedProperty array, int index, int count)
        {
            if (count == 0 || index < 0) return;
            
            if (count > 0)
            {
                if (index >= array.arraySize)
                    array.arraySize = index + count;
                else
                {
                    for (int i = 0; i < count; i++)
                        array.InsertArrayElementAtIndex(index);
                }
            }
            else
            {
                for (int i = 0; i < -count; i++)
                {
                    if (index >= array.arraySize)
                        break;

                    array.DeleteArrayElementAtIndex(index);
                }
            }

            array.serializedObject.ApplyModifiedProperties();
        }

        public static int GetArrayIndex(this SerializedProperty property)
        {
            SerializedProperty parent = property.GetParent();

            if (parent != null && parent.isArray)
            {
                for (int i = 0; i < parent.arraySize; i++)
                    if (parent.GetArrayElementAtIndex(i).propertyPath == property.propertyPath)
                        return i;
            }

            return -1;
        }

        /*public static void ShowAsContext(this GenericMenu menu, bool shouldDiscardMenuOnSecondClick)
        {
            if (Event.current != null)
                DropDown(menu, new Rect(Event.current.mousePosition, Vector2.zero), shouldDiscardMenuOnSecondClick);
        }

        public static void DropDown(this GenericMenu menu, Rect position, bool shouldDiscardMenuOnSecondClick)
        {
            MethodInfo method = typeof(GenericMenu).GetMethod("DropDown", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(Rect), typeof(bool) }, null);

            if (method == null) throw new Exception("Could not find 'DropDown(Rect,bool)' in 'GenericMenu'!");

            method.Invoke(menu, new object[] { position, shouldDiscardMenuOnSecondClick });
        }*/

        public static bool HandleContextEvent(Rect position)
        {
            Event evt = Event.current;
            EventType eventType = evt.type;

            if (((eventType == EventType.MouseDown && Event.current.button == 1) || (eventType == EventType.ContextClick))
                && position.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                return true;
            }

            return false;
        }

        public static void DoPropertyContextMenu(SerializedProperty property, SerializedProperty linkedProperty = null, GenericMenu menu = null)
        {
            GenericMenu contextMenu = FillPropertyContextMenu(property, linkedProperty, menu);

            if (contextMenu != null && contextMenu.GetItemCount() != 0)
            {
                Event.current.Use();
                contextMenu.ShowAsContext();
            }
        }

        public static void DoPropertyDropDown(Rect position, SerializedProperty property, SerializedProperty linkedProperty = null, GenericMenu menu = null)
        {
            GenericMenu contextMenu = FillPropertyContextMenu(property, linkedProperty, menu);

            if (contextMenu != null && contextMenu.GetItemCount() != 0)
            {
                Event.current.Use();
                contextMenu.DropDown(position);
            }
        }

        private static MethodInfo _fillPropertyContextMenuMethod;
        public static GenericMenu FillPropertyContextMenu(SerializedProperty property, SerializedProperty linkedProperty = null, GenericMenu contextMenu = null)
        {
            if (_fillPropertyContextMenuMethod == null)
                _fillPropertyContextMenuMethod = typeof(EditorGUI).GetMethod("FillPropertyContextMenu", BindingFlags.NonPublic | BindingFlags.Static);

            if (_fillPropertyContextMenuMethod == null)
                throw new Exception("Could not find 'FillPropertyContextMenu(SerializedProperty,SerializedProperty,GenericMenu)' in 'EditorGUI'!");

            if (contextMenu == null) contextMenu = new GenericMenu();

            _fillPropertyContextMenuMethod.Invoke(null, new object[] { property, linkedProperty, contextMenu });

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (contextMenu.GetItemCount() > 0) contextMenu.AddSeparator("");

                contextMenu.AddItem(new GUIContent("Properties..."), false, () => {
                    Assembly assembly = typeof(EditorGUI).Assembly;

                    if (assembly == null) throw new Exception("Could not find 'UnityEditor.CoreModule' Assembly!");

                    Type propertyEditorType = assembly.GetType("UnityEditor.PropertyEditor", false, true);

                    if (propertyEditorType == null) throw new Exception("Could not find 'PropertyEditor' in 'UnityEditor.CoreModule'!");

                    MethodInfo method = propertyEditorType.GetMethod("OpenPropertyEditor", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(UnityEngine.Object), typeof(bool) }, null);

                    if (method == null) throw new Exception("Could not find 'FillPropertyContextMenu' in 'EditorGUI'!");

                    method.Invoke(null, new object[] { property.objectReferenceValue, true });
                });
            }

            //contextMenu.AddItem(new GUIContent("Properties..."), false, () => PropertyEd.OpenPropertyEditor(actualObject));
            return contextMenu;
        }

        private static MethodInfo _setBoldDefaultFontMethod;
        public static void SetBoldDefaultFont(bool value)
        {
            if (_setBoldDefaultFontMethod == null)
                _setBoldDefaultFontMethod = typeof(EditorGUIUtility).GetMethod("SetBoldDefaultFont", BindingFlags.Static | BindingFlags.NonPublic);

            if (_setBoldDefaultFontMethod == null)
                throw new Exception("Could not find 'SetBoldDefaultFont(bool)' in 'EditorGUIUtility'!");

            _setBoldDefaultFontMethod.Invoke(null, new object[] { value });
        }
    }
}
