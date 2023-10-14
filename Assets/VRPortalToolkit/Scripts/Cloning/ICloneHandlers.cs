using UnityEngine;
using UnityEngine.EventSystems;

namespace VRPortalToolkit.Cloning
{
    public interface ICloneTriggerEnterHandler : IEventSystemHandler
    {
        void OnCloneTriggerEnter(Transform clone, Collider other);
    }

    public interface ICloneTriggerStayHandler : IEventSystemHandler
    {
        void OnCloneTriggerStay(Transform clone, Collider other);
    }

    public interface ICloneTriggerExitHandler : IEventSystemHandler
    {
        void OnCloneTriggerExit(Transform clone, Collider other);
    }

    public interface ICloneCollisionEnterHandler : IEventSystemHandler
    {
        void OnCloneCollisionEnter(Transform clone, Collision collision);
    }

    public interface ICloneCollisionStayHandler : IEventSystemHandler
    {
        void OnCloneCollisionStay(Transform clone, Collision collision);
    }

    public interface ICloneCollisionExitHandler : IEventSystemHandler
    {
        void OnCloneCollisionExit(Transform clone, Collision collision);
    }
}
