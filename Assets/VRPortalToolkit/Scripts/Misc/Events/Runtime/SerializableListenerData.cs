using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Events
{
    [System.Serializable]
    public class SerializableListenerData
    {
        [SerializeField] private Object[] _objectValues;
        public Object[] objectValues { get => _objectValues; set => _objectValues = value; }

        [SerializeField] private string[] _stringValues;
        public string[] stringValues { get => _stringValues; set => _stringValues = value; }

        [SerializeField] private bool[] _boolValues;
        public bool[] boolValues { get => _boolValues; set => _boolValues = value; }

        [SerializeField] private int[] _intValues;
        public int[] intValues { get => _intValues; set => _intValues = value; }

        [SerializeField] private float[] _floatValues;
        public float[] floatValues { get => _floatValues; set => _floatValues = value; }
    }
}
