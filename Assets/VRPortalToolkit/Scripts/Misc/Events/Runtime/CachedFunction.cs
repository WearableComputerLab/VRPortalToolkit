using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Events
{
    public class CachedFunction<TResult> : CachedMethod
    {
        private event Func<TResult> _func;

        public CachedFunction() : base() { }

        public CachedFunction(MethodInfo method) : base(method) { }

        protected sealed override void OnCached()
        {
            _func = (Func<TResult>)Delegate.CreateDelegate(typeof(Func<TResult>), cachedTarget, method);
        }

        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

            ThrowOnInvalidLength(actualArgs, 0);

            object returnValue = _func();

            if (isLast) return returnValue;

            return next.Invoke(ref returnValue, actualArgs);
        }
    }

    public class CachedFunction<T, TResult> : CachedMethod
    {
        private event Func<T, TResult> _func;

        public CachedFunction() : base() { }

        public CachedFunction(MethodInfo method) : base(method) { }

        protected sealed override void OnCached()
        {
            _func = (Func<T, TResult>)Delegate.CreateDelegate(typeof(Func<T, TResult>), cachedTarget, method);
        }

        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

            ThrowOnInvalidLength(actualArgs, 1);

            ThrowOnInvalidArg<T>(actualArgs[0], 0);

            object returnValue = _func((T)actualArgs[0]);

            if (isLast) return returnValue;

            return next.Invoke(ref returnValue, args);
        }
    }

    public class CachedFunction<T1, T2, TResult> : CachedMethod
    {
        private event Func<T1, T2, TResult> _func;

        public CachedFunction() : base() { }

        public CachedFunction(MethodInfo method) : base(method) { }

        protected sealed override void OnCached()
        {
            _func = (Func<T1, T2, TResult>)Delegate.CreateDelegate(typeof(Func<T1, T2, TResult>), cachedTarget, method);
        }
        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

            ThrowOnInvalidLength(actualArgs, 2);

            ThrowOnInvalidArg<T1>(actualArgs[0], 0);
            ThrowOnInvalidArg<T2>(actualArgs[1], 1);

            object returnValue = _func((T1)actualArgs[0], (T2)actualArgs[1]);

            if (isLast) return returnValue;

            return next.Invoke(ref returnValue, args);
        }
    }

    public class CachedFunction<T1, T2, T3, TResult> : CachedMethod
    {
        private event Func<T1, T2, T3, TResult> _func;

        public CachedFunction() : base() { }

        public CachedFunction(MethodInfo method) : base(method) { }

        protected sealed override void OnCached()
        {
            _func = (Func<T1, T2, T3, TResult>)Delegate.CreateDelegate(typeof(Func<T1, T2, T3, TResult>), cachedTarget, method);
        }

        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

            ThrowOnInvalidLength(actualArgs, 3);

            ThrowOnInvalidArg<T1>(actualArgs[0], 0);
            ThrowOnInvalidArg<T2>(actualArgs[1], 1);
            ThrowOnInvalidArg<T3>(actualArgs[2], 2);

            object returnValue = _func((T1)actualArgs[0], (T2)actualArgs[1], (T3)actualArgs[2]);

            if (isLast) return returnValue;

            return next.Invoke(ref returnValue, args);
        }
    }

    public class CachedFunction<T1, T2, T3, T4, TResult> : CachedMethod
    {
        private event Func<T1, T2, T3, T4, TResult> _func;

        public CachedFunction() : base() { }

        public CachedFunction(MethodInfo method) : base(method) { }

        protected sealed override void OnCached()
        {
            _func = (Func<T1, T2, T3, T4, TResult>)Delegate.CreateDelegate(typeof(Func<T1, T2, T3, T4, TResult>), cachedTarget, method);
        }

        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

            ThrowOnInvalidLength(actualArgs, 4);

            ThrowOnInvalidArg<T1>(actualArgs[0], 0);
            ThrowOnInvalidArg<T2>(actualArgs[1], 1);
            ThrowOnInvalidArg<T3>(actualArgs[2], 2);
            ThrowOnInvalidArg<T4>(actualArgs[3], 3);

            object returnValue = _func((T1)actualArgs[0], (T2)actualArgs[1], (T3)actualArgs[2], (T4)actualArgs[4]);

            if (isLast) return returnValue;

            return next.Invoke(ref returnValue, args);
        }
    }
}
