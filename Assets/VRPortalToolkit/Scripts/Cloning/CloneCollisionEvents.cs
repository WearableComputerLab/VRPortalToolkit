using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Cloning
{
    // TODO: Want to make clones more event driven, so orignal can be cached (right now, due to recycling, there is no certainty that it remains the same between updates
    public class CloneCollisionEvents : MonoBehaviour
    {
        protected void OnTriggerEnter(Collider other)
        {
            if (PortalCloning.TryGetOriginal(gameObject, out GameObject original))
                ExecuteEvents.Execute<ICloneTriggerEnterHandler>(original, null, (x, _) => x.OnCloneTriggerEnter(transform, other));
        }

        protected void OnTriggerStay(Collider other)
        {
            if (PortalCloning.TryGetOriginal(gameObject, out GameObject original))
                ExecuteEvents.Execute<ICloneTriggerStayHandler>(original, null, (x, _) => x.OnCloneTriggerStay(transform, other));
        }

        protected void OnTriggerExit(Collider other)
        {
            if (PortalCloning.TryGetOriginal(gameObject, out GameObject original))
                ExecuteEvents.Execute<ICloneTriggerExitHandler>(original, null, (x, _) => x.OnCloneTriggerExit(transform, other));
        }

        protected void OnCollisionEnter(Collision collision)
        {
            if (PortalCloning.TryGetOriginal(gameObject, out GameObject original))
                ExecuteEvents.Execute<ICloneCollisionEnterHandler>(original, null, (x, _) => x.OnCloneCollisionEnter(transform, collision));

        }

        protected void OnCollisionStay(Collision collision)
        {
            if (PortalCloning.TryGetOriginal(gameObject, out GameObject original))
                ExecuteEvents.Execute<ICloneCollisionStayHandler>(original, null, (x, _) => x.OnCloneCollisionStay(transform, collision));
        }

        protected void OnCollisionExit(Collision collision)
        {
            if (PortalCloning.TryGetOriginal(gameObject, out GameObject original))
                ExecuteEvents.Execute<ICloneCollisionExitHandler>(original, null, (x, _) => x.OnCloneCollisionExit(transform, collision));
        }
    }
}
