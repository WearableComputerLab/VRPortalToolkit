using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Misc.Events
{
    public abstract class CachedProperty : CachedMember
    {
        private PropertyInfo _property;
        public PropertyInfo property {
            get => _property;
            set {
                if (_property != value)
                {
                    _property = value;
                    SetCachedIsDirty();
                }
            }
        }
        public override bool isStatic
        {
            get {
                if (_property != null)
                {
                    MethodInfo method = _property.GetGetMethod();

                    if (method != null) return method.IsStatic;

                    method = _property.GetSetMethod();

                    if (method != null) return method.IsStatic;
                }

                return false;
            }
        }

        public CachedProperty() { }

        public CachedProperty(PropertyInfo property) : base()
        {
            this.property = property;
        }

        public override string GetName()
        {
            if (property != null)
                return property.DeclaringType.Name + "." + property.Name;

            return "CachedProperty";
        }
    }

    public class CachedGetProperty<TResult> : CachedProperty
    {
        private event Func<TResult> _getFunc;

        public CachedGetProperty() : base() { }

        public CachedGetProperty(PropertyInfo property) : base(property) { }

        protected sealed override void OnCached()
        {
            _getFunc = (Func<TResult>)Delegate.CreateDelegate(typeof(Func<TResult>), cachedTarget, property.GetGetMethod());
        }

        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

            ThrowOnInvalidLength(actualArgs, 0);

            object nextValue = _getFunc();

            if (isLast) return nextValue;

            return next.Invoke(ref nextValue, args);
        }
    }

    public class CachedSetProperty<T> : CachedProperty
    {
        private event Func<T> _getFunc;
        private event Action<T> _setAction;

        public CachedSetProperty() : base() { }

        public CachedSetProperty(PropertyInfo property) : base(property) { }

        protected sealed override void OnCached()
        {
            MethodInfo getMethod = property.GetGetMethod(), setMethod = property.GetSetMethod();

            if (getMethod != null && getMethod.IsPublic)
                _getFunc = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), cachedTarget, getMethod);
            else
                _getFunc = null;

            if (setMethod != null && setMethod.IsPublic)
                _setAction = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), cachedTarget, setMethod);
            else
                _setAction = null;
        }

        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

            if (isLast)
            {
                ThrowOnInvalidLength(actualArgs, 1);
                ThrowOnInvalidArg<T>(actualArgs[0], 0);

                if (_setAction != null) _setAction((T)actualArgs[0]);
                return null;
            }

            ThrowOnInvalidLength(actualArgs, 0);

            if (_getFunc == null) return null;

            object nextValue = _getFunc();

            object returnValue = next.Invoke(ref nextValue, args);

            if (_setAction != null && nextValue is T)
                _setAction((T)nextValue);

            return returnValue;
        }
    }
}
