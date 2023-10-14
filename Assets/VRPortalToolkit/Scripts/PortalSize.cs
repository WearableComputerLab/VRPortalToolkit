using Misc.EditorHelpers;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit
{
    public interface IPortalRectRequester
    {
        bool TryGetRect(out Rect rect);
    }

    public interface IPortalSizeRequester
    {
        bool TryGetSize(out Vector2 size);
    }

    /// <summary>
    /// This class is designed so that different classes can request the size and position the want the portal to be.
    /// </summary>
    public class PortalSize : MonoBehaviour
    {
        [SerializeField] private PortalSize _connected;
        public PortalSize connected
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

        private readonly List<IPortalSizeRequester> _sizeRequesters = new List<IPortalSizeRequester>();
        public List<IPortalSizeRequester> sizeRequesters => _sizeRequesters;

        private readonly List<IPortalRectRequester> _rectRequesters = new List<IPortalRectRequester>();
        public List<IPortalRectRequester> rectRequesters => _rectRequesters;

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
            
            foreach (IPortalRectRequester requester in _rectRequesters)
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

            foreach (IPortalSizeRequester requester in _sizeRequesters)
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
