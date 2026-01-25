using Misc.EditorHelpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.UIElements;
using UnityEngine.XR;


namespace VRPortalToolkit.Rendering.Universal
{

    /// <summary>
    /// The rendering mode to use for portals.
    /// </summary>
    public enum RenderMode
    {
        /// <summary>Uses render textures for portal rendering.</summary>
        RenderTexture = 0,

        /// <summary>Uses stencil buffer before opaque objects for portal rendering (better for shadows).</summary>
        StencilEarly = 1,

        /// <summary>Uses stencil buffer before transparent objects for portal rendering (less overdraw).</summary>
        Stencil = 2,

        /// <summary>Uses stencil buffer after transparent objects for portal rendering.</summary>
        StencilLate = 3
    }

    /// <summary>
    /// Defines the algorithm used for portal rendering traversal.
    /// </summary>
    public enum PortalAlgorithm
    {
        /// <summary>Uses breadth-first traversal for portal rendering.</summary>
        BreadthFirst = 0,

        /// <summary>Uses predictive traversal for portal rendering with prioritization.</summary>
        Predictive = 1
    }

    public class NewPortalRenderFeature : ScriptableRendererFeature
    {
        [Tooltip("The rendering mode for portals (RenderTexture, Stencil, etc).")]
        [SerializeField] private RenderMode _renderMode;
        /// <summary>
        /// The rendering mode for portals (RenderTexture, Stencil, etc).
        /// </summary>
        public RenderMode renderMode
        {
            get => _renderMode;
            set
            {
                if (_renderMode != value)
                {
                    _isDirty = true;
                    Validate.UpdateField(this, nameof(_renderMode), _renderMode = value);
                }
            }
        }

        [Tooltip("The algorithm used for portal rendering traversal.")]
        [SerializeField] private PortalAlgorithm _algorithm = PortalAlgorithm.Predictive;
        /// <summary>
        /// The algorithm used for portal rendering traversal.
        /// </summary>
        public PortalAlgorithm algorithm
        {
            get => _algorithm;
            set => _algorithm = value;
        }

        [Tooltip("The layer mask for opaque objects.")]
        [Header("Filtering"), SerializeField] private LayerMask _opaqueLayerMask = -1;
        /// <summary>
        /// The layer mask for opaque objects.
        /// </summary>
        public LayerMask opaqueLayerMask
        {
            get => _opaqueLayerMask;
            set => _opaqueLayerMask = value;
        }

        [Tooltip("The layer mask for transparent objects.")]
        [SerializeField] private LayerMask _transparentLayerMask = -1;
        /// <summary>
        /// The layer mask for transparent objects.
        /// </summary>
        public LayerMask transparentLayerMask
        {
            get => _transparentLayerMask;
            set => _transparentLayerMask = value;
        }

        private bool _isDirty = false;

        [Tooltip("The minimum portal recursion depth.")]
        [Header("Scene Settings"), SerializeField] private int _minDepth = 1;
        /// <summary>
        /// The minimum portal recursion depth.
        /// </summary>
        public int minDepth
        {
            get => _minDepth > 0 ? _minDepth : _minDepth = 0;
            set => _minDepth = value;
        }

        [Tooltip("The maximum portal recursion depth.")]
        [SerializeField] private int _maxDepth = 32;
        /// <summary>
        /// The maximum portal recursion depth.
        /// </summary>
        public int maxDepth
        {
            get => _maxDepth > 0 ? _maxDepth : _maxDepth = 0;
            set => _maxDepth = value;
        }

        [Tooltip("The maximum number of portal renders per frame.")]
        [SerializeField] private int _maxRenders = 32;
        /// <summary>
        /// The maximum number of portal renders per frame.
        /// </summary>
        public int maxRenders
        {
            get => _maxRenders > 0 ? _maxRenders : _maxRenders = 0;
            set => _maxRenders = value;
        }

        [Tooltip("The maximum shadow recursion depth for portals.")]
        [SerializeField] private int _maxShadowDepth = 16;
        /// <summary>
        /// The maximum shadow recursion depth for portals.
        /// </summary>
        public int maxShadowDepth
        {
            get => _maxShadowDepth > 0 ? _maxShadowDepth : _maxShadowDepth = 0;
            set => _maxShadowDepth = value;
        }

#if UNITY_EDITOR
        private bool showResolution => renderMode != RenderMode.Stencil;

