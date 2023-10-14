using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Misc.EditorHelpers
{
    // TODO: Look at this to find out how to use the next property drawer
    // Then make it a generic one so property attributes can be stacked
    // https://github.com/Deadcows/MyBox/blob/master/Attributes/ConditionalFieldAttribute.cs
    public static class SortedEditor
    {
        private struct Key
        {
            public FieldInfo fieldInfo;
            public PropertyAttribute attribute;

            public Key(FieldInfo fieldInfo, PropertyAttribute attribute = null)
            {
                this.fieldInfo = fieldInfo;
                this.attribute = attribute;
            }
        }

        private static Dictionary<Key, PropertyDrawer> nextInQueue;
        private static Dictionary<Type, Type> drawerTypesByType;
        private static HashSet<Type> useForChildren;

        public static void PropertyField(Rect position, SerializedProperty property, GUIContent label = null)
        {
            if (TryGetFirstPropertyDrawer(property, out PropertyDrawer firstDrawer))
                firstDrawer.OnGUI(position, property, label);
            else
                EditorGUI.PropertyField(position, property, label, true);
        }

        public static float GetPropertyHeight(SerializedProperty property, GUIContent label = null)
        {
            if (TryGetFirstPropertyDrawer(property, out PropertyDrawer firstDrawer))
                return firstDrawer.GetPropertyHeight(property, label);

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public static bool TryGetFirstPropertyDrawer(this SerializedProperty serializedProperty, out PropertyDrawer firstPropertyDrawer)
            => (firstPropertyDrawer = GetFirstPropertyDrawer(serializedProperty)) != null;

        public static PropertyDrawer GetFirstPropertyDrawer(this SerializedProperty serializedProperty)
        {
            object parent = serializedProperty.GetParentObject();

            if (parent != null)
            {
                Type type = parent.GetType();

                while (type != null && type != typeof(object))
                {
                    FieldInfo fieldInfo = type.GetField(serializedProperty.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (fieldInfo != null)
                    {
                        CachePropertyDrawer(fieldInfo);

                        if (nextInQueue.TryGetValue(new Key(fieldInfo), out PropertyDrawer nextDrawer))
                            return nextDrawer;
                    }

                    type = type.BaseType;
                }
            }

            return null;
        }

        public static void NextPropertyField(this PropertyDrawer propertyDrawer, Rect position, SerializedProperty property, GUIContent label = null)
        {
            if (TryGetNextPropertyDrawer(propertyDrawer, out PropertyDrawer nextDrawer))
                nextDrawer.OnGUI(position, property, label);
            else
                EditorGUI.PropertyField(position, property, label);
        }

        public static float GetNextPropertyHeight(this PropertyDrawer propertyDrawer, SerializedProperty property, GUIContent label = null)
        {
            if (TryGetNextPropertyDrawer(propertyDrawer, out PropertyDrawer nextDrawer))
                return nextDrawer.GetPropertyHeight(property, label);

            return EditorGUI.GetPropertyHeight(property, label);
        }

        public static bool TryGetNextPropertyDrawer(this PropertyDrawer propertyDrawer, out PropertyDrawer nextPropertyDrawer)
            => (nextPropertyDrawer = GetNextPropertyDrawer(propertyDrawer)) != null;

        public static PropertyDrawer GetNextPropertyDrawer(this PropertyDrawer propertyDrawer)
        {
            if (propertyDrawer.fieldInfo != null)
            {
                CachePropertyDrawer(propertyDrawer.fieldInfo);

                if (propertyDrawer.attribute != null && nextInQueue.TryGetValue(new Key(propertyDrawer.fieldInfo, propertyDrawer.attribute), out PropertyDrawer nextDrawer) && nextDrawer != propertyDrawer)
                    return nextDrawer;
            }

            return null;
        }

        public static IEnumerable<PropertyDrawer> GetPropertyDrawers(this SerializedProperty serializedProperty)
        {
            PropertyDrawer propertyDrawer = GetFirstPropertyDrawer(serializedProperty);

            while (propertyDrawer != null)
            {
                yield return propertyDrawer;

                propertyDrawer = propertyDrawer.GetNextPropertyDrawer();
            }
        }

        public static IEnumerable<PropertyDrawer> GetNextPropertyDrawers(this PropertyDrawer propertyDrawer)
        {
            while (propertyDrawer != null)
            {
                propertyDrawer = propertyDrawer.GetNextPropertyDrawer();

                if (propertyDrawer == null) break;

                yield return propertyDrawer;
            }
        }

        private static void CachePropertyDrawer(FieldInfo fieldInfo)
        {
            if (nextInQueue == null) nextInQueue = new Dictionary<Key, PropertyDrawer>();

            if (fieldInfo != null && !nextInQueue.ContainsKey(new Key(fieldInfo)))
            {
                List<KeyValuePair<PropertyAttribute, PropertyDrawer>> propertyDrawers = new List<KeyValuePair<PropertyAttribute, PropertyDrawer>>();

                TryGetPropertyDrawer(fieldInfo, null, out PropertyDrawer defaultDrawer);//

                foreach (Attribute attribute in fieldInfo.GetCustomAttributes())
                {
                    if (attribute is PropertyAttribute propertyAttribute && TryGetPropertyDrawer(fieldInfo, propertyAttribute, out PropertyDrawer attributeDrawer))
                        propertyDrawers.Add(new KeyValuePair<PropertyAttribute, PropertyDrawer>(propertyAttribute, attributeDrawer));
                }

                propertyDrawers.Sort((i, j) => j.Key.order.CompareTo(i.Key.order));

                if (propertyDrawers.Count > 0)
                {
                    nextInQueue.Add(new Key(fieldInfo), propertyDrawers[0].Value);

                    for (int i = 0, j = 1; j < propertyDrawers.Count; i = j++)
                        nextInQueue.Add(new Key(fieldInfo, propertyDrawers[i].Key), propertyDrawers[j].Value);

                    nextInQueue.Add(new Key(fieldInfo, propertyDrawers[propertyDrawers.Count - 1].Key), defaultDrawer);
                }
                else
                    nextInQueue.Add(new Key(fieldInfo), defaultDrawer);
            }
        }

        private static bool TryGetPropertyDrawer(FieldInfo fieldInfo, PropertyAttribute propertyAttribute, out PropertyDrawer propertyDrawer)
        {
            CacheAllDrawers();

            Type originalType, currentType;

            if (propertyAttribute != null)
                originalType = propertyAttribute.GetType();
            else if (fieldInfo != null)
                originalType = fieldInfo.FieldType;
            else originalType = null;

            currentType = originalType;

            while (currentType != null)
            {
                if (drawerTypesByType.TryGetValue(currentType, out Type drawerType) && (originalType == currentType || useForChildren.Contains(currentType)))
                {
                    try { propertyDrawer = (PropertyDrawer)Activator.CreateInstance(drawerType); }
                    catch (Exception) { propertyDrawer = null; }

                    if (propertyDrawer != null)
                    {
                        Type propertyDrawerType = typeof(PropertyDrawer);

                        if (propertyAttribute != null)
                        {
                            FieldInfo attribute = propertyDrawerType.GetField("m_Attribute", BindingFlags.Instance | BindingFlags.NonPublic);
                            if (attribute != null) attribute.SetValue(propertyDrawer, propertyAttribute);
                        }

                        if (fieldInfo != null)
                        {
                            FieldInfo fieldInfoField = propertyDrawerType.GetField("m_FieldInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                            if (fieldInfoField != null) fieldInfoField.SetValue(propertyDrawer, fieldInfo);
                        }

                        return true;
                    }

                    return false;
                }

                currentType = currentType.BaseType;
            }

            propertyDrawer = null;
            return false;
        }

        private static void CacheAllDrawers()
        {
            if (drawerTypesByType != null) return;

            drawerTypesByType = new Dictionary<Type, Type>();
            useForChildren = new HashSet<Type>();

            Assembly[] allAssembliesInDomain = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in allAssembliesInDomain)
            {
                Type[] types = assembly.GetTypes();

                foreach (Type drawerType in types)
                {
                    if (typeof(PropertyDrawer).IsAssignableFrom(drawerType) && !drawerType.IsInterface && !drawerType.IsAbstract)
                    {
                        IList<CustomAttributeData> customAttributes = CustomAttributeData.GetCustomAttributes(drawerType);

                        //if (customAttributes == null) continue;

                        foreach (CustomAttributeData drawerAttribute in customAttributes)
                        {
                            if (drawerAttribute != null && typeof(CustomPropertyDrawer) == drawerAttribute.AttributeType)
                            {
                                IList<CustomAttributeTypedArgument> args = drawerAttribute.ConstructorArguments;

                                if (args.Count > 0)
                                {
                                    Type associatedType = args[0].Value as Type;

                                    if (associatedType == null || drawerTypesByType.ContainsKey(associatedType)) continue;

                                    drawerTypesByType.Add(associatedType, drawerType);

                                    if (args.Count > 1 && (bool)args[1].Value) useForChildren.Add(associatedType);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}