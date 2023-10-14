using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Portables;

namespace VRPortalToolkit
{
    public interface IPortableHandler
    {
        bool TryTeleportPortable(Transform target, IPortable portable);
    }
}
