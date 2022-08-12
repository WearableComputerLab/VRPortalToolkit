using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Portables
{
    public interface IPortable
    {
        /// <summary>The current origin of the portable.</summary>
        LayerMask portalLayerMask { get; }

        /// <summary>The current origin of the portable.</summary>
        Vector3 GetOrigin();

        /// <summary>Called to teleport the portable through a portal. Should Call "ForceTeleport" in "PortalPhysics">.</summary>
        void Teleport(Portal portal);
    }
}