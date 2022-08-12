using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Misc.EditorHelpers;

namespace Misc.Events
{
    // TODO: Might be useful to add some context menus?
    // Replaced Type with Generic. Could only do one as it was too slow otherwise

    [CustomPropertyDrawer(typeof(SerializableListener), true)]
    public class SerializableListenerDrawer : PropertyDrawer
    {
        private static int DEPTH = 3;

        private SerializableEventBase currentEvent;
        private bool _changeNextUpdate = true;

        private Dictionary<object, object> _previousValues = new Dictionary<object, object>();
        private Dictionary<System.Type, MethodInfo[]> _parseMethods = new Dictionary<System.Type, MethodInfo[]>();
        private Dictionary<KeyValuePair<MethodInfo, System.Type>, MethodInfo> _genericMethods = new Dictionary<KeyValuePair<MethodInfo, System.Type>, MethodInfo>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty targetObjectProperty = property.FindPropertyRelative("_targetObject"),
                targetProcessesProperty = property.FindPropertyRelative("_targetProcesses"),
                targetParametersProperty = property.FindPropertyRelative("_targetParameters"),
                dataProperty = property.FindPropertyRelative("_data");

            // TODO: Dont know why this needs to be double called to work, but the height doesnt work if I don't :/
            if (_changeNextUpdate)
            {
                GUI.changed = true;
                _changeNextUpdate = false;
            }

            position.y += EditorGUIUtility.standardVerticalSpacing;

            currentEvent = property.GetParentObject() as SerializableEventBase;

            if (currentEvent == null) return;

            // Property context menu
            if (EditorUtils.HandleContextEvent(new Rect(position.xMin - EditorGUIUtility.singleLineHeight * 2f, position.yMin, EditorGUIUtility.singleLineHeight * 2f, position.height)))
            {
                EditorUtils.DoPropertyContextMenu(property);
                _changeNextUpdate = true;
            }

            // Expanding
            Rect isExpandedRect = new Rect(position.x, position.y, 0f, EditorGUIUtility.singleLineHeight);

            if (targetParametersProperty.arraySize > 0)
                property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(isExpandedRect, property.isExpanded, GUIContent.none, EditorStyles.foldout);

            // Get paramerType
            System.Type[] argTypes = new System.Type[currentEvent.parameterCount];

            for (int i = 0; i < currentEvent.parameterCount; i++)
                argTypes[i] = currentEvent.GetParameterType(i);

            Rect targetObjectRect = new Rect(position.x, position.y, position.width * 0.28f, EditorGUIUtility.singleLineHeight);
            bool clearOnInvalid = TargetObjectField(targetObjectRect, targetObjectProperty);

            Rect targetPathRect = new Rect(targetObjectRect.xMax + EditorGUIUtility.standardVerticalSpacing, position.y,
                position.width - targetObjectRect.width - EditorGUIUtility.standardVerticalSpacing, EditorGUIUtility.singleLineHeight);

            // Check if member is valid
            bool isValid = TryGetMemberInfo(targetObjectProperty.objectReferenceValue ? targetObjectProperty.objectReferenceValue.GetType() : null,
                typeof(void), targetProcessesProperty, targetParametersProperty, out ProcessMode lastMode, out MemberInfo lastMember);

            if (!isValid && clearOnInvalid)
            {
                targetProcessesProperty.ClearArray();
                targetParametersProperty.ClearArray();
                _changeNextUpdate = true;
                property.serializedObject.ApplyModifiedProperties();
            }

            // Check if path has changed
            if (TargetPathField(targetPathRect, targetObjectProperty, targetProcessesProperty, targetParametersProperty, dataProperty, typeof(void), 4))
            {
                property.isExpanded = true;
                _changeNextUpdate = true;
                property.serializedObject.ApplyModifiedProperties();
            }

            // Check if still valid
            isValid = TryGetMemberInfo(targetObjectProperty.objectReferenceValue ? targetObjectProperty.objectReferenceValue.GetType() : null,
                typeof(void), targetProcessesProperty, targetParametersProperty, out lastMode, out lastMember);

            if (property.isExpanded)
            {
                ParameterInfo[] parameterInfos = null;

                if (isValid && lastMember is MethodInfo methodInfo)
                    parameterInfos = methodInfo.GetParameters();

                // Get values
                int boolIndex = 0, intIndex = 0, floatIndex = 0, stringIndex = 0, objectIndex = 0;

                for (int i = 0; i < targetParametersProperty.arraySize; i++)
                {
                    SerializedProperty parameterProperty = targetParametersProperty.GetArrayElementAtIndex(i);

                    SerializedProperty typeProperty = parameterProperty.FindPropertyRelative("_type"),
                        modeProperty = parameterProperty.FindPropertyRelative("_mode"),
                        processesProperty = parameterProperty.FindPropertyRelative("_processes");

                    if (typeProperty != null && modeProperty != null && processesProperty != null)
                    {
                        // Get rects
                        Rect parameterLabelRect = new Rect(position.x + EditorGUIUtility.singleLineHeight,
                                position.y + (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * (i + 1),
                                targetObjectRect.width - EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight),
                            parameterTypeRect = new Rect(targetPathRect.x, parameterLabelRect.y,
                                (targetPathRect.width - EditorGUIUtility.standardVerticalSpacing) * 0.4f, parameterLabelRect.height),
                            parameterValueRect = new Rect(parameterTypeRect.xMax + EditorGUIUtility.standardVerticalSpacing,
                                parameterLabelRect.y, (targetPathRect.width - EditorGUIUtility.standardVerticalSpacing) * 0.6f, parameterLabelRect.height);

                        System.Type parameterType = System.Type.GetType(typeProperty.stringValue, false);

                        ParameterMode mode = (ParameterMode)modeProperty.longValue;//, newMode;

                        // Get initial values
                        object value = GetParameterValue(mode, dataProperty, boolIndex, intIndex, floatIndex, stringIndex, objectIndex);
                        GetParameterDataCount(mode, value, out int boolCount, out int intCount, out int floatCount, out int stringCount, out int objectCount);

                        ParameterModeField(parameterTypeRect, parameterProperty, dataProperty);

                        if (parameterInfos != null && i < parameterInfos.Length)
                            EditorGUI.LabelField(parameterLabelRect, $"{ObjectNames.NicifyVariableName(parameterInfos[i].Name)} ({EventUtils.GetTypeName(parameterType)})");
                        else if (lastMember != null)
                            EditorGUI.LabelField(parameterLabelRect, $"{lastMember.Name} ({EventUtils.GetTypeName(parameterType)})");
                        else
                            EditorGUI.LabelField(parameterLabelRect, $"({EventUtils.GetTypeName(parameterType)})");

                        if (!modeProperty.hasMultipleDifferentValues)
                        {
                            switch (mode)
                            {
                                case ParameterMode.Args1:
                                    if (currentEvent != null && currentEvent.parameterCount > 0)
                                        TargetPathField(parameterValueRect, currentEvent.GetParameterType(0), processesProperty, null, dataProperty, parameterType, 0);
                                    break;
                                case ParameterMode.Args2:
                                    if (currentEvent != null && currentEvent.parameterCount > 1)
                                        TargetPathField(parameterValueRect, currentEvent.GetParameterType(1), processesProperty, null, dataProperty, parameterType, 0);
                                    break;
                                case ParameterMode.Args3:
                                    if (currentEvent != null && currentEvent.parameterCount > 2)
                                        TargetPathField(parameterValueRect, currentEvent.GetParameterType(2), processesProperty, null, dataProperty, parameterType, 0);
                                    break;
                                case ParameterMode.Args4:
                                    if (currentEvent != null && currentEvent.parameterCount > 3)
                                        TargetPathField(parameterValueRect, currentEvent.GetParameterType(3), processesProperty, null, dataProperty, parameterType, 0);
                                    break;
                                case ParameterMode.Bool:
                                    value = EditorGUI.Toggle(parameterValueRect, (bool)value);
                                    break;
                                case ParameterMode.Int:
                                    if (parameterType.Equals(typeof(LayerMask)))
                                        value = EditorGUI.LayerField(parameterValueRect, (int)value);
                                    else if (parameterType.IsEnum)
                                        value = System.Convert.ToInt32(EditorGUI.EnumFlagsField(parameterValueRect, (System.Enum)System.Enum.ToObject(parameterType, (int)value)));
                                    else
                                        value = EditorGUI.IntField(parameterValueRect, (int)value);
                                    break;
                                case ParameterMode.Vector2Int:
                                    value = EditorGUI.Vector2IntField(parameterValueRect, GUIContent.none, (Vector2Int)value);
                                    break;
                                case ParameterMode.Vector3Int:
                                    value = EditorGUI.Vector3IntField(parameterValueRect, GUIContent.none, (Vector3Int)value);
                                    break;
                                case ParameterMode.Float:
                                    value = EditorGUI.FloatField(parameterValueRect, GUIContent.none, (float)value);
                                    break;
                                case ParameterMode.Vector2:
                                    value = EditorGUI.Vector2Field(parameterValueRect, GUIContent.none, (Vector2)value);
                                    break;
                                case ParameterMode.Vector3:
                                    value = EditorGUI.Vector3Field(parameterValueRect, GUIContent.none, (Vector3)value);
                                    break;
                                case ParameterMode.Vector4:
                                    value = EditorGUI.Vector4Field(parameterValueRect, GUIContent.none, (Vector4)value);
                                    break;
                                case ParameterMode.Char:
                                    string asString = EditorGUI.TextField(parameterValueRect, ((char)value).ToString());
                                    value = asString.Length > 0 ? asString[0] : default(char);
                                    break;
                                case ParameterMode.String:
                                    value = EditorGUI.TextField(parameterValueRect, (string)value);
                                    break;
                                case ParameterMode.Object:
                                    {
                                        SerializedProperty objectProperty = dataProperty.FindPropertyRelative("_objectValues").ForceGetArrayElementAtIndex(objectIndex);

                                        bool clearParameterOnInvalid = TargetObjectField(new Rect(parameterTypeRect.x, parameterTypeRect.y, parameterTypeRect.width - 17, parameterTypeRect.height),
                                            objectProperty, parameterType);

                                        bool parameterIsValid = TryGetMemberInfo(objectProperty.objectReferenceValue ? objectProperty.objectReferenceValue.GetType() : null,
                                            parameterType, processesProperty, null, out _, out _);

                                        if (!parameterIsValid && clearParameterOnInvalid)
                                            processesProperty.ClearArray();

                                        TargetPathField(parameterValueRect, objectProperty, processesProperty, null, dataProperty, parameterType, 0);

                                        value = objectProperty.objectReferenceValue;

                                        break;
                                    }
                                case ParameterMode.Rect:
                                    value = EditorGUI.RectField(parameterValueRect, (Rect)value);
                                    break;
                                case ParameterMode.RectInt:
                                    value = EditorGUI.RectIntField(parameterValueRect, (RectInt)value);
                                    break;
                                case ParameterMode.Bounds:
                                    value = EditorGUI.BoundsField(parameterValueRect, (Bounds)value);
                                    break;
                                case ParameterMode.BoundsInt:
                                    value = EditorGUI.BoundsIntField(parameterValueRect, (BoundsInt)value);
                                    break;
                                case ParameterMode.Color:
                                    value = EditorGUI.ColorField(parameterValueRect, (Color)value);
                                    break;
                                case ParameterMode.Gradient:
                                    value = EditorGUI.GradientField(parameterValueRect, (Gradient)value);
                                    break;
                                case ParameterMode.Curve:
                                    value = EditorGUI.CurveField(parameterValueRect, (AnimationCurve)value);
                                    break;
                                case ParameterMode.Quaternion:
                                    Quaternion quaternion = (Quaternion)value;
                                    Vector4 asVector = EditorGUI.Vector4Field(parameterValueRect, GUIContent.none, new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w));
                                    value = new Quaternion(asVector.x, asVector.y, asVector.z, asVector.w);
                                    break;
                            }

                            // Check the new length (gradiant, etc. could lead to changes in size)
                            GetParameterDataCount(mode, value, out int newBoolCount, out int newIntCount, out int newFloatCount, out int newStringCount, out int newObjectCount);

                            ModifyDataSizes(dataProperty, boolIndex, newBoolCount - boolCount,
                                intIndex, newIntCount - intCount,
                                floatIndex, newFloatCount - floatCount,
                                stringIndex, newStringCount - stringCount,
                                objectIndex, newObjectCount - objectCount);

                            SetParameterValue(mode, value, dataProperty, boolIndex, intIndex, floatIndex, stringIndex, objectIndex);

                            // Update the indices for the next parameter
                            boolIndex += newBoolCount;
                            intIndex += newIntCount;
                            floatIndex += newFloatCount;
                            stringIndex += newStringCount;
                            objectIndex += newObjectCount;
                        }
                    }
                }

                // Ensure there are no accidental extras
                SetDataSizes(dataProperty, boolIndex, intIndex, floatIndex, stringIndex, objectIndex);
            }

