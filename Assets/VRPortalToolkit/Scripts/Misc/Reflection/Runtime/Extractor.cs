using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Reflection
{
    [Serializable]
    public class Extractor<T>
    {
        [SerializeField] private List<ExtractTarget> _persistentTargets = new List<ExtractTarget>();

        private readonly List<ExtractEvent> _persistantEvents = new List<ExtractEvent>();

        private readonly List<ExtractEvent> _extractEvents = new List<ExtractEvent>();

        private readonly List<ExtractEvent> _cachedEvents = new List<ExtractEvent>();
        private readonly List<Type> _cachedTypes = new List<Type>();
        private bool _cacheIsDirty = true;

        internal void Validate()
        {
            if (_persistentTargets.Count == 0 && _persistantEvents.Count == 0)
                return;

            _persistentTargets.Clear();
            _cacheIsDirty = true;

            object[] parameters = new object[2];
            MethodInfo addListener = typeof(ExtractEvent<T>).GetMethod(nameof(AddPersistentListener));

            foreach (ExtractTarget target in _persistentTargets)
            {
                if (target.SourceObject && !string.IsNullOrEmpty(target.TargetName))
                {
                    if (TryGetValidMethodInfo(target.SourceObject.GetType(), target.TargetName, out MethodInfo methodInfo, out Type parameterType))
                    {
                        MethodInfo addGenericListener = addListener.MakeGenericMethod(parameterType);

                        parameters[0] = target.SourceObject;
                        parameters[1] = methodInfo;

                        addGenericListener.Invoke(this, parameters);
                    }
                }
            }
        }

        private void AddPersistentListener<T2>(object target, MethodInfo method) where T2 : T
        {
            _extractEvents.Add(new ExtractEvent<T2>((UnityAction<T2>)Delegate.CreateDelegate(typeof(UnityAction<T2>), target, method)));
        }

        private bool TryGetValidMethodInfo(Type type, string functionName, out MethodInfo methodInfo, out Type parameterType)
        {
            while (type != typeof(object) && type != null)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                if (methods != null)
                {
                    foreach (MethodInfo method in methods)
                    {
                        if (method.Name == functionName)
                        {
                            ParameterInfo[] parameters = method.GetParameters();

                            if (parameters.Length == 1)
                            {
                                parameterType = parameters[0].GetType();

                                if (IsValidType(parameterType))
                                {
                                    methodInfo = method;
                                    return true;
                                }
                            }
                        }
                    }
                };

                type = type.BaseType;
            }

            methodInfo = null;
            parameterType = null;
            return false;
        }

        private bool IsValidType(Type type)
        {
            while (type != null)
            {
                if (type == typeof(T))
                    return true;

                type = type.BaseType;
            }

            return false;
        }

        public int ExtractEventsCount => _persistantEvents.Count + _extractEvents.Count;

        public int ExtractTypesCount {
            get {
                UpdateCache();
                return _cachedTypes.Count;
            }
        }

        protected void UpdateCache()
        {
            if (_cacheIsDirty)
            {
                // Should only call the first time
                if (_persistentTargets.Count != _persistantEvents.Count)
                    Validate();

                _cachedEvents.Clear();
                _cachedEvents.AddRange(_persistantEvents);
                _cachedEvents.AddRange(_extractEvents);

                _cachedTypes.Clear();

                Type type;
                for (int i = 0; i < _cachedEvents.Count; i++)
                {
                    type = _cachedEvents[i].argumentType;

                    if (!_cachedTypes.Contains(type)) _cachedTypes.Add(type);
                }
            }
        }

        public IEnumerable<Type> GetExtractTypes()
        {
            UpdateCache();

            for (int i = 0; i < _cachedTypes.Count; i++)
                yield return _cachedTypes[i];
        }

        public IEnumerable<ExtractEvent> GetExtractEvents()
        {
            UpdateCache();

            for (int i = 0; i < _cachedEvents.Count; i++)
                yield return _cachedEvents[i];
        }

        public void AddListener<T2>(UnityAction<T2> action) where T2 : T
        {
            _cacheIsDirty = true;
            _extractEvents.Add(new ExtractEvent<T2>(action));
        }

        public void RemoveListener<T2>(UnityAction<T2> action) where T2 : T
        {
            for (int i = 0; i < _extractEvents.Count; i++)
            {
                if (_extractEvents[i].Find(action.Target, action.Method))
                {
                    _extractEvents.RemoveAt(i);
                    _cacheIsDirty = true;
                    return;
                }
            }
        }
    }
}
