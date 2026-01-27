using Misc.EditorHelpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Android.Gradle.Manifest;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.UIElements;
using UnityEngine.XR;
using UnityEngine.XR.Management;


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
            set => _renderMode = value;
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

        //[Tooltip("The layer mask for opaque objects.")]
        //[Header("Filtering"), SerializeField] private LayerMask _opaqueLayerMask = -1;
        ///// <summary>
        ///// The layer mask for opaque objects.
        ///// </summary>
        //public LayerMask opaqueLayerMask
        //{
        //    get => _opaqueLayerMask;
        //    set => _opaqueLayerMask = value;
        //}

        //[Tooltip("The layer mask for transparent objects.")]
        //[SerializeField] private LayerMask _transparentLayerMask = -1;
        ///// <summary>
        ///// The layer mask for transparent objects.
        ///// </summary>
        //public LayerMask transparentLayerMask
        //{
        //    get => _transparentLayerMask;
        //    set => _transparentLayerMask = value;
        //}

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

        [Tooltip("The resolution scale for the buffer effect.")]
        [SerializeField, Range(0f, 1f)] private float _bufferResolution = 1f;
        /// <summary>
        /// The resolution scale for the buffer effect.
        /// </summary>
        public float bufferResolution
        {
            get => _bufferResolution;
            set => _bufferResolution = Mathf.Clamp(value, 0f, 1f);
        }

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

        [Tooltip("")]
        [SerializeField, Range(0f, 1f)] private float _editorBufferResolution = 1f;
        /// <summary>
        /// The resolution scale for the buffer effect in the editor.
        /// </summary>
        public float editorBufferResolution
        {
            get => _editorBufferResolution;
            set => _editorBufferResolution = Mathf.Clamp(value, 0f, 1f);
        }

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

        public Material testMaterial;

        public static Camera renderCamera { get; private set; }
        private static UniversalAdditionalCameraData _renderCameraData;

        private static PropertyInfo _renderFeaturesProperty;
        private static FieldInfo _clearDepthsField;

        private PortalRenderNode _root;
        private Queue<ScriptableRenderPass> _passesQueue = new Queue<ScriptableRenderPass>();
        private Camera.StereoscopicEye _currentEye = Camera.StereoscopicEye.Right;

        private PortalCameraSetupPass setupPass;
        private BeginPortalPass beginPass;
        private CompletePortalPass completePass;
        private DrawDepthOnlyPortalsPass depthOnlyPortalPass;
        private DrawBlankPortalsPass blankPortalsRenderPass;
        private DrawTexturePortalsPass renderTexturePortalsPass;
        private IncreaseStencilPortalsPass increaseStencilPortalsPass;
        private DecreaseStencilPortalsPass decreaseStencilPortalsPass;
        private StoreFramePass storeFramePass;
        //private BeginUndoStencilPortalPass beginUndoStencilPass;
        //private CompleteUndoStencilPortalPass completeUndoStencilPass;

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_renderMode), nameof(renderMode));
        }

        /// <inheritdoc/>
        public override void Create()
        {
            setupPass = new PortalCameraSetupPass();
            beginPass = new BeginPortalPass();
            completePass = new CompletePortalPass();
            blankPortalsRenderPass = new DrawBlankPortalsPass();
            renderTexturePortalsPass = new DrawTexturePortalsPass();
            depthOnlyPortalPass = new DrawDepthOnlyPortalsPass();
            increaseStencilPortalsPass = new IncreaseStencilPortalsPass();
            decreaseStencilPortalsPass = new DecreaseStencilPortalsPass();
            storeFramePass = new StoreFramePass();
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

            //if (_restoreClearDepth.HasValue && _clearDepthsField != null)
            //    _clearDepthsField.SetValue(camera.GetUniversalAdditionalCameraData(), _restoreClearDepth.Value);

            PortalRenderStack.Clear();
            RenderPortalsBuffer.ClearBuffers();
        }

        // TODO: Its not ideal that this is called so early, as other calls to this event might also want to move the camera.
        // Maybe call add and readd this every frame to ensure its last?
        protected virtual void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // Decide if this is a camera that will be rendered by this feature
            if (!isActive || camera == renderCamera) return;

            var cameraData = camera.GetUniversalAdditionalCameraData();

            if (!cameraData || cameraData.scriptableRenderer == null) return;

            _renderFeaturesProperty ??= typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
            var features = _renderFeaturesProperty.GetValue(cameraData.scriptableRenderer) as List<ScriptableRendererFeature>;

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

            int minDepth, maxDepth, maxRenders, maxShadowDepth;
            
            //float bufferResolution;

            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.SceneView)
            {
                minDepth = _editorMinDepth;
                maxDepth = _editorMaxDepth;
                maxRenders = _editorMaxRenders;
                maxShadowDepth = _editorMaxShadowDepth;
                bufferResolution = _editorBufferResolution;
            }
            else
            {
                minDepth = _minDepth;
                maxDepth = _maxDepth;
                maxRenders = _maxRenders;
                maxShadowDepth = _maxShadowDepth;
                bufferResolution = _bufferResolution;
            }

            if (camera.stereoEnabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
            {
                _currentEye = _currentEye == Camera.StereoscopicEye.Right ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;

                FrameBuffer.SetCurrent(camera, (Camera.MonoOrStereoscopicEye)_currentEye);
            }
            else
                FrameBuffer.SetCurrent(camera);

            if (_algorithm == PortalAlgorithm.BreadthFirst)
            {
                if (camera.stereoEnabled && XRSettings.stereoRenderingMode != XRSettings.StereoRenderingMode.MultiPass)
                    _root = PortalAlgorithms.GetStereoTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, renderCamera.cullingMask, camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left),
                        camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers());
                else if (camera.stereoEnabled)
                    _root = PortalAlgorithms.GetTree(camera, camera.transform.localToWorldMatrix, camera.GetStereoViewMatrix(_currentEye), camera.GetStereoProjectionMatrix(_currentEye), camera.cullingMask, minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers());
                else
                    _root = PortalAlgorithms.GetTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, camera.cullingMask, minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers());
            }
            else
            {
                // TODO: This could be used for eye tracking
                // Also, might be better to use a rect, instead of a position
                Vector2? focus = null;//new Vector2(0.5f, 0.5f);

                if (camera.stereoEnabled && XRSettings.stereoRenderingMode != XRSettings.StereoRenderingMode.MultiPass)
                    _root = PortalAlgorithms.GetPredictiveStereoTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, renderCamera.cullingMask, camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left),
                        camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers(), focus);
                else if (camera.stereoEnabled)
                    _root = PortalAlgorithms.GetPredictiveTree(camera, camera.transform.localToWorldMatrix, camera.GetStereoViewMatrix(_currentEye), camera.GetStereoProjectionMatrix(_currentEye), camera.cullingMask, minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers(), focus);
                else
                    _root = PortalAlgorithms.GetPredictiveTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, camera.cullingMask, minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers(), focus);
            }

            storeFramePass.resolution = bufferResolution;

            Shader.SetGlobalInt(PropertyID.PortalStencilRef, 0);

            blankPortalsRenderPass.material = portalStereo;
            renderTexturePortalsPass.material = portalStereo;
            depthOnlyPortalPass.depthOnlyMaterial = portalDepthOnly;
            //increaseStencilPortalsPass.increaseMaterial = testMaterial;//portalIncrease;
            //increaseStencilPortalsPass.clearDepthMaterial = testMaterial;//portalClearDepth;
            //decreaseStencilPortalsPass.depthMaterial = testMaterial;//portalDepthOnly;
            //decreaseStencilPortalsPass.clearDepthMaterial = testMaterial;//portalClearDepth;
            //decreaseStencilPortalsPass.decreaseMaterial = testMaterial;//portalDecrease;
            increaseStencilPortalsPass.increaseMaterial = portalIncrease;
            increaseStencilPortalsPass.clearDepthMaterial = portalClearDepth;
            decreaseStencilPortalsPass.depthMaterial = portalDepthOnly;
            decreaseStencilPortalsPass.clearDepthMaterial = portalClearDepth;
            decreaseStencilPortalsPass.decreaseMaterial = portalDecrease;

            _clearDepthsField ??= typeof(UniversalAdditionalCameraData).GetField("m_ClearDepth", BindingFlags.NonPublic | BindingFlags.Instance);

            // Set up frame buffer
            if (renderMode == RenderMode.RenderTexture)
                RenderTexturePortals(camera, cameraData, maxShadowDepth);
            else
                RenderStencilPortals(camera, cameraData, cameraData.scriptableRenderer, maxShadowDepth);
        }

        private void RenderTexturePortals(Camera camera, UniversalAdditionalCameraData cameraData, int maxShadowDepth)
        {
            var isSceneCamera = camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.SceneView;

            float resolution = isSceneCamera ? editorPortalResolution : portalResolution;
            
            RenderTextureDescriptor descriptor;

            if (camera.stereoEnabled)// && XRSettings.stereoRenderingMode != XRSettings.StereoRenderingMode.MultiPass)
            {
                //Debug.Log("Cam?");
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

            PortalRenderStack.Push(_root);
            foreach (PortalRenderNode child in _root.GetPostorderDepthFirst())
            {
                if (child.isValid && child != _root)
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

            if (_root.validChildCount > 0)
                _passesQueue.Enqueue(renderTexturePortalsPass);

            if (_root.invalidChildCount > 0)
                _passesQueue.Enqueue(blankPortalsRenderPass);

            _passesQueue.Enqueue(storeFramePass);
        }

        private static void UpdateCamera(int maxShadowDepth, PortalRenderNode child)
        {
            //renderCamera.clearStencilAfterLightingPass = false;
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

        private void RenderStencilPortals(Camera camera, UniversalAdditionalCameraData cameraData, ScriptableRenderer renderer, int maxShadowDepth)
        {
            StencilManager.Begin(renderer);

            //_clearDepthsField.SetValue(_renderCameraData, false);

            setupPass.clearDepth = true;
            increaseStencilPortalsPass.nodesToIncrease.Clear();
            decreaseStencilPortalsPass.nodesToDecrease.Clear();
            RenderStencilPortalsRecursive(camera, _root, maxShadowDepth);
            PortalRenderStack.Pop();

            //if (root.validChildCount > 0)
            //{
            //    if ((bool)_clearDepthsField.GetValue(cameraData))
            //    {
            //        _restoreClearDepth = true;
            //        _clearDepthsField.SetValue(cameraData, false);
            //    }
            //}

            StencilManager.Complete();

            _passesQueue.Enqueue(storeFramePass);
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
                    
                    _passesQueue.Enqueue(setupPass);
                    _passesQueue.Enqueue(beginPass);
                    _passesQueue.Enqueue(increaseStencilPortalsPass);

                    UpdateCamera(maxShadowDepth, child);

                    renderCamera.targetDisplay = camera.targetDisplay;
                    renderCamera.targetTexture = camera.targetTexture;

                    decreaseStencilPortalsPass.nodesToDecrease.Clear();
                    decreaseStencilPortalsPass.nodesToDecrease.Add(child);
                    _passesQueue.Enqueue(decreaseStencilPortalsPass);
                    _passesQueue.Enqueue(completePass);
                    StencilManager.SetStencil(child.depth);
                    renderCamera.Render();
                    setupPass.clearDepth = false;

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
            while (_passesQueue.TryDequeue(out var pass))
                renderer.EnqueuePass(pass);

            if (renderingData.cameraData.camera != renderCamera)
            {
                // Set up frame buffer
                //if (renderingData.cameraData.camera.stereoEnabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
                //    FrameBuffer.SetCurrent(renderingData.cameraData.camera, (Camera.MonoOrStereoscopicEye)renderingData.cameraData.xr.multipassId);
                //else
                //    FrameBuffer.SetCurrent(renderingData.cameraData.camera);

                //Shader.SetGlobalInt(PropertyID.PortalStencilRef, 0);

                //// This is a real camera being rendered
                ////renderingData.cameraData.historyManager?.RequestAccess<RawColorHistory>();
                ////RawColorHistory colorHistory = renderingData.cameraData.historyManager?.GetHistoryForRead<RawColorHistory>();
                ////blankPortalsRenderPass.lastFrame = colorHistory?.GetPreviousTexture(renderingData.cameraData.xr.multipassId); // 0 gets the immediately previous frame

                //PortalRenderStack.Clear();
                //PortalRenderStack.Push(_roots[renderingData.cameraData.xr.multipassId]);
            }
            else
            {
                // This is the portal render camera being rendered
            }
        }

        public static class StencilManager
        {
            private static FieldInfo _GBufferPass_field;
            private static object _GBufferPass;
            private static Type _GBufferPass_type;
            private static FieldInfo _GBufferPass_RenderStateBlock_field;
            private static RenderStateBlock _GBufferPass_RenderStateBlock;
            private static FieldInfo _GBufferPass_RenderStateBlocks_field;
            private static RenderStateBlock[] _GBufferPass_RenderStateBlocks;
            private static RenderStateBlock[] _renderStateBlocks;

            private static FieldInfo _DrawObjectsPass_RenderStateBlock_field;

            private static FieldInfo _RenderOpaqueForwardOnlyPass_field;
            private static DrawObjectsPass _RenderOpaqueForwardOnlyPass;
            private static RenderStateBlock _RenderOpaqueForwardOnlyPass_RenderStateBlock;

            private static FieldInfo _RenderOpaqueForwardPass_field;
            private static DrawObjectsPass _RenderOpaqueForwardPass;
            private static RenderStateBlock _RenderOpaqueForwardPass_RenderStateBlock;

            private static FieldInfo _RenderOpaqueForwardWithRenderingLayersPass_field;
            private static DrawObjectsPass _RenderOpaqueForwardWithRenderingLayersPass;
            private static RenderStateBlock _RenderOpaqueForwardWithRenderingLayersPass_RenderStateBlock;

            private static FieldInfo _RenderTransparentForwardPass_field;
            private static DrawObjectsPass _RenderTransparentForwardPass;
            private static RenderStateBlock _RenderTransparentForwardPass_RenderStateBlock;
            
            public static void Begin(ScriptableRenderer renderer)
            {
                _GBufferPass_field ??= typeof(UniversalRenderer).GetField("m_GBufferPass", BindingFlags.NonPublic | BindingFlags.Instance);
                _GBufferPass = _GBufferPass_field.GetValue(renderer);

                if (_GBufferPass != null)
                {
                    _GBufferPass_type ??= _GBufferPass.GetType();
                    _GBufferPass_RenderStateBlock_field ??= _GBufferPass_type.GetField("m_RenderStateBlock", BindingFlags.NonPublic | BindingFlags.Instance);
                    _GBufferPass_RenderStateBlocks_field ??= _GBufferPass_type.GetField("s_RenderStateBlocks", BindingFlags.NonPublic | BindingFlags.Static);

                    _GBufferPass_RenderStateBlock = (RenderStateBlock)_GBufferPass_RenderStateBlock_field.GetValue(_GBufferPass);
                    _GBufferPass_RenderStateBlocks = (RenderStateBlock[])_GBufferPass_RenderStateBlocks_field.GetValue(null);

                    if (_renderStateBlocks == null)
                        _renderStateBlocks = new RenderStateBlock[5];
                }

                _RenderOpaqueForwardOnlyPass_field ??= typeof(UniversalRenderer).GetField("m_RenderOpaqueForwardOnlyPass", BindingFlags.NonPublic | BindingFlags.Instance);
                _RenderOpaqueForwardPass_field ??= typeof(UniversalRenderer).GetField("m_RenderOpaqueForwardPass", BindingFlags.NonPublic | BindingFlags.Instance);
                _RenderOpaqueForwardWithRenderingLayersPass_field ??= typeof(UniversalRenderer).GetField("m_RenderOpaqueForwardWithRenderingLayersPass", BindingFlags.NonPublic | BindingFlags.Instance);
                _RenderTransparentForwardPass_field ??= typeof(UniversalRenderer).GetField("m_RenderTransparentForwardPass", BindingFlags.NonPublic | BindingFlags.Instance);

                _DrawObjectsPass_RenderStateBlock_field ??= typeof(DrawObjectsPass).GetField("m_RenderStateBlock", BindingFlags.NonPublic | BindingFlags.Instance);

                GetDrawingObjectsPass(renderer, _RenderOpaqueForwardOnlyPass_field, out _RenderOpaqueForwardOnlyPass, out _RenderOpaqueForwardOnlyPass_RenderStateBlock);
                GetDrawingObjectsPass(renderer, _RenderOpaqueForwardPass_field, out _RenderOpaqueForwardPass, out _RenderOpaqueForwardPass_RenderStateBlock);
                GetDrawingObjectsPass(renderer, _RenderOpaqueForwardWithRenderingLayersPass_field, out _RenderOpaqueForwardWithRenderingLayersPass, out _RenderOpaqueForwardWithRenderingLayersPass_RenderStateBlock);
                GetDrawingObjectsPass(renderer, _RenderTransparentForwardPass_field, out _RenderTransparentForwardPass, out _RenderTransparentForwardPass_RenderStateBlock);

                Debug.Log($"{_GBufferPass}, {_RenderOpaqueForwardOnlyPass}, {_RenderOpaqueForwardPass}, {_RenderOpaqueForwardWithRenderingLayersPass}, {_RenderTransparentForwardPass}");
            }

            public static void SetStencil(int stencilReference)
            {
                StencilState stencilState = new StencilState(true, 255, 255, CompareFunction.Equal);

                StencilState forwardOnlyStencilState = DeferredLights_OverwriteStencil(stencilState, 0b_0110_0000);
                int forwardOnlyStencilRef = stencilReference | 0b_0000_0000;

                if (_GBufferPass != null)
                {
                    var block = _GBufferPass_RenderStateBlock;
                    block.stencilState = forwardOnlyStencilState;
                    block.stencilReference = forwardOnlyStencilRef;
                    block.mask = RenderStateMask.Stencil;

                    _renderStateBlocks[0] = DeferredLights_OverwriteStencil(block, 0b_0110_0000, 0b_0010_0000);
                    _renderStateBlocks[1] = DeferredLights_OverwriteStencil(block, 0b_0110_0000, 0b_0100_0000);
                    _renderStateBlocks[2] = DeferredLights_OverwriteStencil(block, 0b_0110_0000, 0b_0000_0000);
                    _renderStateBlocks[3] = DeferredLights_OverwriteStencil(block, 0b_0110_0000, 0b_0000_0000);  // Fill GBuffer, but skip lighting pass for ComplexLit
                    _renderStateBlocks[4] = _renderStateBlocks[0];

                    _GBufferPass_RenderStateBlock_field.SetValue(_GBufferPass, block);
                    _GBufferPass_RenderStateBlocks_field.SetValue(null, _renderStateBlocks);
                }

                SetStencil(_RenderOpaqueForwardOnlyPass, _RenderOpaqueForwardOnlyPass_RenderStateBlock, stencilState, stencilReference);
                SetStencil(_RenderOpaqueForwardPass, _RenderOpaqueForwardPass_RenderStateBlock, stencilState, stencilReference);
                SetStencil(_RenderOpaqueForwardWithRenderingLayersPass, _RenderOpaqueForwardWithRenderingLayersPass_RenderStateBlock, stencilState, stencilReference);
                SetStencil(_RenderTransparentForwardPass, _RenderTransparentForwardPass_RenderStateBlock, stencilState, stencilReference);
            }

            private static void SetStencil(DrawObjectsPass pass, RenderStateBlock block, in StencilState stencilState, int stencilReference)
            {
                if (pass != null)
                {
                    block.stencilReference = stencilReference;
                    block.mask = RenderStateMask.Stencil;
                    block.stencilState = stencilState;
                    _DrawObjectsPass_RenderStateBlock_field.SetValue(pass, block);
                }
            }

            public static void Complete()
            {
                if (_GBufferPass != null)
                {
                    _GBufferPass_RenderStateBlock_field.SetValue(_GBufferPass, _GBufferPass_RenderStateBlock);
                    _GBufferPass_RenderStateBlocks_field.SetValue(null, _GBufferPass_RenderStateBlocks);
                }

                RestoreDrawingObjectsPass(_RenderOpaqueForwardOnlyPass, _RenderOpaqueForwardOnlyPass_RenderStateBlock);
                RestoreDrawingObjectsPass(_RenderOpaqueForwardPass, _RenderOpaqueForwardPass_RenderStateBlock);
                RestoreDrawingObjectsPass(_RenderOpaqueForwardWithRenderingLayersPass, _RenderOpaqueForwardWithRenderingLayersPass_RenderStateBlock);
                RestoreDrawingObjectsPass(_RenderTransparentForwardPass, _RenderTransparentForwardPass_RenderStateBlock);
            }

            private static void GetDrawingObjectsPass(ScriptableRenderer renderer, FieldInfo fieldInfo, out DrawObjectsPass pass, out RenderStateBlock block)
            {
                pass = (DrawObjectsPass)fieldInfo.GetValue(renderer);
                if (pass != null)
                    block = (RenderStateBlock)_DrawObjectsPass_RenderStateBlock_field.GetValue(pass);
                else
                    block = default;
            }

            private static void RestoreDrawingObjectsPass(DrawObjectsPass pass, in RenderStateBlock block)
            {
                if (pass != null)
                    _DrawObjectsPass_RenderStateBlock_field.SetValue(pass, block);
            }


            private static StencilState DeferredLights_OverwriteStencil(StencilState s, int stencilWriteMask)
            {
                if (!s.enabled)
                {
                    return new StencilState(
                        true,
                        0, (byte)stencilWriteMask,
                        CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep,
                        CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep
                    );
                }

                CompareFunction funcFront = s.compareFunctionFront != CompareFunction.Disabled ? s.compareFunctionFront : CompareFunction.Always;
                CompareFunction funcBack = s.compareFunctionBack != CompareFunction.Disabled ? s.compareFunctionBack : CompareFunction.Always;
                StencilOp passFront = s.passOperationFront;
                StencilOp failFront = s.failOperationFront;
                StencilOp zfailFront = s.zFailOperationFront;
                StencilOp passBack = s.passOperationBack;
                StencilOp failBack = s.failOperationBack;
                StencilOp zfailBack = s.zFailOperationBack;

                return new StencilState(
                    true,
                    (byte)(s.readMask & 0x0F), (byte)(s.writeMask | stencilWriteMask),
                    funcFront, passFront, failFront, zfailFront,
                    funcBack, passBack, failBack, zfailBack
                );
            }

            private static RenderStateBlock DeferredLights_OverwriteStencil(RenderStateBlock block, int stencilWriteMask, int stencilRef)
            {
                if (!block.stencilState.enabled)
                {
                    block.stencilState = new StencilState(
                        true,
                        0, (byte)stencilWriteMask,
                        CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep,
                        CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep
                    );
                }
                else
                {
                    StencilState s = block.stencilState;
                    CompareFunction funcFront = s.compareFunctionFront != CompareFunction.Disabled ? s.compareFunctionFront : CompareFunction.Always;
                    CompareFunction funcBack = s.compareFunctionBack != CompareFunction.Disabled ? s.compareFunctionBack : CompareFunction.Always;
                    StencilOp passFront = s.passOperationFront;
                    StencilOp failFront = s.failOperationFront;
                    StencilOp zfailFront = s.zFailOperationFront;
                    StencilOp passBack = s.passOperationBack;
                    StencilOp failBack = s.failOperationBack;
                    StencilOp zfailBack = s.zFailOperationBack;

                    block.stencilState = new StencilState(
                        true,
                        (byte)(s.readMask & 0x0F), (byte)(s.writeMask | stencilWriteMask),
                        funcFront, passFront, failFront, zfailFront,
                        funcBack, passBack, failBack, zfailBack
                    );
                }

                block.mask |= RenderStateMask.Stencil;
                block.stencilReference = (block.stencilReference & (int)0b_0000_1111) | stencilRef;

                return block;
            }
        }
    }
}