            EditorGUI.EndFoldoutHeaderGroup();

            EditorGUI.EndProperty();
        }

        private List<System.Type> types = new List<System.Type>();

        // TODO: Should manually have object type
        private bool TargetPathField(Rect position, SerializedProperty objectProperty, SerializedProperty processesProperty, SerializedProperty parametersProperty, SerializedProperty dataProperty, System.Type returnType, int maxParametersLength)
        {
            if (EditorUtils.HandleContextEvent(position))
            {
                EditorUtils.DoPropertyContextMenu(parametersProperty != null ? parametersProperty.GetParent() : processesProperty); // TODO: Would be nice if this could handle things better
                _changeNextUpdate = true;
            }

            Object target = objectProperty.objectReferenceValue;
            System.Type targetType = target != null ? target.GetType() : null;

            string noneText = returnType.Equals(targetType) ? "Result" : "No Member", targetText;

            if (processesProperty.hasMultipleDifferentValues)
                targetText = "—";
            else if (target == null || processesProperty.arraySize == 0)
                targetText = noneText;
            else
            {
                targetText = targetType != null ? $"{targetType.Name}." : "";
                targetText += GetMethodsPathName(processesProperty, parametersProperty);
            }

            EditorGUI.BeginDisabledGroup(targetType == null || !IsValidItem(0, targetType, returnType, maxParametersLength, true, false));

            if (EditorGUI.DropdownButton(position, new GUIContent(targetText), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent(noneText), processesProperty == null || processesProperty.arraySize == 0, OnSelectedProcess,
                    new object[] { objectProperty, processesProperty, parametersProperty, dataProperty, target, null, null });

                menu.AddSeparator("");

                GameObject gameObject = null;

                if (target is GameObject)
                    gameObject = (GameObject)target;
                else if (target is Component targetComponent)
                    gameObject = targetComponent.gameObject;

                if (gameObject != null)
                {
                    types.Clear();

                    foreach (Component component in gameObject.GetComponents<Component>())
                    {
                        System.Type type = component.GetType();

                        if (!types.Contains(type)) types.Add(type);
                    }

                    AddItems(menu, 0, returnType, maxParametersLength, objectProperty, processesProperty, parametersProperty, dataProperty, gameObject, new SerializableProcess[0], typeof(GameObject), "", "GameObject", true, false);

                    foreach (System.Type type in types)
                        AddItems(menu, 0, returnType, maxParametersLength, objectProperty, processesProperty, parametersProperty, dataProperty, targetType == type ? target : gameObject.GetComponent(type), new SerializableProcess[0], type, "", EventUtils.GetTypeName(type), true, false);
                }
                else
                    AddItems(menu, 0, returnType, maxParametersLength, objectProperty, processesProperty, parametersProperty, dataProperty, objectProperty?.objectReferenceValue, new SerializableProcess[0], targetType, "", EventUtils.GetTypeName(targetType), true, false);

                // display the menu
                if (menu.GetItemCount() > 0)
                {
                    menu.DropDown(new Rect(new Vector2(position.x, position.y + position.height), Vector2.zero));
                    EditorGUI.EndDisabledGroup();
                    return true;
                }
            }

            EditorGUI.EndDisabledGroup();
            return false;
        }

        private bool TargetPathField(Rect position, System.Type startType, SerializedProperty processesProperty, SerializedProperty parametersProperty, SerializedProperty dataProperty, System.Type returnType, int maxParametersLength)
        {
            if (EditorUtils.HandleContextEvent(position))
            {
                EditorUtils.DoPropertyContextMenu(parametersProperty != null ? parametersProperty.GetParent() : processesProperty); // TODO: Would be nice if this could handle things better
                _changeNextUpdate = true;
            }

            string noneText = returnType.Equals(startType) ? "Result" : "No Member", targetText;

            if (processesProperty.hasMultipleDifferentValues)
                targetText = "—";
            else if (startType == null)
                targetText = noneText;
            else if (processesProperty.arraySize == 0)
            {
                // TODO: Target path might match with no
                targetText = noneText;
            }
            else
                targetText = GetMethodsPathName(processesProperty, parametersProperty);

            EditorGUI.BeginDisabledGroup(!IsValidItem(0, startType, returnType, maxParametersLength, true, false));

            if (EditorGUI.DropdownButton(position, new GUIContent(targetText), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent(noneText), processesProperty.arraySize == 0, OnSelectedProcess,
                    new object[] { null, processesProperty, parametersProperty, dataProperty, null, null, null });

                if (startType != null && IsValidItem(0, startType, returnType, 0, true, false)) // TODO: Check if separator is necessary
                {
                    menu.AddSeparator("");
                    AddItems(menu, 0, returnType, maxParametersLength, null, processesProperty, parametersProperty, dataProperty, null, new SerializableProcess[0], startType, "", "Arg", true, false);
                }

                // display the menu
                if (menu.GetItemCount() > 0)
                {
                    menu.DropDown(new Rect(new Vector2(position.x, position.y + position.height), Vector2.zero));
                    EditorGUI.EndDisabledGroup();
                    return true;
                }
            }

            EditorGUI.EndDisabledGroup();
            return false;
        }

        private static string GetMethodsPathName(SerializedProperty methodsProperty, SerializedProperty parametersProperty)
        {
            string targetText = "";

            for (int i = 0; i < methodsProperty.arraySize; i++)
            {
                SerializedProperty methodProperty = methodsProperty.GetArrayElementAtIndex(i);

                string name = methodProperty.FindPropertyRelative("_name")?.stringValue;

                if (name.StartsWith("set_") || name.StartsWith("get_"))
                {
                    if (i != 0) targetText += ".";
                    targetText += name.Remove(0, 4);
                }
                else
                {
                    switch ((ProcessMode)methodProperty.FindPropertyRelative("_mode").longValue)
                    {
                        case ProcessMode.Method:
                            if (i != 0) targetText += ".";

                            if (i == methodsProperty.arraySize - 1 && parametersProperty != null && parametersProperty.arraySize > 0)
                                targetText += $"{name}(...)";
                            else
                                targetText += $"{name}()";
                            break;

                        case ProcessMode.MethodWithType:
                        case ProcessMode.Parse:
                            if (i != 0) targetText += ".";

                            targetText += $"{name}(...)";
                            break;

                        case ProcessMode.Cast:
                            string[] split = name.Split(',');
                            split = split[0].Split('.');

                            targetText += " as T"; //$" as {split[split.Length - 1]}"; // TODO: use short name
                            break;

                        default: // Field/Property
                            if (i != 0) targetText += ".";
                            targetText += name; // TODO: use short name
                            break;
                    }
                }
            }

            return targetText;
        }

