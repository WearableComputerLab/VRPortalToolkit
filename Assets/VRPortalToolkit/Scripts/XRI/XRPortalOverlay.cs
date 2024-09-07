using Misc.EditorHelpers;
using System;
using TMPro;
using UnityEditorInternal.VersionControl;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Data;
using VRPortalToolkit.Rendering;
using static VRPortalToolkit.XRI.XRPointAndPortal;

namespace VRPortalToolkit.XRI
{
    public class XRPortalOverlay : PortalRendererBase
    {
        private static Mesh _circleMesh;
        private static Mesh _squareMesh;

        [SerializeField] private Transform _origin;
        public Transform origin
        {
            get => _origin;
            set => _origin = value;
        }

        public enum Transition
        {
            None = 0,
            Circle = 1,
            Square = 2,
        }

        [SerializeField] private Transition _transition;
        public Transition transition
        {
            get => _transition;
            set => _transition = value;
        }

#if UNITY_EDITOR
        private bool isAnimated => _transition != Transition.None;

        [ShowIf(nameof(isAnimated))]
#endif
        [SerializeField] private float _transitionTime = 1f;
        public float transitionTime
        {
            get => _transitionTime;
            set => _transitionTime = value;
        }

        [SerializeField] private bool _requireSelected = true;
        public bool requireSelected
        {
            get => _requireSelected;
            set => _requireSelected = value;
        }

        [Flags]
        public enum Trigger
        {
            None = 0,
            IsActivated = 1 << 1,
            DirectionInUse = 1 << 2,
            VelocityThreshold = 1 << 3,
            ManualTrigger = 1 << 4,
        }

        [SerializeField] private Trigger _triggers = Trigger.DirectionInUse;
        public Trigger triggers
        {
            get => _triggers;
            set => _triggers = value;
        }

#if UNITY_EDITOR
        private bool showVelocity => _triggers.HasFlag(Trigger.VelocityThreshold);

        [ShowIf(nameof(showVelocity))]
#endif
        [SerializeField] private float _velocityThreshold = 0.1f;
        public float velocityThreshold
        {
            get => _velocityThreshold;
            set => _velocityThreshold = value;
        }

#if UNITY_EDITOR
        [ShowIf(nameof(showVelocity))]
#endif
        [SerializeField] private float _angularVelocityThreshold = 30f;
        public float angularVelocityThreshold
        {
            get => _angularVelocityThreshold;
            set => _angularVelocityThreshold = value;
        }

#if UNITY_EDITOR
        private bool showManual => _triggers.HasFlag(Trigger.ManualTrigger);

        [ShowIf(nameof(showManual))]
#endif
        [SerializeField] private bool _manualTrigger;
        public bool manualTrigger
        {
            get => _manualTrigger;
            set => _manualTrigger = value;
        }

        [SerializeField] private float _overlayTime = 3f;
        public float overlayTime
        {
            get => _overlayTime;
            set => _overlayTime = value;
        }

        [Serializable]
        private struct RaycastClipping
        {
            public LayerMask raycastMask;
            public QueryTriggerInteraction raycastTriggerInteraction;
            public float raycastRadius;
            public float clippingOffset;
        }

        [SerializeField] private RaycastClipping _raycastClipping = new RaycastClipping() { clippingOffset = 0.1f };

        public LayerMask raycastMask
        {
            get => _raycastClipping.raycastMask;
            set => _raycastClipping.raycastMask = value;
        }

        public QueryTriggerInteraction raycastTriggerInteraction
        {
            get => _raycastClipping.raycastTriggerInteraction;
            set => _raycastClipping.raycastTriggerInteraction = value;
        }

        public float raycastRadius
        {
            get => _raycastClipping.raycastRadius;
            set => _raycastClipping.raycastRadius = value;
        }

        public float clippingOffset
        {
            get => _raycastClipping.clippingOffset;
            set => _raycastClipping.clippingOffset = value;
        }

        [SerializeField] private PortalRendererSettings _overrides;
        public PortalRendererSettings overrides
        {
            get => _overrides;
            set => _overrides = value;
        }
        public override PortalRendererSettings Overrides => _overrides;

        public override IPortal Portal => _interactable?.portal;

        private XRPortalInteractable _interactable;
        private IXRSelectInteractor _interactor;
        private XRBaseController _interactorController;
        private float _transitionState;
        private float _triggeredTimer;
        private bool _triggered;

        private bool _isActivating;

