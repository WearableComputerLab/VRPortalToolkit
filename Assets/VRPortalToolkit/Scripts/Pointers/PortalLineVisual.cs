using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit
{
    public interface IPortalLineRenderable
    {
        int portalRayCount { get; }

        PortalRay GetPortalRay(int portalRayIndex);

        bool TryGetHitInfo(out Vector3 position, out Vector3 normal, out int portalRayIndex, out bool isValidTarget);
    }

    [RequireComponent(typeof(LineRenderer))]
    public class PortalLineVisual : MonoBehaviour
    {
        private static readonly List<PortalRay> _portalRays = new List<PortalRay>();
        private static readonly List<float> _lengths = new List<float>();

        private static readonly List<Vector3> _points = new List<Vector3>();

        [SerializeField] private LineRenderer _lineRenderer;
        public LineRenderer lineRenderer
        {
            get => _lineRenderer;
            set => _lineRenderer = value;
        }

        [SerializeField] private float _lineWidth = 0.02f;
        public float lineWidth
        {
            get => _lineWidth;
            set => _lineWidth = value;
        }

        [SerializeField] private AnimationCurve _widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        public AnimationCurve widthCurve
        {
            get => _widthCurve;
            set => _widthCurve = value;
        }

        [SerializeField]
        private Gradient _validColor = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) },
        };
        public Gradient validColorGradient
        {
            get => _validColor;
            set => _validColor = value;
        }

        [SerializeField]
        private Gradient _invalidColor = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.red, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) },
        };
        public Gradient invalidColorGradient
        {
            get => _invalidColor;
            set => _invalidColor = value;
        }

        [SerializeField] private bool _stopAtContact = true;
        public bool stopAtContact
        {
            get => _stopAtContact;
            set => _stopAtContact = value;
        }

        public IPortalLineRenderable lineRenderable => _lineRenderable;

        private int _currentLength;
        private IPortalLineRenderable _lineRenderable;
        private MaterialPropertyBlock _propertyBlock;
        private readonly List<LineRenderer> _lineRenderers = new List<LineRenderer>();

        private GradientColorKey[] _colorKeys;
        private GradientAlphaKey[] _alphaKeys;
        private Keyframe[] _keyframes;

        protected virtual void Reset()
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        protected virtual void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _lineRenderable = GetComponent<IPortalLineRenderable>();
            if (_lineRenderable == null) Debug.LogError("IPortalLineRenderable not found!");
        }

        protected virtual void OnEnable()
        {
            if (_lineRenderer) _lineRenderer.enabled = true;

            for (int i = 0; i < _currentLength; i++)
            {
                LineRenderer renderer = _lineRenderers[i];
                if (renderer) renderer.gameObject.SetActive(true);
            }
        }

        protected virtual void OnDisable()
        {
            if (_lineRenderer) _lineRenderer.enabled = false;

            for (int i = 0; i < _currentLength; i++)
            {
                LineRenderer renderer = _lineRenderers[i];
                if (renderer) renderer.gameObject.SetActive(false);
            }
        }

        protected virtual void LateUpdate()
        {
            LineRenderer renderer = _lineRenderer;
            if (renderer) renderer.enabled = false;
            _currentLength = 0;

            _lengths.Clear();
            _points.Clear();
            _portalRays.Clear();

            if (_lineRenderer && _lineRenderable != null)
            {
                int portalRaysCount = _lineRenderable.portalRayCount;

                if (portalRaysCount > 0)
                {
                    bool hit = _lineRenderable.TryGetHitInfo(out Vector3 position, out Vector3 normal, out int portalRayIndex, out bool isValidTarget);

                    _lineRenderer.GetPropertyBlock(_propertyBlock);
                    Material[] materials = renderer.sharedMaterials;

                    int usedLength = hit && _stopAtContact ? portalRayIndex + 1 : portalRaysCount;

                    for (int i = 0; i < usedLength; i++)
                        _portalRays.Add(_lineRenderable.GetPortalRay(i));

                    bool hasPortal = false;

                    PortalRay from = _portalRays[0], to;
                    float length, maxLength = 0f;

                    for (int i = 0; i < usedLength; i++)
                    {
                        hasPortal |= _portalRays[i].fromPortal != null;

                        if (i == usedLength - 1)
                        {
                            if (hit && stopAtContact)
                                length = Vector3.Distance(from.origin, position);
                            else
                                length = from.direction.magnitude;
                        }
                        else
                        {
                            to = _portalRays[i + 1];
                            length = to.fromPortal == null ? Vector3.Distance(from.origin, to.origin) : from.direction.magnitude;
                            from = to;
                        }

                        _lengths.Add(length);
                        maxLength += length;
                    }


                    if (maxLength > 0f)
                    {
                        Gradient gradient = isValidTarget ? _validColor : _invalidColor;

                        var colorKeys = gradient.colorKeys;
                        var alphaKeys = gradient.alphaKeys;
                        var curveKeys = widthCurve.keys;

                        PortalRay from1 = _portalRays[0], to1;
                        _points.Add(from1.origin);

                        float start = 0f, endLength = 0f;
                        for (int i = 0; i < usedLength - 1; i++)
                        {
                            endLength += _lengths[i];
                            to1 = _portalRays[i + 1];

                            if (to1.fromPortal != null)
                            {
                                _points.Add(from1.origin + from1.direction);
                                renderer.widthMultiplier = _lineWidth;
                                SetPoints(renderer, ref start, endLength / maxLength, gradient, colorKeys, alphaKeys, curveKeys);

                                // Start a new renderer
                                _points.Add(to1.origin);
                                renderer = GetLineRenderer(_currentLength++);
                                renderer.sharedMaterials = materials;
                                renderer.SetPropertyBlock(_propertyBlock);
                                renderer.alignment = _lineRenderer.alignment;
                                renderer.allowOcclusionWhenDynamic = _lineRenderer.allowOcclusionWhenDynamic;
                                renderer.forceRenderingOff = _lineRenderer.forceRenderingOff;
                                renderer.generateLightingData = _lineRenderer.generateLightingData;
                                renderer.lightmapIndex = _lineRenderer.lightmapIndex;
                                renderer.lightmapScaleOffset = _lineRenderer.lightmapScaleOffset;
                                renderer.rayTracingMode = _lineRenderer.rayTracingMode;
                                renderer.useWorldSpace = _lineRenderer.useWorldSpace;

                                renderer.colorGradient.mode = gradient.mode;
                                renderer.widthCurve.preWrapMode = renderer.widthCurve.preWrapMode;
                                renderer.widthCurve.postWrapMode = renderer.widthCurve.postWrapMode;
                            }
                            else
                                _points.Add(to1.origin);

                            from1 = to1;
                        }

                        endLength += _lengths[usedLength - 1];
                        _points.Add(hit && stopAtContact ? position : from.origin + from.direction);

                        renderer.widthMultiplier = _lineWidth;
                        SetPoints(renderer, ref start, endLength / maxLength, gradient, colorKeys, alphaKeys, curveKeys);
                    }
                }
            }

            for (int i = _currentLength; i < _lineRenderers.Count; i++)
            {
                renderer = _lineRenderers[i];
                if (renderer) renderer.gameObject.SetActive(false);
            }
        }

        private void SetPoints(LineRenderer renderer, ref float start, float end, Gradient gradient, GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys, Keyframe[] keyframes)
        {
            renderer.positionCount = _points.Count;

            for (int i = 0; i < _points.Count; i++)
                renderer.SetPosition(i, _points[i]);

            if (_colorKeys == null || _colorKeys.Length != colorKeys.Length)
                _colorKeys = new GradientColorKey[colorKeys.Length];

            GradientColorKey startColor = new GradientColorKey(gradient.Evaluate(start), start),
                endColor = new GradientColorKey(gradient.Evaluate(end), end);

            // Do color
            for (int i = 0; i < _colorKeys.Length; i++)
            {
                GradientColorKey key = colorKeys[i];

                if (key.time < start)
                    _colorKeys[i] = startColor;
                else if (key.time > end)
                    _colorKeys[i] = endColor;
                else
                {
                    key.time = Mathf.InverseLerp(start, end, key.time);
                    _colorKeys[i] = key;
                }
            }

            if (_alphaKeys == null || _alphaKeys.Length != alphaKeys.Length)
                _alphaKeys = new GradientAlphaKey[alphaKeys.Length];

            GradientAlphaKey startAlpha = new GradientAlphaKey(startColor.color.a, start),
                endAlpha = new GradientAlphaKey(endColor.color.a, end);

            // Do alpha
            for (int i = 0; i < _alphaKeys.Length; i++)
            {
                GradientAlphaKey key = alphaKeys[i];

                if (key.time < start)
                    _alphaKeys[i] = startAlpha;
                else if (key.time > end)
                    _alphaKeys[i] = endAlpha;
                else
                {
                    key.time = Mathf.InverseLerp(start, end, key.time);
                    _alphaKeys[i] = key;
                }
            }

            // Do keyframes
            if (_keyframes == null || _keyframes.Length != keyframes.Length)
                _keyframes = new Keyframe[keyframes.Length];

            for (int i = 0; i < _keyframes.Length; i++)
            {
                Keyframe key = keyframes[i];
                key.time = InverseLerpUnclamped(start, end, key.time);
                _keyframes[i] = key;
            }

            // Need to set it this way for the effect to kick in
            Gradient colorGradient = renderer.colorGradient;
            colorGradient.SetKeys(_colorKeys, _alphaKeys);
            renderer.colorGradient = colorGradient;

            // Need to set it this way for the effect to kick in
            AnimationCurve widthCurve = renderer.widthCurve;
            widthCurve.keys = _keyframes;
            renderer.widthCurve = widthCurve;

            renderer.gameObject.SetActive(true);
            renderer.enabled = true;

            start = end;
            _points.Clear();
        }

        private static float InverseLerpUnclamped(float a, float b, float value) => (value - a) / (b - a);

        private LineRenderer GetLineRenderer(int index)
        {
            LineRenderer renderer;

            while (_lineRenderers.Count <= index)
            {
                GameObject lineObject = new GameObject("LineRender");
                lineObject.SetActive(false);
                lineObject.transform.SetParent(_lineRenderer ? _lineRenderer.transform : transform, false);
                renderer = lineObject.AddComponent<LineRenderer>();
                _lineRenderers.Add(renderer);
            }

            renderer = _lineRenderers[index];
            renderer.enabled = false;
            return renderer;
        }
    }
}
