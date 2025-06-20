using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VRPortalToolkit.Physics
{
    /// <summary>
    /// Interface for handling teleportation events in portal physics.
    /// </summary>
    public interface ITeleportHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called before teleportation occurs.
        /// </summary>
        /// <param name="args">The teleportation arguments.</param>
        void OnPreTeleport(Teleportation args);

        /// <summary>
        /// Called after teleportation occurs.
        /// </summary>
        /// <param name="args">The teleportation arguments.</param>
        void OnPostTeleport(Teleportation args);
    }
}
