using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Portables;

namespace VRPortalToolkit
{
    /// <summary>
    /// Interface for components that handle teleportation of their portable transform.
    /// </summary>
    public interface IPortableHandler
    {
        /// <summary>
        /// Attempts to teleport a portable object.
        /// </summary>
        /// <param name="target">The transform of the portable object.</param>
        /// <param name="portable">The portable interface implementation.</param>
        /// <returns>True if teleportation was performed, false otherwise.</returns>
        bool TryTeleportPortable(Transform target, IPortable portable);
    }
}
