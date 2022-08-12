using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Misc.Events
{
    public class CachedParse<T, TResult> : CachedMethod
    {
        private event Func<T, TResult> _func;

        public CachedParse() : base() { }

        public CachedParse(MethodInfo method) : base(method) { }

        public override string GetName() => $"CachedParse<{nameof(T)},{nameof(T)}>";

        protected sealed override void OnCached()
        {
            _func = (Func<T, TResult>)Delegate.CreateDelegate(typeof(Func<T, TResult>), cachedTarget, method);
        }

        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            if (target == null && Nullable.GetUnderlyingType(typeof(T)) != null)
                throw new ArgumentException($"<{GetName()}>'s passed object is the wrong type. Expected: {typeof(T)}, Found: NULL");
            else if (!(target is T))
                throw new ArgumentException($"<{GetName()}>'s passed object is the wrong type. Expected: {typeof(T)}, Found: {target.GetType()}");

            object returnValue = _func((T)target);

            if (isLast) return returnValue;

            return next.Invoke(ref returnValue, args);
        }
    }
}
