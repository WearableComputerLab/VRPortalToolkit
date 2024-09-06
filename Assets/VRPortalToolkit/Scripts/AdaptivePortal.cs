using Misc.EditorHelpers;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


namespace VRPortalToolkit
{
    public struct AdaptivePortalTransform
    {
        public Vector2 minSize;

        public Vector2 min;

        public Vector2 max;

        public float entryDepth;

        public float exitDepth;

        public static readonly AdaptivePortalTransform identity = new AdaptivePortalTransform()
        {
            min = new Vector2(float.MaxValue, float.MaxValue),
            max = new Vector2(float.MinValue, float.MinValue),
        };

        public AdaptivePortalTransform inverse => new AdaptivePortalTransform()
        {
            minSize = minSize,
            min = new Vector2(-max.x, min.y),
            max = new Vector2(-min.x, max.y),
            entryDepth = -exitDepth,
            exitDepth = -entryDepth,
        };

        public void AddMinMax(Vector2 min, Vector2 max)
        {
            this.min = Vector2.Min(this.min, min);
            this.max = Vector2.Max(this.max, max);
        }
    }

    public interface IAdaptivePortalProcessor
    {
        int Order { get; }

        void Process(ref AdaptivePortalTransform apTransform);
    }

    /// <summary>
    /// This class is designed so that different classes can request the size and position the want the portal to be.
    /// </summary>
    public class AdaptivePortal : MonoBehaviour
    {
        [SerializeField] private AdaptivePortal _connected;
        public AdaptivePortal connected
        {
            get => _connected;
            set => _connected = value;
        }

        [SerializeField] private Transform _offset;
        public Transform offset
        {
            get => _offset;
            set => _offset = value;
        }

        public enum MaintainMode
        {
            None = 0,
            MinSize = 1 << 0,
            Bounds = 2 << 0,
        }

        [SerializeField] private MaintainMode _maintainMode = MaintainMode.Bounds;
        public MaintainMode maintainMode
        {
            get => _maintainMode;
            set => _maintainMode = value;
        }

#if UNITY_EDITOR
        private bool showRect => _maintainMode.HasFlag(MaintainMode.Bounds);
        [ShowIf(nameof(showRect))]
#endif
        [SerializeField] private Rect _maintainBounds = new Rect(-0.5f, -0.5f, 1f, 1f);
        public Rect maintainBounds
        {
            get => _maintainBounds;
            set => _maintainBounds = value;
        }

#if UNITY_EDITOR
        private bool showSize => _maintainMode.HasFlag(MaintainMode.MinSize);
        [ShowIf(nameof(showSize))]
#endif
        [SerializeField] private Vector2 _maintainMinSize = new Vector2(1f, 1f);
        public Vector2 maintainMinSize
        {
            get => _maintainMinSize;
            set => _maintainMinSize = value;
        }

#if UNITY_EDITOR
        private bool showForce => _maintainMode != MaintainMode.None;
        [ShowIf(nameof(showForce))]
#endif
        [SerializeField] private bool _forceMaintain = false;
        public bool forceMaintain
        {
            get => _forceMaintain;
            set => _forceMaintain = value;
        }

        [SerializeField] private Transition _transition;
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

        [ShowIf(nameof(_transition), Transition.MoveTowards)]
        [SerializeField] private float _transitionSpeed = 1f;
        public float transitionSpeed
        {
            get => _transitionSpeed;
            set => _transitionSpeed = value;
        }

        private float _lastTime = float.MinValue;

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
                    /*AdaptivePortalTransform apTransform = GetPose(), connectedTransform = _connected.GetPose().inverse;

                    apTransform.minSize = connectedTransform.minSize = Vector2.Max(apTransform.minSize, connectedTransform.minSize);
                    apTransform.min = connectedTransform.min = Vector2.Min(apTransform.min, connectedTransform.min);
                    apTransform.max = connectedTransform.max = Vector2.Max(apTransform.max, connectedTransform.max);*/

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

            _lastTime = Time.time;
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
                    //min = Vector2.MoveTowards(min, rect.min, distance);
                    //max = Vector2.MoveTowards(max, rect.max, distance);

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

        private static Rect ExpandRect(Rect rect, Vector2 minSize)
        {
            if (minSize.x > rect.width)
            {
                float midX = rect.x + rect.width * 0.5f;
                rect.width = minSize.x;
                rect.x = midX - minSize.x * 0.5f;
            }

            if (minSize.y > rect.height)
            {
                float midY = rect.y + rect.height * 0.5f;
                rect.height = minSize.y;
                rect.x = midY - minSize.y * 0.5f;
            }

            return rect;
        }
    }
}

