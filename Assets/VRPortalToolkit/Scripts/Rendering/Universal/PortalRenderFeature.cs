using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    public class PortalRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private RenderMode _renderMode;
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

        [SerializeField] private PortalAlgorithm _algorithm = PortalAlgorithm.Predictive;
        public PortalAlgorithm algorithm
        {
            get => _algorithm;
            set => _algorithm = value;
        }

        public enum PortalAlgorithm
        {
            BreadthFirst = 0,
            Predictive = 1
        }

        [Header("Filtering"), SerializeField] private LayerMask _opaqueLayerMask = -1;
        public LayerMask opaqueLayerMask
        {
            get => _opaqueLayerMask;
            set => _opaqueLayerMask = value;
        }

        [SerializeField] private LayerMask _transparentLayerMask = -1;
        public LayerMask transparentLayerMask
        {
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
        public int minDepth
        {
            get => _minDepth > 0 ? _minDepth : _minDepth = 0;
            set => _minDepth = value;
        }

        [SerializeField] private int _maxDepth = 32;
        public int maxDepth
        {
            get => _maxDepth > 0 ? _maxDepth : _maxDepth = 0;
            set => _maxDepth = value;
        }

        [SerializeField] private int _maxRenders = 32;
        public int maxRenders
        {
            get => _maxRenders > 0 ? _maxRenders : _maxRenders = 0;
            set => _maxRenders = value;
        }

        [SerializeField] private int _maxShadowDepth = 16;
        public int maxShadowDepth
        {
            get => _maxShadowDepth > 0 ? _maxShadowDepth : _maxShadowDepth = 0;
            set => _maxShadowDepth = value;
        }

#if UNITY_EDITOR
        private bool showResolution => renderMode != RenderMode.Stencil;

        [ShowIf(nameof(showResolution), true, 1)]
#endif
        [SerializeField, Range(0f, 1f)] private float _portalResolution = 1f;
        public float portalResolution
        {
            get => _portalResolution;
            set => _portalResolution = Mathf.Clamp(value, 0f, 1f);
        }

        [SerializeField, Range(0f, 1f)] private float _bufferResolution = 1f;
        public float bufferResolution
        {
            get => _bufferResolution;
            set => _bufferResolution = Mathf.Clamp(value, 0f, 1f);
        }

        [Header("Editor Settings"), SerializeField] private int _editorMinDepth = 0;
        public int editorMinDepth
        {
            get => _editorMinDepth > 0 ? _editorMinDepth : _editorMinDepth = 0;
            set => _editorMinDepth = value;
        }

        [SerializeField] private int _editorMaxDepth = 16;
        public int editorMaxDepth
        {
            get => _editorMaxDepth > 0 ? _editorMaxDepth : _editorMaxDepth = 0;
            set => _editorMaxDepth = value;
        }

        [SerializeField] private int _editorMaxRenders = 16;
        public int editorMaxRenders
        {
            get => _editorMaxRenders > 0 ? _editorMaxRenders : _editorMaxRenders = 0;
            set => _editorMaxRenders = value;
        }

        [SerializeField] private int _editorMaxShadowDepth = 16;
        public int editorMaxShadowDepth
        {
            get => _editorMaxShadowDepth > 0 ? _editorMaxShadowDepth : _editorMaxShadowDepth = 0;
            set => _editorMaxShadowDepth = value;
        }

#if UNITY_EDITOR
        [ShowIf(nameof(showResolution), true, 1)]
#endif
        [SerializeField, Range(0f, 1f)] private float _editorPortalResolution = 1f;
        public float editorPortalResolution
        {
            get => _editorPortalResolution;
            set => _editorPortalResolution = Mathf.Clamp(value, 0f, 1f);
        }

        [SerializeField, Range(0f, 1f)] private float _editorBufferResolution = 1f;
        public float editorBufferResolution
        {
            get => _editorBufferResolution;
            set => _editorBufferResolution = Mathf.Clamp(value, 0f, 1f);
        }

        [Tooltip("Required for both Render Texture Portals, aswell as the buffer effect for Stencil Portals.")]
        [Header("Shaders"), SerializeField] private Material _portalStereo;
        public Material portalStereo
        {
            get => _portalStereo;
            set => _portalStereo = value;
        }

        [Tooltip("Required for Stencil Portals.")]
        [SerializeField] private Material _portalIncrease;
        public Material portalIncrease
        {
            get => _portalIncrease;
            set => _portalIncrease = value;
        }

        [Tooltip("Required for Stencil Portals.")]
        [SerializeField] private Material _portalDecrease;
        public Material portalDecrease
        {
            get => _portalDecrease;
            set => _portalDecrease = value;
        }

        [Tooltip("Required for Stencil Portals.")]
        [SerializeField] private Material _portalClearDepth;
        public Material portalClearDepth
        {
            get => _portalClearDepth;
            set => _portalClearDepth = value;
        }

        [Tooltip("Required for Stencil Portals.")]
        [SerializeField] private Material _portalDepthOnly;
        public Material portalDepthOnly
        {
            get => _portalDepthOnly;
            set => _portalDepthOnly = value;
        }

        [System.NonSerialized] public DrawingSettings opaqueDrawingSettings;

        [System.NonSerialized] public FilteringSettings opaqueFilteringSettings;

        [System.NonSerialized] public DrawingSettings transparentDrawingSettings;

        [System.NonSerialized] public FilteringSettings transparentFilteringSettings;

        [System.NonSerialized] public PortalPassGroup currentGroup;

        //[System.NonSerialized] public Buffer currentBuffer;

        [System.NonSerialized] public Camera renderCamera;

        protected static ShaderTagId[] shaderByIds;

        protected DrawOpaqueObjectsInPortalPass opaqueRenderPasses;
        protected DrawTransparentObjectsInPortalPass renderTransparentsPass;
        protected StoreFramePass storePreviousFramePass;
        protected DrawSkyboxInPortalPass drawSkyBoxPass;
        protected ShadowSettingsInPortalPass disableShadowSettingsPass;
        protected ShadowSettingsInPortalPass enableShadowSettingsPass;

        protected DrawDepthOnlyPortalsPass depthOnlyPass;

        // TODO: You cant undo configuring target, so need to have one for portals, and one for the real world 
        protected DrawBlankPortalsPass portalBlankRenderPass;
        protected DrawBlankPortalsPass blankRenderPass;

        // TODO: You cant undo configuring target, so need to have one for portals, and one for the real world 
        protected DrawTexturePortalsPass portalRenderBufferPass;
        protected DrawTexturePortalsPass renderBufferPass;

        protected List<PortalStencilPasses> portalStencilPasses = new List<PortalStencilPasses>();
        protected List<PortalRenderPasses> portalRenderPasses = new List<PortalRenderPasses>();

        protected class PortalStencilPasses : PortalShadowPasses
        {
            public BeginStencilPortalPass beginRenderPass { get; }
            public CompleteStencilPortalPass completeRenderPass { get; }

            public PortalStencilPasses(PortalRenderFeature feature) : base(feature)
            {
                beginRenderPass = new BeginStencilPortalPass(feature);
                completeRenderPass = new CompleteStencilPortalPass(feature);
            }
        }

        protected class PortalRenderPasses : PortalShadowPasses
        {
            public BeginTexturePortalPass beginRenderPass { get; }
            public CompleteTexturePortalPass completeRenderPass { get; }

            public PortalRenderPasses(PortalRenderFeature feature) : base(feature)
            {
                beginRenderPass = new BeginTexturePortalPass(feature);
                completeRenderPass = new CompleteTexturePortalPass(feature);
            }
        }

        protected class PortalShadowPasses
        {
            public MainLightShadowCasterInPortalPass mainLightShadowCasterPass { get; }
            public AdditionalLightsShadowCasterInPortalPass additionalLightsShadowCasterPass { get; }

            public PortalShadowPasses(PortalRenderFeature feature)
            {
                mainLightShadowCasterPass = new MainLightShadowCasterInPortalPass(feature);
                additionalLightsShadowCasterPass = new AdditionalLightsShadowCasterInPortalPass(feature);
            }
        }

        public virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_renderMode), nameof(renderMode));
        }

        /// <inheritdoc/>
        public override void Create()
        {
            if (renderCamera == null) renderCamera = new GameObject("[Portal Render Camera]").AddComponent<Camera>();
            renderCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            renderCamera.gameObject.SetActive(false);

            opaqueRenderPasses = new DrawOpaqueObjectsInPortalPass(this);
            renderTransparentsPass = new DrawTransparentObjectsInPortalPass(this);
            drawSkyBoxPass = new DrawSkyboxInPortalPass(this);
            portalBlankRenderPass = new DrawBlankPortalsPass(this);
            blankRenderPass = new DrawBlankPortalsPass(this);
            portalRenderBufferPass = new DrawTexturePortalsPass(this);
            depthOnlyPass = new DrawDepthOnlyPortalsPass(this);
            renderBufferPass = new DrawTexturePortalsPass(this);
            storePreviousFramePass = new StoreFramePass(this);
            enableShadowSettingsPass = new ShadowSettingsInPortalPass(this, true);
            disableShadowSettingsPass = new ShadowSettingsInPortalPass(this, false);

            shaderByIds = new ShaderTagId[]
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

            // Copy the camera
            Camera camera = renderingData.cameraData.camera;
            renderCamera.CopyFrom(camera);
            renderCamera.clearFlags = camera.clearFlags;
            renderCamera.worldToCameraMatrix = renderingData.cameraData.GetViewMatrix(0);
            renderCamera.projectionMatrix = renderingData.cameraData.GetProjectionMatrix(0);
            renderCamera.targetTexture = null;

            if (renderingData.cameraData.xrRendering && XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.MultiPass)
            {
                if (renderingData.cameraData.GetProjectionMatrix().m02 <= 0f)
                    FrameBuffer.SetCurrent(camera, Camera.MonoOrStereoscopicEye.Left);
                else
                    FrameBuffer.SetCurrent(camera, Camera.MonoOrStereoscopicEye.Right);
            }
            else
                FrameBuffer.SetCurrent(camera);

            if (currentGroup == null) currentGroup = PortalPassGroupPool.Get();
            currentGroup.mainLightShadowCasterPass = null;
            currentGroup.additionalLightsShadowCasterPass = null;
            currentGroup.parent = null;
            currentGroup.stateBlock = new RenderStateBlock(RenderStateMask.Depth)
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
                    currentGroup.renderNode = PortalAlgorithms.GetStereoTree(renderCamera.transform.localToWorldMatrix, renderCamera.worldToCameraMatrix, renderCamera.projectionMatrix, renderCamera.cullingMask, renderingData.cameraData.GetViewMatrix(0), renderingData.cameraData.GetProjectionMatrix(0),
                        renderingData.cameraData.GetViewMatrix(1), renderingData.cameraData.GetProjectionMatrix(1), minDepth, maxDepth, maxRenders, PortalRenderer.allRenderers);
                else
                    currentGroup.renderNode = PortalAlgorithms.GetTree(renderCamera.transform.localToWorldMatrix, renderingData.cameraData.GetViewMatrix(0), renderingData.cameraData.GetProjectionMatrix(0), renderCamera.cullingMask, minDepth, maxDepth, maxRenders, PortalRenderer.allRenderers);
            }
            else
            {
                // TODO: This could be used for eye tracking
                // Also, might be better to use a rect, instead of a position
                Vector2? focus = null;//new Vector2(0.5f, 0.5f);

                if (renderingData.cameraData.xrRendering && XRGraphics.stereoRenderingMode != XRGraphics.StereoRenderingMode.MultiPass)
                    currentGroup.renderNode = PortalAlgorithms.GetPredictiveStereoTree(renderCamera.transform.localToWorldMatrix, renderCamera.worldToCameraMatrix, renderCamera.projectionMatrix, renderCamera.cullingMask, renderingData.cameraData.GetViewMatrix(0), renderingData.cameraData.GetProjectionMatrix(0),
                        renderingData.cameraData.GetViewMatrix(1), renderingData.cameraData.GetProjectionMatrix(1), minDepth, maxDepth, maxRenders, PortalRenderer.allRenderers, focus);
                else
                    currentGroup.renderNode = PortalAlgorithms.GetPredictiveTree(renderCamera.transform.localToWorldMatrix, renderingData.cameraData.GetViewMatrix(0), renderingData.cameraData.GetProjectionMatrix(0), renderCamera.cullingMask, minDepth, maxDepth, maxRenders, PortalRenderer.allRenderers, focus);
            }

            currentGroup.viewport = new Rect(0f, 0f, renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height);

            // TODO: Dont have access to ForwardRenderingData.shadowTransparentReceive
            bool disableTransparentShadows = false && (renderingData.shadowData.supportsMainLightShadows || renderingData.shadowData.supportsAdditionalLightShadows);

            Shader.SetGlobalInt(PropertyID.PortalStencilRef, 0);

            if (_renderMode == RenderMode.RenderTexture)
            {
                if (portalStereo) portalStereo.SetInt(PropertyID.StencilComp, (int)CompareFunction.Disabled);

                EnqueueRenderNodes(renderer, ref renderingData, currentGroup, maxShadowDepth, disableTransparentShadows);
            }
            else
            {
                if (portalStereo) portalStereo.SetInt(PropertyID.StencilComp, (int)CompareFunction.Equal);

                if (_renderMode == RenderMode.Stencil)
                    EnqueueStencilNodes(renderer, ref renderingData, currentGroup, maxShadowDepth, disableTransparentShadows, 0);
                else if (_renderMode == RenderMode.StencilEarly)
                    EnqueueStencilNodes(renderer, ref renderingData, currentGroup, maxShadowDepth, disableTransparentShadows, 1);
                else
                    EnqueueStencilNodes(renderer, ref renderingData, currentGroup, maxShadowDepth, disableTransparentShadows, -1);
            }

            if (bufferResolution > 0f)
                renderer.EnqueuePass(storePreviousFramePass);

            // Create setting
            DrawingSettings transparent, opaque = new DrawingSettings(shaderByIds[0],
                new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque })
            {
                perObjectData = renderingData.perObjectData,
                mainLightIndex = renderingData.lightData.mainLightIndex,
                enableDynamicBatching = renderingData.supportsDynamicBatching,
                enableInstancing = !renderingData.cameraData.isPreviewCamera
            };

            for (int i = 1; i < shaderByIds.Length; i++)
                opaque.SetShaderPassName(i, shaderByIds[i]);

            transparent = opaque;
            transparent.sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonTransparent };

            // Create portal settings to reuse for all portals
            opaqueDrawingSettings = opaque;
            opaqueFilteringSettings = new FilteringSettings(RenderQueueRange.opaque, opaqueLayerMask);
            transparentDrawingSettings = transparent;
            transparentFilteringSettings = new FilteringSettings(RenderQueueRange.transparent, transparentLayerMask);
        }

        protected virtual void EnqueueStencilNodes(ScriptableRenderer renderer, ref RenderingData renderingData, PortalPassGroup passGroup, int maxShadowDepth, bool disableTransparentShadows, int order)
        {
            EnqueueStencilNodesRecursive(renderer, ref renderingData, passGroup, maxShadowDepth, disableTransparentShadows, order);

            if (passGroup.renderNode.invalidChildCount > 0)
                renderer.EnqueuePass(blankRenderPass);
        }

        protected virtual void EnqueueStencilNodesRecursive(ScriptableRenderer renderer, ref RenderingData renderingData, PortalPassGroup passGroup, int maxShadowDepth, bool disableTransparentShadows, int order)
        {
            renderer.EnqueuePass(depthOnlyPass);

            foreach (PortalRenderNode child in passGroup.renderNode.children)
            {
                if (child.isValid)
                {
                    int index = child.validIndex - 1;

                    while (portalStencilPasses.Count <= index)
                        portalStencilPasses.Add(new PortalStencilPasses(this));

                    // Begin group
                    PortalStencilPasses passPair = portalStencilPasses[index];
                    PortalPassGroup childGroup = passPair.beginRenderPass.passGroup = PortalPassGroupPool.Get();
                    childGroup.renderNode = child;
                    childGroup.parent = passGroup;

                    // Setup state block
                    childGroup.stateBlock = new RenderStateBlock(RenderStateMask.Depth | RenderStateMask.Stencil)
                    {
                        depthState = new DepthState(true, CompareFunction.Less),
                        stencilReference = child.depth,
                        stencilState = new StencilState(true, 255, 255, CompareFunction.Equal),
                    };

                    renderer.EnqueuePass(passPair.beginRenderPass);

                    // Recursive
                    if (order < 0)
                        EnqueueStencilNodesRecursive(renderer, ref renderingData, childGroup, maxShadowDepth, disableTransparentShadows, order);
                    
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
                    renderer.EnqueuePass(opaqueRenderPasses);

                    // Render Blank portals
                    if (child.invalidChildCount > 0)
                        renderer.EnqueuePass(portalBlankRenderPass);

                    if (order == 0)
                        EnqueueStencilNodesRecursive(renderer, ref renderingData, childGroup, maxShadowDepth, disableTransparentShadows, order);

                    // Render Transparents
                    if (disableTransparentShadows)
                        renderer.EnqueuePass(disableShadowSettingsPass);

                    renderer.EnqueuePass(renderTransparentsPass);

                    // Render Skybox
                    renderer.EnqueuePass(drawSkyBoxPass);

                    if (order > 0)
                        EnqueueStencilNodesRecursive(renderer, ref renderingData, childGroup, maxShadowDepth, disableTransparentShadows, order);

                    // Complete group
                    renderer.EnqueuePass(passPair.completeRenderPass);
                }
            }
        }

        protected virtual void EnqueueRenderNodes(ScriptableRenderer renderer, ref RenderingData renderingData, PortalPassGroup passGroup, int maxShadowDepth, bool disableTransparentShadows)
        {
            foreach (PortalRenderNode child in passGroup.renderNode.GetPostorderDepthFirst())
            {
                if (child.isValid && child != passGroup.renderNode)
                {
                    int index = child.validIndex - 1;

                    while (portalRenderPasses.Count <= index)
                        portalRenderPasses.Add(new PortalRenderPasses(this));

                    // Begin group
                    PortalRenderPasses passPair = portalRenderPasses[index];
                    PortalPassGroup childGroup = passPair.beginRenderPass.passGroup = PortalPassGroupPool.Get();
                    childGroup.renderNode = child;
                    childGroup.parent = passGroup; // HMMM

                    // Setup state block
                    childGroup.stateBlock = new RenderStateBlock(RenderStateMask.Depth)
                    {
                        depthState = new DepthState(true, CompareFunction.Less),
                        stencilReference = child.depth,
                    };

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
                    renderer.EnqueuePass(opaqueRenderPasses);

                    // Render Child Portals
                    if (child.validChildCount > 0)
                        renderer.EnqueuePass(portalRenderBufferPass);

                    if (child.invalidChildCount > 0)
                        renderer.EnqueuePass(portalBlankRenderPass);

                    // Render Transparents
                    if (disableTransparentShadows)
                        renderer.EnqueuePass(disableShadowSettingsPass);

                    renderer.EnqueuePass(renderTransparentsPass);

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
