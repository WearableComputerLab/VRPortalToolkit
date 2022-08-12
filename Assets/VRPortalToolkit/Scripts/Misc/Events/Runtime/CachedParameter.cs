using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Misc.Events
{
    public class CachedParameter
    {
        private static object[] EmptyObjects = new object[0];

        private int _index = -1;
        public int index { get => _index; set => _index = value; }

        private object _value;
        public object value { get => _value; set => _value = value; }

        private CachedProcess _beginProcess;
        public CachedProcess beginProcess { get => _beginProcess; set => _beginProcess = value; }

        public CachedParameter(int index, CachedProcess beginProcess = null)
        {
            this.index = index;
            this.beginProcess = beginProcess;
        }

        public CachedParameter(object value, CachedProcess beginProcess = null)
        {
            this.value = value;
            this.beginProcess = beginProcess;
        }

        public object GetValue(object[] args)
        {
            object currentValue;

            if (_index >= 0 && _index < args.Length)
                currentValue = args[_index];
            else
                currentValue = value;

            if (_beginProcess != null)
                return _beginProcess.Invoke(ref currentValue, EmptyObjects);

            return currentValue;
        }
    }
}