/*
namespace VRPortalToolkit
{
    public interface IAdaptivePortalRequester
    {
        //int Priority { get; }
    }

    public interface IAdaptivePortalRectRequester : IAdaptivePortalRequester
    {
        bool TryGetRect(out Rect rect);
    }

    public interface IAdaptivePortalSizeRequester : IAdaptivePortalRequester
    {
        bool TryGetSize(out Vector2 size);
    }

    *//*public interface IAdaptivePortalDepthRequester : IAdaptivePortalRequester
    {
        bool TryGetDepth(out Vector2 size);
    }*//*

    /// <summary>
    /// This class is designed so that different classes can request the size and position the want the portal to be.
    /// </summary>
    public class AdaptivePortalSize : MonoBehaviour
    {
        [SerializeField] private AdaptivePortalSize _connected;
        public AdaptivePortalSize connected
        {
            get => _connected;
            set => _connected = value;
        }

        [SerializeField] private Transform _offset;
        public Transform offset
        {
            get => _offset;
            set => _offset = value;
        }

        [SerializeField] private Rect _maintainRect = new Rect(-0.5f, -0.5f, 1f, 1f);
        public Rect maintainRect
        {
            get => _maintainRect;
            set => _maintainRect = value;
        }

        [SerializeField] private MaintainMode _maintainMode;
        public MaintainMode maintainMode
        {
            get => _maintainMode;
            set => _maintainMode = value;
        }

        public enum MaintainMode
        {
            Rect = 0,
            Move = 1,
            Size = 2,
        }

        [SerializeField] private Transition _transition;
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

        [ShowIf(nameof(_transition), Transition.MoveTowards)]
        [SerializeField] private float _transitionSpeed = 1f;
        public float transitionSpeed
        {
            get => _transitionSpeed;
            set => _transitionSpeed = value;
        }

        private float _lastTime = float.MinValue;

        private readonly List<IAdaptivePortalSizeRequester> _sizeRequesters = new List<IAdaptivePortalSizeRequester>();
        public List<IAdaptivePortalSizeRequester> sizeRequesters => _sizeRequesters;

        private readonly List<IAdaptivePortalRectRequester> _rectRequesters = new List<IAdaptivePortalRectRequester>();
        public List<IAdaptivePortalRectRequester> rectRequesters => _rectRequesters;

        //private readonly List<IAdaptivePortalDepthRequester> _depthRequesters = new List<IAdaptivePortalDepthRequester>();
        //public List<IAdaptivePortalDepthRequester> depthRequesters => _depthRequesters;

        protected virtual void Awake()
        {
            GetComponentsInChildren(_rectRequesters);
            GetComponentsInChildren(_sizeRequesters);
        }

        protected virtual void LateUpdate()
        {
            // Only update if connected hasn't. This keeps scale in sync
            if (!_connected || _connected._lastTime != Time.time)
                UpdatePose();

            _lastTime = Time.time;
        }

        private void UpdatePose()
        {
            bool hasRect = TryGetRect(out Rect rect);

            if (_connected && _connected.TryGetRect(out Rect otherRect))
            {
                // We flip the other on the x axis
                otherRect = Flip(otherRect);

                if (hasRect)
                    rect = CombineRects(rect, otherRect);
                else
                {
                    hasRect = true;
                    rect = otherRect;
                }
            }

            // 
            bool hasMinSize = TryGetSize(out Vector2 minSize);

            if (_connected && _connected.TryGetSize(out Vector2 otherSize))
            {
                if (hasRect)
                    minSize = Vector2.Max(otherSize, minSize);
                else
                {
                    hasRect = true;
                    minSize = otherSize;
                }
            }

            // If there is no rect, then there must be a size
            if (!hasRect)
            {
                if (_maintainMode == MaintainMode.Move)
                {
                    if (_connected && _connected._maintainMode == MaintainMode.Move)
                        rect = CreateMoveSizeRect(minSize, CombineRects(rect, Flip(_connected._maintainRect)));
                    else
                        rect = CreateMoveSizeRect(minSize, rect);
                }
                else if (_connected && _connected._maintainMode == MaintainMode.Move)
                    rect = CreateMoveSizeRect(minSize, Flip(_connected._maintainRect));
                else
                {
                    rect = new Rect(-minSize * 0.5f, minSize);

                    if (_offset) rect.min += (Vector2)_offset.localPosition;
                }
            }
            else
            {
                if (hasMinSize)
                {
                    if (_maintainMode == MaintainMode.Move)
                    {
                        if (_connected && _connected._maintainMode == MaintainMode.Move)
                            rect = CreateMoveSizeRect(rect, minSize, CombineRects(rect, Flip(_connected._maintainRect)));
                        else
                            rect = CreateMoveSizeRect(rect, minSize, rect);
                    }
                    else if (_connected && _connected._maintainMode == MaintainMode.Move)
                        rect = CreateMoveSizeRect(rect, minSize, Flip(_connected._maintainRect));
                    else
                        rect = ExpandRect(rect, minSize);
                }
            }

            ApplyRect(rect, Time.deltaTime);
            _connected?.ApplyRect(Flip(rect), Time.deltaTime);
        }

        private void ApplyRect(Rect rect, float deltaTime)
        {
            if (_offset)
            {
                if (_transition == Transition.MoveTowards)
                {
                    Vector2 min = new Vector2(_offset.localPosition.x - _offset.localScale.x * 0.5f, _offset.localPosition.y - _offset.localScale.y * 0.5f),
                        max = new Vector2(_offset.localPosition.x + _offset.localScale.x * 0.5f, _offset.localPosition.y + _offset.localScale.y * 0.5f);

                    float distance = _transitionSpeed * deltaTime;
                    //min = Vector2.MoveTowards(min, rect.min, distance);
                    //max = Vector2.MoveTowards(max, rect.max, distance);

                    min.x = Mathf.MoveTowards(min.x, rect.min.x, distance);
                    min.y = Mathf.MoveTowards(min.y, rect.min.y, distance);
                    max.x = Mathf.MoveTowards(max.x, rect.max.x, distance);
                    max.y = Mathf.MoveTowards(max.y, rect.max.y, distance);

                    rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                }

                _offset.localPosition = new Vector3(rect.center.x, rect.center.y, _offset.localPosition.z);
                _offset.localScale = new Vector3(rect.width, rect.height, _offset.localScale.z);
            }
        }

        private static Rect ExpandRect(Rect rect, Vector2 minSize)
        {
            if (minSize.x > rect.width)
            {
                float midX = rect.x + rect.width * 0.5f;
                rect.width = minSize.x;
                rect.x = midX - minSize.x * 0.5f;
            }

            if (minSize.y > rect.height)
            {
                float midY = rect.y + rect.height * 0.5f;
                rect.height = minSize.y;
                rect.x = midY - minSize.y * 0.5f;
            }

            return rect;
        }

        private static Rect CombineRects(Rect a, Rect b)
        {
            Vector2 min = Vector2.Min(a.min, b.min), max = Vector2.Max(a.max, b.max);
            var x = Rect.MinMaxRect(min.x, min.y, max.x, max.y);


            //Debug.Log(a + " + " + b + " = " + x);

            return x;
        }

        private static Rect Flip(Rect rect)
        {
            rect.x = -rect.xMax;
            return rect;
        }

        private static Rect CreateMoveSizeRect(Vector2 size, Rect original) =>
            new Rect(-size * 0.5f + original.center, size);

        private static Rect CreateMoveSizeRect(Rect rect, Vector2 minSize, Rect original)
        {
            Vector2 remainingSize = minSize - rect.size,
                min = rect.min, max = rect.max, center = rect.center,
                originalCenter = original.center;

            if (remainingSize.x > 0)
            {
                if (center.x > originalCenter.x)
                {
                    float dif = center.x - originalCenter.x;
                    min.x = Mathf.Max(min.x - dif * 2f, min.x - remainingSize.x);
                }
                else if(center.x < originalCenter.x)
                {
                    float dif = originalCenter.x - center.x;
                    max.x = Mathf.Min(max.x + dif * 2f, max.x + remainingSize.x);
                }
            }

            if (remainingSize.y > 0)
            {
                if (center.y > originalCenter.y)
                {
                    float dif = center.y - originalCenter.y;
                    min.y = Mathf.Max(min.y - dif * 2f, min.y - remainingSize.y);
                }
                else if (center.y < originalCenter.y)
                {
                    float dif = originalCenter.y - center.y;
                    max.y = Mathf.Min(max.y + dif * 2f, max.y + remainingSize.y);
                }
            }

            Rect newRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            
            // Spreads the remainder out
            return ExpandRect(newRect, minSize);
        }

        private bool TryGetRect(out Rect rect)
        {
            bool hasRect = _maintainMode == MaintainMode.Rect;

            Vector2 min, max;
            
            if (hasRect)
            {
                min = _maintainRect.min;
                max = _maintainRect.max;
            }
            else
            {
                min = new Vector2(float.MaxValue, float.MaxValue);
                max = new Vector2(float.MinValue, float.MinValue);
            }
            
            foreach (IAdaptivePortalRectRequester requester in _rectRequesters)
            {
                if (requester != null && requester.TryGetRect(out Rect other))
                {
                    hasRect = true;
                    min = Vector2.Min(other.min, min);
                    max = Vector2.Max(other.max, max);
                }
            }

            if (hasRect)
            {
                rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                return true;
            }

            rect = default;
            return hasRect;
        }


        private bool TryGetSize(out Vector2 size)
        {
            bool hasSize = _maintainMode == MaintainMode.Size || _maintainMode == MaintainMode.Move;
            size = hasSize ? _maintainRect.size : new Vector2(float.MaxValue, float.MaxValue);

            foreach (IAdaptivePortalSizeRequester requester in _sizeRequesters)
            {
                if (requester != null && requester.TryGetSize(out Vector2 other))
                {
                    hasSize = true;
                    size = Vector2.Max(other, size);
                }
            }

            size = default;
            return hasSize;
        }
    }
}
*/