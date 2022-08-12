using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Events;

namespace Misc.Events
{
    // TODO: Should this try next?

    public class CachedAction : CachedMethod
    {
        private event Action _action;

        public CachedAction() : base() { }

        public CachedAction(MethodInfo method) : base(method) { }

        protected sealed override void OnCached()
        {
            _action = (Action)Delegate.CreateDelegate(typeof(Action), cachedTarget, method);
        }

        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

            ThrowOnInvalidLength(actualArgs, 0);

            _action();

            return null;
        }
    }

    public class CachedAction<T> : CachedMethod
    {
        private event Action<T> _action;

        public CachedAction() : base() { }

        public CachedAction(MethodInfo method) : base(method) { }

        protected sealed override void OnCached()
        {
            _action = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), cachedTarget, method);
        }

        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

            ThrowOnInvalidLength(actualArgs, 1);

            ThrowOnInvalidArg<T>(actualArgs[0], 0);

            _action((T)actualArgs[0]);

            return null;
        }
    }

    public class CachedAction<T1, T2> : CachedMethod
    {
        private event Action<T1, T2> _action;

        public CachedAction() : base() { }

        public CachedAction(MethodInfo method) : base(method) { }

        protected sealed override void OnCached()
        {
            _action = (Action<T1, T2>)Delegate.CreateDelegate(typeof(Action<T1, T2>), cachedTarget, method);
        }

        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

            ThrowOnInvalidLength(actualArgs, 2);

            ThrowOnInvalidArg<T1>(actualArgs[0], 0);
            ThrowOnInvalidArg<T1>(actualArgs[1], 1);

            _action((T1)actualArgs[0], (T2)actualArgs[1]);

            return null;
        }
    }

    public class CachedAction<T1, T2, T3> : CachedMethod
    {
        private event Action<T1, T2, T3> _action;

        public CachedAction() : base() { }

        public CachedAction(MethodInfo method) : base(method) { }

        protected sealed override void OnCached()
        {
            _action = (Action<T1, T2, T3>)Delegate.CreateDelegate(typeof(Action<T1, T2, T3>), cachedTarget, method);
        }

        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

            ThrowOnInvalidLength(actualArgs, 3);

            ThrowOnInvalidArg<T1>(actualArgs[0], 0);
            ThrowOnInvalidArg<T1>(actualArgs[1], 1);
            ThrowOnInvalidArg<T1>(actualArgs[2], 2);

            _action((T1)actualArgs[0], (T2)actualArgs[1], (T3)actualArgs[2]);

            return null;
        }
    }

    public class CachedAction<T1, T2, T3, T4> : CachedMethod
    {
        private event Action<T1, T2, T3, T4> _action;

        public CachedAction() : base() { }

        public CachedAction(MethodInfo method) : base(method) { }

        protected sealed override void OnCached()
        {
            _action = (Action<T1, T2, T3, T4>)Delegate.CreateDelegate(typeof(Action<T1, T2, T3, T4>), cachedTarget, method);
        }

        protected sealed override object MemberInvoke(ref object target, object[] args)
        {
            object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

            ThrowOnInvalidLength(actualArgs, 4);

            ThrowOnInvalidArg<T1>(actualArgs[0], 0);
            ThrowOnInvalidArg<T1>(actualArgs[1], 1);
            ThrowOnInvalidArg<T1>(actualArgs[2], 2);
            ThrowOnInvalidArg<T1>(actualArgs[3], 3);

            _action((T1)actualArgs[0], (T2)actualArgs[1], (T3)actualArgs[2], (T4)actualArgs[4]);

            return null;
        }
    }
}
