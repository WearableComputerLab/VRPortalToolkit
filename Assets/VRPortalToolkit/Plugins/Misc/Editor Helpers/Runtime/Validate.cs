using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Misc.EditorHelpers
{

    // TODO: Need to add a garbage collection for destroyed keys, maybe run after a certain number of validates
    // Also maybe allow for reference to be overridden
    public static class Validate
    {
#if UNITY_EDITOR
        private static Dictionary<Key, Element> _previousValues = new Dictionary<Key, Element>();

        private struct Key
        {
            public object source;
            public string field;

            public Key(object source, string field)
            {
                this.source = source;
                this.field = field;
            }
        }

        private class Element
        {
            public System.Type Type { get; private set; }

            private bool _isReference;
            public bool isReference {
                get => _isReference;
                set {
                    if (_isReference != value)
                    {
                        _isReference = value;

                        if (!_isReference)
                        {
                            object temp = _previous;
                            _previous = null;
                            previous = temp;
                        }
                    }
                }
            }

            private bool _isNull;

            private object _previous;
            public object previous {
                get => _isNull || _previous is Object && !(Object)_previous ? null : _previous;
                set {
                    if (value != null)
                    {
                        _isNull = false;
                        System.Type newType = value.GetType();

                        if (_isReference || newType.IsValueType)
                        {
                            Type = newType;
                            _previous = value;
                        }
                        else
                        {
                            string json = JsonUtility.ToJson(value);

                            // TODO: there may be better ways to serialize, but I couldnt find one that could overwrite
                            if (Type != newType)
                            {
                                Type = newType;
                                _previous = JsonUtility.FromJson(json, newType);
                            }
                            else if (_previous != null)
                                JsonUtility.FromJsonOverwrite(json, _previous);
                            else
                                _previous = JsonUtility.FromJson(json, newType);
                        }
                    }
                    else
                        _isNull = true; // Dont actually clear it to save some memory
                }
            }

            public Element(System.Type type)
            {
                if (!type.IsSerializable || type == typeof(Object) || type.IsSubclassOf(typeof(Object)))
                    isReference = true;
            }

            public bool HasChanged(object value)
            {
                if (value == null) return !_isNull && previous != null;

                if (_isNull) return true;

                if (isReference) return value != previous;

                return !value.Equals(previous);
            }
        }
        private static bool TryFindFieldInfo(object value, string field, out FieldInfo fieldInfo)
        {
            System.Type type = value.GetType();

            do
            {
                fieldInfo = type.GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (fieldInfo != null)
                    return true;

                type = type.BaseType;

            } while (type != null);

            return false;
        }
        private static bool TryFindPropertyInfo(object value, string property, out PropertyInfo propertyInfo)
        {
            System.Type type = value.GetType();

            do
            {
                propertyInfo = type.GetProperty(property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (propertyInfo != null)
                    return true;

                type = type.BaseType;

            } while (type != null);

            return false;
        }
#endif

        // TODO: This is a workaround for the thing below
        public static void UpdateField(object source, string field)
        {
#if UNITY_EDITOR
            if (source == null)
            {
                Debug.LogError("Source cannot be null");
                return;
            }

            if (!TryFindFieldInfo(source, field, out FieldInfo fieldInfo))
            {
                Debug.LogError($"'{field}' could not be found in <{source}>");
                return;
            }

            Key key = new Key(source, field);

            if (_previousValues.TryGetValue(key, out Element element))
                element.previous = fieldInfo.GetValue(source);
            else
                _previousValues[key] = new Element(fieldInfo.FieldType) { previous = fieldInfo.GetValue(source) };
#endif
        }

        // TODO: This is a workaround for the thing below
        public static void UpdateField(object source, string field, object value)
        {
#if UNITY_EDITOR
            if (source == null)
            {
                Debug.LogError("Source cannot be null");
                return;
            }

            if (!TryFindFieldInfo(source, field, out FieldInfo fieldInfo))
            {
                Debug.LogError($"'{field}' could not be found in <{source}>");
                return;
            }

            Key key = new Key(source, field);

            if (_previousValues.TryGetValue(key, out Element element))
                element.previous = value;
            else
                _previousValues[key] = new Element(fieldInfo.FieldType) { previous = value };
#endif
        }

        // OH NO!
        // TODO: What if the field is changed by the setter, and we never actually see it? Our previous would be all out of wack
        public static bool FieldWithProperty(object source, string field, string property)
        {
#if UNITY_EDITOR
            if (source == null)
            {
                Debug.LogError("Source cannot be null");
                return false;
            }

            if (!TryFindFieldInfo(source, field, out FieldInfo fieldInfo))
            {
                Debug.LogError($"'{field}' could not be found in <{source}>");
                return false;
            }

            if (!TryFindPropertyInfo(source, property, out PropertyInfo propertyInfo))
            {
                Debug.LogError($"'{property}' property could not be found in <{source}>");
                return false;
            }

            object value = fieldInfo.GetValue(source);

            Key key = new Key(source, field);

            if (_previousValues.TryGetValue(key, out Element element))
            {
                if (element.HasChanged(value))
                {
                    fieldInfo.SetValue(source, element.previous);
                    propertyInfo.SetValue(source, value);
                    element.previous = value;
                    return true;
                }
            }
            else
                _previousValues[key] = new Element(fieldInfo.FieldType) { previous = fieldInfo.GetValue(source) };
#endif
            return false;
        }

        public static bool FieldChanged(object source, string field, System.Action onBeforeChange, System.Action onAfterChange)
        {
#if UNITY_EDITOR
            if (source == null)
            {
                Debug.LogError("Source cannot be null");
                return false;
            }

            if (!TryFindFieldInfo(source, field, out FieldInfo fieldInfo))
            {
                Debug.LogError($"'{field}' could not be found in <{source}>");
                return false;
            }

            object value = fieldInfo.GetValue(source);

            Key key = new Key(source, field);

            if (_previousValues.TryGetValue(key, out Element element))
            {
                if (element.HasChanged(value))
                {
                    if (onBeforeChange != null)
                    {
                        fieldInfo.SetValue(source, element.previous);

                        onAfterChange?.Invoke();

                        fieldInfo.SetValue(source, value);
                    }
                    element.previous = value;

                    onAfterChange?.Invoke();
                    return true;
                }
            }
            else
                _previousValues[key] = new Element(fieldInfo.FieldType) { previous = fieldInfo.GetValue(source) };
#endif
            return false;
        }
    }
}