        [ShowIf(nameof(showResolution), true, 1)]
#endif
        [Tooltip("The resolution scale for portal rendering.")]
        [SerializeField, Range(0f, 1f)] private float _portalResolution = 1f;
        /// <summary>
        /// The resolution scale for portal rendering.
        /// </summary>
        public float portalResolution
        {
            get => _portalResolution;
            set => _portalResolution = Mathf.Clamp(value, 0f, 1f);
        }

        //[Tooltip("The resolution scale for the buffer effect.")]
        //[SerializeField, Range(0f, 1f)] private float _bufferResolution = 1f;
        ///// <summary>
        ///// The resolution scale for the buffer effect.
        ///// </summary>
        //public float bufferResolution
        //{
        //    get => _bufferResolution;
        //    set => _bufferResolution = Mathf.Clamp(value, 0f, 1f);
        //}

        [Tooltip("The minimum portal recursion depth in the editor.")]
        [Header("Editor Settings"), SerializeField] private int _editorMinDepth = 0;
        /// <summary>
        /// The minimum portal recursion depth in the editor.
        /// </summary>
        public int editorMinDepth
        {
            get => _editorMinDepth > 0 ? _editorMinDepth : _editorMinDepth = 0;
            set => _editorMinDepth = value;
        }

        [Tooltip("The maximum portal recursion depth in the editor.")]
        [SerializeField] private int _editorMaxDepth = 16;
        /// <summary>
        /// The maximum portal recursion depth in the editor.
        /// </summary>
        public int editorMaxDepth
        {
            get => _editorMaxDepth > 0 ? _editorMaxDepth : _editorMaxDepth = 0;
            set => _editorMaxDepth = value;
        }

        [Tooltip("The maximum number of portal renders per frame in the editor.")]
        [SerializeField] private int _editorMaxRenders = 16;
        /// <summary>
        /// The maximum number of portal renders per frame in the editor.
        /// </summary>
        public int editorMaxRenders
        {
            get => _editorMaxRenders > 0 ? _editorMaxRenders : _editorMaxRenders = 0;
            set => _editorMaxRenders = value;
        }

        [Tooltip("The maximum shadow recursion depth for portals in the editor.")]
        [SerializeField] private int _editorMaxShadowDepth = 16;
        /// <summary>
        /// The maximum shadow recursion depth for portals in the editor.
        /// </summary>
        public int editorMaxShadowDepth
        {
            get => _editorMaxShadowDepth > 0 ? _editorMaxShadowDepth : _editorMaxShadowDepth = 0;
            set => _editorMaxShadowDepth = value;
        }

#if UNITY_EDITOR
        [ShowIf(nameof(showResolution), true, 1)]
#endif
        [Tooltip("")]
        [SerializeField, Range(0f, 1f)] private float _editorPortalResolution = 1f;
        /// <summary>
        /// The resolution scale for portal rendering in the editor.
        /// </summary>
        public float editorPortalResolution
        {
            get => _editorPortalResolution;
            set => _editorPortalResolution = Mathf.Clamp(value, 0f, 1f);
        }

        //[Tooltip("")]
        //[SerializeField, Range(0f, 1f)] private float _editorBufferResolution = 1f;
        ///// <summary>
        ///// The resolution scale for the buffer effect in the editor.
        ///// </summary>
        //public float editorBufferResolution
        //{
        //    get => _editorBufferResolution;
        //    set => _editorBufferResolution = Mathf.Clamp(value, 0f, 1f);
        //}

        [Tooltip("Required for both Render Texture Portals, aswell as the buffer effect for Stencil Portals.")]
        [Header("Shaders"), SerializeField] private Material _portalStereo;
        /// <summary>
        /// The stereo material for portal rendering.
        /// </summary>
        public Material portalStereo
        {
            get => _portalStereo;
            set => _portalStereo = value;
        }

        [Tooltip("Required for Stencil Portals.")]
        [SerializeField] private Material _portalIncrease;
        /// <summary>
        /// The material for increasing the portal stencil value.
        /// </summary>
        public Material portalIncrease
        {
            get => _portalIncrease;
            set => _portalIncrease = value;
        }

        [Tooltip("Required for Stencil Portals.")]
        /// <summary>
        /// The material for decreasing the portal stencil value.
        /// </summary>
        [SerializeField] private Material _portalDecrease;
        public Material portalDecrease
        {
            get => _portalDecrease;
            set => _portalDecrease = value;
        }

