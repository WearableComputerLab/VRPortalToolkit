using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace VRPortalToolkit
{
    // TODO: Why is the trigger logic and stuff on the transition, not the transition handler?

    public class PortalTransition : TriggerHandler
    {
        [SerializeField] private Portal _portal;
        public Portal portal {
            get => _portal;
            set => _portal = value;
        }
        public void ClearPortal() => portal = null;

        [SerializeField] private PortalTransition _connectedTransition;
        public PortalTransition connectedTransition {
            get => _connectedTransition;
            set
            {
                if (_connectedTransition != value && value != this)
                {
                    PortalTransition previous = _connectedTransition;

                    Validate.UpdateField(this, nameof(_connectedTransition), _connectedTransition = value);
                    if (_connectedTransition) _connectedTransition.connectedTransition = this;

                    if (previous && previous._connectedTransition == this) previous.connectedTransition = null;
                }
            }
        }
        public void ClearConnectedTransition() => connectedTransition = null;

        [SerializeField] private Transform _transitionPlane;
        public Transform transitionPlane
        {
            get => _transitionPlane;
            set => _transitionPlane = value;
        }
        public void ClearSlice() => transitionPlane = null;

        protected virtual void Reset()
        {
            portal = GetComponentInChildren<Portal>(true);
            if (!portal) portal = GetComponentInParent<Portal>();

            if (portal && portal.connectedPortal)
            {
                connectedTransition = portal.connectedPortal.GetComponentInChildren<PortalTransition>(true);
                if (!connectedTransition) connectedTransition = portal.connectedPortal.gameObject.GetComponentInParent<PortalTransition>(true);
            }

            transitionPlane = transform;
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_connectedTransition), nameof(connectedTransition));
        }
    }
}
