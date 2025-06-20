using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Cloning;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit
{
    /// <summary>
    /// Extends PortalRenderClone to add clipping functionality to the clones.
    /// This allows clones to be properly clipped on the portal's plane.
    /// </summary>
    [DefaultExecutionOrder(1030)]
    public class PortalClippableClone : PortalRenderClone
    {
        [Tooltip("The offset distance for the clipping plane to prevent z-fighting")]
        [SerializeField] private float _clippingOffset = -0.001f;
        /// <summary>
        /// The offset distance for the clipping plane to prevent z-fighting.
        /// Negative values move the clipping plane slightly away from the portal.
        /// </summary>
        public float clippingOffset { get => _clippingOffset; set => _clippingOffset = value; }

        /// <summary>
        /// The material property block used to set clipping parameters on renderers.
        /// </summary>
        protected MaterialPropertyBlock _propertyBlock;

        /// <summary>
        /// Updates the clone handler by applying clipping planes to all renderers.
        /// </summary>
        /// <param name="transition">The portal transition associated with this clone.</param>
        /// <param name="handler">The clone handler containing information about the clone.</param>
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

        /// <summary>
        /// Tries to get the clipping plane information from a portal transition.
        /// </summary>
        /// <param name="transition">The portal transition to get information from.</param>
        /// <param name="centre">Output parameter for the center of the clipping plane.</param>
        /// <param name="normal">Output parameter for the normal of the clipping plane.</param>
        /// <returns>True if clipping information was found, false otherwise.</returns>
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
    }
}
