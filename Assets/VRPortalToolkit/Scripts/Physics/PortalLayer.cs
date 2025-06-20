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
    /// <summary>
    /// Represents a portal layer for managing layer transitions and conversions in portal physics.
    /// There should be a trigger volume for the PortalLayer and then inside a smaller trigger volume for the PortalTransition.
    /// This allows for physics through portals to be fully implemented.
    /// </summary>
    public class PortalLayer : MonoBehaviour
    {
        /// <summary>
        /// The portal associated with this layer.
        /// </summary>
        public Portal portal => _portalTransition ? _portalTransition.portal : null;

        [Tooltip("The portal transition associated with this layer.")]
        [SerializeField] private PortalTransition _portalTransition;
        /// <summary>
        /// Gets or sets the portal transition associated with this layer.
        /// </summary>
        public PortalTransition portalTransition {
            get => _portalTransition;
            set => _portalTransition = value;
        }

        [Tooltip("The connected portal layer on the other side of the portal.")]
        [SerializeField] private PortalLayer _connectedLayer;
        /// <summary>
        /// Gets or sets the connected portal layer.
        /// </summary>
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

        [Tooltip("The list of layer conversions for this portal layer.")]
        [SerializeField] protected List<PortalLayerConversion> _layerConversion;
        /// <summary>
        /// Gets a read-only list of layer conversions.
        /// </summary>
        public HeapAllocationFreeReadOnlyList<PortalLayerConversion> readonlyLayerConversion => _layerConversion;

        // outside +0, between +32, inside +64
        protected Dictionary<int, Vector2Int> _conversions = new Dictionary<int, Vector2Int>();

        protected bool _isAwake = false;

        /// <summary>
        /// Represents the state of the portal layer.
        /// </summary>
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
                    : (portalTransition.portal && portalTransition.portal.connected ? portalTransition.portal.connected.gameObject : null);

                if (connected)
                {
                    connectedLayer = connected.GetComponentInChildren<PortalLayer>(true);
                    if (!connectedLayer) connectedLayer = connected.GetComponentInParent<PortalLayer>(true);
                }
            }
        }

        protected virtual void Awake() => TryAwake();

        /// <summary>
        /// Attempts to initialize the portal layer.
        /// </summary>
        /// <returns>True if initialization was successful; otherwise, false.</returns>
        protected virtual bool TryAwake()
        {
            if (!_isAwake)
            {
                UpdateLayerDictionaries();
                return _isAwake = true;
            }

            return false;
        }

        /// <summary>
        /// Converts a layer from one state to another.
        /// </summary>
        /// <param name="from">The initial state.</param>
        /// <param name="to">The target state.</param>
        /// <param name="layer">The layer to convert.</param>
        /// <returns>The converted layer.</returns>
        public virtual int ConvertState(State from, State to, int layer)
        {
            ConvertState(from, to, ref layer);
            return layer;
        }

        /// <summary>
        /// Converts a layer from one state to another.
        /// </summary>
        /// <param name="from">The initial state.</param>
        /// <param name="to">The target state.</param>
        /// <param name="layer">The layer to convert.</param>
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

        /// <summary>
        /// Converts a layer from the Outside state to the Between state.
        /// </summary>
        /// <param name="layer">The layer to convert.</param>
        /// <returns>The converted layer.</returns>
        public virtual int ConvertOutsideToBetween(int layer)
        {
            ConvertOutsideToBetween(ref layer);
            return layer;
        }

        /// <summary>
        /// Converts a layer from the Outside state to the Between state.
        /// </summary>
        /// <param name="layer">The layer to convert.</param>
        public virtual void ConvertOutsideToBetween(ref int layer)
        {
            if (layer < 0 || layer >= 32) return;

            TryAwake();

            if (_conversions.TryGetValue(layer, out Vector2Int options))
                layer = options.x;
        }

        /// <summary>
        /// Converts a layer from the Outside state to the Inside state.
        /// </summary>
        /// <param name="layer">The layer to convert.</param>
        /// <returns>The converted layer.</returns>
        public virtual int ConvertOutsideToInside(int layer)
        {
            ConvertOutsideToInside(ref layer);
            return layer;
        }

        /// <summary>
        /// Converts a layer from the Outside state to the Inside state.
        /// </summary>
        /// <param name="layer">The layer to convert.</param>
        public virtual void ConvertOutsideToInside(ref int layer)
        {
            if (layer < 0 || layer >= 32) return;

            TryAwake();

            if (_conversions.TryGetValue(layer, out Vector2Int options))
                layer = options.y;
        }

        /// <summary>
        /// Converts a layer from the Between state to the Outside state.
        /// </summary>
        /// <param name="layer">The layer to convert.</param>
        /// <returns>The converted layer.</returns>
        public virtual int ConvertBetweenToOutside(int layer)
        {
            ConvertBetweenToOutside(ref layer);
            return layer;
        }

        /// <summary>
        /// Converts a layer from the Between state to the Outside state.
        /// </summary>
        /// <param name="layer">The layer to convert.</param>
        public virtual void ConvertBetweenToOutside(ref int layer)
        {
            if (layer < 0 || layer >= 32) return;

            TryAwake();

            if (_conversions.TryGetValue(layer + 32, out Vector2Int options))
                layer = options.x;
        }

        /// <summary>
        /// Converts a layer from the Between state to the Inside state.
        /// </summary>
        /// <param name="layer">The layer to convert.</param>
        /// <returns>The converted layer.</returns>
        public virtual int ConvertBetweenToInside(int layer)
        {
            ConvertBetweenToInside(ref layer);
            return layer;
        }

        /// <summary>
        /// Converts a layer from the Between state to the Inside state.
        /// </summary>
        /// <param name="layer">The layer to convert.</param>
        public virtual void ConvertBetweenToInside(ref int layer)
        {
            if (layer < 0 || layer >= 32) return;

            TryAwake();

            if (_conversions.TryGetValue(layer + 32, out Vector2Int options))
                layer = options.y;
        }

        /// <summary>
        /// Converts a layer from the Inside state to the Between state.
        /// </summary>
        /// <param name="layer">The layer to convert.</param>
        /// <returns>The converted layer.</returns>
        public virtual int ConvertInsideToBetween(int layer)
        {
            ConvertInsideToBetween(ref layer);
            return layer;
        }

        /// <summary>
        /// Converts a layer from the Inside state to the Between state.
        /// </summary>
        /// <param name="layer">The layer to convert.</param>
        public virtual void ConvertInsideToBetween(ref int layer)
        {
            if (layer < 0 || layer >= 32) return;

            TryAwake();

            if (_conversions.TryGetValue(layer + 64, out Vector2Int options))
                layer = options.y;
        }

        /// <summary>
        /// Converts a layer from the Inside state to the Outside state.
        /// </summary>
        /// <param name="layer">The layer to convert.</param>
        /// <returns>The converted layer.</returns>
        public virtual int ConvertInsideToOutside(int layer)
        {
            ConvertInsideToOutside(ref layer);
            return layer;
        }

        /// <summary>
        /// Converts a layer from the Inside state to the Outside state.
        /// </summary>
        /// <param name="layer">The layer to convert.</param>
        public virtual void ConvertInsideToOutside(ref int layer)
        {
            if (layer < 0 || layer >= 32) return;

            TryAwake();

            if (_conversions.TryGetValue(layer + 64, out Vector2Int options))
                layer = options.x;
        }

        /// <summary>
        /// Removes a layer conversion.
        /// </summary>
        /// <param name="portalLayerConversion">The layer conversion to remove.</param>
        public void DoRemoveLayerConversion(PortalLayerConversion portalLayerConversion) => RemoveLayerConversion(portalLayerConversion);

        /// <summary>
        /// Removes a layer conversion.
        /// </summary>
        /// <param name="portalLayerConversion">The layer conversion to remove.</param>
        /// <returns>True if the conversion was removed; otherwise, false.</returns>
        public virtual bool RemoveLayerConversion(PortalLayerConversion portalLayerConversion)
        {
            if (_layerConversion.RemoveAll(i => i.between == portalLayerConversion.between) > 0)
            {
                UpdateLayerDictionaries();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a layer conversion by its outside layer.
        /// </summary>
        /// <param name="outside">The outside layer to remove.</param>
        public void DoRemoveLayerConversion(int outside) => RemoveLayerConversion(outside);

        /// <summary>
        /// Removes a layer conversion by its outside layer.
        /// </summary>
        /// <param name="outside">The outside layer to remove.</param>
        /// <returns>True if the conversion was removed; otherwise, false.</returns>
        public virtual bool RemoveLayerConversion(int outside)
        {
            if (_layerConversion.RemoveAll(i => i.outside == outside) > 0)
            {
                UpdateLayerDictionaries();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a new layer conversion.
        /// </summary>
        /// <param name="outside">The outside layer.</param>
        /// <param name="between">The between layer.</param>
        /// <param name="inside">The inside layer.</param>
        public void DoAddLayerConversion(int outside, int between, int inside) => AddLayerConversion(outside, between, inside);

        /// <summary>
        /// Adds a new layer conversion.
        /// </summary>
        /// <param name="outside">The outside layer.</param>
        /// <param name="between">The between layer.</param>
        /// <param name="inside">The inside layer.</param>
        /// <returns>True if the conversion was added; otherwise, false.</returns>
        public virtual bool AddLayerConversion(int outside, int between, int inside)
            => AddLayerConversion(new PortalLayerConversion(outside, between, inside));

        /// <summary>
        /// Adds a new layer conversion.
        /// </summary>
        /// <param name="portalLayerConversion">The layer conversion to add.</param>
        public void DoAddLayerConversion(PortalLayerConversion portalLayerConversion) => AddLayerConversion(portalLayerConversion);

        /// <summary>
        /// Adds a new layer conversion.
        /// </summary>
        /// <param name="portalLayerConversion">The layer conversion to add.</param>
        /// <returns>True if the conversion was added; otherwise, false.</returns>
        public virtual bool AddLayerConversion(PortalLayerConversion portalLayerConversion)
        {
            _layerConversion.RemoveAll(i => i == portalLayerConversion);

            _layerConversion.Add(portalLayerConversion);

            UpdateLayerDictionaries();

            return true;
        }

        /// <summary>
        /// Clears all layer conversions.
        /// </summary>
        public virtual void ClearLayerConversions()
        {
            _layerConversion.Clear();
            UpdateLayerDictionaries();
        }

        /// <summary>
        /// Updates the internal dictionaries for layer conversions.
        /// </summary>
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
