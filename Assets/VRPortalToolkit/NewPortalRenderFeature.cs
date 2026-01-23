using Misc.EditorHelpers;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
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

        public static Camera renderCamera;

        private PortalRenderPass _portalPass;

        private PropertyInfo _renderFeaturesProperty;

        private PortalPassNode _rootPassNode;

        private StoreFramePass storePreviousFramePass = new StoreFramePass();

        private struct PortalPassInfo
        {
            public int passesCount;
        }

        private Queue<PortalPassInfo> passInfos = new Queue<PortalPassInfo>();
        private Queue<ScriptableRenderPass> passesQueue = new Queue<ScriptableRenderPass>();


        public virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_renderMode), nameof(renderMode));
        }

        /// <inheritdoc/>
        public override void Create()
        {
            _portalPass = new PortalRenderPass(this);

            PortalPassGroupPool.Release(_rootPassNode);
            _rootPassNode = PortalPassGroupPool.Get();

            // Configures where the render pass should be injected.
            _portalPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        protected override void Dispose(bool disposing)
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        // TODO: Its not ideal that this is called so early, as other calls to this event might also want to move the camera.
        // Maybe call add and readd this every frame to ensure its last?
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // Decide if this is a camera that will be rendered by this feature
            if (camera == renderCamera) return;

            var universalData = camera.GetUniversalAdditionalCameraData();

            if (!universalData || universalData.scriptableRenderer == null) return;

            _renderFeaturesProperty ??= typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
            var features = _renderFeaturesProperty.GetValue(universalData.scriptableRenderer) as List<ScriptableRendererFeature>;

            if (!features.Contains(this)) return;

            // Ensure we have a render camera
            EnsureCamera();

            // Copy the camera
            renderCamera.CopyFrom(camera);
            renderCamera.clearFlags = camera.clearFlags;
            renderCamera.targetTexture = null;
            
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
            float bufferResolution;

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

            if (_algorithm == PortalAlgorithm.BreadthFirst)
            {
                if (camera.stereoEnabled && XRSettings.stereoRenderingMode != XRSettings.StereoRenderingMode.MultiPass)
                    _rootPassNode.renderNode = PortalAlgorithms.GetStereoTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, renderCamera.cullingMask, camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left),
                        camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers());
                else
                    _rootPassNode.renderNode = PortalAlgorithms.GetTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, camera.cullingMask, minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers());
            }
            else
            {
                // TODO: This could be used for eye tracking
                // Also, might be better to use a rect, instead of a position
                Vector2? focus = null;//new Vector2(0.5f, 0.5f);

                if (camera.stereoEnabled && XRSettings.stereoRenderingMode != XRSettings.StereoRenderingMode.MultiPass)
                    _rootPassNode.renderNode = PortalAlgorithms.GetPredictiveStereoTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, renderCamera.cullingMask, camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left),
                        camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers(), focus);
                else
                    _rootPassNode.renderNode = PortalAlgorithms.GetPredictiveTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, camera.cullingMask, minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers(), focus);
            }

            storePreviousFramePass.resolution = bufferResolution;

            Shader.SetGlobalInt(PropertyID.PortalStencilRef, 0);
        }

        private static void EnsureCamera()
        {
            if (!renderCamera)
            {
                renderCamera = new GameObject("[Portal Render Camera]").AddComponent<Camera>();
                renderCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                renderCamera.gameObject.SetActive(false);
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
        }


        /// <summary>
        /// Enqueues stencil-based portal rendering passes.
        /// </summary>
        /// <param name="renderer">The scriptable renderer.</param>
        /// <param name="renderingData">The rendering data.</param>
        /// <param name="passGroup">The portal pass group.</param>
        /// <param name="maxShadowDepth">The maximum shadow depth.</param>
        /// <param name="disableTransparentShadows">Whether to disable shadows on transparent objects.</param>
        /// <param name="order">The rendering order for stencil operations.</param>
        protected virtual void EnqueueStencilNodes(ScriptableRenderer renderer, ref RenderingData renderingData, PortalPassNode passGroup, int maxShadowDepth, bool disableTransparentShadows, int order)
        {
            PortalRenderNode undoNode = TryGetTransitionNode(renderingData, passGroup.renderNode);

            EnqueueStencilNodesRecursive(renderer, ref renderingData, passGroup, maxShadowDepth, disableTransparentShadows, order, undoNode);

            if (passGroup.renderNode.invalidChildCount > 0)
                renderer.EnqueuePass(blankRenderPass);
        }

        private static PortalRenderNode TryGetTransitionNode(RenderingData renderingData, PortalRenderNode renderNode)
        {
            if (PortalRendering.TryGetTransition(renderingData.cameraData.camera, out IPortal portal, out Vector3 transitionCentre, out Vector3 transitionNormal))
            {
                if (!TryFindChild(renderNode, portal, out PortalRenderNode portalNode) && portalNode.isValid)
                    return null;

                // Check if one eye is atleast on the other side
                if (renderNode.isStereo)
                {
                    bool leftTransitioned = !IsFrontSide(transitionCentre, transitionNormal, renderNode.GetStereoViewMatrix(0).inverse.MultiplyPoint(Vector3.zero)),
                        rightTransitioned = !IsFrontSide(transitionCentre, transitionNormal, renderNode.GetStereoViewMatrix(1).inverse.MultiplyPoint(Vector3.zero));

                    if (!leftTransitioned && !rightTransitioned) return null;

                    if (leftTransitioned) portalNode.SetStereoProjectionMatrix(0, portalNode.root.GetStereoProjectionMatrix(0));
                    if (rightTransitioned) portalNode.SetStereoProjectionMatrix(1, portalNode.root.GetStereoProjectionMatrix(1));
                }
                else
                {
                    if (IsFrontSide(transitionCentre, transitionNormal, renderNode.worldToCameraMatrix.inverse.MultiplyPoint(Vector3.zero)))
                        return null;

                    portalNode.worldToCameraMatrix = portalNode.root.worldToCameraMatrix;
                }

                IPortal connected = portal.connected;
                PortalRenderNode undoNode = null;
                foreach (IPortalRenderer portalRenderer in PortalRendering.GetAllPortalRenderers())
                {
                    if (portalRenderer.Portal == connected)
                    {
                        PortalRenderNode other = GetOrAddChild(portalNode, portalRenderer);
                        if (other != null) undoNode = other;
                    }
                }

                if (undoNode != null)
                {
                    renderNode.SortChildren(new RenderNodeComparer(portalNode));
                    undoNode.ComputeMaskAndMatrices();
                    undoNode.isValid = true;
                    return undoNode;
                }
            }

            return null;
        }

        private static PortalRenderNode GetOrAddChild(PortalRenderNode parent, IPortalRenderer renderer)
        {
            if (parent.isStereo)
            {
                if (((1 << renderer.Layer) & parent.cullingMask) == 0) return null;

                Matrix4x4 leftView = parent.GetStereoViewMatrix(0), leftProj = parent.root.GetStereoProjectionMatrix(0),
                    rightView = parent.GetStereoViewMatrix(1), rightProj = parent.root.GetStereoProjectionMatrix(1);

                bool leftValid = renderer.TryGetWindow(parent, leftView.inverse.MultiplyPoint(Vector3.zero), leftView, leftProj, out ViewWindow leftWindow),
                    rightValid = renderer.TryGetWindow(parent, rightView.inverse.MultiplyPoint(Vector3.zero), rightView, rightProj, out ViewWindow rightWindow);

                if ((!leftValid || !leftWindow.IsVisibleThrough(parent.cullingWindow)) && (!rightValid || !rightWindow.IsVisibleThrough(parent.cullingWindow)))
                    return null;

                return parent.GetOrAddChild(renderer, leftWindow, rightWindow);
            }
            else
            {
                if (((1 << renderer.Layer) & parent.cullingMask) == 0) return null;

                if (renderer.TryGetWindow(parent, parent.localToWorldMatrix.GetColumn(3), parent.worldToCameraMatrix, parent.root.projectionMatrix, out ViewWindow window) && window.IsVisibleThrough(parent.cullingWindow))
                    return parent.GetOrAddChild(renderer, window);
            }

            return null;
        }

        private static bool TryFindChild(PortalRenderNode renderNode, IPortal portal, out PortalRenderNode child)
        {
            foreach (PortalRenderNode other in renderNode.children)
            {
                if (other.portal == portal)
                {
                    child = other;
                    return true;
                }
            }

            child = null;
            return false;
        }

        private static bool IsFrontSide(Vector3 transitionCentre, Vector3 transitionNormal, Vector3 position) =>
            Vector3.Dot(position - transitionCentre, transitionNormal) > 0f;

        private readonly struct RenderNodeComparer : IComparer<PortalRenderNode>
        {
            private readonly PortalRenderNode firstNode;

            public RenderNodeComparer(PortalRenderNode firstNode)
            {
                this.firstNode = firstNode;
            }

            public int Compare(PortalRenderNode x, PortalRenderNode y)
            {
                if (x == firstNode)
                {
                    if (y == firstNode) return 0;
                    return -1;
                }
                if (y == firstNode) return 1;
                return 0;
            }
        }

        private void EnqueueStencilNodesRecursive(PortalPassNode passGroup, int maxShadowDepth, bool disableTransparentShadows, int order, PortalRenderNode undoNode = null)
        {
            renderer.EnqueuePass(depthOnlyPass);

            foreach (PortalRenderNode child in passGroup.renderNode.children)
            {
                if (child.isValid)
                {
                    int index = child.validIndex - 1;

                    while (portalStencilPasses.Count <= index)
                        portalStencilPasses.Add(new PortalStencilPasses());

                    // Begin group
                    PortalPassNode childGroup = PortalPassGroupPool.Get();
                    childGroup.renderNode = child;

                    // Setup state block
                    childGroup.stateBlock = new RenderStateBlock(RenderStateMask.Depth | RenderStateMask.Stencil)
                    {
                        depthState = new DepthState(true, CompareFunction.Less),
                        stencilReference = child.depth,
                        stencilState = new StencilState(true, 255, 255, CompareFunction.Equal),
                    };

                    PortalStencilPasses passPair = portalStencilPasses[index];

                    if (child.overrides.depthNormalTexture)
                        renderer.EnqueuePass(portalDepthNormalsPass);

                    if (child != undoNode)
                    {
                        passPair.beginRenderPass.passNode = childGroup;
                        passPair.beginRenderPass.increaseMaterial = _portalIncrease;
                        passPair.beginRenderPass.clearDepthMaterial = _portalClearDepth;
                        renderer.EnqueuePass(passPair.beginRenderPass);
                    }
                    else
                    {
                        // Undo node for transitions through stereo
                        beginUndoStencilPass.passNode = childGroup;
                        beginUndoStencilPass.increaseMaterial = _portalIncrease;
                        beginUndoStencilPass.clearDepthMaterial = _portalClearDepth;
                        renderer.EnqueuePass(beginUndoStencilPass);
                    }

                    // Recursive
                    if (order < 0)
                        EnqueueStencilNodesRecursive(childGroup, maxShadowDepth, disableTransparentShadows, order, undoNode);

                    // TODO: For now, undo node does not support shadows. For some reason, it leads to:
                    // "ArgumentException: RenderTextureDesc width must be greater than zero." in Shadow Utils
                    //if (child != undoNode)
                    //{
                    //    // Main shadows
                    //    if (child.depth <= maxShadowDepth && renderingData.shadowData.supportsMainLightShadows)
                    //    {
                    //        childGroup.mainLightShadowCasterPass = passPair.mainLightShadowCasterPass;
                    //        renderer.EnqueuePass(childGroup.mainLightShadowCasterPass);
                    //    }
                    //    else childGroup.mainLightShadowCasterPass = null;

                    //    if (child != undoNode)
                    //        // Additional shadows
                    //        if (child.depth <= maxShadowDepth && renderingData.shadowData.supportsAdditionalLightShadows)
                    //        {
                    //            childGroup.additionalLightsShadowCasterPass = passPair.additionalLightsShadowCasterPass;
                    //            renderer.EnqueuePass(passPair.additionalLightsShadowCasterPass);
                    //        }
                    //        else childGroup.additionalLightsShadowCasterPass = null;
                    //}

                    // Render Opaques
                    //renderer.EnqueuePass(drawOpaquesPass);

                    // Render Blank portals
                    if (child.invalidChildCount > 0)
                        renderer.EnqueuePass(portalBlankRenderPass);

                    if (order == 0)
                        EnqueueStencilNodesRecursive(childGroup, maxShadowDepth, disableTransparentShadows, order, undoNode);

                    // Render Transparents
                    //if (disableTransparentShadows)
                    //    renderer.EnqueuePass(disableShadowSettingsPass);

                    //renderer.EnqueuePass(drawTransparentsPass);

                    // Render Skybox
                    //renderer.EnqueuePass(drawSkyBoxPass);

                    if (order > 0)
                        EnqueueStencilNodesRecursive(childGroup, maxShadowDepth, disableTransparentShadows, order, undoNode);

                    // Complete group
                    if (child != undoNode)
                    {
                        passPair.completeRenderPass.clearDepthMaterial = _portalClearDepth;
                        passPair.completeRenderPass.decreaseMaterial = _portalDecrease;
                        passPair.completeRenderPass.depthMaterial = _portalDepthOnly;
                        renderer.EnqueuePass(passPair.completeRenderPass);
                    }
                    else
                    {
                        // Undo node for transitions through stereo
                        completeUndoStencilPass.decreaseMaterial = _portalDecrease;
                        renderer.EnqueuePass(completeUndoStencilPass);
                    }
                }
            }
        }

        //class PortalRenderPass : ScriptableRenderPass
        //{
        //    public NewPortalRenderFeature feature;

        //    public PortalRenderPass(NewPortalRenderFeature feature)
        //    {
        //        this.feature = feature;
        //    }

        //    // This class stores the data needed by the RenderGraph pass.
        //    // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
        //    private class PassData
        //    {
        //        public NewPortalRenderFeature feature;

        //        public Camera camera;
        //    }

        //    // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
        //    // It is used to execute draw commands.
        //    static void ExecutePass(PassData data, RasterGraphContext context)
        //    {

        //    }

        //    // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        //    // FrameData is a context container through which URP resources can be accessed and managed.
        //    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        //    {
        //        const string passName = "Render Custom Pass";

        //        // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
        //        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
        //        {
        //            passData.feature = feature;

        //            // Use this scope to set the required inputs and outputs of the pass and to
        //            // setup the passData with the required properties needed at pass execution time.

        //            // Make use of frameData to access resources and camera data through the dedicated containers.
        //            // Eg:
        //            // UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        //            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        //            passData.camera = frameData.Get<UniversalCameraData>().camera;

        //            // Setup pass inputs and outputs through the builder interface.
        //            // Eg:
        //            // builder.UseTexture(sourceTexture);
        //            // TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, cameraData.cameraTargetDescriptor, "Destination Texture", false);

        //            // This sets the render target of the pass to the active color texture. Change it to your own render target as needed.
        //            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

        //            // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
        //            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
        //        }
        //    }
        //}
    }
}