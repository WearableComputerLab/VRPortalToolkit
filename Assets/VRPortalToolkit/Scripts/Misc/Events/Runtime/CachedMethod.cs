using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Misc.Events
{
    public abstract class CachedMethod : CachedMember
    {
        private MethodInfo _method;
        public MethodInfo method {
            get => _method;
            set {
                if (_method != value)
                {
                    _method = value;
                    SetCachedIsDirty();
                }
            }
        }

        public override bool isStatic => _method != null ? _method.IsStatic : true;

        public CachedMethod() { }

        public CachedMethod(MethodInfo method) : base()
        {
            this.method = method;
        }

        public override string GetName()
        {
            if (method != null)
            {
                bool found = false;
                string parameters = "";

                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    if (found) parameters += ",";
                    parameters += parameter.ParameterType.Name;
                    found = true;
                }

                return $"{method.DeclaringType.Name}.{method.Name}({parameters})";
            }

            return "CachedMethod";
        }
    }
}