        private void AddItems(GenericMenu menu, int depth, System.Type returnType, int maxParametersLength,
            SerializedProperty selectedObject, SerializedProperty selectedMethods, SerializedProperty selectedParameters, SerializedProperty data,
            Object currentObject, SerializableProcess[] currentMethods, System.Type currentType, string currentPath, string currentName, bool read, bool write)
        {
            if (depth < DEPTH)
            {
                BindingFlags flags = EventUtility.publicInstanceFlags;//depth == 0 ? EventUtility.publicFlags : EventUtility.publicInstanceFlags;

                // Get all valid fields
                FieldInfo[] fields = currentType.GetFields(flags);
                List<FieldInfo> fieldsList = new List<FieldInfo>(fields.Length);

                foreach (FieldInfo field in fields)
                    if (IsValidField(depth + 1, field, returnType, maxParametersLength)) fieldsList.Add(field);

                fieldsList.Sort(CompareFields);

                // Get all valid properties
                PropertyInfo[] properties = currentType.GetProperties(flags);
                List<PropertyInfo> propertiesList = new List<PropertyInfo>(properties.Length);

                foreach (PropertyInfo property in properties)
                    if (IsValidProperty(depth + 1, property, returnType, maxParametersLength)) propertiesList.Add(property);

                propertiesList.Sort(CompareProperties);

                // Get all valid methods
                MethodInfo[] methods = currentType.GetMethods(flags);
                List<MethodInfo> methodsList = new List<MethodInfo>(methods.Length);

                foreach (MethodInfo method in methods)
                    if (IsValidMethod(depth + 1, method, returnType, maxParametersLength)) methodsList.Add(method);

                methodsList.Sort(CompareMethods);

                // Get all Parse methods
                MethodInfo[] parseMethods = GetParseMethods(returnType, currentType);

                // Get the new path
                string newPath = string.IsNullOrEmpty(currentPath) ? currentName : $"{currentPath}/{currentName}";

                bool hasParent = false, hasChildren = fieldsList.Count + propertiesList.Count + methodsList.Count > 0;

                // Try as is
                if ((read && currentType.Equals(returnType)) || (write && returnType.Equals(typeof(void)))) // TODO: No wonder this is never called, fields wont ever have a current type of typeof(void), need an exception
                {
                    bool isSelected = IsSelected(currentObject, currentMethods, null, selectedObject, selectedMethods, null);

                    System.Type[] parameterTypes = returnType.Equals(typeof(void)) ? new System.Type[] { currentType } : null;

                    object[] parameters = new object[] { selectedObject, selectedMethods, selectedParameters, data, currentObject, currentMethods, parameterTypes };

                    if (hasChildren || parseMethods.Length > 0)
                        menu.AddItem(new GUIContent($"{newPath}/Result"), isSelected, OnSelectedProcess, parameters);
                    else
                        menu.AddItem(new GUIContent(newPath), isSelected, OnSelectedProcess, parameters);

                    hasParent = true;
                }
                else if (read && CanCast(currentType, returnType)) // Try casting
                {
                    SerializableProcess newMethod = new SerializableProcess("", ProcessMode.Cast);
                    SerializableProcess[] newMethods = AddToArray(currentMethods, newMethod);

                    AddItems(menu, depth, returnType, maxParametersLength, selectedObject, selectedMethods, selectedParameters, data,
                        currentObject, newMethods, returnType, newPath, $"Result as {EventUtils.GetTypeName(returnType)}", true, false);

                    hasParent = true;
                }

                // Try parsing
                foreach (MethodInfo parseMethod in parseMethods)
                {
                    SerializableProcess newProcess = new SerializableProcess(parseMethod.Name, ProcessMode.Parse);
                    SerializableProcess[] newProcesses = AddToArray(currentMethods, newProcess);

                    AddItems(menu, depth, returnType, maxParametersLength, selectedObject, selectedMethods, selectedParameters, data,
                        currentObject, newProcesses, returnType, newPath, $"{EventUtils.GetTypeName(returnType)}.{parseMethod.Name} (Result)", true, false);

                    hasParent = true;
                }

                if (hasParent && hasChildren) menu.AddSeparator($"{newPath}/");
                if (fieldsList.Count > 0) menu.AddDisabledItem(new GUIContent($"{newPath}/Fields"), false);

                // Add fields
                foreach (FieldInfo field in fieldsList)
                {
                    SerializableProcess[] newMethods = AddToArray(currentMethods, new SerializableProcess(field.Name, ProcessMode.Field));

                    AddItems(menu, depth + 1, returnType, maxParametersLength, selectedObject, selectedMethods, selectedParameters, data,
                        currentObject, newMethods, field.FieldType, newPath, $"{EventUtils.GetTypeName(field.FieldType)} {field.Name}",
                        true, !(field.IsLiteral || field.IsInitOnly));
                }

                // Separtor between field and properties
                if (fieldsList.Count > 0 && propertiesList.Count + methodsList.Count > 0) menu.AddSeparator($"{newPath}/");
                if (propertiesList.Count > 0) menu.AddDisabledItem(new GUIContent($"{newPath}/Properties"), false);

                // Add properties
                foreach (PropertyInfo property in propertiesList)
                {
                    SerializableProcess[] newMethods = AddToArray(currentMethods, new SerializableProcess(property.Name, ProcessMode.Property));

                    MethodInfo getMethod = property.GetGetMethod(), setMethod = property.GetSetMethod();

                    AddItems(menu, depth + 1, returnType, maxParametersLength, selectedObject, selectedMethods, selectedParameters, data,
                        currentObject, newMethods, property.PropertyType, newPath, $"{EventUtils.GetTypeName(property.PropertyType)} {property.Name}",
                        property.CanRead && getMethod != null && getMethod.IsPublic,
                        property.CanWrite && setMethod != null && setMethod.IsPublic);
                }

                // Separtor between properties and methods
                if (propertiesList.Count > 0 && methodsList.Count > 0)
                    menu.AddSeparator($"{newPath}/");

                if (methodsList.Count > 0) menu.AddDisabledItem(new GUIContent($"{newPath}/Methods"), false);

                string currentGroup = null;

                for (int i = 0; i < methodsList.Count; i++)
                {
                    MethodInfo method = methodsList[i];

                    bool isGeneric = method.IsGenericMethodDefinition;
                    if (isGeneric) method = GetGenericMethod(method, returnType);

                    ParameterInfo[] parameters = method.GetParameters();

                    // Group similar together
                    string newerPath = newPath;

                    if (method.Name == currentGroup)
                        newerPath = $"{newPath}/{currentGroup} (...)";
                    else if (i != methodsList.Count - 1 && method.Name == methodsList[i + 1].Name)
                    {
                        currentGroup = method.Name;
                        newerPath = $"{newPath}/{currentGroup} (...)";
                    }
                    else
                        newerPath = newPath;

                    // Get the new name
                    string newName;
                    if (isGeneric) newName = $"{method.Name}<T> (";
                    else newName = $"{method.Name} (";


                    for (int j = 0; j < parameters.Length; j++)
                    {
                        ParameterInfo parameter = parameters[j];

                        if (j == 0)
                            newName += EventUtils.GetTypeName(parameter.ParameterType);
                        else
                            newName += $",{EventUtils.GetTypeName(parameter.ParameterType)}";
                    }

                    newName += ")";

                    if (!method.ReturnType.Equals(typeof(void))) newName = $"{EventUtils.GetTypeName(method.ReturnType)} {newName}";

                    // Get serializable method
                    SerializableProcess newMethod;

                    if (!returnType.Equals(typeof(void)) && !method.ReturnType.Equals(typeof(void)) && parameters.Length == 1 && parameters[0].ParameterType == typeof(System.Type))
                        newMethod = new SerializableProcess(method.Name, ProcessMode.MethodWithType);
                    else
                        newMethod = new SerializableProcess(method.Name, ProcessMode.Method);

                    SerializableProcess[] newMethods = AddToArray(currentMethods, newMethod);

                    if (read && !method.ReturnType.Equals(typeof(void)) && (parameters.Length == 0 || newMethod.mode == ProcessMode.MethodWithType))
                    {
                        AddItems(menu, depth + 1, returnType, maxParametersLength, selectedObject, selectedMethods, selectedParameters, data,
                            currentObject, newMethods, method.ReturnType, newerPath, newName, true, false);
                    }
                    else if (method.ReturnType.Equals(returnType) && depth + 1 < DEPTH)
                    {
                        if (returnType.Equals(typeof(void)))
                            newMethod.mode = ProcessMode.Method;

                        System.Type[] newParameters = new System.Type[parameters.Length];
                        for (int j = 0; j < newParameters.Length; j++)
                            newParameters[j] = parameters[j].ParameterType;

                        menu.AddItem(new GUIContent($"{newerPath}/{newName}"), IsSelected(currentObject, newMethods, newParameters, selectedObject, selectedMethods, selectedParameters), OnSelectedProcess,
                            new object[] { selectedObject, selectedMethods, selectedParameters, data, currentObject, newMethods, newParameters });
                    }
                }
            }
        }

        private MethodInfo[] GetParseMethods(System.Type returnType, System.Type currentType)
        {
            if (!typeof(void).Equals(returnType) && !typeof(void).Equals(currentType) && !returnType.Equals(currentType))
            {
                // Cache Parse Methods
                if (!_parseMethods.TryGetValue(returnType, out MethodInfo[] methods))
                {
                    methods = returnType.GetMethods(EventUtility.publicStaticFlags);

                    List<MethodInfo> methodsList = new List<MethodInfo>(methods.Length);

                    foreach (MethodInfo method in methods)
                    {
                        if (!method.ReturnType.Equals(returnType)) continue;

                        ParameterInfo[] parameterTypes = method.GetParameters();

                        if (parameterTypes.Length == 1) methodsList.Add(method);
                    }

                    _parseMethods[returnType] = methods = methodsList.ToArray();
                }

                int length = 0, index = 0;

                // Return valid methods
                foreach (MethodInfo method in methods)
                {
                    ParameterInfo[] parameterTypes = method.GetParameters();

                    if (currentType.Equals(parameterTypes[0].ParameterType)) length++;
                }

                MethodInfo[] validMethods = new MethodInfo[length];

                foreach (MethodInfo method in methods)
                {
                    ParameterInfo[] parameterTypes = method.GetParameters();

                    if (currentType.Equals(parameterTypes[0].ParameterType)) validMethods[index++] = method;
                }

                return validMethods;
            }

            return new MethodInfo[0];
        }

