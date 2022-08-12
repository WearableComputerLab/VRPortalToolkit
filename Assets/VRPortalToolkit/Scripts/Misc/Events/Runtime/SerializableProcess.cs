using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Events
{
    public enum ProcessMode
    {
        Cast = 0,
        Field = 1,
        Property = 2,
        Method = 3,
        MethodWithType = 4,
        Parse = 5
    }

    [System.Serializable]
    public class SerializableProcess
    {
        [SerializeField] private string _name;
        public string name { get => _name; set => _name = value; }

        [SerializeField] private ProcessMode _mode;
        public ProcessMode mode { get => _mode; set => _mode = value; }

        public SerializableProcess() : base() { }

        public SerializableProcess(string name, ProcessMode mode) : base()
        {
            this.name = name;
            this.mode = mode;
        }

        public override bool Equals(object obj)
        {
            return obj is SerializableProcess method && _name == method._name && _mode == method._mode;
        }

        public override int GetHashCode()
        {
            int hashCode = 692707066;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_name);
            hashCode = hashCode * -1521134295 + _mode.GetHashCode();
            return hashCode;
        }
    }
}
