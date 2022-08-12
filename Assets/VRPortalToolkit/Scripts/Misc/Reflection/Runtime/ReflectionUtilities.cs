using System;
using System.Collections;
using System.Reflection;

namespace Misc.Reflection
{
    public static class ReflectionUtilities
    {
        public static bool TryGetValue<T>(object source, string name, out T value, MemberMode members = MemberMode.IncludeFields, BindingMode binding = BindingMode.IncludePublic | BindingMode.IncludeInstanced)
        {
            object asObject = GetValue(source, name, members, binding);

            if (asObject is T)
            {
                value = (T)asObject;
                return true;
            }

            value = default(T);
            return false;
        }

        public static T GetValue<T>(object source, string name, MemberMode members = MemberMode.IncludeFields, BindingMode binding = BindingMode.IncludePublic | BindingMode.IncludeInstanced)
        {
            TryGetValue<T>(source, name, out T value, members, binding);
            return value;
        }

        public static bool TryGetValue(object source, string name, out object value, MemberMode members = MemberMode.IncludeFields, BindingMode binding = BindingMode.IncludePublic | BindingMode.IncludeInstanced)
        {
            value = GetValue(source, name, members, binding);
            return value != null;
        }

        public static object GetValue(object source, string name, MemberMode members = MemberMode.IncludeFields, BindingMode binding = BindingMode.IncludePublic | BindingMode.IncludeInstanced)
        {
            if (source != null && !string.IsNullOrEmpty(name) && binding != BindingMode.None)
            {
                BindingFlags actualBinding = GetBindingFlags(binding);

                Type type = source.GetType();

                do
                {
                    if (members.HasFlag(MemberMode.IncludeFields))
                    {
                        FieldInfo field = type.GetField(name, actualBinding);

                        if (field != null) return field.GetValue(source);
                    }

                    if (members.HasFlag(MemberMode.IncludeProperties))
                    {
                        PropertyInfo property = type.GetProperty(name, actualBinding);

                        if (property != null) return property.GetValue(source, null);
                    }

                    if (members.HasFlag(MemberMode.IncludeMethods))
                    {
                        MethodInfo method = type.GetMethod(name, actualBinding);

                        if (method != null) return method.Invoke(source, null);
                    }

                    type = type.BaseType;

                } while (type != null);
            }

            return null;
        }

        public static bool TryGetValue<T>(object source, string name, int index, out T value, MemberMode members = MemberMode.IncludeFields, BindingMode binding = BindingMode.IncludePublic | BindingMode.IncludeInstanced)
        {
            object asObject = GetValue(source, name, index, members, binding);

            if (asObject is T)
            {
                value = (T)asObject;
                return true;
            }

            value = default(T);
            return false;
        }

        public static T GetValue<T>(object source, string name, int index, MemberMode members = MemberMode.IncludeFields, BindingMode binding = BindingMode.IncludePublic | BindingMode.IncludeInstanced)
        {
            TryGetValue<T>(source, name, index, out T value, members, binding);
            return value;
        }

        public static bool TryGetValue(object source, string name, int index, out object value, MemberMode members = MemberMode.IncludeFields, BindingMode binding = BindingMode.IncludePublic | BindingMode.IncludeInstanced)
        {
            TryGetValue(source, name, index, out value, members, binding);
            return value != null;
        }

        public static object GetValue(object source, string name, int index, MemberMode members = MemberMode.IncludeFields, BindingMode binding = BindingMode.IncludePublic | BindingMode.IncludeInstanced)
        {
            IEnumerable enumerable = GetValue(source, name, members, binding) as IEnumerable;

            if (enumerable != null)
            {
                IEnumerator enm = enumerable.GetEnumerator();

                while (index-- >= 0) enm.MoveNext();

                return enm.Current;
            }

            return GetValue(source, name);
        }

        public static bool SetValue(object target, string name, object value, MemberMode members = MemberMode.IncludeFields, BindingMode binding = BindingMode.IncludePublic | BindingMode.IncludeInstanced)
        {
            if (value != null && !string.IsNullOrEmpty(name) && binding != BindingMode.None)
            {
                BindingFlags actualBinding = GetBindingFlags(binding);

                Type type = target.GetType();

                if (members.HasFlag(MemberMode.IncludeFields))
                {
                    FieldInfo field = type.GetField(name, actualBinding);

                    if (field != null)
                    {
                        if (value == null)
                        {
                            // Can only use null if the value is nullable
                            if (Nullable.GetUnderlyingType(field.FieldType) == null)
                                return false;
                        }
                        else
                            if (!TypeIsValid(value.GetType(), field.FieldType)) return false;

                        field.SetValue(target, value);
                        return true;
                    }
                }

                if (members.HasFlag(MemberMode.IncludeProperties))
                {
                    PropertyInfo property = type.GetProperty(name, actualBinding);

                    if (property != null)
                    {
                        if (value == null)
                        {
                            // Can only use null if the value is nullable
                            if (Nullable.GetUnderlyingType(property.PropertyType) == null)
                                return false;
                        }
                        else if (!TypeIsValid(value.GetType(), property.PropertyType)) return false;

                        property.SetValue(target, value);
                        return true;
                    }
                }

                if (members.HasFlag(MemberMode.IncludeMethods))
                {
                    MethodInfo method = type.GetMethod(name, actualBinding);

                    if (method != null)
                    {
                        ParameterInfo[] parameters = method.GetParameters();

                        if (value is object[])
                        {
                            object[] values = (object[])value;
                            object innerValue;

                            if (parameters.Length != values.Length)
                                return false;

                            for (int i = 0; i < values.Length; i++)
                            {
                                innerValue = values[i];

                                if (innerValue == null)
                                {
                                    // Can only use null if the value is nullable
                                    if (Nullable.GetUnderlyingType(parameters[i].ParameterType) == null)
                                        return false;
                                }
                                else if (!TypeIsValid(innerValue.GetType(), parameters[i].ParameterType)) return false;
                            }

                            method.Invoke(target, values);
                        }
                        else
                        {
                            if (parameters.Length == 0)
                            {
                                if (value == null)
                                    method.Invoke(target, null);
                            }
                            else if (parameters.Length == 1)
                            {
                                if (value == null)
                                {
                                    // Can only use null if the value is nullable
                                    if (Nullable.GetUnderlyingType(parameters[0].ParameterType) == null)
                                        return false;
                                }
                                else if (!TypeIsValid(value.GetType(), parameters[0].ParameterType)) return false;

                                method.Invoke(target, new object[] { value });
                            }
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TypeIsValid(Type typeA, Type typeB)
        {
            return typeA == typeB || typeA.IsSubclassOf(typeB);
        }

        private static BindingFlags GetBindingFlags(BindingMode mode)
        {
            BindingFlags flags = (BindingFlags)0;

            if (mode.HasFlag(BindingMode.IncludePublic))
                flags |= BindingFlags.Public;
            if (mode.HasFlag(BindingMode.IncludeNonPublic))
                flags |= BindingFlags.NonPublic;
            if (mode.HasFlag(BindingMode.IgnoreCase))
                flags |= BindingFlags.IgnoreCase;
            if (mode.HasFlag(BindingMode.IncludeInstanced))
                flags |= BindingFlags.Instance;
            if (mode.HasFlag(BindingMode.IncludeStatic))
                flags |= BindingFlags.Static;

            return flags;
        }
    }
}