        protected virtual void Awake()
        {
            _interactable = GetComponent<XRPortalInteractable>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _interactable.activated.AddListener(OnActivated);
            _interactable.deactivated.AddListener(OnDeactivated);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _interactable.activated.RemoveListener(OnActivated);
            _interactable.deactivated.RemoveListener(OnDeactivated);
        }
        private void OnActivated(ActivateEventArgs args) => _isActivating = true;

        private void OnDeactivated(DeactivateEventArgs args) => _isActivating = false;

        private void LateUpdate()
        {
            if (_interactor == null && _interactable.isSelected)
            {
                _interactor = _interactable.interactorsSelecting[0];
                _interactorController = _interactor.transform.gameObject.GetComponentInParent<XRBaseController>();
            }
            else
                _interactor = null;

            if (_triggered)
            {
                _triggeredTimer += Time.deltaTime;

                if (_triggeredTimer > _overlayTime)
                    _triggered = false;
            }

            if (HasTrigger())
            {
                _triggeredTimer = 0f;
                _triggered = true;
            }

            if (_requireSelected && !_interactable.isSelected)
                _triggered = false;

            // Update transition
            if (_transition == Transition.None)
                _transitionState = _triggered ? 1f : 0f;
            else
            {
                float step = _transitionTime == 0 ? 1f : (Time.deltaTime / _transitionTime);

                if (_triggered)
                    _transitionState = Mathf.Min(1f, _transitionState + step);
                else
                    _transitionState = Mathf.Max(0f, _transitionState - step);
            }
        }

        private bool HasTrigger()
        {
            if (_triggers.HasFlag(Trigger.ManualTrigger) && manualTrigger)
                return true;

            if (_triggers.HasFlag(Trigger.IsActivated) && _isActivating)
                return true;
            
            if (_triggers.HasFlag(Trigger.DirectionInUse) && _interactorController && _interactorController is ActionBasedController controller)
            {
                InputAction action = controller.directionalAnchorRotationAction.action;
                
                if (action != null && action.ReadValue<Vector2>() != Vector2.zero)
                    return true;
            }

            if (_triggers.HasFlag(Trigger.VelocityThreshold))
            {
                // Note is easier to check if the entry portal has moved than it is to check if the exit has, but really its the exits velocity that matters so...
                if (_interactable.connected && _interactable.connected.linkedMovement != XRPortalInteractable.LinkedMovement.None)
                {
                    if (XRUtils.GetThrowingVelocity(_interactable).magnitude > _velocityThreshold)
                        return true;

                    if (XRUtils.GetThrowingAngularVelocity(_interactable).magnitude > _angularVelocityThreshold)
                        return true;
                }
            }

            return false;
        }

        public override bool TryGetWindow(PortalRenderNode renderNode, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 proj, out ViewWindow innerWindow)
        {
            if (isActiveAndEnabled && _transitionState > 0f && renderNode.depth == 0)// && renderNode.camera.cameraType != CameraType.SceneView && renderNode.camera.cameraType != CameraType.Preview)
            {
                if (_transitionState < 1f && _transition != Transition.None)
                {
                    Matrix4x4 localToWorld = GetTransitionLocalToWorld(renderNode.camera);

                    if (transition == Transition.Circle)
                        innerWindow = ViewWindow.GetWindow(view, proj, GetCircleMesh().bounds, localToWorld);
                    else
                        innerWindow = ViewWindow.GetWindow(view, proj, GetSquareMesh().bounds, localToWorld);

                    return innerWindow.IsValid();
                }

                innerWindow = new ViewWindow(1f, 1f, 0f);
                return true;
            }

            innerWindow = default;
            return false;
        }

        public override void RenderDefault(PortalRenderNode renderNode, CommandBuffer commandBuffer)
        {
            // Do not render default overlays
            return;
        }

        public override void Render(PortalRenderNode renderNode, CommandBuffer commandBuffer, Material material, MaterialPropertyBlock properties = null)
        {
            if (isActiveAndEnabled)
            {
                //Debug.Log("B");
                //if (_transitionState >= 1 || _transition == Transition.None)
                //    commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, 0, properties);
                //else
                commandBuffer.DrawMesh(_transition == Transition.Circle ? GetCircleMesh() : GetSquareMesh(),
                    GetTransitionLocalToWorld(renderNode.camera), material, 0, 0, properties);
            }
        }

