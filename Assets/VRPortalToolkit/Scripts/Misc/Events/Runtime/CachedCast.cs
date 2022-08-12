using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Misc.Events
{
    public class CachedCast : CachedProcess
    {
        private Type _type;
        public Type type { get => _type; set => _type = value; }

        public CachedCast(Type type)
        {
            this.type = type;
        }

        public override object Invoke(ref object obj, object[] args)
        {
            if (obj != null && _type != null)
            {
                Type objType = obj.GetType();

                do
                {
                    if (objType == type)
                    {
                        if (isLast) return obj;

                        return next.Invoke(ref obj, args);
                    }

                    objType = objType.BaseType;

                } while (objType != null);

                object converted;

                try { converted = Convert.ChangeType(obj, _type); }
                catch { return null; }

                if (isLast) return converted;

                return next.Invoke(ref converted, args);
            }

            return null;
        }
    }

    public class CachedCast<T, TResult> : CachedProcess
    {
        private event Func<T, TResult> _func;
        public Func<T, TResult> func {
            get => _func;
            set => _func = value;
        }

        public CachedCast() : base() { }

        public override string GetName() => $"CachedCast<{nameof(T)},{nameof(T)}>";

        public CachedCast(MethodInfo method) : base()
        {
            _func = (Func<T, TResult>)Delegate.CreateDelegate(typeof(Func<T, TResult>), method);
        }

        public CachedCast(Func<T, TResult> func) : base()
        {
            _func = func;
        }

        public override object Invoke(ref object obj, object[] args)
        {
            if (_func != null && obj is T objAsT)
            {
                object returnValue = _func.Invoke(objAsT);
                
                if (isLast) return returnValue;

                return next.Invoke(ref returnValue, args);
            }

            return null;
        }
    }

    /*public class CachedCast<T> : CachedProcess
    {
        public CachedCast() { }

        public override object Invoke(object obj, object[] args)
        {
            if (obj != null)
            {
                if (obj is T asT)
                    return asT;

                try
                {
                    asT = (T)obj;
                    return asT;
                }
                catch
                {
                    Debug.LogError($"Could not cast from <{obj.GetType()}> to <{typeof(T)}>");
                }
            }

            return null;
        }
    }*/
}
