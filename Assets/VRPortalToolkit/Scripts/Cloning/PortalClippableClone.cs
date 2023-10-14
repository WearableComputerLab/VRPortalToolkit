using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Cloning;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit
{
    // TODO: Need an offset to hide things
    // TODO: One time this failed to unclip... (Happened when teleportation occured)

    [DefaultExecutionOrder(1030)]
    public class PortalClippableClone : PortalRenderClone
    {
        [SerializeField] private float _clippingOffset = -0.001f;
        public float clippingOffset { get => _clippingOffset; set => _clippingOffset = value; }

        protected MaterialPropertyBlock _propertyBlock;

        protected override void UpdateCloneHandler(PortalTransition transition, CloneHandler handler)
        {
            Vector3 teleportCentre, teleportNormal;

            base.UpdateCloneHandler(transition, handler);

            TryGetSlice(transition.connectedTransition, out teleportCentre, out teleportNormal);

            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();

            foreach (PortalCloneInfo<Renderer> info in handler.renderers)
            {
                if (info)
                {
                    info.original.GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetVector(PropertyID.ClippingCentre, teleportCentre);
                    _propertyBlock.SetVector(PropertyID.ClippingNormal, teleportNormal);
                    info.clone.SetPropertyBlock(_propertyBlock);
                }
            }
        }

        protected virtual bool TryGetSlice(PortalTransition transition, out Vector3 centre, out Vector3 normal)
        {
            if (transition && transition.transitionPlane)
            {
                centre = transition.transitionPlane.position;
                normal = -transition.transitionPlane.forward;

                if (clippingOffset != 0f)
                    centre -= normal * clippingOffset;

                return true;
            }

            centre = Vector3.zero;
            normal = Vector3.zero;
            return false;
        }

        /*protected virtual Vector3 ClosestPoint(Vector3 position, Collider collider)
        {
            if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider || (collider is MeshCollider meshCollider && meshCollider.convex))
                return collider.ClosestPoint(position);

            return collider.ClosestPointOnBounds(position);
        }*/
    }
}
