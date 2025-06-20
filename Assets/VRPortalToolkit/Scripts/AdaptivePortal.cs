using Misc.EditorHelpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit
{
    /// <summary>
    /// Represents the transform data for an adaptive portal.
    /// This struct is passed through each adaptive portal processor before being used to calculate the final pose of the portal.
    /// It contains information about the minimum and maximum bounds, minimum size, and entry/exit depths.
    /// </summary>
    public struct AdaptivePortalTransform
    {
        /// <summary>
        /// The minimum size that the portal should maintain.
        /// </summary>
        public Vector2 minSize;

        /// <summary>
        /// The minimum bounds of the portal.
        /// </summary>
        public Vector2 min;

        /// <summary>
        /// The maximum bounds of the portal.
        /// </summary>
        public Vector2 max;

        /// <summary>
        /// The depth of the portal's entry.
        /// </summary>
        public float entryDepth;

        /// <summary>
        /// The depth of the portal's exit.
        /// </summary>
        public float exitDepth;

        /// <summary>
        /// An identity transform with extreme min/max values.
        /// </summary>
        public static readonly AdaptivePortalTransform identity = new AdaptivePortalTransform()
        {
            min = new Vector2(float.MaxValue, float.MaxValue),
            max = new Vector2(float.MinValue, float.MinValue),
        };

        /// <summary>
        /// Returns the inverse of this transform, swapping min/max and entry/exit depths.
        /// </summary>
        public AdaptivePortalTransform inverse => new AdaptivePortalTransform()
        {
            minSize = minSize,
            min = new Vector2(-max.x, min.y),
            max = new Vector2(-min.x, max.y),
            entryDepth = -exitDepth,
            exitDepth = -entryDepth,
        };

        /// <summary>
        /// Expands the current min and max bounds to include the provided min and max values.
        /// </summary>
        /// <param name="min">The minimum bounds to include.</param>
        /// <param name="max">The maximum bounds to include.</param>
        public void AddMinMax(Vector2 min, Vector2 max)
        {
            this.min = Vector2.Min(this.min, min);
            this.max = Vector2.Max(this.max, max);
        }
    }

    /// <summary>
    /// Interface for processors that can modify an <see cref="AdaptivePortalTransform"/>.
    /// Processors are sorted by their <see cref="Order"/> property before being applied.
    /// </summary>
    public interface IAdaptivePortalProcessor
    {
        /// <summary>
        /// The order in which this processor should be applied relative to others.
        /// Lower values are processed first.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Processes and potentially modifies the given <see cref="AdaptivePortalTransform"/>.
        /// </summary>
        /// <param name="apTransform">The transform data to process.</param>
        void Process(ref AdaptivePortalTransform apTransform);
    }

    /// <summary>
    /// Represents an adaptive portal that can dynamically adjust its bounds and behavior based on connected objects and settings.
    /// </summary>
    public class AdaptivePortal : MonoBehaviour
    {
        [Tooltip("The portal connected to this adaptive portal.")]
        [SerializeField] private AdaptivePortal _connected;
        /// <summary>
        /// The portal connected to this adaptive portal.
        /// </summary>
        public AdaptivePortal connected
        {
            get => _connected;
            set => _connected = value;
        }

        [Tooltip("The transform offset for this portal.")]
        [SerializeField] private Transform _offset;
        /// <summary>
        /// The transform offset for this portal.
        /// </summary>
        public Transform offset
        {
            get => _offset;
            set => _offset = value;
        }

        [Flags]
        public enum MaintainMode
        {
            None = 0,
            MinSize = 1 << 1,
            Bounds = 1 << 2,
        }

        [Tooltip("The mode for maintaining the portal's bounds.")]
        [SerializeField] private MaintainMode _maintainMode = MaintainMode.Bounds;
        /// <summary>
        /// The mode for maintaining the portal's bounds.
        /// </summary>
        public MaintainMode maintainMode
        {
            get => _maintainMode;
            set => _maintainMode = value;
        }

#if UNITY_EDITOR
        private bool showRect => _maintainMode.HasFlag(MaintainMode.Bounds);
        [ShowIf(nameof(showRect))]
#endif
        [Tooltip("The bounds to maintain for this portal.")]
        [SerializeField] private Rect _maintainBounds = new Rect(-0.5f, -0.5f, 1f, 1f);
        /// <summary>
        /// The bounds to maintain for this portal.
        /// </summary>
        public Rect maintainBounds
        {
            get => _maintainBounds;
            set => _maintainBounds = value;
        }

#if UNITY_EDITOR
        private bool showSize => _maintainMode.HasFlag(MaintainMode.MinSize);
        [ShowIf(nameof(showSize))]
#endif
        [Tooltip("The minimum size to maintain for this portal.")]
        [SerializeField] private Vector2 _maintainMinSize = new Vector2(1f, 1f);
        /// <summary>
        /// The minimum size to maintain for this portal.
        /// </summary>
        public Vector2 maintainMinSize
        {
            get => _maintainMinSize;
            set => _maintainMinSize = value;
        }

#if UNITY_EDITOR
        private bool showForce => _maintainMode != MaintainMode.None;
        [ShowIf(nameof(showForce))]
#endif
        [Tooltip("Whether to force maintaining the portal's bounds.")]
        [SerializeField] private bool _forceMaintain = false;
        /// <summary>
        /// Whether to force maintaining the portal's bounds.
        /// </summary>
        public bool forceMaintain
        {
            get => _forceMaintain;
            set => _forceMaintain = value;
        }

        [Tooltip("The transition mode for this portal.")]
        [SerializeField] private Transition _transition;
        /// <summary>
        /// The transition mode for this portal.
        /// </summary>
        public Transition transition
        {
            get => _transition;
            set => _transition = value;
        }

        public enum Transition
        {
            Instant = 0,
            MoveTowards = 1,
        }

        [Tooltip("The speed of the transition.")]
        [ShowIf(nameof(_transition), Transition.MoveTowards)]
        [SerializeField] private float _transitionSpeed = 1f;
        /// <summary>
        /// The speed of the transition.
        /// </summary>
        public float transitionSpeed
        {
            get => _transitionSpeed;
            set => _transitionSpeed = value;
        }

        private readonly List<IAdaptivePortalProcessor> _processors = new List<IAdaptivePortalProcessor>();

        private bool _isPrimary = false;

        protected virtual void Awake()
        {
            GetComponentsInChildren(_processors);
        }

        protected virtual void LateUpdate()
        {
            if (!_connected)
            {
                AdaptivePortalTransform apTransform = MaintainDefault();

                _processors.Sort(new ProcessorComparer());
                for (int i = 0; i < _processors.Count; i++)
                    _processors[i].Process(ref apTransform);

                if (_forceMaintain) MaintainDefault(apTransform);

                GetPose(apTransform, out Vector2 min, out Vector2 max, out float depth);
                ApplyTransform(min, max, depth);
            }
            else
            {
                if (_isPrimary && _connected._isPrimary)
                    _isPrimary = false;

                if (!_connected._isPrimary) _isPrimary = true;

                // Only update if connected hasn't. This keeps scale in sync
                if (_isPrimary)
                {
                    AdaptivePortalTransform apTransform = MaintainDefault();
                    apTransform = _connected.MaintainDefault(apTransform.inverse).inverse;

                    _processors.Sort(new ProcessorComparer());
                    _connected._processors.Sort(new ProcessorComparer());

                    // Go through both this and the connected
                    int i = 0, j = 0;
                    while (i < _processors.Count && j < _connected._processors.Count)
                    {
                        if (_processors[i].Order < _connected._processors[j].Order)
                            _processors[i++].Process(ref apTransform);
                        else
                        {
                            apTransform = apTransform.inverse;
                            _connected._processors[j++].Process(ref apTransform);
                            apTransform = apTransform.inverse;
                        }
                    }

                    if (_forceMaintain) apTransform = MaintainDefault(apTransform);
                    if (_connected._forceMaintain) apTransform = MaintainDefault(apTransform.inverse).inverse;

                    GetPose(apTransform, out Vector2 min, out Vector2 max, out float depth);
                    GetPose(apTransform.inverse, out Vector2 connectedMin, out Vector2 connectedMax, out float connectedDepth);

                    ApplyTransform(min, max, depth);
                    _connected.ApplyTransform(connectedMin, connectedMax, connectedDepth);
                }
            }
        }

        private AdaptivePortalTransform MaintainDefault() => MaintainDefault(AdaptivePortalTransform.identity);

        private AdaptivePortalTransform MaintainDefault(AdaptivePortalTransform apTransform)
        {
            if (maintainMode.HasFlag(MaintainMode.Bounds))
                apTransform.AddMinMax(_maintainBounds.min, _maintainBounds.max);

            if (maintainMode.HasFlag(MaintainMode.MinSize))
                apTransform.minSize = Vector2.Max(apTransform.minSize, _maintainMinSize);

            return apTransform;
        }

        private struct ProcessorComparer : IComparer<IAdaptivePortalProcessor>
        {
            public int Compare(IAdaptivePortalProcessor x, IAdaptivePortalProcessor y) => x.Order.CompareTo(y.Order);
        }

        private AdaptivePortalTransform GetPose()
        {
            // Get default
            AdaptivePortalTransform apTransform = AdaptivePortalTransform.identity;

            if (maintainMode.HasFlag(MaintainMode.Bounds))
            {
                apTransform.min = _maintainBounds.min;
                apTransform.max = _maintainBounds.max;
            }

            if (maintainMode.HasFlag(MaintainMode.MinSize))
                apTransform.minSize = _maintainMinSize;

            // Run processors
            _processors.Sort(new ProcessorComparer());

            for (int i = 0; i < _processors.Count; i++)
            {
                _processors[i].Process(ref apTransform);
            }

            // Force bounds if required
            if (_forceMaintain)
            {
                if (_maintainMode.HasFlag(MaintainMode.MinSize))
                    apTransform.minSize = Vector2.Max(apTransform.minSize, _maintainMinSize);

                if (_maintainMode.HasFlag(MaintainMode.Bounds))
                    apTransform.AddMinMax(_maintainBounds.min, _maintainBounds.max);
            }

            return apTransform;
        }

        private void GetPose(AdaptivePortalTransform apTransform, out Vector2 min, out Vector2 max, out float depth)
        {
            // Get final min and max
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MaxValue, float.MaxValue);
            depth = apTransform.entryDepth;

            if (apTransform.min.x <= apTransform.max.x)
            {
                min.x = apTransform.min.x;
                max.x = apTransform.max.x;
            }
            else if (_maintainMode.HasFlag(MaintainMode.Bounds))
                min.x = max.x = _maintainBounds.center.x;
            else
                min.x = max.x = _offset.localPosition.x;

            if (apTransform.min.y < apTransform.max.y)
            {
                min.y = apTransform.min.y;
                max.y = apTransform.max.y;
            }
            else if (_maintainMode.HasFlag(MaintainMode.Bounds))
                min.y = max.y = _maintainBounds.center.y;
            else
                min.y = max.y = _offset.localPosition.y;

            // Apply min size
            Vector2 padding = (apTransform.minSize - (max - min)) * 0.5f;

            if (padding.x > 0f)
            {
                min.x -= padding.x;
                max.x += padding.x;
            }

            if (padding.y > 0f)
            {
                min.y -= padding.y;
                max.y += padding.y;
            }
        }

        private void ApplyTransform(Vector2 min, Vector2 max, float depth)
        {
            if (_offset)
            {
                if (_transition == Transition.MoveTowards)
                {
                    Vector2 currentMin = new Vector2(_offset.localPosition.x - _offset.localScale.x * 0.5f, _offset.localPosition.y - _offset.localScale.y * 0.5f),
                        currentMax = new Vector2(_offset.localPosition.x + _offset.localScale.x * 0.5f, _offset.localPosition.y + _offset.localScale.y * 0.5f);

                    float distance = _transitionSpeed * Time.deltaTime;

                    min.x = Mathf.MoveTowards(currentMin.x, min.x, distance);
                    min.y = Mathf.MoveTowards(currentMin.y, min.y, distance);
                    max.x = Mathf.MoveTowards(currentMax.x, max.x, distance);
                    max.y = Mathf.MoveTowards(currentMax.y, max.y, distance);

                    depth = Mathf.MoveTowards(_offset.localPosition.z, depth, distance);
                }

                Vector2 center = (min + max) * 0.5f, size = max - min;

                _offset.localPosition = new Vector3(center.x, center.y, depth);
                _offset.localScale = new Vector3(size.x, size.y, 1f);
            }
        }
    }
}