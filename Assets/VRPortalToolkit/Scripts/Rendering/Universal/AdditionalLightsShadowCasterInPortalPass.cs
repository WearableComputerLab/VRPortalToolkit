using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering.Universal
{
    public class AdditionalLightsShadowCasterInPortalPass : AdditionalLightsShadowCasterPass
    {
        public PortalRenderFeature feature { get; protected set; }

        public bool enabled { get; set; } = true;

        protected Texture prevShadowTexture;
        protected List<Matrix4x4> prevWorldToShadow = new List<Matrix4x4>(2);
        protected Vector4 prevShadowParams;
        protected Vector4[] prevCascadeShadowSplitSpheres = new Vector4[4];
        protected Vector4 prevCascadeShadowSplitSphereRadii;
        protected Vector4[] prevShadowOffset = new Vector4[4];
        protected Vector4 prevShadowmapSize;

        public AdditionalLightsShadowCasterInPortalPass(PortalRenderFeature feature) : base(RenderPassEvent.AfterRenderingOpaques)
        {
            this.feature = feature;
            profilingSampler = new ProfilingSampler(nameof(AdditionalLightsShadowCasterInPortalPass));
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (enabled)
            {
                // Store Previous
                prevShadowTexture = Shader.GetGlobalTexture(PropertyID.AdditionalLightsShadowmapTexture);
                Shader.GetGlobalMatrixArray(PropertyID.AdditionalLightsWorldToShadow, prevWorldToShadow);
                prevShadowParams = Shader.GetGlobalVector(PropertyID.AdditionalLightShadowParams);
                prevShadowOffset[0] = Shader.GetGlobalVector(PropertyID.AdditionalShadowOffset0);
                prevShadowOffset[1] = Shader.GetGlobalVector(PropertyID.AdditionalShadowOffset1);
                prevShadowOffset[2] = Shader.GetGlobalVector(PropertyID.AdditionalShadowOffset2);
                prevShadowOffset[3] = Shader.GetGlobalVector(PropertyID.AdditionalShadowOffset3);
                prevShadowmapSize = Shader.GetGlobalVector(PropertyID.AdditionalShadowmapSize);

                base.Configure(cmd, cameraTextureDescriptor);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (enabled)
            {
                if (feature.currentGroup.renderNode.isStereo)
                {
                    CommandBuffer cmd = CommandBufferPool.Get();

                    CameraUtility.StopSinglePass(cmd);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    base.Execute(context, ref renderingData);

                    CameraUtility.StartSinglePass(cmd);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
                else
                    base.Execute(context, ref renderingData);
            }
        }

        // Deliberately clear
        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public virtual void OnPortalCleanup(CommandBuffer cmd)
        {
            if (enabled)
            {
                base.OnCameraCleanup(cmd);

                // Restore previous
                Shader.SetGlobalTexture(PropertyID.AdditionalLightsShadowmapTexture, prevShadowTexture);
                Shader.SetGlobalMatrixArray(PropertyID.AdditionalLightsWorldToShadow, prevWorldToShadow);
                Shader.SetGlobalVector(PropertyID.AdditionalLightShadowParams, prevShadowParams);
                Shader.SetGlobalVector(PropertyID.AdditionalShadowOffset0, prevShadowOffset[0]);
                Shader.SetGlobalVector(PropertyID.AdditionalShadowOffset1, prevShadowOffset[1]);
                Shader.SetGlobalVector(PropertyID.AdditionalShadowOffset2, prevShadowOffset[2]);
                Shader.SetGlobalVector(PropertyID.AdditionalShadowOffset3, prevShadowOffset[3]);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, prevShadowTexture != null);
            }
        }
    }
}
