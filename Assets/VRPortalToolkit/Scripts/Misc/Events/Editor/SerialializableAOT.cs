using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
using Misc.EditorHelpers;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;

// TODO: Maybe force other methods to exists?

namespace Misc.Events
{
    /**
     * This class is used to add support for ahead-of-time compiling for serializable events.
     * When building to specific platforms, it checks every serializable event in the project
     * and creates a script to force the compiler to add the needed generic classes.
     */
    public class SerialializableAOT : IPreprocessBuildWithReport
    {
        private const string FILE = "Assets/TempSerializableAOT.cs";
        private static bool enabled = false;

        public int callbackOrder => 0;

        // TODO: Would be better if we could be certain if aot was supported
        public void OnPreprocessBuild(BuildReport report)
        {
            bool isIL2CPP = PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup) == ScriptingImplementation.IL2CPP;

            if (PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup) == ApiCompatibilityLevel.NET_4_6)
            {
                // https://docs.unity3d.com/Manual/ScriptingRestrictions.html
                switch (report.summary.platformGroup)
                {
                    case BuildTargetGroup.Standalone:
                    case BuildTargetGroup.iOS:
                    case BuildTargetGroup.Android:
                    case BuildTargetGroup.WebGL:
                        if (isIL2CPP) OnPreBuild();
                        break;
                }
            }
        }

        [PostProcessBuild(0)]
        public static void OnPostBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (enabled)
            {
                if (File.Exists(FILE))
                    File.Delete(FILE);

                enabled = false;
            }
        }

        public static void OnPreBuild()
        {
            HashSet<System.Type> types = new HashSet<System.Type>();
            HashSet<MethodInfo> methodInfos = new HashSet<MethodInfo>();
            HashSet<System.Type> ignore = new HashSet<System.Type>();

            // Get Prefabs
            string[] guids = AssetDatabase.FindAssets($"t:GameObject");

            foreach (string guid in guids)
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid));

                foreach (Object asset in assets)
                    if (asset is GameObject gameObject)
                        foreach (MonoBehaviour behaviour in gameObject.GetComponentsInChildren<MonoBehaviour>(true))
                            AddTypes(behaviour, types, methodInfos, ignore);
            }

            // Get Scriptable Objects
            guids = AssetDatabase.FindAssets($"t:ScriptableObject");

            foreach (string guid in guids)
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid));

                foreach (Object asset in assets)
                    if (asset is ScriptableObject scriptableObject)
                        AddTypes(scriptableObject, types, methodInfos, ignore);
            }

            // Get objects in scenes
            SceneSetup[] sceneSetup = EditorSceneManager.GetSceneManagerSetup();

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);

                    Scene actualScene = SceneManager.GetActiveScene();

                    if (actualScene != null && actualScene.IsValid())
                    {
                        foreach (MonoBehaviour behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
                            AddTypes(behaviour, types, methodInfos, ignore);
                    }
                }
            }

            EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);

            if (types.Count == 0) return;

            using (StreamWriter file = File.CreateText(FILE))
            {
                enabled = true;

                file.WriteLine("using System;\n");

                file.Write("namespace ");
                file.WriteLine(typeof(SerialializableAOT).Namespace);
                file.WriteLine("{");

                file.WriteLine("\tpublic class TempSerializableAOT\n\t{");
                file.WriteLine("\t\tpublic void UsedOnlyForAOTCodeGeneration()\n\t\t{");

                //SerializableEventBase[] s = new SerializableEventBase[0];
                //foreach (SerializableEventBase @event in s)
                foreach (System.Type type in types)
                {
                    file.Write("\t\t\tnew ");
                    file.Write(GetName(type, false));
                    file.WriteLine("();");
                }

                foreach (MethodInfo method in methodInfos)
                {
                    file.Write("\t\t\tdefault(");
                    file.Write(GetName(method.DeclaringType));
                    file.Write(").");
                    file.Write(GetMethod(method));
                    file.WriteLine(";");
                }

                file.WriteLine("\n\t\t\tthrow new InvalidOperationException(\"This method is used for AOT code generation only. Do not call it at runtime.\");");

                // Close method, class, and namespace
                file.WriteLine("\t\t}\n\t}\n}\n");
            }

            AssetDatabase.Refresh();
        }

        // Hmm, The ignore might prevent a list of
        public static void AddTypes(Object obj, HashSet<System.Type> types, HashSet<MethodInfo> methodInfos, HashSet<System.Type> ignore)
        {
            if (!obj) return;

            System.Type type = obj.GetType();

            if (ignore.Contains(type)) return;

            SerializedObject serializedObject = new SerializedObject(obj);
            SerializedProperty property = serializedObject.GetIterator();

            bool checkInChildren = true, shouldIgnoreNextTime = true;

            while (property.Next(checkInChildren))
            {
                checkInChildren = true;

                if (property.hasChildren)
                {
                    SerializedProperty listeners = property.FindPropertyRelative("_serializableListeners");

                    if (listeners != null)
                    {
                        object asObject = property.GetObject();

                        if (asObject != null && typeof(SerializableEventBase).IsAssignableFrom(asObject.GetType()))
                        {
                            shouldIgnoreNextTime = false;
                            checkInChildren = false;

                            for (int i = 0; i < listeners.arraySize; i++)
                            {
                                SerializedProperty listener = listeners.GetArrayElementAtIndex(i);
                                SerializedProperty parameters = listener.FindPropertyRelative("_targetParameters");

                                System.Type[] parameterTypes = new System.Type[parameters.arraySize];

                                for (int j = 0; j < parameterTypes.Length; j++)
                                {
                                    SerializedProperty parameter = parameters.GetArrayElementAtIndex(j);

                                    System.Type parameterType = System.Type.GetType(parameter.FindPropertyRelative("_type").stringValue, false);
                                    parameterTypes[j] = parameterType;

                                    AddParameter(types, methodInfos, asObject as SerializableEventBase, parameterType, (ParameterMode)parameter.FindPropertyRelative("_mode").longValue, parameter.FindPropertyRelative("_processes"));
                                }

                                AddProcesses(types, methodInfos, listener.FindPropertyRelative("_targetObject").objectReferenceValue?.GetType(), listener.FindPropertyRelative("_targetProcesses"), parameterTypes);
                            }
                        }
                    }
                }
            }

            if (shouldIgnoreNextTime) ignore.Add(type);
        }

        public static string GetName(System.Type type, bool fullName = true)
        {
            string name = fullName ? type.FullName : type.Name;

            if (type.IsGenericType)
            {
                string genericArguments = type.GetGenericArguments().Select(x => GetName(x)).Aggregate((i, j) => $"{i}, {j}");

                return $"{name.Substring(0, name.IndexOf("`"))}<{genericArguments}>";
            }

            return name;
        }

        public static string GetMethod(MethodInfo method)
        {
            string name = method.Name;

            if (method.IsGenericMethod)
            {
                string genericArguments = method.GetGenericArguments().Select(x => GetName(x)).Aggregate((i, j) => $"{i}, {j}");

                name = $"{name}<{genericArguments}>";
            }

            ParameterInfo[] parameterInfos = method.GetParameters();

            if (parameterInfos.Length > 0)
            {
                string parameters = parameterInfos.Select(x => $"default({GetName(x.GetType())})").Aggregate((i, j) => $"{i}, {j}");

                return $"{name}({parameters})";
            }

            return $"{name}()";
        }

        private static void AddParameter(HashSet<System.Type> types, HashSet<MethodInfo> methodInfos, SerializableEventBase @event, System.Type parameterType, ParameterMode mode, SerializedProperty processes)
        {
            switch (mode)
            {
                case ParameterMode.Args1:
                        AddProcesses(types, methodInfos, @event.GetParameterType(0), processes, System.Type.EmptyTypes, parameterType);
                    return;

                case ParameterMode.Args2:
                        AddProcesses(types, methodInfos, @event.GetParameterType(1), processes, System.Type.EmptyTypes, parameterType);
                    return;

                case ParameterMode.Args3:
                        AddProcesses(types, methodInfos, @event.GetParameterType(2), processes, System.Type.EmptyTypes, parameterType);
                    return;

                case ParameterMode.Args4:
                        AddProcesses(types, methodInfos, @event.GetParameterType(3), processes, System.Type.EmptyTypes, parameterType);
                    return;
            }
        }

        private static void AddProcesses(HashSet<System.Type> types, HashSet<MethodInfo> methodInfos, System.Type sourceType, SerializedProperty processes, System.Type[] parameterTypes, System.Type targetType = null)
        {
            bool isSet = targetType == null || targetType.Equals(typeof(void));

            if (processes != null && processes.arraySize >= 0)
            {
                MethodInfo cachedMethod;
                PropertyInfo cachedProperty;
                FieldInfo cachedField;

                for (int i = 0; i < processes.arraySize; i++)
                {
                    bool isLast = i == processes.arraySize - 1;
                    SerializedProperty process = processes.GetArrayElementAtIndex(i);

                    string name = process.FindPropertyRelative("_name").stringValue;

                    switch ((ProcessMode)process.FindPropertyRelative("_mode").longValue)
                    {
                        case ProcessMode.Field:
                            if (EventUtility.TryGetField(sourceType, name, out cachedField))
                            {
                                // TODO: Ensure generic fields?
                                /*if (isSet)
                                    cached = new CachedSetField(cachedField);
                                else
                                    cached = new CachedGetField(cachedField);*/

                                sourceType = cachedField.FieldType;
                            }
                            break;

                        case ProcessMode.Property:
                            // TODO: Ensure generic properties?
                            if (EventUtility.TryGetProperty(sourceType, name, out cachedProperty))
                            {
                                types.Add((isSet ? typeof(CachedSetProperty<>) : typeof(CachedGetProperty<>)).MakeGenericType(cachedProperty.PropertyType));

                                sourceType = cachedProperty.PropertyType;
                            }
                            break;

                        case ProcessMode.Method:
                            if (EventUtility.TryGetMethod(sourceType, name, isLast ? parameterTypes : System.Type.EmptyTypes, out cachedMethod))
                            {
                                // TODO: Ensure generic methods?
                                // Make generic version if necessary
                                if (cachedMethod.IsGenericMethodDefinition)
                                {
                                    cachedMethod = cachedMethod.MakeGenericMethod(targetType);
                                    methodInfos.Add(cachedMethod);
                                }

                                if (TryGetCachedMethodType(cachedMethod, parameterTypes, out System.Type type))
                                    types.Add(type);

                                sourceType = cachedMethod.ReturnType;
                            }
                            break;

                        case ProcessMode.MethodWithType:
                            EventUtility.SingleType[0] = typeof(System.Type);

                            if (EventUtility.TryGetMethod(sourceType, name, EventUtility.SingleType, out cachedMethod))
                            {
                                // TODO: Ensure generic methods?
                                // Make generic version if necessary
                                if (cachedMethod.IsGenericMethod)
                                {
                                    cachedMethod = cachedMethod.MakeGenericMethod(targetType);
                                    methodInfos.Add(cachedMethod);
                                }

                                if (TryGetCachedMethodType(cachedMethod, parameterTypes, out System.Type type))
                                    types.Add(type);

                                sourceType = cachedMethod.ReturnType;
                            }
                            break;

                        case ProcessMode.Cast:
                            System.Type newType;

                            if (string.IsNullOrEmpty(name))
                                newType = targetType;
                            else
                                newType = System.Type.GetType(name, false);

                            if (newType != null)
                            {
                                if (EventUtility.TryGetCastMethod(sourceType, newType, out MemberInfo _))
                                    types.Add(typeof(CachedCast<,>).MakeGenericType(new System.Type[] { sourceType, newType }));

                                sourceType = newType;
                            }

                            break;

                        case ProcessMode.Parse:
                            EventUtility.SingleType[0] = sourceType;

                            if (EventUtility.TryGetMethod(sourceType, process.name, EventUtility.SingleType, out cachedMethod))
                            {
                                types.Add(typeof(CachedParse<,>).MakeGenericType(sourceType, cachedMethod.ReturnType));

                                sourceType = cachedMethod.ReturnType;
                            }
                            break;
                    }
                }
            }
        }

        private static bool TryGetCachedMethodType(MethodInfo method, System.Type[] parameterTypes, out System.Type type)
        {
            System.Type returnType = method.ReturnType;
            type = null;

            if (returnType == null || returnType.Equals(typeof(void)))
            {
                if (parameterTypes == null || parameterTypes.Length == 0)
                    return false;
                else
                {
                    if (parameterTypes.Length == 1)
                        type = typeof(CachedAction<>);
                    else if (parameterTypes.Length == 2)
                        type = typeof(CachedAction<,>);
                    else if (parameterTypes.Length == 3)
                        type = typeof(CachedAction<,,>);
                    else if (parameterTypes.Length == 4)
                        type = typeof(CachedAction<,,,>);
                    else
                        return false;

                    type = type.MakeGenericType(parameterTypes);
                }
            }
            else
            {
                if (parameterTypes == null || parameterTypes.Length == 0)
                    type = typeof(CachedFunction<>);
                else if (parameterTypes.Length == 1)
                    type = typeof(CachedFunction<,>);
                else if (parameterTypes.Length == 2)
                    type = typeof(CachedFunction<,,>);
                else if (parameterTypes.Length == 3)
                    type = typeof(CachedFunction<,,,>);
                else if (parameterTypes.Length == 4)
                    type = typeof(CachedFunction<,,,,>);
                else
                    return false;

                System.Type[] typeArguments = new System.Type[parameterTypes != null ? parameterTypes.Length + 1 : 1];

                for (int i = 0; i < typeArguments.Length - 1; i++)
                    typeArguments[i] = parameterTypes[i];

                typeArguments[typeArguments.Length - 1] = returnType;

                type = type.MakeGenericType(typeArguments);

            }

            return type != null;
        }
    }
}
