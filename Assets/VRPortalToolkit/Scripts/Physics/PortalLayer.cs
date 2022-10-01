using Misc;
using Misc.EditorHelpers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using VRPortalToolkit.Data;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit
{
    public class PortalLayer : MonoBehaviour
    {
        public Portal portal => _portalTransition ? _portalTransition.portal : null;

        [SerializeField] private PortalTransition _portalTransition;
        public PortalTransition portalTransition {
            get => _portalTransition;
            set => _portalTransition = value;
        }
        public void ClearPortalTransition() => portalTransition = null;

        [SerializeField] private PortalLayer _connectedLayer;
        public PortalLayer connectedLayer {
            get => _connectedLayer;
            set {
                if (_connectedLayer != value && value != this)
                {
                    PortalLayer previous = _connectedLayer;

                    Validate.UpdateField(this, nameof(_connectedLayer), _connectedLayer = value);
                    if (_connectedLayer) _connectedLayer.connectedLayer = this;

                    if (previous && previous._connectedLayer == this) previous.connectedLayer = null;
                }
            }
        }
        public void ClearConnectedSpace() => connectedLayer = null;

        [SerializeField] protected List<PortalLayerConversion> _layerConversion;
        public HeapAllocationFreeReadOnlyList<PortalLayerConversion> readonlyLayerConversion => _layerConversion;

        // outside +0, between +32, inside +64
        protected Dictionary<int, Vector2Int> _conversions = new Dictionary<int, Vector2Int>();

        protected bool _isAwake = false;

        public enum State
        {
            Outside,
            Between,
            Inside
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_connectedLayer), nameof(connectedLayer));
            if (Application.isPlaying) Validate.FieldChanged(this, nameof(_layerConversion), ClearLayerConversions, UpdateLayerDictionaries);
        }

        protected virtual void Reset()
        {
            portalTransition = GetComponentInChildren<PortalTransition>(true);

            if (!portalTransition) portalTransition = GetComponentInParent<PortalTransition>();

            if (portalTransition)
            {
                GameObject connected = portalTransition.connectedTransition ? portalTransition.connectedTransition.gameObject
                    : (portalTransition.portal && portalTransition.portal.connectedPortal ? portalTransition.portal.connectedPortal.gameObject : null);

                if (connected)
                {
                    connectedLayer = connected.GetComponentInChildren<PortalLayer>(true);
                    if (!connectedLayer) connectedLayer = connected.GetComponentInParent<PortalLayer>(true);
                }
            }
        }

        protected virtual void Awake() => TryAwake();

        protected virtual bool TryAwake()
        {
            if (!_isAwake)
            {
                UpdateLayerDictionaries();
                return _isAwake = true;
            }

            return false;
        }

        public virtual int ConvertState(State from, State to, int layer)
        {
            ConvertState(from, to, ref layer);
            return layer;
        }

        public virtual void ConvertState(State from, State to, ref int layer)
        {
            if (from == to) return;

            switch (from)
            {
                case State.Between:
                {
                    if (to == State.Inside)
                        ConvertBetweenToInside(ref layer);
                    else
                        ConvertBetweenToOutside(ref layer);
                    break;
                }
                case State.Inside:
                {
                    if (to == State.Between)
                        ConvertInsideToBetween(ref layer);
                    else
                        ConvertInsideToOutside(ref layer);
                    break;
                }
                default: // State.Outside:
                {
                    if (to == State.Between)
                        ConvertOutsideToBetween(ref layer);
                    else
                        ConvertOutsideToInside(ref layer);
                    break;
                }
            }
        }

        public virtual int ConvertOutsideToBetween(int layer)
        {
            ConvertOutsideToBetween(ref layer);
            return layer;
        }

        public virtual void ConvertOutsideToBetween(ref int layer)
        {
            if (layer < 0 || layer >= 32) return;

            TryAwake();

            if (_conversions.TryGetValue(layer, out Vector2Int options))
                layer = options.x;
        }

        public virtual int ConvertOutsideToInside(int layer)
        {
            ConvertOutsideToInside(ref layer);
            return layer;
        }

        public virtual void ConvertOutsideToInside(ref int layer)
        {
            if (layer < 0 || layer >= 32) return;

            TryAwake();

            if (_conversions.TryGetValue(layer, out Vector2Int options))
                layer = options.y;
        }

        public virtual int ConvertBetweenToOutside(int layer)
        {
            ConvertBetweenToOutside(ref layer);
            return layer;
        }

        public virtual void ConvertBetweenToOutside(ref int layer)
        {
            if (layer < 0 || layer >= 32) return;

            TryAwake();

            if (_conversions.TryGetValue(layer + 32, out Vector2Int options))
                layer = options.x;
        }

        public virtual int ConvertBetweenToInside(int layer)
        {
            ConvertBetweenToInside(ref layer);
            return layer;
        }

        public virtual void ConvertBetweenToInside(ref int layer)
        {
            if (layer < 0 || layer >= 32) return;

            TryAwake();

            if (_conversions.TryGetValue(layer + 32, out Vector2Int options))
                layer = options.y;
        }

        public virtual int ConvertInsideToBetween(int layer)
        {
            ConvertInsideToBetween(ref layer);
            return layer;
        }

        public virtual void ConvertInsideToBetween(ref int layer)
        {
            if (layer < 0 || layer >= 32) return;

            TryAwake();

            if (_conversions.TryGetValue(layer + 64, out Vector2Int options))
                layer = options.y;
        }

        public virtual int ConvertInsideToOutside(int layer)
        {
            ConvertInsideToOutside(ref layer);
            return layer;
        }

        public virtual void ConvertInsideToOutside(ref int layer)
        {
            if (layer < 0 || layer >= 32) return;

            TryAwake();

            if (_conversions.TryGetValue(layer + 64, out Vector2Int options))
                layer = options.x;
        }

        public void DoRemoveLayerConversion(PortalLayerConversion portalLayerConversion) => RemoveLayerConversion(portalLayerConversion);

        public virtual bool RemoveLayerConversion(PortalLayerConversion portalLayerConversion)
        {
            if (_layerConversion.RemoveAll(i => i.between == portalLayerConversion.between) > 0)
            {
                UpdateLayerDictionaries();
                return true;
            }

            return false;
        }

        public void DoRemoveLayerConversion(int outside) => RemoveLayerConversion(outside);

        public virtual bool RemoveLayerConversion(int outside)
        {
            if (_layerConversion.RemoveAll(i => i.outside == outside) > 0)
            {
                UpdateLayerDictionaries();
                return true;
            }

            return false;
        }

        public void DoAddLayerConversion(int outside, int between, int inside) => AddLayerConversion(outside, between, inside);

        public virtual bool AddLayerConversion(int outside, int between, int inside)
            => AddLayerConversion(new PortalLayerConversion(outside, between, inside));

        public void DoAddLayerConversion(PortalLayerConversion portalLayerConversion) => AddLayerConversion(portalLayerConversion);

        public virtual bool AddLayerConversion(PortalLayerConversion portalLayerConversion)
        {
            _layerConversion.RemoveAll(i => i == portalLayerConversion);

            _layerConversion.Add(portalLayerConversion);

            UpdateLayerDictionaries();

            return true;
        }

        public virtual void ClearLayerConversions()
        {
            _layerConversion.Clear();
            UpdateLayerDictionaries();
        }

        protected virtual void UpdateLayerDictionaries()
        {
            _conversions.Clear();

            if (_layerConversion != null)
            {
                int outside, between, inside;

                foreach (PortalLayerConversion conversion in _layerConversion)
                {
                    // Just incase any data is wrong
                    outside = Mathf.Clamp(conversion.outside, 0, 31);
                    between = Mathf.Clamp(conversion.between, 0, 31);
                    inside = Mathf.Clamp(conversion.inside, 0, 31);

                    _conversions[outside] = new Vector2Int(between, inside);
                    _conversions[between + 32] = new Vector2Int(outside, inside);
                    _conversions[inside + 64] = new Vector2Int(outside, between);
                }
            }
        }
    }
}
