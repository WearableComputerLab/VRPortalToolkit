using UnityEngine;
using UnityEditor;

namespace Misc.Reflection
{
    [CustomEditor(typeof(ReflectionInvoker))]
    [CanEditMultipleObjects]
    public class ReflectionInvokerEditor : Editor
    {
        protected SerializedProperty targetObject;
        protected SerializedProperty memberName;

        protected SerializedProperty memberMode;
        protected SerializedProperty bindingMode;

        protected SerializedProperty valueType;

        protected SerializedProperty objectValue;
        protected SerializedProperty boolValue;
        protected SerializedProperty intValue;
        protected SerializedProperty stringValue;
        protected SerializedProperty xValue;
        protected SerializedProperty yValue;
        protected SerializedProperty zValue;
        protected SerializedProperty wValue;

        protected SerializedProperty invoked;
        protected SerializedProperty failed;

        protected virtual void OnEnable()
        {
            targetObject = serializedObject.FindProperty("_target");
            memberName = serializedObject.FindProperty("_memberName");

            memberMode = serializedObject.FindProperty("_memberMode");
            bindingMode = serializedObject.FindProperty("_bindingMode");

            valueType = serializedObject.FindProperty("_valueType");

            objectValue = serializedObject.FindProperty("_objectValue");
            boolValue = serializedObject.FindProperty("_boolValue");
            intValue = serializedObject.FindProperty("_intValue");
            stringValue = serializedObject.FindProperty("_stringValue");
            xValue = serializedObject.FindProperty("_xValue");
            yValue = serializedObject.FindProperty("_yValue");
            zValue = serializedObject.FindProperty("_zValue");
            wValue = serializedObject.FindProperty("_wValue");

            invoked = serializedObject.FindProperty("invoked");
            failed = serializedObject.FindProperty("failed");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(targetObject);
            EditorGUILayout.PropertyField(memberName);
            EditorGUILayout.PropertyField(memberMode);
            EditorGUILayout.PropertyField(bindingMode);
            EditorGUILayout.PropertyField(valueType);

            switch ((ReflectionInvoker.ValueType)valueType.longValue)
            {
                case ReflectionInvoker.ValueType.Bool:
                    boolValue.boolValue = EditorGUILayout.Toggle("Value", boolValue.boolValue);
                    SetValue(boolValue.boolValue);
                    
                    break;

                case ReflectionInvoker.ValueType.Int:
                    intValue.intValue = EditorGUILayout.IntField("Value", boolValue.intValue);
                    SetValue(intValue.intValue);
                    break;

                case ReflectionInvoker.ValueType.Float:
                    xValue.floatValue = EditorGUILayout.FloatField("Value", xValue.floatValue);
                    SetValue(xValue.floatValue);
                    break;

                case ReflectionInvoker.ValueType.Vector2:
                {
                    Vector2 vector = EditorGUILayout.Vector2Field("Value", new Vector2(xValue.floatValue, yValue.floatValue));
                    xValue.floatValue = vector.x;
                    yValue.floatValue = vector.y;
                    SetValue(vector);
                    break;
                }

                case ReflectionInvoker.ValueType.Vector3:
                {
                    Vector3 vector = EditorGUILayout.Vector3Field("Value", new Vector3(xValue.floatValue, yValue.floatValue, zValue.floatValue));
                    xValue.floatValue = vector.x;
                    yValue.floatValue = vector.y;
                    yValue.floatValue = vector.z;
                    SetValue(vector);
                    break;
                }

                case ReflectionInvoker.ValueType.Vector4:
                {
                    Vector4 vector = EditorGUILayout.Vector4Field("Value", new Vector4(xValue.floatValue, yValue.floatValue, zValue.floatValue, wValue.floatValue));
                    xValue.floatValue = vector.x;
                    yValue.floatValue = vector.y;
                    yValue.floatValue = vector.z;
                    wValue.floatValue = vector.w;
                    SetValue(vector);
                    break;
                }

                case ReflectionInvoker.ValueType.Quaternion:
                {
                    Vector4 vector = EditorGUILayout.Vector4Field("Value", new Vector4(xValue.floatValue, yValue.floatValue, zValue.floatValue, wValue.floatValue));
                    xValue.floatValue = vector.x;
                    yValue.floatValue = vector.y;
                    yValue.floatValue = vector.z;
                    wValue.floatValue = vector.w;
                    SetValue(new Quaternion(vector.x, vector.y, vector.z, vector.w));
                    break;
                }

                case ReflectionInvoker.ValueType.String:
                    stringValue.stringValue = EditorGUILayout.TextField("Value", stringValue.stringValue);
                    SetValue(stringValue.stringValue);
                    break;

                case ReflectionInvoker.ValueType.Object:
                    objectValue.objectReferenceValue = EditorGUILayout.ObjectField("Value", objectValue.objectReferenceValue, typeof(Object), true);
                    SetValue(objectValue.objectReferenceValue);
                    break;
            }

            serializedObject.ApplyModifiedProperties();


            if (GUILayout.Button("Invoke"))
            {
                foreach (Object target in serializedObject.targetObjects)
                    ((ReflectionInvoker)target).Invoke();
            }

            EditorGUILayout.PropertyField(invoked);
            EditorGUILayout.PropertyField(failed);
        }

        public virtual void SetValue(object value)
        {
            foreach (Object target in serializedObject.targetObjects)
                ((ReflectionInvoker)target).Value = value;
        }
    }
}