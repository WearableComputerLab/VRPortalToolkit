using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Portables
{
    [DefaultExecutionOrder(1000)]
    public class ForcePortableCheck : MonoBehaviour
    {
        protected virtual void LateUpdate()
        {
            PortalPhysics.ForcePortalCheck(transform);
        }
    }
}