        public MethodInfo GetGenericMethod(MethodInfo methodInfo, System.Type type)
        {
            KeyValuePair<MethodInfo, System.Type> pair = new KeyValuePair<MethodInfo, System.Type>(methodInfo, type);

            if (!_genericMethods.TryGetValue(pair, out MethodInfo method))
            {
                if (type != null && !type.Equals(typeof(void)) && methodInfo.IsGenericMethodDefinition && !methodInfo.DeclaringType.Equals(type))
                {
                    try
                    {
                        method = methodInfo.MakeGenericMethod(type);

                        if (method.IsGenericMethod) { } // Used to force an error
                    }
                    catch { method = null; }
                }

                _genericMethods[pair] = method;
            }

            return method;
        }

        private bool IsSelected(Object targetObject, SerializableProcess[] targetMethods, System.Type[] targetParameters, SerializedProperty selectedObject, SerializedProperty selectedMethods, SerializedProperty selectedParameters)
        {
            if (targetObject == selectedObject?.objectReferenceValue)
            {
                int targetLength = targetMethods != null ? targetMethods.Length : 0,
                    selectedLength = selectedMethods != null ? selectedMethods.arraySize : 0;

                if (targetLength == selectedLength)
                {
                    for (int i = 0; i < targetMethods.Length; i++)
                    {
                        SerializableProcess targetMethod = targetMethods[i];
                        SerializedProperty selectedMethod = selectedMethods.GetArrayElementAtIndex(i);

                        if (targetMethod == null || (long)targetMethod.mode != selectedMethod.FindPropertyRelative("_mode").longValue
                            || targetMethod.name != selectedMethod.FindPropertyRelative("_name").stringValue)
                            return false;
                    }

                    if (selectedParameters != null && targetMethods.Length > 0 && targetMethods[targetMethods.Length - 1].mode == ProcessMode.Method)
                    {
                        if (targetParameters.Length != selectedParameters.arraySize)
                            return false;

                        for (int i = 0; i < targetParameters.Length; i++)
                        {
                            if (targetParameters[i].AssemblyQualifiedName != selectedParameters.GetArrayElementAtIndex(i).FindPropertyRelative("_type").stringValue)
                                return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }
        private void OnSelectedProcess(object asObject)
        {
            if (asObject is object[] asArray && asArray.Length == 7)
            {
                OnSelectedProcess(asArray[0] as SerializedProperty, asArray[1] as SerializedProperty, asArray[2] as SerializedProperty, asArray[3] as SerializedProperty,
                    asArray[4] as Object, asArray[5] as SerializableProcess[], asArray[6] as System.Type[]);
            }
        }

        private void OnSelectedProcess(SerializedProperty targetObject, SerializedProperty targetMethods, SerializedProperty targetParameters, SerializedProperty data,
            Object newObject, SerializableProcess[] newProcesses, System.Type[] newParameters)
        {
            // TODO: Probably need to clear serializable data (Store it for a backup somewhere)

            if (targetObject != null && newObject != targetObject.objectReferenceValue)
                targetObject.objectReferenceValue = newObject;

            if (targetMethods != null)
            {
                int newMethodsLength = newProcesses != null ? newProcesses.Length : 0;
                targetMethods.arraySize = newMethodsLength;

                for (int i = 0; i < newMethodsLength; i++)
                {
                    SerializableProcess method = newProcesses[i];
                    SerializedProperty methodProperty = targetMethods.GetArrayElementAtIndex(i);

                    methodProperty.FindPropertyRelative("_name").stringValue = method.name;
                    methodProperty.FindPropertyRelative("_mode").longValue = (long)method.mode;
                }

                targetMethods.serializedObject.ApplyModifiedProperties();
            }

            if (targetParameters != null)
            {
                if (newParameters == null || newParameters.Length == 0)
                    targetParameters.ClearArray();
                else
                {
                    int remainingParametersCount = Mathf.Min(newParameters.Length, targetParameters.arraySize);

                    targetParameters.arraySize = newParameters.Length;

                    for (int i = 0; i < remainingParametersCount; i++)
                    {
                        System.Type newParameter = newParameters[i];
                        SerializedProperty parameterProperty = targetParameters.GetArrayElementAtIndex(i);

                        if (newParameter.AssemblyQualifiedName != parameterProperty.FindPropertyRelative("_type")?.stringValue)
                            SetType(parameterProperty, data, newParameter, i);
                    }

                    for (int i = remainingParametersCount; i < newParameters.Length; i++)
                        SetType(targetParameters.GetArrayElementAtIndex(i), data, newParameters[i], i);
                }

                targetParameters.serializedObject.ApplyModifiedProperties();
            }
        }

        private void SetType(SerializedProperty parameter, SerializedProperty data, System.Type type, int parameterIndex)
        {
            if (parameter == null || data == null) return;

            SerializedProperty typeProperty = parameter.FindPropertyRelative("_type");
            SerializedProperty modeProperty = parameter.FindPropertyRelative("_mode");
            SerializedProperty methodsProperty = parameter.FindPropertyRelative("_processes");

            System.Type previousType = System.Type.GetType(typeProperty.stringValue, false);
            typeProperty.stringValue = type.AssemblyQualifiedName;

            methodsProperty.ClearArray();

            if (currentEvent != null)
            {
                int argsIndex = -1;

                for (int i = 0; i < currentEvent.parameterCount; i++)
                {
                    int index = (i + parameterIndex) % currentEvent.parameterCount;

                    if (currentEvent.GetParameterType(index).Equals(type))
                    {
                        argsIndex = index;
                        break;
                    }
                }

                if (argsIndex == -1)
                {
                    for (int i = 0; i < currentEvent.parameterCount; i++)
                    {
                        int index = (i + parameterIndex) % currentEvent.parameterCount;

                        if (currentEvent.GetParameterType(index).Equals(type))
                        {
                            argsIndex = index;
                            break;
                        }
                    }
                }

                ParameterMode newMode;

                if (argsIndex == 0) newMode = ParameterMode.Args1;
                else if (argsIndex == 1) newMode = ParameterMode.Args2;
                else if (argsIndex == 2) newMode = ParameterMode.Args3;
                else if (argsIndex == 3) newMode = ParameterMode.Args4;
                else if (type.Equals(typeof(bool)))
                    newMode = ParameterMode.Bool;
                else if (type.Equals(typeof(int)) || type.Equals(typeof(short)) || type.Equals(typeof(long)) || type.IsEnum)
                    newMode = ParameterMode.Int;
                else if (type.Equals(typeof(float)) || type.Equals(typeof(double)))
                    newMode = ParameterMode.Float;
                else if (type.Equals(typeof(char)))
                    newMode = ParameterMode.Char;
                else if (type.Equals(typeof(string)))
                    newMode = ParameterMode.String;
                else if (type.Equals(typeof(Vector2)))
                    newMode = ParameterMode.Vector2;
                else if (type.Equals(typeof(Vector3)))
                    newMode = ParameterMode.Vector3;
                else if (type.Equals(typeof(Vector4)))
                    newMode = ParameterMode.Vector4;
                else if (type.Equals(typeof(Vector2Int)))
                    newMode = ParameterMode.Vector2Int;
                else if (type.Equals(typeof(Vector3Int)))
                    newMode = ParameterMode.Vector3Int;
                else if (type.Equals(typeof(Rect)))
                    newMode = ParameterMode.Rect;
                else if (type.Equals(typeof(RectInt)))
                    newMode = ParameterMode.RectInt;
                else if (type.Equals(typeof(Bounds)))
                    newMode = ParameterMode.Bounds;
                else if (type.Equals(typeof(BoundsInt)))
                    newMode = ParameterMode.BoundsInt;
                else if (type.Equals(typeof(Color)) || type.Equals(typeof(Color32)))
                    newMode = ParameterMode.Color;
                else if (type.Equals(typeof(Gradient)))
                    newMode = ParameterMode.Gradient;
                else if (type.Equals(typeof(AnimationCurve)))
                    newMode = ParameterMode.Curve;
                else if (type.Equals(typeof(Quaternion)))
                    newMode = ParameterMode.Quaternion;
                else if (type.IsAssignableFrom(typeof(Object)))
                    newMode = ParameterMode.Object;
                else if (currentEvent.parameterCount <= 0)
                    newMode = ParameterMode.Default;
                else if (parameterIndex == 0) newMode = ParameterMode.Args1;
                else if (parameterIndex == 1) newMode = ParameterMode.Args2;
                else if (parameterIndex == 2) newMode = ParameterMode.Args3;
                else if (parameterIndex == 3) newMode = ParameterMode.Args4;
                else newMode = ParameterMode.Args1;

                if ((ParameterMode)modeProperty.longValue != newMode || !(previousType != null && previousType.IsEnum == type.IsEnum))
                    SetParameterMode(parameter, data, modeProperty, newMode);
            }
        }

        private void SetParameterMode(SerializedProperty parameter, SerializedProperty data, SerializedProperty modeProperty, ParameterMode newMode)
        {
            // Where this parameter starts
            GetParameterIndices(parameter, data, out int boolIndex, out int intIndex, out int floatIndex, out int stringIndex, out int objectIndex);

            ParameterMode previousMode = (ParameterMode)modeProperty.longValue;

            object value = GetParameterValue(previousMode, data, boolIndex, intIndex, floatIndex, stringIndex, objectIndex);
            StorePreviousData(parameter, value);
            GetParameterDataCount(previousMode, value, out int boolCount, out int intCount, out int floatCount, out int stringCount, out int objectCount);
            ModifyDataSizes(data, boolIndex, -boolCount, intIndex, -intCount, floatIndex, -floatCount, stringIndex, -stringCount, objectIndex, -objectCount);

            modeProperty.longValue = (long)newMode;

            value = GetDefaultValue(newMode, parameter);
            GetParameterDataCount(newMode, value, out boolCount, out intCount, out floatCount, out stringCount, out objectCount);
            ModifyDataSizes(data, boolIndex, boolCount, intIndex, intCount, floatIndex, floatCount, stringIndex, stringCount, objectIndex, objectCount);
            SetParameterValue(newMode, value, data, boolIndex, intIndex, floatIndex, stringIndex, objectIndex);

            modeProperty.serializedObject.ApplyModifiedProperties();
        }


        #region Valid Check

        private bool IsValidField(int depth, FieldInfo fieldInfo, System.Type returnType, int maxParametersLength)
        {
            if (fieldInfo.IsLiteral || fieldInfo.IsInitOnly) return false;

            foreach (CustomAttributeData attribute in fieldInfo.CustomAttributes)
                if (attribute.AttributeType.Equals(typeof(System.ObsoleteAttribute))) return false;

            return IsValidItem(depth, fieldInfo.FieldType, returnType, maxParametersLength, true, !fieldInfo.IsInitOnly);
        }

        private bool IsValidProperty(int depth, PropertyInfo propertyInfo, System.Type returnType, int maxParametersLength)
        {
            foreach (CustomAttributeData attribute in propertyInfo.CustomAttributes)
                if (attribute.AttributeType.Equals(typeof(System.ObsoleteAttribute))) return false;

            MethodInfo getMethod = propertyInfo.GetGetMethod(), setMethod = propertyInfo.GetSetMethod();

            // TODO: Is Valid Item might assume it can write, even when it cant
            return IsValidItem(depth, propertyInfo.PropertyType, returnType, maxParametersLength,
                propertyInfo.CanRead && getMethod != null && getMethod.IsPublic,
                propertyInfo.CanWrite && setMethod != null && setMethod.IsPublic);
        }

        private bool IsValidMethod(int depth, MethodInfo methodInfo, System.Type returnType, int maxParametersLength)
        {
            if (methodInfo.IsSpecialName) return false;

            foreach (CustomAttributeData attribute in methodInfo.CustomAttributes)
                if (attribute.AttributeType.Equals(typeof(System.ObsoleteAttribute))) return false;

            if (methodInfo.IsGenericMethodDefinition)
            {
                methodInfo = GetGenericMethod(methodInfo, returnType);

                if (methodInfo == null) return false;
            }

            ParameterInfo[] parameters = methodInfo.GetParameters();

            if (parameters.Length > maxParametersLength/* && !(!returnType.Equals(typeof(void)) && !methodInfo.DeclaringType.Equals(returnType) &&
                parameters.Length == 1 && parameters[0].ParameterType.Equals(typeof(System.Type)))*/) // TODO: This is for MethodWithType
                return false;

            bool read = !methodInfo.ReturnType.Equals(typeof(void));

            return IsValidItem(depth, methodInfo.ReturnType, returnType, maxParametersLength, read, !read);
        }

        private bool IsValidItem(int depth, System.Type currentType, System.Type returnType, int maxParametersLength, bool read, bool write)
        {
            if (currentType != null && depth < DEPTH)
            {
                if ((read && currentType.Equals(returnType)) || (write && returnType.Equals(typeof(void)))) return true;

                if (CanCast(currentType, returnType)) return true;

                if (read && IsValidRecursion(depth, currentType, returnType, maxParametersLength))
                    return true;
            }

            return false;
        }

        private bool IsValidRecursion(int depth, System.Type currentType, System.Type returnType, int maxParametersLength)
        {
            if (currentType != null && depth + 1 < DEPTH)
            {
                BindingFlags flags = EventUtility.publicInstanceFlags;

                foreach (FieldInfo field in currentType.GetFields(flags))
                    if (IsValidField(depth + 1, field, returnType, maxParametersLength)) return true;

                foreach (PropertyInfo property in currentType.GetProperties(flags))
                    if (IsValidProperty(depth + 1, property, returnType, maxParametersLength)) return true;

                foreach (MethodInfo method in currentType.GetMethods(flags))
                    if (IsValidMethod(depth + 1, method, returnType, maxParametersLength)) return true;

                foreach (MethodInfo _ in GetParseMethods(returnType, currentType))
                    if (IsValidItem(depth, returnType, returnType, maxParametersLength, true, false)) return true;
            }

            return false;
        }

        #endregion

        #region Sort Elements

        private static int CompareFields(FieldInfo x, FieldInfo y)
            => x.Name.CompareTo(y.Name);

        private static int CompareProperties(PropertyInfo x, PropertyInfo y)
            => x.Name.CompareTo(y.Name);

        private static int CompareMethods(MethodInfo x, MethodInfo y)
        {
            int alphabetical = x.Name.CompareTo(y.Name);

            if (alphabetical != 0) return alphabetical;

            // Otherwise they have the same name
            ParameterInfo[] parametersX = x.GetParameters(), parametersY = y.GetParameters();

            // Try the types in alphabetical order
            int minLength = Mathf.Min(parametersX.Length, parametersY.Length);

            for (int i = 0; i < minLength; i++)
            {
                alphabetical = parametersX[i].ParameterType.Name.CompareTo(parametersY[i].ParameterType.Name);

                if (alphabetical != 0) return alphabetical;
            }

            // Shorter before longer parameters
            if (parametersX.Length != parametersY.Length)
                return parametersX.Length < parametersY.Length ? -1 : 1;

            return 0;
        }

        #endregion

        #region Parameter Field

        private bool ParameterModeField(Rect position, SerializedProperty parameterProperty, SerializedProperty dataProperty)
        {
            if (parameterProperty == null) return false;

            SerializedProperty typeProperty = parameterProperty.FindPropertyRelative("_type"),
                modeProperty = parameterProperty.FindPropertyRelative("_mode");

            if (typeProperty == null || modeProperty == null) return false;

            System.Type parameterType = System.Type.GetType(typeProperty.stringValue, false);

            if (parameterType == null) return false;

            GenericMenu menu = new GenericMenu();

            string parameterTypeName = EventUtils.GetTypeName(parameterType);

            if (currentEvent != null)
            {
                if (currentEvent.parameterCount > 0 && IsValidItem(0, currentEvent.GetParameterType(0), parameterType, 0, true, false))
                    AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Args1, $"{GetParameterName(ParameterMode.Args1)}");

                if (currentEvent.parameterCount > 1 && IsValidItem(0, currentEvent.GetParameterType(1), parameterType, 0, true, false))
                    AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Args2, $"{GetParameterName(ParameterMode.Args2)}");

                if (currentEvent.parameterCount > 2 && IsValidItem(0, currentEvent.GetParameterType(2), parameterType, 0, true, false))
                    AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Args3, $"{GetParameterName(ParameterMode.Args3)}");

                if (currentEvent.parameterCount > 3 && IsValidItem(0, currentEvent.GetParameterType(3), parameterType, 0, true, false))
                    AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Args4, $"{GetParameterName(ParameterMode.Args4)}");
            }

            AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Object, $"Object");

