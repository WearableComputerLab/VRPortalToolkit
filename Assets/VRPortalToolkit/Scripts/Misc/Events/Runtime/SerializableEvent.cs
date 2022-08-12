using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Events
{
    // TODO: Would it be better not to create the lists until they are actually required?
    // Might save memory for some events (given lots of these are likely never to actually have nonserialized listeners)

    // TODO: Should delay be built into the serializable events?
    // Would you stack calls? or just use the last?
    // Should individual delays be allowed per event, or would they all do the same?
    // Could the delay be modified in code
    // Maybe this is too much of a bag of worms...

    [System.Serializable]
    public class SerializableEvent : SerializableEventBase
    {
        public void Invoke() => base.Invoke();

        public override System.Type GetParameterType(int index) => null;

        public override int parameterCount => 0;
    }

    [System.Serializable]
    public class SerializableEvent<T> : SerializableEventBase
    {
        public void AddListener(UnityAction<T> action)
        {
            if (action != null) AddListener(new ActionListener<T>(action));
        }

        public void RemoveListener(UnityAction<T> action) => RemoveListener(action, null);

        public void Invoke(T arg) => base.Invoke(arg);

        public override System.Type GetParameterType(int index)
        {
            if (index == 0) return typeof(T);

            throw new System.IndexOutOfRangeException();
        }

        public override int parameterCount => 1;
    }

    [System.Serializable]
    public class SerializableEvent<T0, T1> : SerializableEventBase
    {
        public void AddListener(UnityAction<T0, T1> action)
        {
            if (action != null) AddListener(new ActionListener<T0, T1>(action));
        }

        public void RemoveListener(UnityAction<T0, T1> action) => RemoveListener(action, null);

        public void Invoke(T0 arg0, T1 arg1) => base.Invoke(arg0, arg1);

        public override System.Type GetParameterType(int index)
        {
            if (index == 0) return typeof(T0);
            if (index == 1) return typeof(T1);

            throw new System.IndexOutOfRangeException();
        }

        public override int parameterCount => 2;
    }

    [System.Serializable]
    public class SerializableEvent<T0, T1, T2> : SerializableEventBase
    {
        public void AddListener(UnityAction<T0, T1, T2> action)
        {
            if (action != null) AddListener(new ActionListener<T0, T1, T2>(action));
        }

        public void RemoveListener(UnityAction<T0, T1, T2> action) => RemoveListener(action, null);

        public void Invoke(T0 arg0, T1 arg1, T2 arg2) => base.Invoke(arg0, arg1, arg2);

        public override System.Type GetParameterType(int index)
        {
            if (index == 0) return typeof(T0);
            if (index == 1) return typeof(T1);
            if (index == 2) return typeof(T2);

            throw new System.IndexOutOfRangeException();
        }

        public override int parameterCount => 3;
    }

    [System.Serializable]
    public class SerializableEvent<T0, T1, T2, T3> : SerializableEventBase
    {
        public void AddListener(UnityAction<T0, T1, T2, T3> action)
        {
            if (action != null) AddListener(new ActionListener<T0, T1, T2, T3>(action));
        }

        public void RemoveListener(UnityAction<T0, T1, T2, T3> action) => RemoveListener(action, null);

        public void Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3) => base.Invoke(arg0, arg1, arg2, arg3);

        public override System.Type GetParameterType(int index)
        {
            if (index == 0) return typeof(T0);
            if (index == 1) return typeof(T1);
            if (index == 2) return typeof(T2);
            if (index == 3) return typeof(T3);

            throw new System.IndexOutOfRangeException();
        }

        public override int parameterCount => 4;
    }
}
