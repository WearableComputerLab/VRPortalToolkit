using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VRPortalToolkit.Physics
{
    public interface ITeleportHandler : IEventSystemHandler
    {
        void OnPreTeleport(Teleportation args);

        void OnPostTeleport(Teleportation args);
    }
}
