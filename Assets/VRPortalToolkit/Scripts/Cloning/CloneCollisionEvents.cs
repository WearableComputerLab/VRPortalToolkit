using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Cloning
{
    /// <summary>
    /// Forwards collision and trigger events from a clone to the original GameObject using event interfaces.
    /// </summary>
    public class CloneCollisionEvents : MonoBehaviour
    {
        /// <summary>
        /// Called when a collider enters the trigger attached to the clone.
        /// </summary>
        protected void OnTriggerEnter(Collider other)
        {
            if (PortalCloning.TryGetOriginal(gameObject, out GameObject original))
                ExecuteEvents.Execute<ICloneTriggerEnterHandler>(original, null, (x, _) => x.OnCloneTriggerEnter(transform, other));
        }

        /// <summary>
        /// Called once per frame for every collider that is touching the trigger attached to the clone.
        /// </summary>
        protected void OnTriggerStay(Collider other)
        {
            if (PortalCloning.TryGetOriginal(gameObject, out GameObject original))
                ExecuteEvents.Execute<ICloneTriggerStayHandler>(original, null, (x, _) => x.OnCloneTriggerStay(transform, other));
        }

        /// <summary>
        /// Called when a collider exits the trigger attached to the clone.
        /// </summary>
        protected void OnTriggerExit(Collider other)
        {
            if (PortalCloning.TryGetOriginal(gameObject, out GameObject original))
                ExecuteEvents.Execute<ICloneTriggerExitHandler>(original, null, (x, _) => x.OnCloneTriggerExit(transform, other));
        }

        /// <summary>
        /// Called when the clone starts colliding with another collider.
        /// </summary>
        protected void OnCollisionEnter(Collision collision)
        {
            if (PortalCloning.TryGetOriginal(gameObject, out GameObject original))
                ExecuteEvents.Execute<ICloneCollisionEnterHandler>(original, null, (x, _) => x.OnCloneCollisionEnter(transform, collision));

        }

        /// <summary>
        /// Called once per frame for every collider that is touching the clone.
        /// </summary>
        protected void OnCollisionStay(Collision collision)
        {
            if (PortalCloning.TryGetOriginal(gameObject, out GameObject original))
                ExecuteEvents.Execute<ICloneCollisionStayHandler>(original, null, (x, _) => x.OnCloneCollisionStay(transform, collision));
        }

        /// <summary>
        /// Called when the clone stops colliding with another collider.
        /// </summary>
        protected void OnCollisionExit(Collision collision)
        {
            if (PortalCloning.TryGetOriginal(gameObject, out GameObject original))
                ExecuteEvents.Execute<ICloneCollisionExitHandler>(original, null, (x, _) => x.OnCloneCollisionExit(transform, collision));
        }
    }
}
