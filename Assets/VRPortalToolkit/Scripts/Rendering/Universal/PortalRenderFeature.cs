using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Data;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering.Universal
{
    public class PortalRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private RenderMode _renderMode;
        public RenderMode renderMode {
            get => _renderMode;
            set {
                if (_renderMode != value)
                {
                    _isDirty = true;
                    Validate.UpdateField(this, nameof(_renderMode), _renderMode = value);
                }
            }
        }

        [SerializeField] private PortalAlgorithm _algorithm = PortalAlgorithm.Predictive;
        public PortalAlgorithm algorithm {
            get => _algorithm;
            set => _algorithm = value;
        }

        public enum PortalAlgorithm
        {
            BreadthFirst = 0,
            Predictive = 1
        }

        [Header("Filtering"), SerializeField] private LayerMask _opaqueLayerMask = -1;
        public LayerMask opaqueLayerMask {
            get => _opaqueLayerMask;
            set => _opaqueLayerMask = value;
        }

        [SerializeField] private LayerMask _transparentLayerMask = -1;
        public LayerMask transparentLayerMask {
            get => _transparentLayerMask;
            set => _transparentLayerMask = value;
        }

        private bool _isDirty = false;

        // TODO: Remove render mode, instead have a stencil max, and when that runs out, use render textures
        public enum RenderMode
        {
            RenderTexture = 0,
            StencilEarly = 1, // Before opaque, better for shadows memory
            Stencil = 2, // Before transparent, less overdraw?
            StencilLate = 3, // After transparent
        }

        [Header("Scene Settings"), SerializeField] private int _minDepth = 1;
        public int minDepth {
            get => _minDepth > 0 ? _minDepth : _minDepth = 0;
            set => _minDepth = value;
        }

        [SerializeField] private int _maxDepth = 32;
        public int maxDepth {
            get => _maxDepth > 0 ? _maxDepth : _maxDepth = 0;
            set => _maxDepth = value;
        }

        [SerializeField] private int _maxRenders = 32;
        public int maxRenders {
            get => _maxRenders > 0 ? _maxRenders : _maxRenders = 0;
            set => _maxRenders = value;
        }

        [SerializeField] private int _maxShadowDepth = 16;
        public int maxShadowDepth {
            get => _maxShadowDepth > 0 ? _maxShadowDepth : _maxShadowDepth = 0;
            set => _maxShadowDepth = value;
        }

#if UNITY_EDITOR
        private bool showResolution => renderMode != RenderMode.Stencil;

        [ShowIf(nameof(showResolution), true, 1)]
#endif
        [SerializeField, Range(0f, 1f)] private float _portalResolution = 1f;
        public float portalResolution {
            get => _portalResolution;
            set => _portalResolution = Mathf.Clamp(value, 0f, 1f);
        }

        [SerializeField, Range(0f, 1f)] private float _bufferResolution = 1f;
        public float bufferResolution {
            get => _bufferResolution;
            set => _bufferResolution = Mathf.Clamp(value, 0f, 1f);
        }

        [Header("Editor Settings"), SerializeField] private int _editorMinDepth = 0;
        public int editorMinDepth {
            get => _editorMinDepth > 0 ? _editorMinDepth : _editorMinDepth = 0;
            set => _editorMinDepth = value;
        }

        [SerializeField] private int _editorMaxDepth = 16;
        public int editorMaxDepth {
            get => _editorMaxDepth > 0 ? _editorMaxDepth : _editorMaxDepth = 0;
            set => _editorMaxDepth = value;
        }

        [SerializeField] private int _editorMaxRenders = 16;
        public int editorMaxRenders {
            get => _editorMaxRenders > 0 ? _editorMaxRenders : _editorMaxRenders = 0;
            set => _editorMaxRenders = value;
        }

        [SerializeField] private int _editorMaxShadowDepth = 16;
        public int editorMaxShadowDepth {
            get => _editorMaxShadowDepth > 0 ? _editorMaxShadowDepth : _editorMaxShadowDepth = 0;
            set => _editorMaxShadowDepth = value;
        }

#if UNITY_EDITOR
        [ShowIf(nameof(showResolution), true, 1)]
#endif
        [SerializeField, Range(0f, 1f)] private float _editorPortalResolution = 1f;
        public float editorPortalResolution {
            get => _editorPortalResolution;
            set => _editorPortalResolution = Mathf.Clamp(value, 0f, 1f);
        }

        [SerializeField, Range(0f, 1f)] private float _editorBufferResolution = 1f;
        public float editorBufferResolution {
            get => _editorBufferResolution;
            set => _editorBufferResolution = Mathf.Clamp(value, 0f, 1f);
        }

        [Tooltip("Required for both Render Texture Portals, aswell as the buffer effect for Stencil Portals.")]
        [Header("Shaders"), SerializeField] private Material _portalStereo;
        public Material portalStereo {
            get => _portalStereo;
            set => _portalStereo = value;
        }

        [Tooltip("Required for Stencil Portals.")]
        [SerializeField] private Material _portalIncrease;
        public Material portalIncrease {
            get => _portalIncrease;
            set => _portalIncrease = value;
        }

        [Tooltip("Required for Stencil Portals.")]
        [SerializeField] private Material _portalDecrease;
        public Material portalDecrease {
            get => _portalDecrease;
            set => _portalDecrease = value;
        }

        [Tooltip("Required for Stencil Portals.")]
        [SerializeField] private Material _portalClearDepth;
        public Material portalClearDepth {
            get => _portalClearDepth;
            set => _portalClearDepth = value;
        }

        [Tooltip("Required for Stencil Portals.")]
        [SerializeField] private Material _portalDepthOnly;
        public Material portalDepthOnly {
            get => _portalDepthOnly;
            set => _portalDepthOnly = value;
        }

        protected PortalPassNode rootPassNode;

        public static Camera renderCamera;

        protected static ShaderTagId[] shaderByIds;

        protected BeginPortalPass beginPass;
        protected DrawObjectsInPortalPass drawOpaquesPass;
        protected DrawObjectsInPortalPass drawTransparentsPass;
        protected DrawSkyboxInPortalPass drawSkyBoxPass;
        protected ShadowSettingsInPortalPass disableShadowSettingsPass;
        protected ShadowSettingsInPortalPass enableShadowSettingsPass;
        protected CompletePortalPass completePass;
        protected StoreFramePass storePreviousFramePass;

        protected DrawDepthOnlyPortalsPass depthOnlyPass;

        // TODO: You cant undo configuring target, so need to have one for portals, and one for the real world 
        protected DrawBlankPortalsPass portalBlankRenderPass;
        protected DrawBlankPortalsPass blankRenderPass;

        // TODO: You cant undo configuring target, so need to have one for portals, and one for the real world 
        protected DrawTexturePortalsPass portalRenderBufferPass;
        protected DrawTexturePortalsPass renderBufferPass;

        protected List<PortalStencilPasses> portalStencilPasses = new List<PortalStencilPasses>();
        protected List<PortalRenderPasses> portalRenderPasses = new List<PortalRenderPasses>();

        protected BeginUndoStencilPortalPass beginUndoStencilPass;
        protected CompleteUndoStencilPortalPass completeUndoStencilPass;

        protected class PortalStencilPasses : PortalShadowPasses
        {
            public BeginStencilPortalPass beginRenderPass { get; }
            public CompleteStencilPortalPass completeRenderPass { get; }

            public PortalStencilPasses() : base()
            {
                beginRenderPass = new BeginStencilPortalPass();
                completeRenderPass = new CompleteStencilPortalPass();
            }
        }

        protected class PortalRenderPasses : PortalShadowPasses
        {
            public BeginTexturePortalPass beginRenderPass { get; }
            public CompleteTexturePortalPass completeRenderPass { get; }

            public PortalRenderPasses() : base()
            {
                beginRenderPass = new BeginTexturePortalPass();
                completeRenderPass = new CompleteTexturePortalPass();
            }
        }

        protected class PortalShadowPasses
        {
            public MainLightShadowCasterInPortalPass mainLightShadowCasterPass { get; }
            public AdditionalLightsShadowCasterInPortalPass additionalLightsShadowCasterPass { get; }

            public PortalShadowPasses()
            {
                mainLightShadowCasterPass = new MainLightShadowCasterInPortalPass();
                additionalLightsShadowCasterPass = new AdditionalLightsShadowCasterInPortalPass();
            }
        }

        public virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_renderMode), nameof(renderMode));
        }

        /// <inheritdoc/>
        public override void Create()
        {
            if (!renderCamera)
            {
                renderCamera = new GameObject("[Portal Render Camera]").AddComponent<Camera>();
                renderCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                renderCamera.gameObject.SetActive(false);
            }

            PortalPassGroupPool.Release(rootPassNode);
            rootPassNode = PortalPassGroupPool.Get();

            beginPass = new BeginPortalPass() { portalPassNode = rootPassNode };
            drawOpaquesPass = new DrawObjectsInPortalPass();
            drawTransparentsPass = new DrawObjectsInPortalPass();
            drawSkyBoxPass = new DrawSkyboxInPortalPass();
            portalBlankRenderPass = new DrawBlankPortalsPass();
            blankRenderPass = new DrawBlankPortalsPass();
            portalRenderBufferPass = new DrawTexturePortalsPass();
            depthOnlyPass = new DrawDepthOnlyPortalsPass();
            renderBufferPass = new DrawTexturePortalsPass();
            storePreviousFramePass = new StoreFramePass();
            enableShadowSettingsPass = new ShadowSettingsInPortalPass(true);
            disableShadowSettingsPass = new ShadowSettingsInPortalPass(false);
            completePass = new CompletePortalPass();
            beginUndoStencilPass = new BeginUndoStencilPortalPass();
            completeUndoStencilPass = new CompleteUndoStencilPortalPass();

            if (shaderByIds == null) shaderByIds = new ShaderTagId[]
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("LightweightForward")
            };

            _isDirty = false;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_isDirty) Create();

            renderer.EnqueuePass(beginPass);

            // Copy the camera
            Camera camera = renderingData.cameraData.camera;
            renderCamera.CopyFrom(camera);
            renderCamera.clearFlags = camera.clearFlags;
            renderCamera.targetTexture = null;

            if (renderingData.cameraData.xrRendering && XRGraphics.stereoRenderingMode != XRGraphics.StereoRenderingMode.MultiPass)
            {
                camera.TryGetCullingParameters(true, out var cullingParameters);
                renderCamera.projectionMatrix = cullingParameters.stereoProjectionMatrix;
                renderCamera.worldToCameraMatrix = cullingParameters.stereoViewMatrix;
            }
            else
            {
                renderCamera.worldToCameraMatrix = renderingData.cameraData.GetViewMatrix(0);
                renderCamera.projectionMatrix = renderingData.cameraData.GetProjectionMatrix(0);
            }

            if (renderingData.cameraData.xrRendering && XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.MultiPass)
            {
                if (renderingData.cameraData.GetProjectionMatrix().m02 <= 0f)
                    FrameBuffer.SetCurrent(camera, Camera.MonoOrStereoscopicEye.Left);
                else
                    FrameBuffer.SetCurrent(camera, Camera.MonoOrStereoscopicEye.Right);
            }
            else
                FrameBuffer.SetCurrent(camera);

            rootPassNode.mainLightShadowCasterPass = null;
            rootPassNode.additionalLightsShadowCasterPass = null;
            rootPassNode.stateBlock = new RenderStateBlock(RenderStateMask.Depth)
            {
                depthState = new DepthState(true, CompareFunction.Less),
                stencilReference = 0,
            };

            int minDepth, maxDepth, maxRenders, maxShadowDepth;
            float bufferResolution;

            if (renderingData.cameraData.isPreviewCamera || renderingData.cameraData.isSceneViewCamera)
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
                if (renderingData.cameraData.xrRendering && XRGraphics.stereoRenderingMode != XRGraphics.StereoRenderingMode.MultiPass)
                    rootPassNode.renderNode = PortalAlgorithms.GetStereoTree(camera, camera.transform.localToWorldMatrix, renderCamera.worldToCameraMatrix, renderCamera.projectionMatrix, renderCamera.cullingMask, renderingData.cameraData.GetViewMatrix(0), renderingData.cameraData.GetProjectionMatrix(0),
                        renderingData.cameraData.GetViewMatrix(1), renderingData.cameraData.GetProjectionMatrix(1), minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers());
                else
                    rootPassNode.renderNode = PortalAlgorithms.GetTree(camera, camera.transform.localToWorldMatrix, renderingData.cameraData.GetViewMatrix(0), renderingData.cameraData.GetProjectionMatrix(0), renderCamera.cullingMask, minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers());
            }
            else
            {
                // TODO: This could be used for eye tracking
                // Also, might be better to use a rect, instead of a position
                Vector2? focus = null;//new Vector2(0.5f, 0.5f);

                if (renderingData.cameraData.xrRendering && XRGraphics.stereoRenderingMode != XRGraphics.StereoRenderingMode.MultiPass)
                    rootPassNode.renderNode = PortalAlgorithms.GetPredictiveStereoTree(camera, camera.transform.localToWorldMatrix, renderCamera.worldToCameraMatrix, renderCamera.projectionMatrix, renderCamera.cullingMask, renderingData.cameraData.GetViewMatrix(0), renderingData.cameraData.GetProjectionMatrix(0),
                        renderingData.cameraData.GetViewMatrix(1), renderingData.cameraData.GetProjectionMatrix(1), minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers(), focus);
                else
                    rootPassNode.renderNode = PortalAlgorithms.GetPredictiveTree(camera, camera.transform.localToWorldMatrix, renderingData.cameraData.GetViewMatrix(0), renderingData.cameraData.GetProjectionMatrix(0), renderCamera.cullingMask, minDepth, maxDepth, maxRenders, PortalRendering.GetAllPortalRenderers(), focus);
            }

            rootPassNode.viewport = new Rect(0f, 0f, renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height);

            // TODO: Dont have access to ForwardRenderingData.shadowTransparentReceive
            bool disableTransparentShadows = false && (renderingData.shadowData.supportsMainLightShadows || renderingData.shadowData.supportsAdditionalLightShadows);

            Shader.SetGlobalInt(PropertyID.PortalStencilRef, 0);

            GetDrawingSettings(renderingData, camera, out DrawingSettings opaqueDrawing, out FilteringSettings opaqueFiltering, out DrawingSettings transparentDrawing, out FilteringSettings transparentFiltering);
            drawOpaquesPass.drawingSettings = opaqueDrawing;
            drawOpaquesPass.filteringSettings = opaqueFiltering;
            drawTransparentsPass.drawingSettings = transparentDrawing;
            drawTransparentsPass.filteringSettings = transparentFiltering;

            blankRenderPass.material = _portalStereo;
            renderBufferPass.material = _portalStereo;
            portalBlankRenderPass.material = _portalStereo;
            portalRenderBufferPass.material = _portalStereo;

            depthOnlyPass.depthOnlyMaterial = _portalDepthOnly;

            if (_renderMode == RenderMode.RenderTexture)
            {
                if (portalStereo) portalStereo.SetInt(PropertyID.StencilComp, (int)CompareFunction.Disabled);

                EnqueueRenderNodes(renderer, ref renderingData, rootPassNode, maxShadowDepth, disableTransparentShadows);
            }
            else
            {
                if (portalStereo) portalStereo.SetInt(PropertyID.StencilComp, (int)CompareFunction.Equal);

                if (_renderMode == RenderMode.Stencil)
                    EnqueueStencilNodes(renderer, ref renderingData, rootPassNode, maxShadowDepth, disableTransparentShadows, 0);
                else if (_renderMode == RenderMode.StencilEarly)
                    EnqueueStencilNodes(renderer, ref renderingData, rootPassNode, maxShadowDepth, disableTransparentShadows, 1);
                else
                    EnqueueStencilNodes(renderer, ref renderingData, rootPassNode, maxShadowDepth, disableTransparentShadows, -1);
            }

            if (bufferResolution > 0f)
            {
                //storePreviousFramePass.rootRenderNode = rootPassNode.renderNode;
                storePreviousFramePass.resolution = bufferResolution;
                renderer.EnqueuePass(storePreviousFramePass);
            }

            renderer.EnqueuePass(completePass);
        }

        private void GetDrawingSettings(in RenderingData renderingData, Camera camera, out DrawingSettings opaqueDrawingSettings, out FilteringSettings opaqueFilteringSettings, out DrawingSettings transparentDrawingSettings, out FilteringSettings transparentFilteringSettings)
        {
            opaqueDrawingSettings = new DrawingSettings(shaderByIds[0],
                            new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque })
            {
                perObjectData = renderingData.perObjectData,
                mainLightIndex = renderingData.lightData.mainLightIndex,
                enableDynamicBatching = renderingData.supportsDynamicBatching,
                enableInstancing = !renderingData.cameraData.isPreviewCamera
            };
            for (int i = 1; i < shaderByIds.Length; i++)
                opaqueDrawingSettings.SetShaderPassName(i, shaderByIds[i]);

            transparentDrawingSettings = opaqueDrawingSettings;
            transparentDrawingSettings.sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonTransparent };

            // Create portal settings to reuse for all portals
            opaqueFilteringSettings = new FilteringSettings(RenderQueueRange.opaque, opaqueLayerMask);
            transparentFilteringSettings = new FilteringSettings(RenderQueueRange.transparent, transparentLayerMask);
        }

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
                    if (portalRenderer.portal == connected)
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


        public static PortalRenderNode GetOrAddChild(PortalRenderNode parent, IPortalRenderer renderer)
        {
            if (parent.isStereo)
            {
                if (((1 << renderer.layer) & parent.cullingMask) == 0) return null;

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
                if (((1 << renderer.layer) & parent.cullingMask) == 0) return null;

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

        internal void EnqueueStencilNodesRecursive(ScriptableRenderer renderer, ref RenderingData renderingData, PortalPassNode passGroup, int maxShadowDepth, bool disableTransparentShadows, int order, PortalRenderNode undoNode = null)
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
                        EnqueueStencilNodesRecursive(renderer, ref renderingData, childGroup, maxShadowDepth, disableTransparentShadows, order, undoNode);

                    // TODO: For now, undo node does not support shadows. For some reason, it leads to:
                    // "ArgumentException: RenderTextureDesc width must be greater than zero." in Shadow Utils
                    if (child != undoNode)
                    {
                        // Main shadows
                        if (child.depth <= maxShadowDepth && renderingData.shadowData.supportsMainLightShadows)
                        {
                            childGroup.mainLightShadowCasterPass = passPair.mainLightShadowCasterPass;
                            renderer.EnqueuePass(childGroup.mainLightShadowCasterPass);
                        }
                        else childGroup.mainLightShadowCasterPass = null;

                        if (child != undoNode)
                            // Additional shadows
                            if (child.depth <= maxShadowDepth && renderingData.shadowData.supportsAdditionalLightShadows)
                            {
                                childGroup.additionalLightsShadowCasterPass = passPair.additionalLightsShadowCasterPass;
                                renderer.EnqueuePass(passPair.additionalLightsShadowCasterPass);
                            }
                            else childGroup.additionalLightsShadowCasterPass = null;
                    }

                    // Render Opaques
                    renderer.EnqueuePass(drawOpaquesPass);

                    // Render Blank portals
                    if (child.invalidChildCount > 0)
                        renderer.EnqueuePass(portalBlankRenderPass);

                    if (order == 0)
                        EnqueueStencilNodesRecursive(renderer, ref renderingData, childGroup, maxShadowDepth, disableTransparentShadows, order, undoNode);

                    // Render Transparents
                    if (disableTransparentShadows)
                        renderer.EnqueuePass(disableShadowSettingsPass);

                    renderer.EnqueuePass(drawTransparentsPass);

                    // Render Skybox
                    renderer.EnqueuePass(drawSkyBoxPass);

                    if (order > 0)
                        EnqueueStencilNodesRecursive(renderer, ref renderingData, childGroup, maxShadowDepth, disableTransparentShadows, order, undoNode);

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

        protected virtual void EnqueueRenderNodes(ScriptableRenderer renderer, ref RenderingData renderingData, PortalPassNode passGroup, int maxShadowDepth, bool disableTransparentShadows)
        {
            float resolution = (renderingData.cameraData.isPreviewCamera || renderingData.cameraData.isSceneViewCamera) ? editorPortalResolution : portalResolution;

            foreach (PortalRenderNode child in passGroup.renderNode.GetPostorderDepthFirst())
            {
                if (child.isValid && child != passGroup.renderNode)
                {
                    int index = child.validIndex - 1;

                    while (portalRenderPasses.Count <= index)
                        portalRenderPasses.Add(new PortalRenderPasses());

                    // Begin group
                    PortalRenderPasses passPair = portalRenderPasses[index];
                    PortalPassNode childGroup = passPair.beginRenderPass.portalPassNode = PortalPassGroupPool.Get();
                    childGroup.renderNode = child;

                    // Setup state block
                    childGroup.stateBlock = new RenderStateBlock(RenderStateMask.Depth)
                    {
                        depthState = new DepthState(true, CompareFunction.Less),
                        stencilReference = child.depth,
                    };

                    passPair.beginRenderPass.Resolution = resolution;
                    renderer.EnqueuePass(passPair.beginRenderPass);

                    // Main shadows
                    if (child.depth <= maxShadowDepth && renderingData.shadowData.supportsMainLightShadows)
                    {
                        childGroup.mainLightShadowCasterPass = passPair.mainLightShadowCasterPass;
                        renderer.EnqueuePass(childGroup.mainLightShadowCasterPass);
                    }
                    else childGroup.mainLightShadowCasterPass = null;

                    // Additional shadows
                    if (child.depth <= maxShadowDepth && renderingData.shadowData.supportsAdditionalLightShadows)
                    {
                        childGroup.additionalLightsShadowCasterPass = passPair.additionalLightsShadowCasterPass;
                        renderer.EnqueuePass(passPair.additionalLightsShadowCasterPass);
                    }
                    else childGroup.additionalLightsShadowCasterPass = null;

                    // Render Opaques
                    renderer.EnqueuePass(drawOpaquesPass);

                    // Render Child Portals
                    if (child.validChildCount > 0)
                        renderer.EnqueuePass(portalRenderBufferPass);

                    if (child.invalidChildCount > 0)
                        renderer.EnqueuePass(portalBlankRenderPass);

                    // Render Transparents
                    if (disableTransparentShadows)
                        renderer.EnqueuePass(disableShadowSettingsPass);

                    renderer.EnqueuePass(drawTransparentsPass);

                    // Render Skybox
                    renderer.EnqueuePass(drawSkyBoxPass);

                    // Complete group
                    renderer.EnqueuePass(passPair.completeRenderPass);
                }
            }

            if (passGroup.renderNode.validChildCount > 0)
                renderer.EnqueuePass(renderBufferPass);

            if (passGroup.renderNode.invalidChildCount > 0)
                renderer.EnqueuePass(blankRenderPass);
        }
    }
}
