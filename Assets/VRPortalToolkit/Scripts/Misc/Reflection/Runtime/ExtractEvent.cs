using System;
using System.Reflection;
using UnityEngine.Events;

namespace Misc.Reflection
{
    public abstract class ExtractEvent
    {
        public abstract Type argumentType { get; }

        public abstract bool Find(object target, MethodInfo methodInfo);

        public abstract void Invoke(object argument);
    }

    public class ExtractEvent<T> : ExtractEvent
    {
        public override Type argumentType => typeof(T);

        private UnityAction<T> action;

        public ExtractEvent(UnityAction<T> action)
        {
            this.action = action;
        }

        public override bool Find(object target, MethodInfo methodInfo)
            => action != null && action.Target == target && action.Method == methodInfo;

        public override void Invoke(object argument)
        {
            if (argument is T argumentT)
                action?.Invoke(argumentT);
        }
    }
}
