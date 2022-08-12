using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Events
{
    // TODO: Would be really cool if you could add custom listeners
    // You could feed custom data, which would be cool.

    [Serializable]
    public abstract class SerializableEventBase
    {

        protected static object[] EmptyObjects = new object[0];
        protected static object[] SingleObject = new object[1];

        [SerializeField] private SerializableListener[] _serializableListeners;

        private CachedListener[] _cachedListeners;

        private bool _serializableIsDirty = true;
        private bool isDirty = true;

        private List<ActionListenerBase> _cachedActionListeners;
        private List<ActionListenerBase> actionListeners;

        public abstract Type GetParameterType(int index);

        public abstract int parameterCount { get; }

        protected void AddListener(ActionListenerBase listener)
        {
            if (listener != null)
            {
                isDirty = true;

                if (actionListeners == null) actionListeners = new List<ActionListenerBase>();
                actionListeners.Add(listener);
            }
        }

        protected void RemoveListener(Delegate @delegate, object[] args)
        {
            int index = actionListeners.FindIndex(i => i.Find(@delegate, args));

            if (index >= 0)
            {
                isDirty = true;
                actionListeners.RemoveAt(index);
            }
        }

        public void AddListener(UnityAction action)
        {
            if (action != null) AddListener(new ActionListener(action));
        }

        public void RemoveListener(UnityAction action)
        {
            RemoveListener(action, null);
        }

        public void AddListener<T>(UnityAction<T> action, T value)
        {
            if (action != null) AddListener(new ActionListener<T>(action, value));
        }

        public void RemoveListener<T>(UnityAction<T> action, T value)
        {
            RemoveListener(action, new object[] { value });
        }

        public void AddListener<T0, T1>(UnityAction<T0, T1> action, T0 value0, T1 value1)
        {
            if (action != null) AddListener(new ActionListener<T0, T1>(action, value0, value1));
        }

        public void RemoveListener<T0, T1>(UnityAction<T0, T1> action, T0 value0, T1 value1)
        {
            RemoveListener(action, new object[] { value0, value1 });
        }

        public void AddListener<T0, T1, T2>(UnityAction<T0, T1, T2> action, T0 value0, T1 value1, T2 value2)
        {
            if (action != null) AddListener(new ActionListener<T0, T1, T2>(action, value0, value1, value2));
        }

        public void RemoveListener<T0, T1, T2>(UnityAction<T0, T1, T2> action, T0 value0, T1 value1, T2 value2)
        {
            RemoveListener(action, new object[] { value0, value1, value2 });
        }

        public void AddListener<T0, T1, T2, T3>(UnityAction<T0, T1, T2, T3> action, T0 value0, T1 value1, T2 value2, T3 value3)
        {
            if (action != null) AddListener(new ActionListener<T0, T1, T2, T3>(action, value0, value1, value2, value3));
        }

        public void RemoveListener<T0, T1, T2, T3>(UnityAction<T0, T1, T2, T3> action, T0 value0, T1 value1, T2 value2, T3 value3)
        {
            RemoveListener(action, new object[] { value0, value1, value2, value3 });
        }

        public void RemoveAllListeners()
        {
            isDirty = true;
            actionListeners = null;
        }

        protected virtual void OnValidate()
        {
            _serializableIsDirty = true;
        }

        #region Serializable Events

        protected void Invoke(params object[] args)
        {
            if (_serializableIsDirty)
            {
                CacheSerializable();
                _serializableIsDirty = false;
            }

            if (_cachedListeners != null)
            {
                try
                {
                    for (int i = 0; i < _cachedListeners.Length; i++)
                    {
                        CachedListener listener = _cachedListeners[i];
                        if (listener != null) listener.Invoke(args);
                    }
                }
                catch (Exception ex) { Debug.LogException(ex); }
            }

            if (actionListeners != null)
            {
                if (isDirty)
                {
                    if (_cachedActionListeners == null)
                        _cachedActionListeners = new List<ActionListenerBase>(actionListeners.Capacity);
                    else
                        _cachedActionListeners.Clear();

                    _cachedActionListeners.AddRange(actionListeners);
                    isDirty = false;
                }

                for (int i = 0; i < _cachedActionListeners.Count; i++)
                    _cachedActionListeners[i].Invoke(args);
            }
        }

        private void CacheSerializable()
        {
            _cachedListeners = new CachedListener[_serializableListeners.Length];

            for (int i = 0; i < _cachedListeners.Length; i++)
            {
                SerializableListener serializableListener = _serializableListeners[i];

                int boolIndex = 0, intIndex = 0, floatIndex = 0, stringIndex = 0, objectIndex = 0;

                if (serializableListener != null && serializableListener.targetObject != null)
                {
                    CachedParameter[] cachedParameters = new CachedParameter[serializableListener.targetParameters != null ? serializableListener.targetParameters.Length : 0];

                    System.Type[] parameterTypes = new System.Type[cachedParameters.Length];

                    for (int j = 0; j < cachedParameters.Length; j++)
                    {
                        SerializableParameter serializableParameter = serializableListener.targetParameters[j];

                        if (serializableParameter != null)
                        {
                            System.Type parameterType = System.Type.GetType(serializableParameter.type, false);
                            parameterTypes[j] = parameterType;
                            cachedParameters[j] = GetCachedParameter(serializableListener, parameterType, serializableParameter.mode, serializableParameter.processes,
                                ref boolIndex, ref intIndex, ref floatIndex, ref stringIndex, ref objectIndex);
                        }
                    }

                    CachedProcess beginProcess = GetProcesses(serializableListener.targetObject.GetType(), serializableListener.targetProcesses, parameterTypes, null);

                    _cachedListeners[i] = new CachedListener(serializableListener.targetObject, beginProcess, cachedParameters);
                }
            }
        }

        private CachedParameter GetCachedParameter(SerializableListener serializableListener, System.Type parameterType, ParameterMode mode, SerializableProcess[] processes, ref int boolIndex, ref int intIndex, ref int floatIndex, ref int stringIndex, ref int objectIndex)
        {
            SerializableListenerData data = serializableListener.data;

            switch (mode)
            {
                case ParameterMode.Args1:
                    return new CachedParameter(0,
                        GetProcesses(GetParameterType(0), processes, Type.EmptyTypes, parameterType));

                case ParameterMode.Args2:
                    return new CachedParameter(1,
                        GetProcesses(GetParameterType(1), processes, Type.EmptyTypes, parameterType));

                case ParameterMode.Args3:
                    return new CachedParameter(2,
                        GetProcesses(GetParameterType(2), processes, Type.EmptyTypes, parameterType));

                case ParameterMode.Args4:
                    return new CachedParameter(3,
                        GetProcesses(GetParameterType(3), processes, Type.EmptyTypes, parameterType));

                case ParameterMode.Bool:
                    return new CachedParameter(GetOrDefault(data.boolValues, boolIndex++));

                case ParameterMode.Int:
                    int intValue = GetOrDefault(data.intValues, intIndex++);

                    if (parameterType.Equals(typeof(LayerMask)))
                        return new CachedParameter((LayerMask)intValue);

                    return new CachedParameter(intValue);

                case ParameterMode.Vector2Int:
                    return new CachedParameter(new Vector2Int(
                        GetOrDefault(data.intValues, intIndex++),
                        GetOrDefault(data.intValues, intIndex++)));

                case ParameterMode.Vector3Int:
                    return new CachedParameter(new Vector3Int(
                        GetOrDefault(data.intValues, intIndex++),
                        GetOrDefault(data.intValues, intIndex++),
                        GetOrDefault(data.intValues, intIndex++)));

                case ParameterMode.Float:
                    return new CachedParameter(GetOrDefault(data.floatValues, floatIndex++));

                case ParameterMode.Vector2:
                    return new CachedParameter(new Vector2(
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++)));

                case ParameterMode.Vector3:
                    return new CachedParameter(new Vector3(
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++)));

                case ParameterMode.Vector4:
                    return new CachedParameter(new Vector4(
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++)));

                case ParameterMode.String:
                    return new CachedParameter(GetOrDefault(data.stringValues, stringIndex++));

                case ParameterMode.Char:
                    string stringValue = GetOrDefault(data.stringValues, stringIndex++);

                    if (string.IsNullOrEmpty(stringValue))
                        return new CachedParameter(default(char));

                    return new CachedParameter(stringValue[0]);

                case ParameterMode.Object:
                    UnityEngine.Object objectValue = GetOrDefault(data.objectValues, objectIndex++);

                    return new CachedParameter(objectValue,
                        GetProcesses(objectValue.GetType(), processes, System.Type.EmptyTypes, parameterType));

                case ParameterMode.Rect:
                    return new CachedParameter(new Rect(
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++)));

                case ParameterMode.RectInt:
                    return new CachedParameter(new RectInt(
                        GetOrDefault(data.intValues, intIndex++),
                        GetOrDefault(data.intValues, intIndex++),
                        GetOrDefault(data.intValues, intIndex++),
                        GetOrDefault(data.intValues, intIndex++)));

                case ParameterMode.Bounds:
                    return new CachedParameter(new Bounds(
                        new Vector3(
                            GetOrDefault(data.floatValues, floatIndex++),
                            GetOrDefault(data.floatValues, floatIndex++),
                            GetOrDefault(data.floatValues, floatIndex++)
                        ),
                        new Vector3(
                            GetOrDefault(data.floatValues, floatIndex++),
                            GetOrDefault(data.floatValues, floatIndex++),
                            GetOrDefault(data.floatValues, floatIndex++)
                        )));

                case ParameterMode.BoundsInt:
                    return new CachedParameter(new Bounds(
                        new Vector3(
                            GetOrDefault(data.intValues, intIndex++),
                            GetOrDefault(data.intValues, intIndex++),
                            GetOrDefault(data.intValues, intIndex++)
                        ),
                        new Vector3(
                            GetOrDefault(data.intValues, intIndex++),
                            GetOrDefault(data.intValues, intIndex++),
                            GetOrDefault(data.intValues, intIndex++)
                        )));

                case ParameterMode.Color:
                    return new CachedParameter(new Color(
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++)
                    ));

                case ParameterMode.Gradient:
                    Gradient gradient = new Gradient();

                    gradient.mode = (GradientMode)GetOrDefault(data.intValues, intIndex++);

                    GradientColorKey[] colorKeys = new GradientColorKey[GetOrDefault(data.intValues, intIndex++)];

                    for (int i = 0; i < colorKeys.Length; i++)
                        colorKeys[i] = new GradientColorKey(
                            new Color(
                                GetOrDefault(data.floatValues, floatIndex++),
                                GetOrDefault(data.floatValues, floatIndex++),
                                GetOrDefault(data.floatValues, floatIndex++),
                                GetOrDefault(data.floatValues, floatIndex++)
                            ),
                            GetOrDefault(data.floatValues, floatIndex++)
                        );

                    GradientAlphaKey[] alphaKeys = new GradientAlphaKey[GetOrDefault(data.intValues, intIndex++)];

                    for (int i = 0; i < alphaKeys.Length; i++)
                        alphaKeys[i] = new GradientAlphaKey(
                            GetOrDefault(data.floatValues, floatIndex++),
                            GetOrDefault(data.floatValues, floatIndex++)
                        );

                    gradient.SetKeys(colorKeys, alphaKeys);

                    return new CachedParameter(gradient);

                case ParameterMode.Curve:
                    AnimationCurve curve = new AnimationCurve();

                    curve.preWrapMode = (WrapMode)GetOrDefault(data.intValues, intIndex++);
                    curve.postWrapMode = (WrapMode)GetOrDefault(data.intValues, intIndex++);

                    Keyframe[] keys = new Keyframe[GetOrDefault(data.intValues, intIndex++)];

                    for (int i = 0; i < keys.Length; i++)
                        keys[i] = new Keyframe(
                            GetOrDefault(data.floatValues, floatIndex++),
                            GetOrDefault(data.floatValues, floatIndex++),
                            GetOrDefault(data.floatValues, floatIndex++),
                            GetOrDefault(data.floatValues, floatIndex++),
                            GetOrDefault(data.floatValues, floatIndex++),
                            GetOrDefault(data.floatValues, floatIndex++)
                        );

                    curve.keys = keys;

                    return new CachedParameter(curve);
                case ParameterMode.Quaternion:
                    return new CachedParameter(new Quaternion(
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++),
                        GetOrDefault(data.floatValues, floatIndex++)));
            }

            // Default: null or default
            if (parameterType.IsValueType)
                return new CachedParameter(Activator.CreateInstance(parameterType));

            return new CachedParameter(null);
        }

        private T GetOrDefault<T>(T[] array, int index)
        {
            if (array != null && index >= 0 && index <= array.Length)
                return array[index];

            return default(T);
        }

        private CachedProcess GetProcesses(Type sourceType, SerializableProcess[] processes, Type[] parameterTypes, Type targetType = null)
        {
            bool isSet = targetType == null || targetType.Equals(typeof(void));

            if (processes != null && processes.Length >= 0)
            {
                CachedProcess beginProcess = null, previousProcess = null, currentProcess = null;

                MethodInfo cachedMethod;
                PropertyInfo cachedProperty;
                FieldInfo cachedField;

                for (int i = 0; i < processes.Length; i++)
                {
                    previousProcess = currentProcess;

                    bool isLast = i == processes.Length - 1;
                    SerializableProcess process = processes[i];

                    switch (process.mode)
                    {
                        case ProcessMode.Field:
                            if (EventUtility.TryGetField(sourceType, process.name, out cachedField))
                            {
                                CachedField cached;

                                if (isSet)
                                    cached = new CachedSetField(cachedField);
                                else
                                    cached = new CachedGetField(cachedField);

                                if (!isLast || !isSet) cached.overrideArgs = EmptyObjects;

                                currentProcess = cached;
                                sourceType = cachedField.FieldType;
                            }
                            break;

                        case ProcessMode.Property:
                            if (EventUtility.TryGetProperty(sourceType, process.name, out cachedProperty))
                            {
                                CachedProperty cached = (CachedProperty)System.Activator.CreateInstance((isSet ? typeof(CachedSetProperty<>) : typeof(CachedGetProperty<>)).MakeGenericType(cachedProperty.PropertyType));
                                cached.property = cachedProperty;

                                if (!isLast || !isSet) cached.overrideArgs = EmptyObjects;

                                currentProcess = cached;
                                sourceType = cachedProperty.PropertyType;
                            }
                            break;

                        case ProcessMode.Method:
                            if (EventUtility.TryGetMethod(sourceType, process.name, isLast ? parameterTypes : System.Type.EmptyTypes, out cachedMethod))
                            {
                                // Make generic version if necessary
                                if (cachedMethod.IsGenericMethodDefinition)
                                    cachedMethod = cachedMethod.MakeGenericMethod(targetType);

                                currentProcess = GetCachedMethod(cachedMethod, parameterTypes, isLast ? null : EmptyObjects);
                                sourceType = cachedMethod.ReturnType;
                            }
                            break;

                        case ProcessMode.MethodWithType:
                            EventUtility.SingleType[0] = typeof(System.Type);

                            if (EventUtility.TryGetMethod(sourceType, process.name, EventUtility.SingleType, out cachedMethod))
                            {
                                // Make generic version if necessary
                                if (cachedMethod.IsGenericMethod) cachedMethod = cachedMethod.MakeGenericMethod(targetType);

                                currentProcess = GetCachedMethod(cachedMethod, parameterTypes, new object[] { targetType });


                                sourceType = cachedMethod.ReturnType;
                            }
                            break;

                        case ProcessMode.Cast:
                            System.Type newType;

                            if (string.IsNullOrEmpty(process.name))
                                newType = targetType;
                            else
                                newType = System.Type.GetType(process.name, false);

                            if (newType != null)
                            {
                                if (EventUtility.TryGetCastMethod(sourceType, newType, out MemberInfo methodInfo))
                                    currentProcess = (CachedProcess)System.Activator.CreateInstance(typeof(CachedCast<,>).MakeGenericType(new System.Type[] { sourceType, newType }), methodInfo);
                                else
                                    currentProcess = new CachedCast(newType);

                                sourceType = newType;
                            }

                            break;

                        case ProcessMode.Parse:
                            EventUtility.SingleType[0] = sourceType;

                            if (EventUtility.TryGetMethod(sourceType, process.name, EventUtility.SingleType, out cachedMethod))
                            {
                                CachedMethod cached = (CachedMethod)System.Activator.CreateInstance(typeof(CachedParse<,>).MakeGenericType(sourceType, cachedMethod.ReturnType));
                                cached.method = cachedMethod;

                                currentProcess = cached;
                                sourceType = cachedMethod.ReturnType;
                            }
                            break;
                    }

                    if (beginProcess == null) beginProcess = currentProcess;

                    if (previousProcess != null) previousProcess.next = currentProcess;
                }

                return beginProcess;
            }

            return null;
        }

        private CachedMethod GetCachedMethod(MethodInfo method, System.Type[] parameterTypes, object[] overrideArgs = null)
        {
            CachedMethod cache;
            System.Type type, returnType = method.ReturnType;

            if (returnType == null || returnType.Equals(typeof(void)))
            {
                if (parameterTypes == null || parameterTypes.Length == 0)
                    cache = new CachedAction();
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
                        return null;

                    cache = (CachedMethod)System.Activator.CreateInstance(type.MakeGenericType(parameterTypes));
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
                    return null;

                System.Type[] typeArguments = new System.Type[parameterTypes != null ? parameterTypes.Length + 1 : 1];

                for (int i = 0; i < typeArguments.Length - 1; i++)
                    typeArguments[i] = parameterTypes[i];

                typeArguments[typeArguments.Length - 1] = returnType;

                cache = (CachedMethod)System.Activator.CreateInstance(type.MakeGenericType(typeArguments));
            }

            cache.method = method;
            cache.overrideArgs = overrideArgs;
            return cache;
        }

        #endregion
    }
}