        [Tooltip("Required for Stencil Portals.")]
        [SerializeField] private Material _portalClearDepth;
        /// <summary>
        /// The material for clearing portal depth.
        /// </summary>
        public Material portalClearDepth
        {
            get => _portalClearDepth;
            set => _portalClearDepth = value;
        }

        [Tooltip("Required for Stencil Portals.")]
        [SerializeField] private Material _portalDepthOnly;
        /// <summary>
        /// The material for rendering portal depth only.
        /// </summary>
        public Material portalDepthOnly
        {
            get => _portalDepthOnly;
            set => _portalDepthOnly = value;
        }

        public static Camera renderCamera { get; private set; }
        private static UniversalAdditionalCameraData _renderCameraData;

        private PropertyInfo _renderFeaturesProperty;

        private PortalRenderNode _rootNode;

        private Queue<ScriptableRenderPass> _passesQueue = new Queue<ScriptableRenderPass>();

        private BeginPortalPass beginPass;
        private CompletePortalPass completePass;
        private DrawDepthOnlyPortalsPass depthOnlyPortalPass;
        private DrawBlankPortalsPass blankPortalsRenderPass;
        private DrawTexturePortalsPass renderTexturePortalsPass;
        private IncreaseStencilPortalsPass increaseStencilPortalsPass;
        private DecreaseStencilPortalsPass decreaseStencilPortalsPass;


