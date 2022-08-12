using Misc.Data;
using Misc.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Observables
{
    public class FloatToBoolean : MonoBehaviour
    {
        [SerializeField] private FloatRange _range = new FloatRange(0,1);
        public virtual FloatRange range { get => _range; set => _range = value; }

        public SerializableEvent<bool> transformed = new SerializableEvent<bool>();

        public void Receive(float value)
        {
            transformed?.Invoke(range.Contains(value));
        }
    }
}
