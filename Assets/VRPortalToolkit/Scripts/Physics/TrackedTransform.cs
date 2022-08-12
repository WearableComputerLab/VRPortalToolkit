using System;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Portables;

namespace VRPortalToolkit.Physics
{
    internal class TrackedTransform
    {
        public Transform transform;
        public IPortable portable;
        public Vector3 previousOrigin;
    }
}