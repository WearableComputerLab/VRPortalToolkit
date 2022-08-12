using Misc.Data;
using Misc.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Observables
{
    public class FloatToBooleanOld : MonoBehaviour
    {
        [SerializeField] private FloatRange _range = new FloatRange(0,1);
        public virtual FloatRange range { get => _range; set => _range = value; }

        [Header("Events")]
        public SerializableEvent onTrue = new SerializableEvent();
        public SerializableEvent onFalse = new SerializableEvent();

        public void Receive(float value)
        {
            if (range.Contains(value))
                onTrue?.Invoke();
            else
                onFalse?.Invoke();
        }
    }
}
