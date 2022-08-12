using System;
using UnityEngine;

namespace VRPortalToolkit.Physics
{
    internal class TrackedPortal
    {
        public Portal portal;

        public Matrix4x4 currentLocalToWorld;

        public Matrix4x4 previousLocalToWorld;

        public bool hasChanged;
    }
}