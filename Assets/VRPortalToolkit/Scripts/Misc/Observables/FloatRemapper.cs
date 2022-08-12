using Misc.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Observables
{
    public class FloatRemapper : MonoBehaviour
    {
        [SerializeField] private AnimationCurve _curve = AnimationCurve.Linear(0, 0, 1f, 1f);
        public AnimationCurve curve { get => _curve; set => _curve = value; }

        public SerializableEvent<float> emmited = new SerializableEvent<float>();

        public virtual void Process(float value)
        {
            float newValue;

            if (curve != null)
                newValue = curve.Evaluate(value);
            else
                newValue = value;

            emmited?.Invoke(newValue);
        }
    }
}
