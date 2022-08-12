using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering.Universal
{
    public class MainLightShadowCasterInPortalPass : MainLightShadowCasterPass
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

        public MainLightShadowCasterInPortalPass(PortalRenderFeature feature) : base(RenderPassEvent.AfterRenderingOpaques)
        {
            this.feature = feature;
            profilingSampler = new ProfilingSampler(nameof(MainLightShadowCasterInPortalPass));
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (enabled)
            {
                // Store Previous
                prevShadowTexture = Shader.GetGlobalTexture(PropertyID.MainLightShadowmapTexture);
                Shader.GetGlobalMatrixArray(PropertyID.MainLightWorldToShadow, prevWorldToShadow);
                prevShadowParams = Shader.GetGlobalVector(PropertyID.MainLightShadowParams);
                prevCascadeShadowSplitSpheres[0] = Shader.GetGlobalVector(PropertyID.CascadeShadowSplitSpheres0);
                prevCascadeShadowSplitSpheres[1] = Shader.GetGlobalVector(PropertyID.CascadeShadowSplitSpheres1);
                prevCascadeShadowSplitSpheres[2] = Shader.GetGlobalVector(PropertyID.CascadeShadowSplitSpheres2);
                prevCascadeShadowSplitSpheres[3] = Shader.GetGlobalVector(PropertyID.CascadeShadowSplitSpheres3);
                prevCascadeShadowSplitSphereRadii = Shader.GetGlobalVector(PropertyID.CascadeShadowSplitSphereRadii);
                prevShadowOffset[0] = Shader.GetGlobalVector(PropertyID.MainLightShadowOffset0);
                prevShadowOffset[1] = Shader.GetGlobalVector(PropertyID.MainLightShadowOffset1);
                prevShadowOffset[2] = Shader.GetGlobalVector(PropertyID.MainLightShadowOffset2);
                prevShadowOffset[3] = Shader.GetGlobalVector(PropertyID.MainLightShadowOffset3);
                prevShadowmapSize = Shader.GetGlobalVector(PropertyID.MainLightShadowmapSize);

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

                cmd.SetGlobalTexture(PropertyID.MainLightShadowmapTexture, prevShadowTexture);

                // Restore previous
                Shader.SetGlobalTexture(PropertyID.MainLightShadowmapTexture, prevShadowTexture);
                Shader.SetGlobalMatrixArray(PropertyID.MainLightWorldToShadow, prevWorldToShadow);
                Shader.SetGlobalVector(PropertyID.MainLightShadowParams, prevShadowParams);
                Shader.SetGlobalVector(PropertyID.CascadeShadowSplitSpheres0, prevCascadeShadowSplitSpheres[0]);
                Shader.SetGlobalVector(PropertyID.CascadeShadowSplitSpheres1, prevCascadeShadowSplitSpheres[1]);
                Shader.SetGlobalVector(PropertyID.CascadeShadowSplitSpheres2, prevCascadeShadowSplitSpheres[2]);
                Shader.SetGlobalVector(PropertyID.CascadeShadowSplitSpheres3, prevCascadeShadowSplitSpheres[3]);
                Shader.SetGlobalVector(PropertyID.CascadeShadowSplitSphereRadii, prevCascadeShadowSplitSphereRadii);
                Shader.SetGlobalVector(PropertyID.MainLightShadowOffset0, prevShadowOffset[0]);
                Shader.SetGlobalVector(PropertyID.MainLightShadowOffset1, prevShadowOffset[1]);
                Shader.SetGlobalVector(PropertyID.MainLightShadowOffset2, prevShadowOffset[2]);
                Shader.SetGlobalVector(PropertyID.MainLightShadowOffset3, prevShadowOffset[3]);

                if (prevShadowTexture != null)
                {
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, true);
                    //CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, true); // TODO: This one might not be true
                }
                else
                {
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, false);
                    //CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, false);
                }
            }
        }
    }
}
