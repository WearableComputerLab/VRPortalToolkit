using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Misc.Events
{
    public class CachedListener
    {
        private object _target;
        public object target { get => _target; set => _target = value; }

        private CachedProcess _beginProcess;
        public CachedProcess beginProcess { get => _beginProcess; set => _beginProcess = value; }

        private CachedParameter[] _parameters;
        public CachedParameter[] parameters { get => _parameters; set => _parameters = value; }

        private object[] _newArgs;

        public CachedListener() { }

        public CachedListener(object target, CachedProcess beginProcess, CachedParameter[] parameters) : base()
        {
            this.target = target;
            this.beginProcess = beginProcess;
            this.parameters = parameters;
        }

        public void Invoke(object[] args)
        {
            if (_beginProcess != null && _target != null)
            {
                int length = _parameters != null ? _parameters.Length : 0;

                if (_newArgs == null || _newArgs.Length != length)
                    _newArgs = new object[length];

                for (int i = 0; i < length; i++)
                {
                    CachedParameter parameter = _parameters[i];

                    if (parameter != null)
                        _newArgs[i] = parameter.GetValue(args);
                }

                object currentTarget = _target;

                if (_beginProcess != null)
                    _beginProcess.Invoke(ref currentTarget, _newArgs);
            }
        }
    }
}
