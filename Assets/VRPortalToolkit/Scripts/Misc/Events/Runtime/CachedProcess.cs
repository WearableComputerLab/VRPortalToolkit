using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Events
{
    public abstract class CachedProcess
    {
        private CachedProcess _next;
        public CachedProcess next {
            get => _next;
            set => _next = value;
        }

        public object[] _overrideArgs;
        public object[] overrideArgs { get => _overrideArgs; set => _overrideArgs = value; }

        public bool hasOverrideArgs => _overrideArgs != null;

        public bool isLast => _next == null;

        public virtual string GetName() => "CachedProcess";

        public abstract object Invoke(ref object obj, object[] args);

        protected void ThrowOnInvalidArg<T>(object arg, int index)
        {
            if (arg == null)
            {
                if (Nullable.GetUnderlyingType(typeof(T)) != null)
                    throw new ArgumentException($"<{GetName()}>'s args[{index}] is of the wrong type. Expected: {typeof(T)}, Found: NULL");
            }
            else if (!(arg is T))
                throw new ArgumentException($"<{GetName()}>'s args[{index}] is of the wrong type. Expected: {typeof(T)}, Found: {arg.GetType()}");
        }

        protected void ThrowOnInvalidLength(object[] args, int validLength)
        {
            if (args == null)
            {
                if (args.Length != 0)
                    throw new ArgumentException($"<{GetName()}>'s args is invalid size. Expected: {validLength}, Found: NULL");
            }
            else if (args.Length != validLength)
                throw new ArgumentException($"<{GetName()}>'s args is invalid size. Expected: {validLength}, Found: {args.Length}");
        }

        protected static bool AllowInvoke(object target)
        {
            if (target == null)
                return false;

            // UnityEngine object
            UnityEngine.Object unityObj = target as UnityEngine.Object;
            if (!ReferenceEquals(unityObj, null))
                return unityObj != null;

            // Normal object
            return true;
        }
    }
}