        //private BeginUndoStencilPortalPass beginUndoStencilPass;
        //private CompleteUndoStencilPortalPass completeUndoStencilPass;

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_renderMode), nameof(renderMode));
        }

        /// <inheritdoc/>
        public override void Create()
        {
            beginPass = new BeginPortalPass();
            completePass = new CompletePortalPass();
            blankPortalsRenderPass = new DrawBlankPortalsPass();
            renderTexturePortalsPass = new DrawTexturePortalsPass();
            depthOnlyPortalPass = new DrawDepthOnlyPortalsPass();
            increaseStencilPortalsPass = new IncreaseStencilPortalsPass();
            decreaseStencilPortalsPass = new DecreaseStencilPortalsPass();
            //beginUndoStencilPass = new BeginUndoStencilPortalPass();
            //completeUndoStencilPass = new CompleteUndoStencilPortalPass();

            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        protected override void Dispose(bool disposing)
        {
            if (renderCamera)
                DestroyImmediate(renderCamera.gameObject, false);

            RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        private void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> list)
        {
            if (!isActive) return;

            // Ensure begin camera rendering is always last
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!isActive || camera == renderCamera) return;

            PortalRenderStack.Clear();
            RenderPortalsBuffer.ClearBuffers();
        }

        // TODO: Its not ideal that this is called so early, as other calls to this event might also want to move the camera.
        // Maybe call add and readd this every frame to ensure its last?
        protected virtual void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // Decide if this is a camera that will be rendered by this feature
            if (!isActive || camera == renderCamera) return;

            var universalData = camera.GetUniversalAdditionalCameraData();

            if (!universalData || universalData.scriptableRenderer == null) return;

            //if (universalData.scriptableRenderer is UniversalRenderer d) d.data
            
            _renderFeaturesProperty ??= typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
            var features = _renderFeaturesProperty.GetValue(universalData.scriptableRenderer) as List<ScriptableRendererFeature>;

            if (!features.Contains(this)) return;

            // Ensure we have a render camera
            EnsureCamera();

            // Copy the camera
            renderCamera.CopyFrom(camera);
            renderCamera.targetTexture = null;

            if (renderCamera.cameraType == CameraType.Preview || renderCamera.cameraType == CameraType.SceneView)
                renderCamera.cameraType = CameraType.Game;

            if (camera.stereoEnabled && XRSettings.stereoRenderingMode != XRSettings.StereoRenderingMode.MultiPass)
            {
                camera.TryGetCullingParameters(true, out var cullingParameters);
                renderCamera.projectionMatrix = cullingParameters.stereoProjectionMatrix;
                renderCamera.worldToCameraMatrix = cullingParameters.stereoViewMatrix;
            }
            else
            {
                renderCamera.worldToCameraMatrix = camera.worldToCameraMatrix;
                renderCamera.projectionMatrix = camera.projectionMatrix;
            }

            // Set up frame buffer
            if (camera.stereoEnabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
            {
                if (camera.projectionMatrix.m02 <= 0f)
                    FrameBuffer.SetCurrent(camera, Camera.MonoOrStereoscopicEye.Left);
                else
                    FrameBuffer.SetCurrent(camera, Camera.MonoOrStereoscopicEye.Right);
            }
            else
                FrameBuffer.SetCurrent(camera);

            int minDepth, maxDepth, maxRenders, maxShadowDepth;
            
            //float bufferResolution;

            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.SceneView)
            {
                minDepth = _editorMinDepth;
                maxDepth = _editorMaxDepth;
                maxRenders = _editorMaxRenders;
                maxShadowDepth = _editorMaxShadowDepth;
                //bufferResolution = _editorBufferResolution;
            }
            else
            {
                minDepth = _minDepth;
                maxDepth = _maxDepth;
                maxRenders = _maxRenders;
                maxShadowDepth = _maxShadowDepth;
                //bufferResolution = _bufferResolution;
            }

            if (_algorithm == PortalAlgorithm.BreadthFirst)
            {
                if (camera.stereoEnabled && XRSettings.stereoRenderingMode != XRSettings.StereoRenderingMode.MultiPass)
                    _rootNode = PortalAlgorithms.GetStereoTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, renderCamera.cullingMask, camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left),
                        camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers());
                else
                    _rootNode = PortalAlgorithms.GetTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, camera.cullingMask, minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers());
            }
            else
            {
                // TODO: This could be used for eye tracking
                // Also, might be better to use a rect, instead of a position
                Vector2? focus = null;//new Vector2(0.5f, 0.5f);

                if (camera.stereoEnabled && XRSettings.stereoRenderingMode != XRSettings.StereoRenderingMode.MultiPass)
                    _rootNode = PortalAlgorithms.GetPredictiveStereoTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, renderCamera.cullingMask, camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left),
                        camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers(), focus);
                else
                    _rootNode = PortalAlgorithms.GetPredictiveTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, camera.cullingMask, minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers(), focus);
            }

            //storePreviousFramePass.resolution = bufferResolution;

            Shader.SetGlobalInt(PropertyID.PortalStencilRef, 0);

            blankPortalsRenderPass.material = portalStereo;
            renderTexturePortalsPass.material = portalStereo;
            depthOnlyPortalPass.depthOnlyMaterial = portalDepthOnly;
            increaseStencilPortalsPass.increaseMaterial = portalIncrease;
            increaseStencilPortalsPass.clearDepthMaterial = portalClearDepth;
            decreaseStencilPortalsPass.depthMaterial = portalDepthOnly;
            decreaseStencilPortalsPass.clearDepthMaterial = portalClearDepth;
            decreaseStencilPortalsPass.decreaseMaterial = portalDecrease;

            if (renderMode == RenderMode.RenderTexture)
                RenderTexturePortals(camera, _rootNode, maxShadowDepth);
            else
                RenderStencilPortals(camera, _rootNode, maxShadowDepth);
        }

        private void RenderTexturePortals(Camera camera, PortalRenderNode root, int maxShadowDepth)
        {
            var isSceneCamera = camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.SceneView;

            float resolution = isSceneCamera ? editorPortalResolution : portalResolution;
            
            RenderTextureDescriptor descriptor;

            if (camera.stereoEnabled && XRSettings.stereoRenderingMode != XRSettings.StereoRenderingMode.MultiPass)
            {
                descriptor = XRSettings.eyeTextureDesc;
            }
            else if (camera.targetTexture != null)
            {
                // Get descriptor from an existing RenderTexture
                descriptor = camera.targetTexture.descriptor;
            }
            else
            {
                // Create a new descriptor based on camera properties
                descriptor = new RenderTextureDescriptor(
                    camera.pixelWidth,
                    camera.pixelHeight,
                    RenderTextureFormat.Default,
                    24 // Depth bits
                );
            }

            descriptor.msaaSamples = 1;
            descriptor.enableRandomWrite = true;
            descriptor.width = Mathf.Max(1, (int)(descriptor.width * resolution));
            descriptor.height = Mathf.Max(1, (int)(descriptor.height * resolution));

            PortalRenderStack.Push(root);
            foreach (PortalRenderNode child in root.GetPostorderDepthFirst())
            {
                if (child.isValid && child != root)
                {
                    PortalRenderStack.Push(child);

                    _passesQueue.Enqueue(beginPass);

                    // Render Child Portals
                    if (child.validChildCount > 0)
                        _passesQueue.Enqueue(renderTexturePortalsPass);

                    if (child.invalidChildCount > 0)
                        _passesQueue.Enqueue(blankPortalsRenderPass);

                    UpdateCamera(maxShadowDepth, child);

                    var buffer = RenderPortalsBuffer.GetBuffer(child);
                    buffer.UpdateTexture(descriptor);
                    renderCamera.targetTexture = buffer.texture;

                    _passesQueue.Enqueue(completePass);

                    renderCamera.Render();
                    renderCamera.targetTexture = null;
                    PortalRenderStack.Pop();
                }
            }

            if (root.validChildCount > 0)
                _passesQueue.Enqueue(renderTexturePortalsPass);

            if (root.invalidChildCount > 0)
                _passesQueue.Enqueue(blankPortalsRenderPass);
        }

        private static void UpdateCamera(int maxShadowDepth, PortalRenderNode child)
        {
            renderCamera.clearStencilAfterLightingPass = false;
            renderCamera.transform.SetPositionAndRotation(child.localToWorldMatrix.GetPosition(), child.localToWorldMatrix.rotation);
            renderCamera.worldToCameraMatrix = child.worldToCameraMatrix;
            renderCamera.projectionMatrix = child.projectionMatrix;

            if (child.isStereo)
            {
                renderCamera.SetStereoViewMatrix(Camera.StereoscopicEye.Left, child.GetStereoViewMatrix(0));
                renderCamera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, child.GetStereoProjectionMatrix(0));
                renderCamera.SetStereoViewMatrix(Camera.StereoscopicEye.Right, child.GetStereoViewMatrix(1));
                renderCamera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, child.GetStereoProjectionMatrix(1));
                _renderCameraData.allowXRRendering = true;
            }
            else
                _renderCameraData.allowXRRendering = false;

            _renderCameraData.renderShadows = child.depth <= maxShadowDepth;

            renderCamera.rect = child.cullingWindow.GetRect();
        }

        private void RenderStencilPortals(Camera camera, PortalRenderNode root, int maxShadowDepth)
        {
            increaseStencilPortalsPass.nodesToIncrease.Clear();
            decreaseStencilPortalsPass.nodesToDecrease.Clear();
            RenderStencilPortalsRecursive(camera, root, maxShadowDepth);
            PortalRenderStack.Pop();
        }

        private void RenderStencilPortalsRecursive(Camera camera, PortalRenderNode parent, int maxShadowDepth)
        {
            PortalRenderStack.Push(parent);

            foreach (PortalRenderNode child in parent.children)
            {
                if (child.isValid)
                {
                    increaseStencilPortalsPass.nodesToIncrease.Add(child);
                    RenderStencilPortalsRecursive(camera, child, maxShadowDepth);
                    
                    _passesQueue.Enqueue(beginPass);
                    _passesQueue.Enqueue(increaseStencilPortalsPass);

                    UpdateCamera(maxShadowDepth, child);

                    renderCamera.targetDisplay = camera.targetDisplay;
                    renderCamera.targetTexture = camera.targetTexture;

                    decreaseStencilPortalsPass.nodesToDecrease.Clear();
                    decreaseStencilPortalsPass.nodesToDecrease.Add(child);
                    _passesQueue.Enqueue(decreaseStencilPortalsPass);
                    _passesQueue.Enqueue(completePass);
                    renderCamera.Render();

                    increaseStencilPortalsPass.nodesToIncrease.Clear();
                    PortalRenderStack.Pop();
                }
            }

            if (parent.validChildCount > 0)
                _passesQueue.Enqueue(depthOnlyPortalPass);

            if (parent.invalidChildCount > 0)
                _passesQueue.Enqueue(blankPortalsRenderPass);
        }

        private static void EnsureCamera()
        {
            if (!renderCamera)
            {
                renderCamera = new GameObject("[Portal Render Camera]").AddComponent<Camera>();
                renderCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                renderCamera.gameObject.SetActive(false);
                _renderCameraData = renderCamera.GetUniversalAdditionalCameraData();
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera != renderCamera)
            {
                // This is a real camera being rendered
            }
            else
            {
                // This is the portal render camera being rendered
            }

            while (_passesQueue.TryDequeue(out var pass))
            {
                renderer.EnqueuePass(pass);
            }
        }
    }
}