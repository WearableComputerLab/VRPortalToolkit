using UnityEngine;
using UnityEngine.EventSystems;

namespace VRPortalToolkit.Cloning
{
    /// <summary>
    /// Interface for handling trigger enter events from a clone.
    /// </summary>
    public interface ICloneTriggerEnterHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called when a clone enters a trigger.
        /// </summary>
        void OnCloneTriggerEnter(Transform clone, Collider other);
    }

    /// <summary>
    /// Interface for handling trigger stay events from a clone.
    /// </summary>
    public interface ICloneTriggerStayHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called when a clone stays in a trigger.
        /// </summary>
        void OnCloneTriggerStay(Transform clone, Collider other);
    }

    /// <summary>
    /// Interface for handling trigger exit events from a clone.
    /// </summary>
    public interface ICloneTriggerExitHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called when a clone exits a trigger.
        /// </summary>
        void OnCloneTriggerExit(Transform clone, Collider other);
    }

    /// <summary>
    /// Interface for handling collision enter events from a clone.
    /// </summary>
    public interface ICloneCollisionEnterHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called when a clone starts colliding with another object.
        /// </summary>
        void OnCloneCollisionEnter(Transform clone, Collision collision);
    }

    /// <summary>
    /// Interface for handling collision stay events from a clone.
    /// </summary>
    public interface ICloneCollisionStayHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called when a clone continues colliding with another object.
        /// </summary>
        void OnCloneCollisionStay(Transform clone, Collision collision);
    }

    /// <summary>
    /// Interface for handling collision exit events from a clone.
    /// </summary>
    public interface ICloneCollisionExitHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called when a clone stops colliding with another object.
        /// </summary>
        void OnCloneCollisionExit(Transform clone, Collision collision);
    }
}
