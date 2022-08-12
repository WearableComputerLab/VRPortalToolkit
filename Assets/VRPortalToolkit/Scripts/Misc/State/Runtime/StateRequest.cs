using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc
{
    [System.Serializable]
    public struct StateRequest<TSource>
    {
        public TSource source;
        public bool state;
    }
}