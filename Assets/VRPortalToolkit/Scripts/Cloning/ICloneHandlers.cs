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
        /// <param name="clone">The transform of the clone that entered the trigger.</param>
        /// <param name="other">The collider that was entered.</param>
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
        /// <param name="clone">The transform of the clone that is staying in the trigger.</param>
        /// <param name="other">The collider that is being stayed in.</param>
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
        /// <param name="clone">The transform of the clone that exited the trigger.</param>
        /// <param name="other">The collider that was exited.</param>
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
        /// <param name="clone">The transform of the clone that started colliding.</param>
        /// <param name="collision">Information about the collision.</param>
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
        /// <param name="clone">The transform of the clone that is continuing to collide.</param>
        /// <param name="collision">Information about the collision.</param>
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
        /// <param name="clone">The transform of the clone that stopped colliding.</param>
        /// <param name="collision">Information about the collision.</param>
        void OnCloneCollisionExit(Transform clone, Collision collision);
    }
}
