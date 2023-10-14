using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering.Universal
{
    public static class PortalPassStack
    {
        private static List<PortalPassNode> portalPassNodes = new List<PortalPassNode>();

        public static void Clear()
        {
            portalPassNodes.Clear();
        }

        public static void Push(PortalPassNode node)
        {
            if (node != null) portalPassNodes.Add(node);
        }

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

        public static PortalPassNode Parent
        {
            get
            {
                if (portalPassNodes.Count > 1)
                    return portalPassNodes[portalPassNodes.Count - 2];

                return null;
            }
        }

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

    public class PortalPassNode
    {
        public PortalRenderNode renderNode;

        public RenderStateBlock stateBlock;

        //public PortalPassGroup parent;

        public Rect viewport = new Rect(0, 0, 1, 1);

        public MainLightShadowCasterInPortalPass mainLightShadowCasterPass;
        public AdditionalLightsShadowCasterInPortalPass additionalLightsShadowCasterPass;

        private RenderTexture _colorTexture;
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

    internal static class PortalPassGroupPool
    {
        private static List<PortalPassNode> _groups = new List<PortalPassNode>();

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

        internal static void Release(PortalPassNode node)
        {
            if (node != null)
                _groups.Add(node);
        }
    }
}
