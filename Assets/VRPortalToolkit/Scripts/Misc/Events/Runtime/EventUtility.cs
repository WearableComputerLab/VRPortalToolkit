using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Misc.Events
{
    public static class EventUtility
    {
        public static BindingFlags publicFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        public static BindingFlags publicStaticFlags = BindingFlags.Public | BindingFlags.Static;
        public static BindingFlags publicInstanceFlags = BindingFlags.Public | BindingFlags.Instance;

        public static Type[] SingleType = new Type[1];

        public static bool TryGetCastMethod(Type from, Type to, out MemberInfo methodInfo)
        {
            if (TryGetCastMethodSingle(from, from, to, out methodInfo))
                return true;

            return TryGetCastMethodSingle(to, from, to, out methodInfo);
        }

        private static bool TryGetCastMethodSingle(Type source, Type from, Type to, out MemberInfo methodInfo)
        {
            foreach (MethodInfo method in source.GetMethods(publicInstanceFlags))
            {
                if (method.IsSpecialName && method.ReturnType.Equals(to))
                {
                    ParameterInfo[] parameters = method.GetParameters();

                    if (parameters.Length == 1)
                    {
                        ParameterInfo parameter = parameters[0];

                        if (parameter.ParameterType.Equals(from) && (method.Name == "op_Implicit" || method.Name == "op_Explicit"))
                        {
                            methodInfo = method;
                            return true;
                        }
                    }
                }
            }

            methodInfo = null;
            return false;
        }

        public static bool TryGetMethod(Type type, string functionName, Type[] argumentTypes, out MethodInfo methodInfo)
        {
            while (type != null)
            {
                methodInfo = type.GetMethod(functionName, publicFlags, null, argumentTypes, null);

                if (methodInfo != null && !methodInfo.IsSpecialName)
                {
                    /*bool isValid = true;

                    ParameterInfo[] parameters = methodInfo.GetParameters();

                    if (parameters.Length != argumentTypes.Length) break;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        Type parameterType = parameters[i].GetType(),
                            argumentType = argumentTypes[i];

                        isValid = parameterType.IsPrimitive == argumentType.IsPrimitive;

                        if (!isValid) break;
                    }

                    if (isValid)*/ return true;
                }

                type = type.BaseType;
            }

            methodInfo = null;
            return false;
        }

        public static bool TryGetField(Type type, string fieldName, out FieldInfo fieldInfo)
        {
            while (type != null)
            {
                fieldInfo = type.GetField(fieldName, publicFlags);

                if (fieldInfo != null)
                    return true;

                type = type.BaseType;
            }

            fieldInfo = null;
            return false;
        }

        public static bool TryGetProperty(Type type, string propertyName, out PropertyInfo propertyInfo)
        {
            while (type != null)
            {
                propertyInfo = type.GetProperty(propertyName, publicFlags);

                if (propertyInfo != null)
                    return true;

                type = type.BaseType;
            }

            propertyInfo = null;
            return false;
        }
    }
}
