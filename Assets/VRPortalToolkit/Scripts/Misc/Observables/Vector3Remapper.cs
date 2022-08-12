using Misc.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Observables
{
    public class Vector3Remapper : MonoBehaviour
    {
        [SerializeField] private AnimationCurve _magnitudeCurve = AnimationCurve.Linear(0, 0, 1f, 1f);
        public AnimationCurve curve { get => _magnitudeCurve; set => _magnitudeCurve = value; }

        [Header("Events")]
        public SerializableEvent<Vector3> emmited = new SerializableEvent<Vector3>();
        public SerializableEvent failed = new SerializableEvent();

        public virtual void Process(Vector3 value)
        {
            if (value != Vector3.zero)
            {
                float newMagnitude;

                if (curve != null)
                    newMagnitude = curve.Evaluate(value.magnitude);
                else
                    newMagnitude = value.magnitude;

                emmited?.Invoke(value.normalized * newMagnitude);
            }
            else
                failed?.Invoke();
        }
    }
}
