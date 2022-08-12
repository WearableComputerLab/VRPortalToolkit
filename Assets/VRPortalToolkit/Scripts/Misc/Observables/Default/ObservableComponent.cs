using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Observables
{
    public class ObservableComponent : Observable<Component>
    {
        protected override bool IsValueEqual(Component other)
            => currentValue == other;
    }
}