            string typeName = EventUtils.GetTypeName(parameterType);

            if (parameterType.Equals(typeof(bool))) AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Bool, typeName);
            else if (parameterType.IsEnum || parameterType.Equals(typeof(int)) || parameterType.Equals(typeof(short)) || parameterType.Equals(typeof(long)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Int, typeName);
            else if (parameterType.Equals(typeof(float)) || parameterType.Equals(typeof(double)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Float, typeName);
            else if (parameterType.Equals(typeof(char)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Char, typeName);
            else if (parameterType.Equals(typeof(string)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.String, typeName);
            else if (parameterType.Equals(typeof(Vector2)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Vector2, typeName);
            else if (parameterType.Equals(typeof(Vector3)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Vector3, typeName);
            else if (parameterType.Equals(typeof(Vector4)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Vector4, typeName);
            else if (parameterType.Equals(typeof(Vector2Int)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Vector2Int, typeName);
            else if (parameterType.Equals(typeof(Vector3Int)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Vector3Int, typeName);
            else if (parameterType.Equals(typeof(Rect)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Rect, typeName);
            else if (parameterType.Equals(typeof(RectInt)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.RectInt, typeName);
            else if (parameterType.Equals(typeof(Bounds)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Bounds, typeName);
            else if (parameterType.Equals(typeof(BoundsInt)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.BoundsInt, typeName);
            else if (parameterType.Equals(typeof(Color)) || parameterType.Equals(typeof(Color32)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Color, typeName);
            else if (parameterType.Equals(typeof(Gradient)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Gradient, typeName);
            else if (parameterType.Equals(typeof(AnimationCurve)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Curve, typeName);
            else if (parameterType.Equals(typeof(Quaternion)))
                AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Quaternion, typeName);

            string defaultText = parameterType != null && parameterType.IsValueType ? $"Default ({parameterTypeName})" : "Null";
            AddParameterTypeField(menu, parameterProperty, dataProperty, ParameterMode.Default, defaultText);

            EditorGUI.BeginDisabledGroup(menu.GetItemCount() <= 1);

            string currentProperty;
            Rect newPosition = position;

            if (!modeProperty.hasMultipleDifferentValues)
            {
                ParameterMode currentMode = (ParameterMode)modeProperty.longValue;

                if (currentMode == ParameterMode.Object)
                {
                    currentProperty = "";
                    newPosition = new Rect(position.x + position.width - 19, position.y, 19, position.height);
                }
                else if (currentMode == ParameterMode.Default)
                    currentProperty = defaultText;
                else if (currentMode == ParameterMode.Args1 || currentMode == ParameterMode.Args1
                    || currentMode == ParameterMode.Args1 || currentMode == ParameterMode.Args1)
                    currentProperty = GetParameterName(currentMode);
                else
                    currentProperty = typeName;
            }
            else
                currentProperty = "—";

            if (EditorGUI.DropdownButton(newPosition, new GUIContent(currentProperty), FocusType.Passive))
            {
                menu.DropDown(new Rect(new Vector2(position.x, position.y + position.height), Vector2.zero));

                EditorGUI.EndDisabledGroup();
                return true;
            }

            EditorGUI.EndDisabledGroup();
            return false;
        }

        private void AddParameterTypeField(GenericMenu menu, SerializedProperty parameterProperty, SerializedProperty dataProperty, ParameterMode mode, string name)
        {
            SerializedProperty modeProperty = parameterProperty.FindPropertyRelative("_mode");

            menu.AddItem(new GUIContent(name), (ParameterMode)modeProperty.longValue == mode, OnParameterModeSelected,
                new object[] { parameterProperty, dataProperty, mode });
        }

        private void OnParameterModeSelected(object asObject)
        {
            if (asObject is object[] asArray && asArray.Length == 3)
                OnParameterModeSelected(asArray[0] as SerializedProperty, asArray[1] as SerializedProperty, (ParameterMode)asArray[2]);
        }

        private void OnParameterModeSelected(SerializedProperty parameter, SerializedProperty data, ParameterMode mode)
        {
            SerializedProperty modeProperty = parameter.FindPropertyRelative("_mode");

            if ((ParameterMode)modeProperty?.longValue != mode)
                SetParameterMode(parameter, data, modeProperty, mode);
        }

        private string GetParameterName(ParameterMode mode)
        {
            switch (mode)
            {
                case ParameterMode.Args1:
                    if (currentEvent != null && currentEvent.parameterCount == 1)
                        return $"Arg ({EventUtils.GetTypeName(currentEvent?.GetParameterType(0))})";
                    return $"Arg  1 ({EventUtils.GetTypeName(currentEvent?.GetParameterType(0))})";
                case ParameterMode.Args2:
                    return $"Arg 2 ({EventUtils.GetTypeName(currentEvent?.GetParameterType(1))})";
                case ParameterMode.Args3:
                    return $"Arg 3 ({EventUtils.GetTypeName(currentEvent?.GetParameterType(2))})";
                case ParameterMode.Args4:
                    return $"Arg 4 ({EventUtils.GetTypeName(currentEvent?.GetParameterType(3))})";
                case ParameterMode.Bool:
                    return "bool";
                case ParameterMode.Int:
                    return "int";
                case ParameterMode.Vector2Int:
                    return "Vector2Int";
                case ParameterMode.Vector3Int:
                    return "Vector3Int";
                case ParameterMode.Float:
                    return "float";
                case ParameterMode.Vector2:
                    return "Vector2";
                case ParameterMode.Vector3:
                    return "Vector3";
                case ParameterMode.Vector4:
                    return "Vector4";
                case ParameterMode.Char:
                    return "char";
                case ParameterMode.String:
                    return "string";
                case ParameterMode.Object:
                    return "Object";
                case ParameterMode.Rect:
                    return "Rect";
                case ParameterMode.RectInt:
                    return "RectInt";
                case ParameterMode.Bounds:
                    return "Bounds";
                case ParameterMode.BoundsInt:
                    return "BoundsInt";
                case ParameterMode.Color:
                    return "Color";
                case ParameterMode.Gradient:
                    return "Gradient";
                case ParameterMode.Curve:
                    return "Curve";
                case ParameterMode.Quaternion:
                    return "Quaternion";
                default:
                    return "Default";
            }
        }

        #endregion

        private bool CanCast(System.Type from, System.Type to)
        {
            if (to.Equals(from) || from.Equals(typeof(void)) || to.Equals(typeof(void))) return false;

            if (from.IsAssignableFrom(to)) return true;

            /*if (from.IsAssignableFrom(typeof(int)))
                return true; // int -> LayerMask*/

            System.Type toBase = to.BaseType;

            // Try upcasting
            while (toBase != null)
            {
                if (toBase.Equals(from))
                    return true;

                toBase = toBase.BaseType;
            }

            return EventUtility.TryGetCastMethod(from, to, out MemberInfo _);
        }

        private bool TargetObjectField(Rect position, SerializedProperty objectProperty, System.Type preferredType = null)
        {
            if (objectProperty == null) return false;

            if (EditorUtils.HandleContextEvent(position))
                EditorUtils.DoPropertyDropDown(new Rect(position.xMin, position.yMax, 0f, 0f), objectProperty);

            if (objectProperty.hasMultipleDifferentValues)
            {
                EditorGUI.ObjectField(position, objectProperty, typeof(Object), new GUIContent());

                if (!objectProperty.hasMultipleDifferentValues)
                {
                    Object newObject = objectProperty.objectReferenceValue;

                    if (TryGetPreferredType(ref newObject, preferredType))
                        objectProperty.objectReferenceValue = newObject;

                    return true;
                }
            }
            else
            {
                Object beforeObject = objectProperty.objectReferenceValue,
                    newObject = EditorGUI.ObjectField(position, beforeObject, typeof(Object), true);

                if (!TryGetPreferredType(ref newObject, preferredType))
                {
                    if (beforeObject != null && newObject != null)
                        TryGetPreferredType(ref newObject, beforeObject.GetType());
                }

                if (beforeObject != newObject)
                {
                    objectProperty.objectReferenceValue = newObject;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetPreferredType(ref Object current, System.Type preferredType)
        {
            if (current == null || preferredType == null) return false;

            if (preferredType.Equals(typeof(GameObject)))
            {
                if (current is Component newComponent)
                {
                    current = newComponent.gameObject;
                    return true;
                }
            }
            else if (preferredType.IsSubclassOf(typeof(Component)))
            {
                if (current is GameObject newGameObject)
                {
                    if (newGameObject.TryGetComponent(preferredType, out Component newOther))
                    {
                        current = newOther;
                        return true;
                    }
                    // else
                    // TODO: Could instead try to find the first component with the available first method
                }
                else if (current is Component newComponent)
                {
                    if (newComponent.TryGetComponent(preferredType, out Component newOther))
                    {
                        current = newOther;
                        return true;
                    }
                }
            }

            return false;
        }

        #region Array Property Methods

        private object GetParameterValue(ParameterMode mode, SerializedProperty dataProperty, int boolIndex, int intIndex, int floatIndex, int stringIndex, int objectIndex)
        {
            // TODO: Could optimize by only looking for required properties
            GetDataProperties(dataProperty, out SerializedProperty boolsProperty, out SerializedProperty intsProperty,
                out SerializedProperty floatsProperty, out SerializedProperty stringsProperty, out SerializedProperty objectsProperty);

            switch (mode)
            {
                case ParameterMode.Bool:
                    return boolsProperty.ForceGetArrayElementAtIndex(boolIndex++).boolValue;

                case ParameterMode.Int:
                    return intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue;

                case ParameterMode.Vector2Int:
                    return new Vector2Int(intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue, intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue);

                case ParameterMode.Vector3Int:
                    return new Vector3Int(intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue, intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue, intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue);

                case ParameterMode.Float:
                    return floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue;

                case ParameterMode.Vector2:
                    return new Vector2(floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue);

                case ParameterMode.Vector3:
                    return new Vector3(floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue);

                case ParameterMode.Vector4:
                    return new Vector4(floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue);

                case ParameterMode.Char:
                    {
                        SerializedProperty property = stringsProperty.ForceGetArrayElementAtIndex(stringIndex++);

                        string value = property.stringValue;

                        if (value == null || value.Length == 0)
                            return default(char);

                        return value[0];
                    }
                case ParameterMode.String:
                    return stringsProperty.ForceGetArrayElementAtIndex(stringIndex++).stringValue;

                case ParameterMode.Object:
                    return objectsProperty.ForceGetArrayElementAtIndex(objectIndex++).objectReferenceValue;

                case ParameterMode.Rect:
                    return new Rect(floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue);

                case ParameterMode.RectInt:
                    return new RectInt(intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue, intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue,
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue, intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue);

                case ParameterMode.Bounds:
                    return new Bounds(
                        new Vector3(floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue),
                        new Vector3(floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue)
                    );

                case ParameterMode.BoundsInt:
                    return new BoundsInt(
                        new Vector3Int(intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue, intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue, intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue),
                        new Vector3Int(intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue, intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue, intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue)
                    );
                case ParameterMode.Color:
                    return new Color(floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue);

                case ParameterMode.Gradient:
                    Gradient gradient = new Gradient();

                    gradient.mode = (GradientMode)intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue;

                    GradientColorKey[] colorKeys = new GradientColorKey[intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue];

                    for (int i = 0; i < colorKeys.Length; i++)
                        colorKeys[i] = new GradientColorKey(
                            new Color(
                                floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                                floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                                floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                                floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue
                            ),
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue
                        );

                    GradientAlphaKey[] alphaKeys = new GradientAlphaKey[intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue];

                    for (int i = 0; i < alphaKeys.Length; i++)
                        alphaKeys[i] = new GradientAlphaKey(
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue
                        );

                    gradient.SetKeys(colorKeys, alphaKeys);

                    return gradient;

                case ParameterMode.Curve:
                    AnimationCurve curve = new AnimationCurve();

                    curve.preWrapMode = (WrapMode)intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue;
                    curve.postWrapMode = (WrapMode)intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue;

                    Keyframe[] keys = new Keyframe[intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue];

                    for (int i = 0; i < keys.Length; i++)
                        keys[i] = new Keyframe(
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue
                        );

                    curve.keys = keys;

                    return curve;

                case ParameterMode.Quaternion:
                    return new Quaternion(floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue,
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue, floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue);
            }

            return null;
        }

        private object GetDefaultValue(ParameterMode mode, SerializedProperty parameter)
        {
            // Stored previous values
            switch (mode)
            {
                case ParameterMode.Bool:
                case ParameterMode.Int:
                case ParameterMode.Vector2Int:
                case ParameterMode.Vector3Int:
                case ParameterMode.Float:
                case ParameterMode.Vector2:
                case ParameterMode.Vector3:
                case ParameterMode.Vector4:
                case ParameterMode.Char:
                case ParameterMode.String:
                case ParameterMode.Rect:
                case ParameterMode.RectInt:
                case ParameterMode.Bounds:
                case ParameterMode.BoundsInt:
                case ParameterMode.Color:
                case ParameterMode.Gradient:
                case ParameterMode.Curve:
                case ParameterMode.Quaternion:
                    if (_previousValues.TryGetValue(mode, out object value))
                        return value;
                    break;

                case ParameterMode.Object:
                    if (parameter != null && _previousValues.TryGetValue(parameter.propertyPath, out object value2))
                        return value2;
                    break;

                default:
                    // TODO: Nothing to do
                    break;
            }

            // Actual default values
            switch (mode)
            {
                case ParameterMode.Bool:
                    return 0;

                case ParameterMode.Int:
                    return 1;

                case ParameterMode.Vector2Int:
                    return Vector2Int.zero;

                case ParameterMode.Vector3Int:
                    return Vector3Int.zero;

                case ParameterMode.Float:
                    return 0f;

                case ParameterMode.Vector2:
                    return Vector2.zero;

                case ParameterMode.Vector3:
                    return Vector3.zero;

                case ParameterMode.Vector4:
                    return Vector4.zero;

                case ParameterMode.Char:
                    return default(char);

                case ParameterMode.String:
                    return "";

                case ParameterMode.Object:
                    return null;

                case ParameterMode.Rect:
                    return Rect.zero;

                case ParameterMode.RectInt:
                    return new RectInt();

                case ParameterMode.Bounds:
                    return new Bounds();

                case ParameterMode.BoundsInt:
                    return new BoundsInt();

                case ParameterMode.Color:
                    return default(Color);

                case ParameterMode.Gradient:
                    return new Gradient();

                case ParameterMode.Curve:
                    return AnimationCurve.Linear(0, 0, 1, 1);

                case ParameterMode.Quaternion:
                    return Quaternion.identity;
            }

            return null;
        }

        private bool SetParameterValue(ParameterMode mode, object value, SerializedProperty dataProperty, int boolIndex, int intIndex, int floatIndex, int stringIndex, int objectIndex)
        {
            GetDataProperties(dataProperty, out SerializedProperty boolsProperty, out SerializedProperty intsProperty,
                out SerializedProperty floatsProperty, out SerializedProperty stringsProperty, out SerializedProperty objectsProperty);

            switch (mode)
            {
                case ParameterMode.Bool:
                    if (value is bool @bool)
                    {
                        boolsProperty.ForceGetArrayElementAtIndex(boolIndex++).boolValue = @bool;
                        return true;
                    }
                    return false;

                case ParameterMode.Int:
                    if (value is int @int)
                    {
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = @int;
                        return true;
                    }
                    return false;

                case ParameterMode.Vector2Int:
                    if (value is Vector2Int vector2Int)
                    {
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = vector2Int.x;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = vector2Int.y;
                        return true;
                    }
                    return false;

                case ParameterMode.Vector3Int:
                    if (value is Vector3Int vector3Int)
                    {
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = vector3Int.x;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = vector3Int.y;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = vector3Int.z;
                        return true;
                    }
                    return false;

                case ParameterMode.Float:
                    if (value is float @float)
                    {
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = @float;
                        return true;
                    }
                    return false;

                case ParameterMode.Vector2:
                    if (value is Vector2 vector2)
                    {
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = vector2.x;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = vector2.y;
                        return true;
                    }
                    return false;

                case ParameterMode.Vector3:
                    if (value is Vector3 vector3)
                    {
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = vector3.x;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = vector3.y;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = vector3.z;
                        return true;
                    }
                    return false;

                case ParameterMode.Vector4:
                    if (value is Vector4 vector4)
                    {
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = vector4.x;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = vector4.y;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = vector4.z;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = vector4.w;
                        return true;
                    }
                    return false;

                case ParameterMode.Char:
                    {
                        if (value is char @char)
                        {
                            stringsProperty.ForceGetArrayElementAtIndex(stringIndex++).stringValue = @char.ToString();
                            return true;
                        }
                        return false;
                    }
                case ParameterMode.String:
                    if (value is string @string)
                    {
                        stringsProperty.ForceGetArrayElementAtIndex(stringIndex++).stringValue = @string;
                        return true;
                    }
                    return false;

                case ParameterMode.Object:
                    if (value is Object @object)
                    {
                        objectsProperty.ForceGetArrayElementAtIndex(objectIndex++).objectReferenceValue = @object;
                        return true;
                    }
                    return false;

                case ParameterMode.Rect:
                    if (value is Rect rect)
                    {
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = rect.x;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = rect.y;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = rect.width;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = rect.height;
                        return true;
                    }
                    return false;

                case ParameterMode.RectInt:
                    if (value is RectInt rectInt)
                    {
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = rectInt.x;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = rectInt.y;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = rectInt.width;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = rectInt.height;
                        return true;
                    }
                    return false;

                case ParameterMode.Bounds:
                    if (value is Bounds bounds)
                    {
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = bounds.center.x;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = bounds.center.y;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = bounds.center.z;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = bounds.size.x;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = bounds.size.y;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = bounds.size.z;
                        return true;
                    }
                    return false;

                case ParameterMode.BoundsInt:
                    if (value is BoundsInt boundsInt)
                    {
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = boundsInt.position.x;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = boundsInt.position.y;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = boundsInt.position.z;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = boundsInt.size.x;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = boundsInt.size.y;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = boundsInt.size.z;
                        return true;
                    }
                    return false;

                case ParameterMode.Color:
                    if (value is Color)
                    {
                        Color asType = (Color)value;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = asType.r;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = asType.g;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = asType.b;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = asType.a;
                        return true;
                    }
                    return false;

                case ParameterMode.Gradient:
                    if (value is Gradient gradient)
                    {
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = (int)gradient.mode;

                        GradientColorKey[] colorKeys = gradient.colorKeys;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = colorKeys.Length;

                        for (int i = 0; i < colorKeys.Length; i++)
                        {
                            GradientColorKey colorKey = colorKeys[i];
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = colorKey.color.r;
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = colorKey.color.g;
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = colorKey.color.b;
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = colorKey.color.a;
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = colorKey.time;
                        }

                        GradientAlphaKey[] alphaKeys = gradient.alphaKeys;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = colorKeys.Length;

                        for (int i = 0; i < alphaKeys.Length; i++)
                        {
                            GradientAlphaKey alphaKey = alphaKeys[i];
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = alphaKey.alpha;
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = alphaKey.time;
                        }
                        return true;
                    }
                    return false;

                case ParameterMode.Curve:
                    if (value is AnimationCurve curve)
                    {
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = (int)curve.preWrapMode;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = (int)curve.postWrapMode;

                        Keyframe[] keys = curve.keys;
                        intsProperty.ForceGetArrayElementAtIndex(intIndex++).intValue = keys.Length;

                        for (int i = 0; i < keys.Length; i++)
                        {
                            Keyframe key = keys[i];

                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = key.time;
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = key.value;
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = key.inTangent;
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = key.outTangent;
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = key.inWeight;
                            floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = key.outWeight;
                        }
                        return true;
                    }
                    return false;

                case ParameterMode.Quaternion:
                    if (value is Quaternion quaternion)
                    {
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = quaternion.x;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = quaternion.y;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = quaternion.z;
                        floatsProperty.ForceGetArrayElementAtIndex(floatIndex++).floatValue = quaternion.w;
                        return true;
                    }
                    return false;
            }

            return false;
        }

        private bool GetParameterDataCount(ParameterMode mode, object value, out int boolCount, out int intCount, out int floatCount, out int stringCount, out int objectCount)
        {
            boolCount = intCount = floatCount = stringCount = objectCount = 0;

            switch (mode)
            {
                case ParameterMode.Bool:
                    if (value is bool)
                    {
                        boolCount = 1;
                        return true;
                    }
                    return false;

                case ParameterMode.Int:
                    if (value is int)
                    {
                        intCount = 1;
                        return true;
                    }
                    return false;

                case ParameterMode.Vector2Int:
                    if (value is Vector2Int)
                    {
                        intCount = 2;
                        return true;
                    }
                    return false;

                case ParameterMode.Vector3Int:
                    if (value is Vector3Int)
                    {
                        intCount = 3;
                        return true;
                    }
                    return false;

                case ParameterMode.Float:
                    if (value is float)
                    {
                        floatCount = 1;
                        return true;
                    }
                    return false;

                case ParameterMode.Vector2:
                    if (value is Vector2)
                    {
                        floatCount = 2;
                        return true;
                    }
                    return false;

                case ParameterMode.Vector3:
                    if (value is Vector3)
                    {
                        floatCount = 3;
                        return true;
                    }
                    return false;

                case ParameterMode.Vector4:
                    if (value is Vector4)
                    {
                        floatCount = 4;
                        return true;
                    }
                    return false;

                case ParameterMode.Char:
                    {
                        if (value is char)
                        {
                            stringCount = 1;
                            return true;
                        }
                        return false;
                    }
                case ParameterMode.String:
                    if (value is string)
                    {
                        stringCount = 1;
                        return true;
                    }
                    return false;

                case ParameterMode.Object:
                    if (value is Object)
                    {
                        objectCount = 1;
                        return true;
                    }
                    return false;

                case ParameterMode.Rect:
                    if (value is Rect)
                    {
                        floatCount = 4;
                        return true;
                    }
                    return false;

                case ParameterMode.RectInt:
                    if (value is RectInt)
                    {
                        intCount = 4;
                        return true;
                    }
                    return false;

                case ParameterMode.Bounds:
                    if (value is Bounds)
                    {
                        floatCount = 6;
                        return true;
                    }
                    return false;

                case ParameterMode.BoundsInt:
                    if (value is BoundsInt)
                    {
                        intCount = 6;
                        return true;
                    }
                    return false;

                case ParameterMode.Color:
                    if (value is Color)
                    {
                        floatCount = 4;
                        return true;
                    }
                    return false;

                case ParameterMode.Gradient:
                    if (value is Gradient gradient)
                    {
                        intCount = 3;
                        floatCount = 5 * gradient.colorKeys.Length + 2 * gradient.alphaKeys.Length;
                        return true;
                    }
                    return false;

                case ParameterMode.Curve:
                    if (value is AnimationCurve curve)
                    {
                        intCount = 3;
                        floatCount = 6 * curve.keys.Length;
                        return true;
                    }
                    return false;

                case ParameterMode.Quaternion:
                    if (value is Quaternion)
                    {
                        floatCount = 4;
                        return true;
                    }
                    return false;
            }

            return false;
        }

        private void GetParameterIndices(SerializedProperty parameterProperty, SerializedProperty dataProperty, out int boolIndex, out int intIndex, out int floatIndex, out int stringIndex, out int objectIndex)
        {
            SerializedProperty parameters = parameterProperty.GetParent();

            boolIndex = intIndex = floatIndex = stringIndex = objectIndex = 0;

            if (parameters != null && parameters.isArray)
            {
                int count = Mathf.Max(parameterProperty.GetArrayIndex(), parameters.arraySize);

                for (int i = 0; i < count; i++)
                {
                    // Get the mode
                    SerializedProperty modeProperty = parameters.GetArrayElementAtIndex(i).FindPropertyRelative("_mode");
                    ParameterMode mode = (ParameterMode)modeProperty.longValue;

                    // Increment the index
                    object value = GetParameterValue(mode, dataProperty, boolIndex, intIndex, floatIndex, stringIndex, objectIndex);
                    GetParameterDataCount(mode, value, out int boolCount, out int intCount, out int floatCount, out int stringCount, out int objectCount);

                    boolIndex += boolCount;
                    intIndex += intCount;
                    floatIndex += floatCount;
                    stringIndex += stringCount;
                    objectIndex += objectCount;
                }
            }
        }

        private void ModifyDataSizes(SerializedProperty dataProperty, int boolIndex, int boolCount, int intIndex, int intCount,
            int floatIndex, int floatCount, int stringIndex, int stringCount, int objectIndex, int objectCount)
        {
            GetDataProperties(dataProperty, out SerializedProperty boolsProperty, out SerializedProperty intsProperty,
                out SerializedProperty floatsProperty, out SerializedProperty stringsProperty, out SerializedProperty objectsProperty);

            boolsProperty.ModifyArraySize(boolIndex, boolCount);
            intsProperty.ModifyArraySize(intIndex, intCount);
            floatsProperty.ModifyArraySize(floatIndex, floatCount);
            stringsProperty.ModifyArraySize(stringIndex, stringCount);
            objectsProperty.ModifyArraySize(objectIndex, objectCount);
        }

        private void SetDataSizes(SerializedProperty dataProperty, int boolCount, int intCount, int floatCount, int stringCount, int objectCount)
        {
            GetDataProperties(dataProperty, out SerializedProperty boolsProperty, out SerializedProperty intsProperty,
                out SerializedProperty floatsProperty, out SerializedProperty stringsProperty, out SerializedProperty objectsProperty);

            boolsProperty?.SetArraySize(boolCount);
            intsProperty?.SetArraySize(intCount);
            floatsProperty?.SetArraySize(floatCount);
            stringsProperty?.SetArraySize(stringCount);
            objectsProperty?.SetArraySize(objectCount);
        }

        private void GetDataProperties(SerializedProperty dataProperty, out SerializedProperty boolsProperty, out SerializedProperty intsProperty,
            out SerializedProperty floatsProperty, out SerializedProperty stringsProperty, out SerializedProperty objectsProperty)
        {
            boolsProperty = dataProperty.FindPropertyRelative("_boolValues");
            intsProperty = dataProperty.FindPropertyRelative("_intValues");
            floatsProperty = dataProperty.FindPropertyRelative("_floatValues");
            stringsProperty = dataProperty.FindPropertyRelative("_stringValues");
            objectsProperty = dataProperty.FindPropertyRelative("_objectValues");
        }

        private void StorePreviousData(SerializedProperty parameterProperty, object value)
        {
            SerializedProperty modeProperty = parameterProperty.FindPropertyRelative("_mode");
            ParameterMode mode = (ParameterMode)modeProperty.longValue;

            switch (mode)
            {
                case ParameterMode.Bool:
                case ParameterMode.Vector2Int:
                case ParameterMode.Vector3Int:
                case ParameterMode.Float:
                case ParameterMode.Vector2:
                case ParameterMode.Vector3:
                case ParameterMode.Vector4:
                case ParameterMode.Char:
                case ParameterMode.String:
                case ParameterMode.Rect:
                case ParameterMode.RectInt:
                case ParameterMode.Bounds:
                case ParameterMode.BoundsInt:
                case ParameterMode.Color:
                case ParameterMode.Gradient:
                case ParameterMode.Curve:
                case ParameterMode.Quaternion:
                    _previousValues[mode] = value;
                    break;

                case ParameterMode.Int:
                    System.Type type = System.Type.GetType(parameterProperty.FindPropertyRelative("_type").stringValue);

                    if (type != null && type.IsEnum)
                        _previousValues[type] = value;
                    else
                        _previousValues[mode] = value;
                    break;

                case ParameterMode.Object:
                    _previousValues[parameterProperty.propertyPath] = value;
                    break;

                default:
                    // TODO: Nothing to do
                    break;
            }
        }

        #endregion

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty targetParametersProperty = property.FindPropertyRelative("_targetParameters");

            if (targetParametersProperty == null) return EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 4;

            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * targetParametersProperty.arraySize
                + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 4;
        }

        private static bool TryGetMemberInfo(System.Type startType, System.Type returnType, SerializedProperty processesProperty, SerializedProperty parametersProperty, out ProcessMode lastMode, out MemberInfo lastMember)
        {
            if (startType != null && processesProperty != null)
            {
                System.Type[] types = new System.Type[parametersProperty != null ? parametersProperty.arraySize : 0];
                System.Type currentType;

                for (int i = 0; i < types.Length; i++)
                {
                    SerializedProperty parameterProperty = parametersProperty.GetArrayElementAtIndex(i);
                    types[i] = currentType = System.Type.GetType(parameterProperty.FindPropertyRelative("_type").stringValue, false);

                    if (currentType == null)
                    {
                        lastMode = default(ProcessMode);
                        lastMember = null;
                        return false;
                    }
                }

                currentType = startType;

                for (int i = 0; i < processesProperty.arraySize; i++)
                {
                    if (currentType == null) break;

                    SerializedProperty processProperty = processesProperty.GetArrayElementAtIndex(i);
                    lastMode = (ProcessMode)processProperty.FindPropertyRelative("_mode").longValue;
                    string name = processProperty.FindPropertyRelative("_name").stringValue;

                    switch (lastMode)
                    {
                        case ProcessMode.Field:
                            if (EventUtility.TryGetField(currentType, name, out FieldInfo field))
                            {
                                currentType = field.FieldType;

                                if (i == processesProperty.arraySize - 1)
                                {
                                    lastMember = field;
                                    return true;
                                }
                            }
                            else
                                currentType = null;
                            break;
                        case ProcessMode.Property:
                            if (EventUtility.TryGetProperty(currentType, name, out PropertyInfo property))
                            {
                                currentType = property.PropertyType;

                                if (i == processesProperty.arraySize - 1)
                                {
                                    lastMember = property;
                                    return true;
                                }
                            }
                            else
                                currentType = null;
                            break;
                        case ProcessMode.Method:
                            if (i == processesProperty.arraySize - 1)
                            {
                                if (EventUtility.TryGetMethod(currentType, name, types, out MethodInfo method))
                                {
                                    lastMember = method;
                                    return true;
                                }
                                else
                                    currentType = null;
                            }
                            else
                            {
                                if (EventUtility.TryGetMethod(currentType, name, System.Type.EmptyTypes, out MethodInfo method))
                                    currentType = method.ReturnType;
                                else
                                    currentType = null;
                            }
                            break;
                        case ProcessMode.MethodWithType:
                            {
                                EventUtility.SingleType[0] = typeof(System.Type);

                                if (EventUtility.TryGetMethod(currentType, name, EventUtility.SingleType, out MethodInfo method))
                                {
                                    currentType = method.ReturnType;

                                    if (i == processesProperty.arraySize - 1)
                                    {
                                        lastMember = method;
                                        return true;
                                    }
                                }
                                else
                                    currentType = null;
                                break;
                            }
                        default: // Cast
                            currentType = returnType;

                            if (i == processesProperty.arraySize - 1)
                            {
                                lastMember = null;
                                return returnType != null;
                            }
                            break;
                    }
                }
            }

            lastMode = default(ProcessMode);
            lastMember = null;
            return false;
        }

        private static T[] AddToArray<T>(T[] methods, T method)
        {
            T[] newMethods = new T[methods != null ? methods.Length + 1 : 1];
            methods.CopyTo(newMethods, 0);
            newMethods[newMethods.Length - 1] = method;
            return newMethods;
        }
    }
}
