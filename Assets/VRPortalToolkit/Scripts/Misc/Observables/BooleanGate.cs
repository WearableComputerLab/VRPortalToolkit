using Misc.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit
{
    public class BooleanGate : MonoBehaviour
    {
        public SerializableEvent onTrue = new SerializableEvent();
        public SerializableEvent onFalse = new SerializableEvent();

        public virtual void Recieve(bool value)
        {
            if (value) onTrue.Invoke();
            else onFalse?.Invoke();
        }
    }
}
