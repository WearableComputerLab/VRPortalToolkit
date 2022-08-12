using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Events
{
    [System.Serializable]
    public class SerializableListener
    {
        [SerializeField] private Object _targetObject;
        public Object targetObject { get => _targetObject; set => _targetObject = value; }

        [SerializeField] private SerializableProcess[] _targetProcesses;
        public SerializableProcess[] targetProcesses { get => _targetProcesses; set => _targetProcesses = value; }

        [SerializeField] private SerializableParameter[] _targetParameters;
        public SerializableParameter[] targetParameters { get => _targetParameters; set => _targetParameters = value; }

        [SerializeField] private SerializableListenerData _data;
        public SerializableListenerData data { get => _data; set => _data = value; }
    }
}
