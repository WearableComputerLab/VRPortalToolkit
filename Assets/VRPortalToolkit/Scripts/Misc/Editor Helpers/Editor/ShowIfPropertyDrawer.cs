using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Misc.EditorHelpers;

namespace Misc.EditorHelpers
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfPropertyDrawer : PropertyDrawer
    {
        private static object[] emptyArgs = new object[0];
        private static Dictionary<KeyValuePair<FieldInfo,ShowIfAttribute>, MemberInfo> membersByAttribute= new Dictionary<KeyValuePair<FieldInfo, ShowIfAttribute>, MemberInfo>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
                this.NextPropertyField(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
                return this.GetNextPropertyHeight(property, label);

            return 0f;
        }

        protected virtual bool ShouldShow(SerializedProperty property)
        {
            ShowIfAttribute showIf = attribute as ShowIfAttribute;
            
            if (showIf != null)
            {
                object parentObject = property.GetParentObject();

                if (parentObject != null)
                {
                    if (!membersByAttribute.TryGetValue(new KeyValuePair<FieldInfo, ShowIfAttribute>(fieldInfo, showIf), out MemberInfo member))
                    {
                        System.Type parentType = parentObject.GetType();

                        while (parentType != null)
                        {
                            FieldInfo field = parentType.GetField(showIf.memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                            if (field != null)
                            {
                                member = field;
                                break;
                            }

                            PropertyInfo info = parentType.GetProperty(showIf.memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                            if (info != null && info.CanRead)
                            {
                                member = info;
                                break;
                            }

                            MethodInfo method = parentType.GetMethod(showIf.memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, System.Type.EmptyTypes, null);

                            if (method != null)
                            {
                                member = method;
                                break;
                            }

                            parentType = parentType.BaseType;
                        }

                        membersByAttribute[new KeyValuePair<FieldInfo, ShowIfAttribute>(fieldInfo, showIf)] = member;
                    }

                    if (member != null)
                    {
                        if (member is FieldInfo field)
                            return CheckEquals(field.GetValue(parentObject), showIf.value);
                        else if (member is PropertyInfo info)
                            return CheckEquals(info.GetValue(parentObject), showIf.value);
                        else if (member is MethodInfo method)
                            return CheckEquals(method.Invoke(parentObject, emptyArgs), showIf.value);

                    }
                }

                Debug.LogError("\"" + showIf.memberName + "\" not found!");
            }

            return true;
        }

        private bool CheckEquals(object a, object b)
        {
            if (a == null) return b == null;

            return a.Equals(b);
        }
    }
}
