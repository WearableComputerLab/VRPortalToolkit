using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Portables
{
    /// <summary>
    /// Interface for objects that can be teleported through portals.
    /// </summary>
    public interface IPortable
    {
        /// <summary>
        /// The layer mask used to determine which portals this object can interact with.
        /// </summary>
        LayerMask portalLayerMask { get; }

        /// <summary>
        /// Gets the current origin position of this portable object.
        /// </summary>
        /// <returns>The world position of the origin point.</returns>
        Vector3 GetOrigin();

        /// <summary>
        /// Teleports this object through the specified portal.
        /// Should call "ForceTeleport" in PortalPhysics.
        /// </summary>
        /// <param name="portal">The portal to teleport through.</param>
        void Teleport(Portal portal);
    }
}