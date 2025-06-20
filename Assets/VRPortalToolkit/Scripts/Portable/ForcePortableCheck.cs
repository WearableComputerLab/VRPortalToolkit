using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Portables
{
    /// <summary>
    /// Forces a portal check for the attached transform during LateUpdate.
    /// Used to ensure objects are correctly teleported through portals.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class ForcePortableCheck : MonoBehaviour
    {
        /// <summary>
        /// Forces a portal check for this transform during LateUpdate.
        /// </summary>
        protected virtual void LateUpdate()
        {
            PortalPhysics.ForcePortalCheck(transform);
        }
    }
}
