using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Static utility class for managing the current stack of portal pass nodes.
    /// </summary>
    public static class PortalPassStack
    {
        private static List<PortalPassNode> portalPassNodes = new List<PortalPassNode>();

        /// <summary>
        /// Clears all nodes from the stack.
        /// </summary>
        public static void Clear()
        {
            portalPassNodes.Clear();
        }

        /// <summary>
        /// Pushes a node onto the stack.
        /// </summary>
        /// <param name="node">The node to push.</param>
        public static void Push(PortalPassNode node)
        {
            if (node != null) portalPassNodes.Add(node);
        }

        /// <summary>
        /// Pops the top node from the stack.
        /// </summary>
        /// <returns>The removed node, or null if the stack is empty or contains only one node.</returns>
        public static PortalPassNode Pop()
        {
            if (portalPassNodes.Count > 1)
            {
                PortalPassNode removed = portalPassNodes[portalPassNodes.Count - 1];
                portalPassNodes.RemoveAt(portalPassNodes.Count - 1);
                return removed;
            }

            return null;
        }

        /// <summary>
        /// Gets the parent node of the current node.
        /// </summary>
        public static PortalPassNode Parent
        {
            get
            {
                if (portalPassNodes.Count > 1)
                    return portalPassNodes[portalPassNodes.Count - 2];

                return null;
            }
        }

        /// <summary>
        /// Gets the current (top) node on the stack.
        /// </summary>
        public static PortalPassNode Current
        {
            get
            {
                if (portalPassNodes.Count > 0)
                    return portalPassNodes[portalPassNodes.Count - 1];

                return null;
            }
        }
    }

    /// <summary>
    /// Represents a node in the portal rendering pass hierarchy.
    /// Contains information about render state, viewport, and shadow caster passes.
    /// </summary>
    public class PortalPassNode
    {
        /// <summary>
        /// The portal render node associated with this pass node.
        /// </summary>
        public PortalRenderNode renderNode;

        /// <summary>
        /// The render state block to use for this pass.
        /// </summary>
        public RenderStateBlock stateBlock;

        //public PortalPassGroup parent;

        /// <summary>
        /// The viewport rect for this pass.
        /// </summary>
        public Rect viewport = new Rect(0, 0, 1, 1);

        /// <summary>
        /// The main light shadow caster pass for this portal.
        /// </summary>
        public MainLightShadowCasterInPortalPass mainLightShadowCasterPass;
        
        /// <summary>
        /// The additional lights shadow caster pass for this portal.
        /// </summary>
        public AdditionalLightsShadowCasterInPortalPass additionalLightsShadowCasterPass;

        private RenderTexture _colorTexture;
        /// <summary>
        /// The color texture used for this portal rendering pass.
        /// </summary>
        public RenderTexture colorTexture
        {
            get => _colorTexture;
            set
            {
                if (_colorTexture != value)
                {
                    _colorTexture = value;
                    _colorTarget = new RenderTargetIdentifier(_colorTexture, 0, CubemapFace.Unknown, -1);
                }
            }
        }

        private RenderTargetIdentifier _colorTarget;
        /// <summary>
        /// The render target identifier for the color texture.
        /// </summary>
        public RenderTargetIdentifier colorTarget => _colorTarget;

        private RenderingData _renderingData;

        private Vector4 _lightPos;
        private Vector4 _lightColor;
        private Vector4 _lightOcclusionChannel;
        private Vector4 _additionalLightsCount;
        private Vector4 _worldSpaceCameraPos;

        //private ComputeBuffer _lightDataBuffer;
        //private ComputeBuffer _lightIndicesBuffer;

        private Vector4[] _additionalLightPositions;
        private Vector4[] _additionalLightColors;
        private Vector4[] _additionalLightAttenuations;
        private Vector4[] _additionalLightSpotDirections;
        private Vector4[] _additionalLightOcclusionProbeChannels;

        private static List<Vector4> tempList = new List<Vector4>();

        /// <summary>
        /// Sets the view and projection matrices in the command buffer based on the render node.
        /// </summary>
        /// <param name="cmd">The command buffer to modify.</param>
        /// <param name="setViewport">Whether to also set the viewport.</param>
        public void SetViewAndProjectionMatrices(CommandBuffer cmd, bool setViewport = true)
        {
            cmd.SetViewProjectionMatrices(renderNode.worldToCameraMatrix, renderNode.projectionMatrix);

            // TODO: I dont think this actually offered any improvement
            if (setViewport) cmd.SetViewport(viewport);

            if (renderNode.isStereo)
            {
                cmd.SetStereoViewProjectionMatrices(renderNode.GetStereoViewMatrix(0), renderNode.GetStereoProjectionMatrix(0),
                    renderNode.GetStereoViewMatrix(1), renderNode.GetStereoProjectionMatrix(1));
            }
        }

        /// <summary>
        /// Stores the current rendering state for later restoration.
        /// </summary>
        /// <param name="renderingData">The rendering data to store.</param>
        public void StoreState(ref RenderingData renderingData)
        {
            _renderingData = renderingData;

            _lightPos = Shader.GetGlobalVector(PropertyID.MainLightPosition);
            _lightColor = Shader.GetGlobalVector(PropertyID.MainLightColor);
            _lightOcclusionChannel = Shader.GetGlobalVector(PropertyID.MainLightOcclusionProbesChannel);

            _additionalLightsCount = Shader.GetGlobalVector(PropertyID.AdditionalLightsCount);
            if (_additionalLightsCount.x != 0f)
            {
                //_lightDataBuffer = Shader.GetGlobalBuffer(AdditionalLightsBufferId);
                //_lightIndicesBuffer = Shader.GetGlobalBuffer(AdditionalLightsIndicesId);

                GetGlobalVectorArray(PropertyID.AdditionalLightsPosition, ref _additionalLightPositions);
                GetGlobalVectorArray(PropertyID.AdditionalLightsColor, ref _additionalLightColors);
                GetGlobalVectorArray(PropertyID.AdditionalLightsAttenuation, ref _additionalLightAttenuations);
                GetGlobalVectorArray(PropertyID.AdditionalLightsSpotDir, ref _additionalLightSpotDirections);
                GetGlobalVectorArray(PropertyID.AdditionalLightOcclusionProbeChannel, ref _additionalLightOcclusionProbeChannels);
            }

            _worldSpaceCameraPos = Shader.GetGlobalVector(PropertyID.WorldSpaceCameraPos);
        }

        private void GetGlobalVectorArray(int id, ref Vector4[] array)
        {
            Shader.GetGlobalVectorArray(id, tempList);

            if (array == null || array.Length != tempList.Count)
                array = new Vector4[tempList.Count];

            for (int i = 0; i < array.Length; i++)
                array[i] = tempList[i];

            tempList.Clear();
        }

        /// <summary>
        /// Restores the previously stored rendering state.
        /// </summary>
        /// <param name="cmd">The command buffer to modify.</param>
        /// <param name="renderingData">Output parameter that will be set to the stored rendering data.</param>
        public void RestoreState(CommandBuffer cmd, ref RenderingData renderingData)
        {
            renderingData = _renderingData;

            cmd.SetGlobalVector(PropertyID.MainLightPosition, _lightPos);
            cmd.SetGlobalVector(PropertyID.MainLightColor, _lightColor);
            cmd.SetGlobalVector(PropertyID.MainLightOcclusionProbesChannel, _lightOcclusionChannel);

            cmd.SetGlobalVector(PropertyID.AdditionalLightsCount, _additionalLightsCount);
            if (_additionalLightsCount.x != 0f)
            {
                //cmd.SetGlobalBuffer(AdditionalLightsBufferId, _lightDataBuffer);
                //cmd.SetGlobalBuffer(AdditionalLightsIndicesId, _lightIndicesBuffer);

                cmd.SetGlobalVectorArray(PropertyID.AdditionalLightsPosition, _additionalLightPositions);
                cmd.SetGlobalVectorArray(PropertyID.AdditionalLightsColor, _additionalLightColors);
                cmd.SetGlobalVectorArray(PropertyID.AdditionalLightsAttenuation, _additionalLightAttenuations);
                cmd.SetGlobalVectorArray(PropertyID.AdditionalLightsSpotDir, _additionalLightSpotDirections);
                cmd.SetGlobalVectorArray(PropertyID.AdditionalLightOcclusionProbeChannel, _additionalLightOcclusionProbeChannels);
            }

            cmd.SetGlobalVector(PropertyID.WorldSpaceCameraPos, _worldSpaceCameraPos);
        }
    }

    /// <summary>
    /// Pool for managing PortalPassNode instances to reduce garbage collection.
    /// </summary>
    internal static class PortalPassGroupPool
    {
        private static List<PortalPassNode> _groups = new List<PortalPassNode>();

        /// <summary>
        /// Gets a PortalPassNode from the pool or creates a new one if the pool is empty.
        /// </summary>
        /// <returns>A PortalPassNode instance.</returns>
        internal static PortalPassNode Get()
        {
            if (_groups.Count > 0)
            {
                PortalPassNode group = _groups[_groups.Count - 1];
                _groups.RemoveAt(_groups.Count - 1);

                group.renderNode = null;
                group.colorTexture = null;

                return group;
            }

            return new PortalPassNode();
        }

        /// <summary>
        /// Releases a PortalPassNode back to the pool.
        /// </summary>
        /// <param name="node">The node to release.</param>
        internal static void Release(PortalPassNode node)
        {
            if (node != null)
                _groups.Add(node);
        }
    }
}
