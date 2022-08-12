using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Events
{
    public enum ParameterMode
    {
        Default = 0,
        Args1 = 1,
        Args2 = 2,
        Args3 = 3,
        Args4 = 4,
        Bool = 5,
        Int = 6,
        Vector2Int = 7,
        Vector3Int = 8,
        Float = 9,
        Vector2 = 10,
        Vector3 = 11,
        Vector4 = 12,
        Char = 13,
        String = 14,
        Object = 15,
        Rect = 16,
        RectInt = 17,
        Bounds = 18,
        BoundsInt = 19,
        Color = 20,
        Gradient = 21,
        Curve = 22,
        Quaternion = 23
    }

    [Serializable]
    public class SerializableParameter : ISerializationCallbackReceiver
    {
        [SerializeField] private string _type;
        public string type { get => _type; set => _type = value; }

        [SerializeField] private ParameterMode _mode;
        public ParameterMode mode { get => _mode; set => _mode = value; }

        [SerializeField] private SerializableProcess[] _processes;
        public SerializableProcess[] processes { get => _processes; set => _processes = value; }

        public void OnBeforeSerialize()
        {
            _type = TidyAssemblyTypeName(_type);
        }

        public void OnAfterDeserialize()
        {
            _type = TidyAssemblyTypeName(_type);
        }

        // TODO: Borrowed from https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/UnityEvent/UnityEvent.cs
        internal static string TidyAssemblyTypeName(string assemblyTypeName)
        {
            if (string.IsNullOrEmpty(assemblyTypeName))
                return assemblyTypeName;

            int min = int.MaxValue;
            int i = assemblyTypeName.IndexOf(", Version=");
            if (i != -1)
                min = Math.Min(i, min);
            i = assemblyTypeName.IndexOf(", Culture=");
            if (i != -1)
                min = Math.Min(i, min);
            i = assemblyTypeName.IndexOf(", PublicKeyToken=");
            if (i != -1)
                min = Math.Min(i, min);

            if (min != int.MaxValue)
                assemblyTypeName = assemblyTypeName.Substring(0, min);

            // Strip module assembly name.

            // The non-modular version will always work, due to type forwarders.
            // This way, when a type gets moved to a differnet module, previously serialized UnityEvents still work.
            i = assemblyTypeName.IndexOf(", UnityEngine.");
            if (i != -1 && assemblyTypeName.EndsWith("Module"))
                assemblyTypeName = assemblyTypeName.Substring(0, i) + ", UnityEngine";
            return assemblyTypeName;
        }
    }
}
