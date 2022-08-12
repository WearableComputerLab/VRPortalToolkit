using UnityEngine;

namespace VRPortalToolkit.Physics
{
    public struct PortalRay
    {
        public Portal fromPortal { get; }

        public Matrix4x4 localToWorldMatrix { get; }

        public float localDistance { get; }

        public Vector3 origin => localToWorldMatrix.GetColumn(3);

        public Vector3 direction => localToWorldMatrix.GetColumn(2) * localDistance;

        public PortalRay(Portal fromPortal, Matrix4x4 localToWorldMatrix, float localDistance)
        {
            this.localDistance = localDistance;
            this.localToWorldMatrix = localToWorldMatrix;
            this.fromPortal = fromPortal;
        }
    }
}