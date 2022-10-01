using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Cloning;

namespace VRPortalToolkit
{
    // TODO: Need an offset to hide things
    // TODO: One time this failed to unclip... (Happened when teleportation occured)

    public class PortalClippableClone : PortalRenderClone
    {
        public override GameObject original {
            get => base.original;
            set {
                if (base.original != value && backupRenderers != null)
                    backupRenderers = null;

                base.original = value;
            }
        }

        [Header("Clipping Settings")]
        [SerializeField] private float _clippingOffset = -0.001f;
        public float clippingOffset { get => _clippingOffset; set => _clippingOffset = value; }

        [SerializeField] private string _clippingCentreProperty = "_ClippingCentre";
        public string clippingCentreProperty { get => _clippingCentreProperty; set => _clippingCentreProperty = value; }

        [SerializeField] private string _clippingNormalProperty = "_ClippingNormal";
        public string clippingNormalProperty { get => _clippingNormalProperty; set => _clippingNormalProperty = value; }

        protected Renderer[] backupRenderers;

        protected MaterialPropertyBlock _propertyBlock;

        public override void Apply()
        {
            base.Apply();

            // Just in case there are no clones
            if (maxCloneCount == 0 && sortedTransitions.Count > 0)
            {
                if (backupRenderers == null)
                    backupRenderers = original.GetComponentsInChildren<Renderer>();

                PortalTransition transition = sortedTransitions[0];

                TryGetSlice(transition, out Vector3 centre, out Vector3 normal);

                if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();

                foreach (Renderer renderer in backupRenderers)
                {
                    renderer.GetPropertyBlock(_propertyBlock);

                    _propertyBlock.SetVector(_clippingCentreProperty, centre);
                    _propertyBlock.SetVector(_clippingNormalProperty, normal);
                    renderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }

        protected override void OnTriggerExitTransition(PortalTransition transition)
        {
            // Clear if this is the current transition
            if (sortedTransitions.Count > 0 && sortedTransitions[0] == transition)
            {
                if (currentClones.TryGetValue(transition, out CloneHandler handler))
                {
                    if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();

                    foreach (PortalCloneInfo<Renderer> info in handler.renderers)
                    {
                        info.original.GetPropertyBlock(_propertyBlock);

                        _propertyBlock.SetVector(_clippingNormalProperty, Vector3.zero);
                        info.original.SetPropertyBlock(_propertyBlock);
                    }
                }
            }

            base.OnTriggerExitTransition(transition);

            // Clear if this is the last transition, and there are no clones
            if (maxCloneCount == 0 && sortedTransitions.Count == 0)
            {
                if (backupRenderers == null)
                    backupRenderers = original.GetComponentsInChildren<Renderer>();

                if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();

                foreach (Renderer renderer in backupRenderers)
                {
                    renderer.GetPropertyBlock(_propertyBlock);

                    _propertyBlock.SetVector(_clippingNormalProperty, Vector3.zero);
                    renderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }

        protected override void UpdateCloneHandler(PortalTransition transition, CloneHandler handler)
        {
            base.UpdateCloneHandler(transition, handler);

            Vector3 centre, normal, teleportCentre, teleportNormal;

            base.UpdateCloneHandler(transition, handler);

            TryGetSlice(transition.connectedTransition, out teleportCentre, out teleportNormal);

            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();

            if (sortedTransitions.Count > 0 && sortedTransitions[0] == transition)
            {
                TryGetSlice(transition, out centre, out normal);

                foreach (PortalCloneInfo<Renderer> info in handler.renderers)
                {
                    if (info)
                    {
                        info.original.GetPropertyBlock(_propertyBlock);

                        _propertyBlock.SetVector(_clippingCentreProperty, centre);
                        _propertyBlock.SetVector(_clippingNormalProperty, normal);
                        info.original.SetPropertyBlock(_propertyBlock);

                        _propertyBlock.SetVector(_clippingCentreProperty, teleportCentre);
                        _propertyBlock.SetVector(_clippingNormalProperty, teleportNormal);
                        info.clone.SetPropertyBlock(_propertyBlock);
                    }
                }
            }
            else
            {
                foreach (PortalCloneInfo<Renderer> info in handler.renderers)
                {
                    if (info)
                    {
                        info.original.GetPropertyBlock(_propertyBlock);

                        _propertyBlock.SetVector(_clippingCentreProperty, teleportCentre);
                        _propertyBlock.SetVector(_clippingNormalProperty, teleportNormal);
                        info.clone.SetPropertyBlock(_propertyBlock);
                    }
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

        protected virtual Vector3 ClosestPoint(Vector3 position, Collider collider)
        {
            if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider || (collider is MeshCollider meshCollider && meshCollider.convex))
                return collider.ClosestPoint(position);

            return collider.ClosestPointOnBounds(position);
        }
    }
}
