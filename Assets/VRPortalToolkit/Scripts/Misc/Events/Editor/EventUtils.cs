using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Events
{
    public static class EventUtils
    {
        public static string GetTypeName(System.Type type)
        {
            if (type == null) return "";

            if (type.IsEnum)
                return type.Name;

            switch (System.Type.GetTypeCode(type))
            {
                case System.TypeCode.Boolean:
                    return "bool";
                case System.TypeCode.Byte:
                    return "byte";
                case System.TypeCode.Char:
                    return "char";
                case System.TypeCode.Decimal:
                    return "decimal";
                case System.TypeCode.Single:
                    return "float";
                case System.TypeCode.Double:
                    return "double";
                case System.TypeCode.Int16:
                    return "short";
                case System.TypeCode.Int32:
                    return "int";
                case System.TypeCode.Int64:
                    return "long";
                case System.TypeCode.SByte:
                    return "sbyte";
                case System.TypeCode.String:
                    return "string";
                case System.TypeCode.UInt16:
                    return "ushort";
                case System.TypeCode.UInt32:
                    return "ulong";
                case System.TypeCode.UInt64:
                    return "ulong";
            }

            if (type.IsGenericType)
            {
                string arguments = "";

                System.Type[] genericArguments = type.GenericTypeArguments;

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    if (i == 0)
                        arguments += GetTypeName(genericArguments[i]);
                    else
                        arguments += $", {GetTypeName(genericArguments[i])}";
                }

                return $"{type.Name.Split('`')[0]}<{arguments}>";
            }

            if (type.Equals(typeof(object)))
                return "object";

            return type.Name;
        }

        public static void Replace(SerializedProperty unityEvent, SerializedProperty serialEvent)
        {
            if (serialEvent == null) return;

            SerializedProperty listeners = serialEvent.FindPropertyRelative("_serializableListeners");
            listeners.arraySize = 0;

            CopyTo(unityEvent, serialEvent);
        }

        public static void CopyTo(SerializedProperty unityEvent, SerializedProperty serialEvent)
        {
            CopyTo(unityEvent, serialEvent, 0);
        }

        public static void CopyTo(SerializedProperty unityEvent, SerializedProperty serialEvent, int destinationIndex)
        {
            if (unityEvent == null) return;

            SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");

            CopyTo(unityEvent, 0, serialEvent, destinationIndex, calls.arraySize);
        }

        // TODO: Think I know how to implement this now
        // TODO: Doesnt use call state
        // Converts from UnityEvent
        public static void CopyTo(SerializedProperty unityEvent, int sourceIndex, SerializedProperty serialEvent, int destinationIndex, int count)
        {
            if (unityEvent == null || serialEvent == null) return;

            SerializedProperty serialListeners = serialEvent.FindPropertyRelative("_serializableListeners");
            SerializedProperty unityCalls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");

            //UnityEventBase asUnityEvent = unityEvent.GetParentObject() as UnityEventBase;
            SerializableEventBase asSerialEvent = unityEvent.GetParentObject() as SerializableEventBase;

            if (sourceIndex >= unityEvent.arraySize) return;

            if (destinationIndex >= unityEvent.arraySize)
                serialEvent.arraySize = destinationIndex + 1;

            int last = Mathf.Min(sourceIndex + count);

            for (int i = sourceIndex, j = destinationIndex; i < last; i++, j++)
            {
                serialListeners.InsertArrayElementAtIndex(j);

                SerializedProperty call = unityCalls.GetArrayElementAtIndex(i),
                    listener = serialListeners.GetArrayElementAtIndex(j);

                listener.FindPropertyRelative("_targetObject").objectReferenceValue = call.FindPropertyRelative("m_Target").objectReferenceValue;

                SerializedProperty processes = listener.FindPropertyRelative("_targetProcesses");
                processes.arraySize = 1;

                SerializedProperty process = processes.GetArrayElementAtIndex(0);
                process.FindPropertyRelative("_name").stringValue = call.FindPropertyRelative("m_MethodName").stringValue;

                SerializedProperty parameters = listener.FindPropertyRelative("_targetParameters"), parameter;
                parameters.arraySize = 0;

                SerializedProperty objectValues = listener.FindPropertyRelative("_data._objectValues"),
                    stringValues = listener.FindPropertyRelative("_data._stringValues"),
                    boolValues = listener.FindPropertyRelative("_data._boolValues"),
                    intValues = listener.FindPropertyRelative("_data._intValues"),
                    floatValues = listener.FindPropertyRelative("_data._floatValues");

                objectValues.arraySize = 0;
                stringValues.arraySize = 0;
                boolValues.arraySize = 0;
                intValues.arraySize = 0;
                floatValues.arraySize = 0;

                switch (call.FindPropertyRelative("m_Mode").longValue)
                {
                    case 1: // Void
                        parameters.arraySize = 0;
                        break;

                    case 2: // Object
                        objectValues.arraySize = 1;
                        objectValues.GetArrayElementAtIndex(0).objectReferenceValue = call.FindPropertyRelative("m_Arguments.m_ObjectArgument").objectReferenceValue;

                        parameters.arraySize = 1;
                        parameter = parameters.GetArrayElementAtIndex(0);
                        parameter.FindPropertyRelative("_type").stringValue = call.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue;
                        parameter.FindPropertyRelative("_mode").longValue = (long)ParameterMode.Object;
                        parameter.FindPropertyRelative("_processes").arraySize = 0;
                        break;

                    case 3: // Int
                        intValues.arraySize = 1;
                        intValues.GetArrayElementAtIndex(0).intValue = call.FindPropertyRelative("m_Arguments.m_IntArgument").intValue;

                        parameters.arraySize = 1;
                        parameter = parameters.GetArrayElementAtIndex(0);
                        parameter.FindPropertyRelative("_type").stringValue = typeof(int).AssemblyQualifiedName;
                        parameter.FindPropertyRelative("_mode").longValue = (long)ParameterMode.Int;
                        parameter.FindPropertyRelative("_processes").arraySize = 0;
                        break;

                    case 4: // Float
                        floatValues.arraySize = 1;
                        floatValues.GetArrayElementAtIndex(0).floatValue = call.FindPropertyRelative("m_Arguments.m_FloatArgument").floatValue;

                        parameters.arraySize = 1;
                        parameter = parameters.GetArrayElementAtIndex(0);
                        parameter.FindPropertyRelative("_type").stringValue = typeof(float).AssemblyQualifiedName;
                        parameter.FindPropertyRelative("_mode").longValue = (long)ParameterMode.Float;
                        parameter.FindPropertyRelative("_processes").arraySize = 0;
                        break;

                    case 5: // String
                        stringValues.arraySize = 1;
                        stringValues.GetArrayElementAtIndex(0).stringValue = call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue;

                        parameters.arraySize = 1;
                        parameter = parameters.GetArrayElementAtIndex(0);
                        parameter.FindPropertyRelative("_type").stringValue = typeof(string).AssemblyQualifiedName;
                        parameter.FindPropertyRelative("_mode").longValue = (long)ParameterMode.String;
                        parameter.FindPropertyRelative("_processes").arraySize = 0;
                        break;

                    case 6: // Bool
                        boolValues.arraySize = 1;
                        boolValues.GetArrayElementAtIndex(0).boolValue = call.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue;

                        parameters.arraySize = 1;
                        parameter = parameters.GetArrayElementAtIndex(0);
                        parameter.FindPropertyRelative("_type").stringValue = typeof(bool).AssemblyQualifiedName;
                        parameter.FindPropertyRelative("_mode").longValue = (long)ParameterMode.Bool;
                        parameter.FindPropertyRelative("_processes").arraySize = 0;
                        break;

                    default: // Event Defined
                        parameters.arraySize = asSerialEvent.parameterCount;

                        for (int k = 0; k < asSerialEvent.parameterCount; k++)
                        {
                            ParameterMode mode;

                            if (k == 0) mode = ParameterMode.Args1;
                            else if (k == 1) mode = ParameterMode.Args2;
                            else if (k == 2) mode = ParameterMode.Args3;
                            else mode = ParameterMode.Args4;

                            parameter = parameters.GetArrayElementAtIndex(k);
                            parameter.FindPropertyRelative("_type").stringValue = asSerialEvent.GetParameterType(k).AssemblyQualifiedName;
                            parameter.FindPropertyRelative("_mode").longValue = (long)mode;
                            parameter.FindPropertyRelative("_processes").arraySize = 0;
                        }

                        break;
                }
                //target.FindPropertyRelative("_targetObject").objectReferenceValue = call.FindPropertyRelative("m_Target").objectReferenceValue;
            }

            serialListeners.serializedObject.ApplyModifiedProperties();
            Validate(serialEvent);
        }

        private static MethodInfo _validateMethod;
        private static object[] _emptyArgs = new object[0];

        public static void Validate(SerializedProperty property)
        {
            if (_validateMethod == null) _validateMethod = typeof(SerializableEventBase).GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance);

            if (_validateMethod != null)
            {
                foreach (Object obj in property.serializedObject.targetObjects)
                {
                    if (property.TryGetObject(obj, out SerializableEventBase serializedEvent))
                        _validateMethod.Invoke(serializedEvent, _emptyArgs);
                }
            }
        }
    }
}
