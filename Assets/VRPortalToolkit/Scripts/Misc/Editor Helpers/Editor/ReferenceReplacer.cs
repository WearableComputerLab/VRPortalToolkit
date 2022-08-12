using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Misc.EditorHelpers
{
    public class ReferenceReplacer : EditorWindow
    {
        private static string[] tabs = new string[] { "Reference Replacer", "Prefab Replacer" };

        public int tabIndex = 0;

        public Object originalReference;
        public Object replacementReference;

        public GameObject prefab;
        public bool updateOverrides = true;

        [MenuItem("Tools/Replacer/Replace Reference")]
        private static void InitializeReplaceReference()
        {
            EditorWindow window = GetWindow(typeof(ReferenceReplacer), false, "Replace Reference");
            ((ReferenceReplacer)window).tabIndex = 0;
            //, new Rect(0, 0, EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth, EditorGUIUtility.singleLineHeight * 10f)
            window.Show();
        }

        [MenuItem("Tools/Replacer/Replace With Prefab")]
        private static void InitializeReplaceWithPrefab()
        {
            EditorWindow window = GetWindow(typeof(ReferenceReplacer), false, "Replace Reference");
            ((ReferenceReplacer)window).tabIndex = 1;
            window.Show();
        }

        protected void OnGUI()
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            tabIndex = GUILayout.Toolbar(tabIndex, tabs, GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.25f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginScrollView(Vector2.zero);

            switch (tabIndex)
            {
                case 0:
                    {
                        originalReference = EditorGUILayout.ObjectField("Original", originalReference, typeof(Object), true);

                        replacementReference = EditorGUILayout.ObjectField("Replacement", replacementReference, typeof(Object), true);

                        EditorGUI.BeginDisabledGroup(!originalReference || !replacementReference);

                        if (GUILayout.Button("Replace Reference"))
                            ReplaceWithReference(originalReference, replacementReference);

                        EditorGUILayout.HelpBox("Replaces all serialized references from one object to another.", MessageType.Info);

                        EditorGUI.EndDisabledGroup();
                        break;
                    }
                default:
                    {
                        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), true);

                        updateOverrides = EditorGUILayout.Toggle("Update Overrides", updateOverrides);

                        EditorGUI.BeginDisabledGroup(!prefab || Selection.gameObjects.Length == 0);

                        if (GUILayout.Button("Replace Selected Objects"))
                            ReplaceWithPrefab(prefab, Selection.gameObjects, updateOverrides);

                        EditorGUILayout.HelpBox("Select a number of any number of GameObjects in the Scene Hierachy that you want to be become the selected Prefab. " +
                            "The order of child transforms of the GameObject should match the Prefab" +
                            "All references to these GameObjects will also be updated.", MessageType.Info);

                        EditorGUI.EndDisabledGroup();
                        break;
                    }
            }

            GUILayout.EndScrollView();
        }

        public static void ReplaceWithReference(Object original, Object replacement)
        {
            if (!original || !replacement)
            {
                Debug.LogError("Replace invalid");
                return;
            }

            SerializedObject serializedObject;
            SerializedProperty property;
            bool hasChanged;

            // Replace references
            foreach (Component component in FindObjectsOfType<Component>(true))
            {
                serializedObject = new SerializedObject(component);
                property = serializedObject.GetIterator();
                hasChanged = false;

                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == original)
                    {
                        Undo.RecordObject(component, $"Updated {property.name} from <{original}> to <{replacement}>");
                        property.objectReferenceValue = replacement;
                        hasChanged = true;
                    }
                }

                if (hasChanged) serializedObject.ApplyModifiedProperties();
                serializedObject.Dispose();
            }
        }

        public static void ReplaceWithPrefab(GameObject prefab, GameObject[] originals, bool updateOverrides = true)
        {
            if (!prefab || originals == null)
            {
                Debug.LogError("Replace invalid");
                return;
            }

            GameObject original, instance;
            Dictionary<Object, Object> instanceByOriginal = new Dictionary<Object, Object>();
            SerializedObject serializedObject;
            SerializedProperty property;
            bool hasChanged;
            Object instanceObject;

            // Create Instances
            for (int i = 0; i < originals.Length; i++)
            {
                original = originals[i];

                if (!original) continue;

                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(instance, $"Replaced <{original}> with <{instance}>");

                GetInstanceByOriginal(instanceByOriginal, original, instance, new List<Component>());

                // Replace references
                foreach (Component component in FindObjectsOfType<Component>(true))
                {
                    serializedObject = new SerializedObject(component);
                    property = serializedObject.GetIterator();
                    hasChanged = false;

                    while (property.NextVisible(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue)
                        {
                            if (instanceByOriginal.TryGetValue(property.objectReferenceValue, out instanceObject))
                            {
                                Undo.RecordObject(component, $"Updated {property.name} from <{property.objectReferenceValue}> to <{instanceObject}>");
                                property.objectReferenceValue = instanceObject;
                                hasChanged = true;
                            }
                        }
                    }

                    if (hasChanged) serializedObject.ApplyModifiedProperties();
                    serializedObject.Dispose();
                }

                if (updateOverrides)
                {
                    foreach (var pair in instanceByOriginal)
                        EditorUtility.CopySerialized(pair.Key, pair.Value);
                }

                // Update to match new transform
                instance.transform.parent = original.transform.parent;
                instance.transform.position = original.transform.position;
                instance.transform.rotation = original.transform.rotation;
                instance.transform.localScale = original.transform.localScale;
                instance.transform.SetSiblingIndex(original.transform.GetSiblingIndex());
                instance.name = original.name;

                Undo.DestroyObjectImmediate(original);

                instanceByOriginal.Clear();
            }
        }

        private static void GetInstanceByOriginal(Dictionary<Object, Object> instanceByOriginal, GameObject original, GameObject instance, List<Component> components)
        {
            instanceByOriginal.Add(original, instance);

            components.Clear();
            original.GetComponents(components);
            Component[] instanceComponents = instance.GetComponents<Component>();
            Component originalComponent, instanceComponent;
            System.Type type;

            for (int i = 0; i < instanceComponents.Length; i++)
            {
                instanceComponent = instanceComponents[i];
                type = instanceComponent.GetType();
                originalComponent = null;

                for (int j = 0; j < components.Count; j++)
                {
                    originalComponent = components[j];

                    if (originalComponent.GetType() == type)
                    {
                        components.RemoveAt(j);
                        break;
                    }

                    originalComponent = null;
                }

                if (originalComponent)
                    instanceByOriginal.Add(originalComponent, instanceComponent);
                else
                    DestroyImmediate(instanceComponent);
            }

            // Add the extras
            foreach (Component component in components)
            {
                instanceComponent = instance.AddComponent(component.GetType());
                EditorUtility.CopySerialized(component, instanceComponent);
            }

            // Continue on
            int originalCount = original.transform.childCount, instanceCount = instance.transform.childCount;
            Transform child;

            for (int i = 0; i < instanceCount && i < originalCount; i++)
                GetInstanceByOriginal(instanceByOriginal, original.transform.GetChild(i).gameObject, instance.transform.GetChild(i).gameObject, components);

            // Disable unused children
            for (int i = originalCount; i < instanceCount; i++)
            {
                child = instance.transform.GetChild(i);
                child.gameObject.SetActive(false);
            }

            // Move remaining children
            while (original.transform.childCount > instanceCount)
            {
                child = original.transform.GetChild(instanceCount);
                Undo.SetTransformParent(child, instance.transform, $"Set <{child}>'s parent from <{original.transform}> to <{instance.transform}>");
            }
        }
    }
}