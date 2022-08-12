using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Reflection
{
    public class ReflectionInvoker : MonoBehaviour
    {
        [SerializeField] private Object _target = null;
        public Object Target { get => _target; set => _target = value; }

        [SerializeField] private string _memberName = "";
        public string MemberName { get => _memberName; set => _memberName = value; }


        [SerializeField] private MemberMode _memberMode = (MemberMode)~0;
        public MemberMode MemberMode { get => _memberMode; set => _memberMode = value; }

        [SerializeField] private BindingMode _bindingMode = (BindingMode)~0;
        public BindingMode BindingMode { get => _bindingMode; set => _bindingMode = value; }

        private object _value = null;
        public object Value
        {
            get => _value;
            set {

                if (_value != value)
                {
#if UNITY_EDITOR
                    if (value == null)
                    {
                        if (_valueType != ValueType.Object)
                            _valueType = ValueType.None;
                    }
                    if (value is bool)
                    {
                        _boolValue = (bool)value;
                        _valueType = ValueType.Bool;
                    }
                    else if (value is int)
                    {
                        _intValue = (int)value;
                        _valueType = ValueType.Int;
                    }
                    else if (value is float)
                    {
                        _xValue = (float)value;
                        _valueType = ValueType.Float;
                    }
                    else if (value is Vector2)
                    {
                        _xValue = ((Vector2)value).x;
                        _yValue = ((Vector2)value).y;
                        _valueType = ValueType.Vector2;
                    }
                    else if (value is Vector3)
                    {
                        _xValue = ((Vector3)value).x;
                        _yValue = ((Vector3)value).y;
                        _zValue = ((Vector3)value).z;
                        _valueType = ValueType.Vector3;
                    }
                    else if (value is Vector4)
                    {
                        _xValue = ((Vector4)value).x;
                        _yValue = ((Vector4)value).y;
                        _zValue = ((Vector4)value).z;
                        _wValue = ((Vector4)value).w;
                        _valueType = ValueType.Vector3;
                    }
                    else if (value is Quaternion)
                    {
                        _xValue = ((Quaternion)value).x;
                        _yValue = ((Quaternion)value).y;
                        _zValue = ((Quaternion)value).z;
                        _wValue = ((Quaternion)value).w;
                        _valueType = ValueType.Quaternion;
                    }
                    else if (value is string)
                    {
                        _stringValue = (string)value;
                        _valueType = ValueType.String;
                    }
                    else if (value is Object)
                    {
                        _objectValue = (Object)value;
                        _valueType = ValueType.Object;
                    }
                    else
                        _valueType = ValueType.Other;
#endif

                    _value = value;
                }
            }
        }

        public UnityEvent invoked = new UnityEvent();

        public UnityEvent failed = new UnityEvent();

        protected bool initialised = false;

        [SerializeField] private ValueType _valueType = ValueType.None;

        public enum ValueType
        {
            None = 0,
            Bool = 1,
            Int = 2,
            Float = 3,
            Vector2 = 4,
            Vector3 = 5,
            Vector4 = 6,
            Quaternion = 7,
            String = 8,
            Object = 9,
            Other = 10
        }

        [SerializeField] private Object _objectValue;
        [SerializeField] private bool _boolValue;
        [SerializeField] private int _intValue;
        [SerializeField] private string _stringValue;
        [SerializeField] private float _xValue;
        [SerializeField] private float _yValue;
        [SerializeField] private float _zValue;
        [SerializeField] private float _wValue;

        public virtual void Invoke(string methodName)
        {
            _memberName = methodName;
            Invoke();
        }

        public virtual void Invoke(object value)
        {
            _value = value;
            initialised = true;
            Invoke();
        }

        public virtual void Invoke(params object[] value) => Invoke((object)value);

        public virtual void Invoke(string methodName, object value)
        {
            _memberName = methodName;
            Invoke(value);
        }

        public virtual void Invoke(string methodName, params object[] value) => Invoke(methodName, (object)value);

        public virtual void Invoke()
        {
            if (Target)
            {
                if (!initialised)
                {
                    switch (_valueType)
                    {
                        case ValueType.Bool:
                            _value = _boolValue;
                            break;

                        case ValueType.Int:
                            _value = _intValue;
                            break;

                        case ValueType.Float:
                            _value = _xValue;
                            break;

                        case ValueType.Vector2:
                            _value = new Vector2(_xValue, _yValue);
                            break;

                        case ValueType.Vector3:
                            _value = new Vector3(_xValue, _yValue, _zValue);
                            break;

                        case ValueType.Vector4:
                            _value = new Vector4(_xValue, _yValue, _zValue, _wValue);
                            break;

                        case ValueType.Quaternion:
                            _value = new Quaternion(_xValue, _yValue, _zValue, _wValue);
                            break;

                        case ValueType.String:
                            _value = _stringValue;
                            break;

                        case ValueType.Object:
                            _value = _objectValue;
                            break;

                    }

                    initialised = true;
                }

                if (ReflectionUtilities.SetValue(Target, MemberName, Value, MemberMode, BindingMode))
                    invoked?.Invoke();
                else
                    failed?.Invoke();
            }
        }
    }
}