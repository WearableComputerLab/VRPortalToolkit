using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Misc.Reflection;
using Misc.EditorHelpers;

namespace Misc.Update
{
    // TODO: Might be useful to add context menus

    // Group by
    // - Update
    // - Fixed
    // - Camera Events
    // - Render Pipeline
    // - Sources
    // - Wait For Seconds

    // TODO: This can't handle multiple objects
    [CustomPropertyDrawer(typeof(UpdateMask))]
    public class UpdateMaskDrawer : PropertyDrawer
    {
        private GUIStyle _foldoutStyle;
        protected GUIStyle foldoutStyle
        {
            get
            {
                if (_foldoutStyle == null)
                {
                    _foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader);
                    _foldoutStyle.fontStyle = FontStyle.Normal;
                }

                return _foldoutStyle;
            }
        }

        //protected List<EventSource> sourcesList = new List<EventSource>();
        //protected List<float> waitForSecondsList = new List<float>();

        private static string updateFlagsGroupName = "Dynamic Update/";
        private static string fixedFlagsGroupName = "Fixed Update/";
        private static string cameraFlagsGroupName = "Camera Events/";
        private static string renderingFlagsGroupName = "Render Pipeline Events/";

        private static UpdateFlags updateFlagsGroup = UpdateFlags.Update | UpdateFlags.LateUpdate | UpdateFlags.WaitForEndOfFrame | UpdateFlags.NullUpdate;
        private static UpdateFlags fixedFlagsGroup = UpdateFlags.FixedUpdate | UpdateFlags.WaitForFixedUpdate;
        private static UpdateFlags cameraFlagsGroup = UpdateFlags.OnPreCull | UpdateFlags.OnPreRender | UpdateFlags.OnPostRender;
        private static UpdateFlags renderingFlagsGroup = UpdateFlags.BeginCameraRendering | UpdateFlags.BeginContextRendering | UpdateFlags.BeginFrameRendering
            | UpdateFlags.EndCameraRendering | UpdateFlags.EndContextRendering | UpdateFlags.EndFrameRendering;

        private static MethodInfo _validateMethod;
        private static object[] _emptyArgs;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty sourcesProperty = property.FindPropertyRelative("_sources");
            SerializedProperty waitForSecondsProperty = property.FindPropertyRelative("_waitForSeconds");
            SerializedProperty updateFlagsProperty = property.FindPropertyRelative("_updateFlags");

            EditorGUI.BeginChangeCheck();

