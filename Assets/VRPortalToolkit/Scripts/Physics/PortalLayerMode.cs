using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Physics
{
    /// <summary>
    /// Specifies how portal layers are applied to objects.
    /// </summary>
    public enum PortalLayerMode
    {
        /// <summary>
        /// Ignore portal layers.
        /// </summary>
        Ignore = 0,
        /// <summary>
        /// Apply portal layers to colliders only.
        /// </summary>
        CollidersOnly = 1,
        /// <summary>
        /// Apply portal layers to all GameObjects.
        /// </summary>
        AllGameObjects = 2,
    }
}