        private Matrix4x4 GetTransitionLocalToWorld(Camera camera)
        {
            Vector3 originPosition = _origin ? _origin.position : transform.position;

            float clipPlane = camera.nearClipPlane + 0.1f;

            originPosition = camera.WorldToViewportPoint(originPosition);
            originPosition = camera.ViewportToWorldPoint(new Vector3(originPosition.x, originPosition.y, Mathf.Max(clipPlane, originPosition.z)));

            float scale = Vector3.Distance(camera.ViewportToWorldPoint(new Vector3(0f, 0f, clipPlane)), new Vector3(1f, 1f, clipPlane));

            Matrix4x4 localToWorld = Matrix4x4.TRS(
                Vector3.Lerp(originPosition, camera.ViewportToWorldPoint(new Vector3(0f, 0f, clipPlane)), _transitionState),
                Quaternion.LookRotation(-camera.transform.forward, camera.transform.up),
                Vector3.one * _transitionState * scale);
            return localToWorld;
        }

        public override bool TryGetClippingPlane(PortalRenderNode renderNode, out Vector3 clippingPlaneCentre, out Vector3 clippingPlaneNormal)
        {
            Vector3 start = renderNode.teleportMatrix.MultiplyPoint(_origin ? _origin.position : transform.position),
                end = renderNode.localToWorldMatrix.GetColumn(3);

            Ray ray = new Ray(start, end - start);

            if (Raycast(start, end, ray, out RaycastHit hitinfo))
            {
                clippingPlaneCentre = start + ray.direction * (hitinfo.distance - clippingOffset);
                clippingPlaneNormal = hitinfo.normal;
                return true;
            }

            clippingPlaneCentre = default;
            clippingPlaneNormal = default;
            return false;
        }

        private bool Raycast(Vector3 start, Vector3 end, Ray ray, out RaycastHit hitinfo)
        {
            if (raycastRadius > 0)
                return UnityEngine.Physics.SphereCast(ray, raycastRadius, out hitinfo, Vector3.Distance(start, end), raycastMask, raycastTriggerInteraction);

            return UnityEngine.Physics.Raycast(ray, out hitinfo, Vector3.Distance(start, end), raycastMask, raycastTriggerInteraction);
        }

        private const int circleVertexCount = 32;

        private static Mesh GetCircleMesh()
        {
            if (!_circleMesh)
            {
                // https://gist.github.com/olegknyazev/50dbe786ba14ec1a71af801856acaf7e
                _circleMesh = new Mesh();
                Vector3[] vertices = new Vector3[circleVertexCount];
                Vector2[] uvs = new Vector2[circleVertexCount];
                int[] indices = new int[(vertices.Length - 2) * 3];
                float segmentWidth = Mathf.PI * 2f / (circleVertexCount - 2);
                float angle = 0f;

                uvs[0] = new Vector2(0.5f, 0.5f);

                for (int i = 1; i < circleVertexCount; i++)
                {
                    Vector2 vertex = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.5f;
                    vertices[i] = new Vector3(vertex.x, vertex.y, 0f);
                    uvs[i] = new Vector2(vertex.x + 0.5f, vertex.y + 0.5f);

                    angle -= segmentWidth;

                    if (i > 1)
                    {
                        int j = (i - 2) * 3;
                        indices[j + 0] = 0;
                        indices[j + 1] = i - 1;
                        indices[j + 2] = i;
                    }
                }

                _circleMesh.SetVertices(vertices);
                _circleMesh.SetUVs(0, uvs);
                _circleMesh.SetIndices(indices, MeshTopology.Triangles, 0);
                _circleMesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, 1f));
            }

            return _circleMesh;
        }

        private static Mesh GetSquareMesh()
        {
            if (!_squareMesh)
            {
                _squareMesh = new Mesh();

                Vector3[] vertices = new Vector3[4]
                {
                    new Vector3(-0.5f, -0.5f),
                    new Vector3(-0.5f, 0.5f),
                    new Vector3(0.5f, -0.5f),
                    new Vector3(0.5f, 0.5f)
                };

                int[] triangles = new int[6] { 0, 1, 2, 1, 3, 2 };

                Vector2[] uvs = new Vector2[4] {
                    new Vector2 (0f, 0f),
                    new Vector2 (0f, 1f),
                    new Vector2 (1f, 0f),
                    new Vector2 (1f, 1f)
                };

                _squareMesh.SetVertices(vertices);
                _squareMesh.SetUVs(0, uvs);
                _squareMesh.SetIndices(triangles, MeshTopology.Triangles, 0);
                _squareMesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, 1f));
            }

            return _squareMesh;
        }
    }
}