            Rect labelPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), label, property);

            //UpdateFlags updateFlags = (UpdateFlags)updateMaskProperty.intValue;
            GetHasFlags(property, out bool hasSources, out bool hasWaitForSeconds);

            string labelText = label.text;

            // Draw field
            Rect flagsPosition = new Rect(position.x + EditorGUIUtility.labelWidth + EditorGUIUtility.standardVerticalSpacing, position.y,
                position.width - EditorGUIUtility.labelWidth - EditorGUIUtility.standardVerticalSpacing, EditorGUIUtility.singleLineHeight);

            // TODO, sometimes cant paste
            if (EditorUtils.HandleContextEvent(flagsPosition))
                EditorUtils.DoPropertyContextMenu(updateFlagsProperty);

            if (HandleDragAndDrop(labelPosition, sourcesProperty))
            {
                if (!updateFlagsProperty.hasMultipleDifferentValues)
                {
                    // Add sources flag
                    updateFlagsProperty.longValue = (long)(((UpdateFlags)updateFlagsProperty.longValue) | UpdateFlags.Sources);
                }
            }

            UpdateFlagsField(property, flagsPosition);

            // Draw label as foldout
            Rect current = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - EditorGUIUtility.standardVerticalSpacing/*position.width*/, EditorGUIUtility.singleLineHeight);

            EditorGUI.EndProperty();

            if (hasSources || hasWaitForSeconds)
            {
                property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(current, property.isExpanded, labelText, foldoutStyle);
                EditorGUI.EndFoldoutHeaderGroup();
            }
            else
            {
                property.isExpanded = false;
                EditorGUI.LabelField(current, label);
            }

            GetHasFlags(property, out hasSources, out hasWaitForSeconds);

            if (!property.isExpanded)
            {
                if ((!hasSources && hasSources) || (!hasWaitForSeconds && hasWaitForSeconds))
                    property.isExpanded = true;
            }

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                if (hasSources) DrawSources(position, sourcesProperty, ref current);

                if (hasWaitForSeconds) DrawWaitForSeconds(position, waitForSecondsProperty, ref current);

                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
                Validate(property);
        }

        private static void Validate(SerializedProperty property)
        {
            if (_validateMethod == null) _validateMethod = typeof(UpdateMask).GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance);

            if (_validateMethod != null)
            {
                foreach (Object obj in property.serializedObject.targetObjects)
                {
                    if (property.TryGetObject(obj, out UpdateMask updateMask))
                        _validateMethod.Invoke(updateMask, _emptyArgs);
                }
            }
        }

        private static void GetHasFlags(SerializedProperty property, out bool hasSources, out bool hasWaitForSeconds)
        {
            hasSources = true;
            hasWaitForSeconds = true;

            foreach (Object obj in property.serializedObject.targetObjects)
            {
                if (property.TryGetObject(obj, out UpdateMask updateMask))
                {
                    if (!updateMask.UpdateFlags.HasFlag(UpdateFlags.Sources)) hasSources = false;
                    if (!updateMask.UpdateFlags.HasFlag(UpdateFlags.WaitForSeconds)) hasWaitForSeconds = false;
                }
            }
        }

        /*private static void RemoveFlags(SerializedProperty property, UpdateFlags flags)
        {
            if (property == null) return;

            foreach (Object obj in property.serializedObject.targetObjects)
            {
                if (property.TryGetObject(obj, out UpdateMask updateMask))
                    updateMask.UpdateFlags &= ~flags;
            }
        }

        private static void AddFlags(SerializedProperty property, UpdateFlags flags)
        {
            if (property == null) return;

            foreach (Object obj in property.serializedObject.targetObjects)
            {
                if (property.TryGetObject(obj, out UpdateMask updateMask))
                    updateMask.UpdateFlags |= flags;
            }
        }*/

        private bool HandleDragAndDrop(Rect position, SerializedProperty sources)
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
                                sources.InsertArrayElementAtIndex(sources.arraySize);

                                SerializedProperty listener = sources.GetArrayElementAtIndex(sources.arraySize - 1);

                                listener.FindPropertyRelative("SourceObject").objectReferenceValue = source;
                                listener.FindPropertyRelative("EventName").stringValue = string.Empty;
                            }

                            sources.serializedObject.ApplyModifiedProperties();

                            return true;
                        }

                        Event.current.Use();
                        break;
                }
            }

            return false;
        }

        protected virtual void UpdateFlagsField(SerializedProperty updateMask, Rect position, UpdateFlags validFlags = (UpdateFlags)~0)
        {
            string currentText;

            SerializedProperty updateFlags = updateMask.FindPropertyRelative("_updateFlags");
            UpdateFlags currentFlags = (UpdateFlags)updateFlags.longValue;

            if (updateFlags.hasMultipleDifferentValues)
            {
                currentText = "—";
                currentFlags = UpdateFlags.Never;
            }
            else if (currentFlags == UpdateFlags.Never)
                currentText = "Never";
            else
            {
                bool first = true;
                currentText = "";

                foreach (UpdateFlags flag in System.Enum.GetValues(typeof(UpdateFlags)))
                {
                    if (flag != UpdateFlags.Never && currentFlags.HasFlag(flag))
                    {
                        if (first)
                            first = false;
                        else
                            currentText += ", ";

                        currentText += ObjectNames.NicifyVariableName(flag.ToString());
                    }
                }

                if (first) currentText = "Never";
            }

            if (EditorGUI.DropdownButton(position, new GUIContent(currentText), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();

                AddFlagToMenu(menu, "Never", updateMask, currentFlags, UpdateFlags.Never);

                if ((validFlags & (validFlags - 1)) != 0) // Has more than one flag
                    AddFlagToMenu(menu, "Everything", updateMask, currentFlags, validFlags);

                if (validFlags != UpdateFlags.Never)
                    menu.AddSeparator("");

                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.Update, updateFlagsGroup, updateFlagsGroupName, validFlags);
                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.NullUpdate, updateFlagsGroup, updateFlagsGroupName, validFlags);
                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.LateUpdate, updateFlagsGroup, updateFlagsGroupName, validFlags);
                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.WaitForEndOfFrame, updateFlagsGroup, updateFlagsGroupName, validFlags);

                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.FixedUpdate, fixedFlagsGroup, fixedFlagsGroupName, validFlags);
                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.WaitForFixedUpdate, fixedFlagsGroup, fixedFlagsGroupName, validFlags);

                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.OnPreCull, cameraFlagsGroup, cameraFlagsGroupName, validFlags);
                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.OnPreRender, cameraFlagsGroup, cameraFlagsGroupName, validFlags);
                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.OnPostRender, cameraFlagsGroup, cameraFlagsGroupName, validFlags);
                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.OnBeforeRender, cameraFlagsGroup, cameraFlagsGroupName, validFlags);

                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.BeginContextRendering, renderingFlagsGroup, renderingFlagsGroupName, validFlags);
                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.BeginFrameRendering, renderingFlagsGroup, renderingFlagsGroupName, validFlags);
                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.BeginCameraRendering, renderingFlagsGroup, renderingFlagsGroupName, validFlags);
                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.EndFrameRendering, renderingFlagsGroup, renderingFlagsGroupName, validFlags);
                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.EndContextRendering, renderingFlagsGroup, renderingFlagsGroupName, validFlags);
                TryAddFlagToMenu(menu, updateMask, currentFlags, UpdateFlags.EndCameraRendering, renderingFlagsGroup, renderingFlagsGroupName, validFlags);

                if (validFlags.HasFlag(UpdateFlags.Sources))
                    AddFlagToMenu(menu, "Sources", updateMask, currentFlags, UpdateFlags.Sources);

                if (validFlags.HasFlag(UpdateFlags.WaitForSeconds))
                    AddFlagToMenu(menu, "Wait For Seconds", updateMask, currentFlags, UpdateFlags.WaitForSeconds);

                menu.DropDown(new Rect(new Vector2(position.x, position.y + position.height), Vector2.zero));
            }
        }

        protected static void TryAddFlagToMenu(GenericMenu menu, SerializedProperty updateMask, UpdateFlags currentFlags, UpdateFlags newFlag, UpdateFlags flagGroup, string groupName, UpdateFlags validFlags)
        {
            if (validFlags.HasFlag(newFlag))
            {
                string path = ObjectNames.NicifyVariableName(newFlag.ToString());

                UpdateFlags remainding = flagGroup & validFlags;

                // If this is not the last one, and there are other flags outside of this group
                if (remainding != newFlag)// && remainding != flagGroup)
                    path = groupName + path;

                AddFlagToMenu(menu, path, updateMask, currentFlags, newFlag);
            }
        }

        protected static void AddFlagToMenu(GenericMenu menu, string path, SerializedProperty updateMask, UpdateFlags currentFlags, UpdateFlags newFlag)
        {
            UpdateFlags newFlags;

            if (newFlag == UpdateFlags.Never)
                newFlags = UpdateFlags.Never;
            else if ((newFlag & (newFlag - 1)) != 0) // Has more than one flag
                newFlags = newFlag;
            else if (currentFlags.HasFlag(newFlag))
                newFlags = currentFlags & ~newFlag;
            else
                newFlags = currentFlags | newFlag;

            menu.AddItem(new GUIContent(path), newFlag == UpdateFlags.Never ? currentFlags == UpdateFlags.Never : currentFlags.HasFlag(newFlag),
                OnUpdateFlagSelected, new object[] { updateMask, newFlags });
        }

        protected static void OnUpdateFlagSelected(object asObject)
        {
            if (asObject is object[] asArray && asArray.Length == 2)
                OnUpdateFlagSelected(asArray[0] as SerializedProperty, (UpdateFlags)asArray[1]);
        }
        protected static void OnUpdateFlagSelected(SerializedProperty property, UpdateFlags flags)
        {
            if (property == null) return;

            property.FindPropertyRelative("_updateFlags").longValue = (long)flags;
            property.serializedObject.ApplyModifiedProperties();

            Validate(property);
        }

        protected virtual void DrawSources(Rect position, SerializedProperty sourcesProperty, ref Rect current)
        {
            current = new Rect(position.x, current.y + current.height + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUI.GetPropertyHeight(sourcesProperty));

            //sourcesList.Clear();
            //int previousSize = PropertyToSourcesList(sourcesProperty);

            //EditorGUI.BeginChangeCheck();
            HandleDragAndDrop(new Rect(current.xMin, current.yMin, current.width, EditorGUIUtility.singleLineHeight), sourcesProperty);

            EditorGUI.PropertyField(current, sourcesProperty, new GUIContent(sourcesProperty.displayName), true);
            /*if (EditorGUI.EndChangeCheck() && property.TryGetObject(out UpdateMask updateMask))
            {
                // TODO: There must be a better way to do this
                // TODO: What if we add a validate function that we manually call if there are ever any changes, that way I can just use my Validate code
                int newSize = PropertyToSourcesList(sourcesProperty);

                SourcesListToProperty(sourcesProperty, 0, previousSize);
                sourcesProperty.serializedObject.ApplyModifiedProperties();
                RemoveFlags(property, UpdateFlags.Sources);
                //updateSource.UpdateFlags &= ~UpdateFlags.Sources;

                SourcesListToProperty(sourcesProperty, previousSize, previousSize + newSize);
                property.FindPropertyRelative("_updateFlags").intValue = (int)updateMask.UpdateFlags;
                sourcesProperty.serializedObject.ApplyModifiedProperties();
                AddFlags(property, UpdateFlags.Sources);
                //updateSource.UpdateFlags |= UpdateFlags.Sources;
            }*/
        }

        protected virtual void DrawWaitForSeconds(Rect position, SerializedProperty waitForSecondsProperty, ref Rect current)
        {
            current = new Rect(position.x, current.y + current.height + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUI.GetPropertyHeight(waitForSecondsProperty));

            //waitForSecondsList.Clear();
            //int previousSize = PropertyToWaitForSecondsList(waitForSecondsProperty);

            //EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(current, waitForSecondsProperty, new GUIContent(waitForSecondsProperty.displayName), true);
            /*if (EditorGUI.EndChangeCheck() && property.TryGetObject(out UpdateMask updateMask))
            {
                // TODO: There must be a better way to do this
                int newSize = PropertyToWaitForSecondsList(waitForSecondsProperty);

                WaitForSecondsListToProperty(waitForSecondsProperty, 0, previousSize);
                waitForSecondsProperty.serializedObject.ApplyModifiedProperties();
                RemoveFlags(property, UpdateFlags.WaitForSeconds);
                //updateSource.UpdateFlags &= ~UpdateFlags.WaitForSeconds;

                WaitForSecondsListToProperty(waitForSecondsProperty, previousSize, previousSize + newSize);
                property.FindPropertyRelative("_updateFlags").intValue = (int)updateMask.UpdateFlags;
                waitForSecondsProperty.serializedObject.ApplyModifiedProperties();
                AddFlags(property, UpdateFlags.WaitForSeconds);
                //updateSource.UpdateFlags |= UpdateFlags.WaitForSeconds;
            }*/
        }

        /*protected virtual int PropertyToSourcesList(SerializedProperty property)
        {
            int count = property.arraySize;

            SerializedProperty eventProperty;
            for (int i = 0; i < count; i++)
            {
                eventProperty = property.GetArrayElementAtIndex(i);
                sourcesList.Add(new EventSource(eventProperty.FindPropertyRelative("SourceObject").objectReferenceValue,
                    eventProperty.FindPropertyRelative("EventName").stringValue));
            }

            return count;
        }*/

        /*protected virtual void SourcesListToProperty(SerializedProperty property, int start, int end)
        {
            property.arraySize = end - start;

            SerializedProperty eventProperty;
            EventSource source;

            for (int i = start; i < end; i++)
            {
                eventProperty = property.GetArrayElementAtIndex(i - start);
                source = sourcesList[i];

                eventProperty.FindPropertyRelative("SourceObject").objectReferenceValue = source.SourceObject;
                eventProperty.FindPropertyRelative("EventName").stringValue = source.EventName;
            }
        }*/

        /*protected virtual int PropertyToWaitForSecondsList(SerializedProperty property)
        {
            int count = property.arraySize;

            for (int i = 0; i < count; i++)
                waitForSecondsList.Add(property.GetArrayElementAtIndex(i).floatValue);

            return count;
        }*/

        /*protected virtual void WaitForSecondsListToProperty(SerializedProperty property, int start, int end)
        {
            property.arraySize = end - start;

            for (int i = start; i < end; i++)
                property.GetArrayElementAtIndex(i - start).floatValue = waitForSecondsList[i];
        }*/

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            UpdateFlags mask = (UpdateFlags)property.FindPropertyRelative("_updateFlags").intValue;

            float height = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                if (mask.HasFlag(UpdateFlags.Sources))
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_sources")) + EditorGUIUtility.standardVerticalSpacing;

                if (mask.HasFlag(UpdateFlags.WaitForSeconds))
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_waitForSeconds")) + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }
    }
}
