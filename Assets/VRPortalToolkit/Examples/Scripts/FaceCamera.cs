using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit.Examples
{
    public class FaceCamera : MonoBehaviour
    {
        protected void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            PortalRendering.onPreRender += OnPortalPreRender;
            PortalRendering.onPostRender += OnPortalPostRender;
        }

        protected void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            PortalRendering.onPreRender -= OnPortalPreRender;
            PortalRendering.onPostRender -= OnPortalPostRender;
        }

        private void OnPortalPreRender(PortalRenderNode renderNode) =>
            FacePosition(renderNode.localToWorldMatrix.GetColumn(3));

        private void OnPortalPostRender(PortalRenderNode renderNode) =>
            FacePosition(renderNode.parent.localToWorldMatrix.GetColumn(3));

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera) =>
            FacePosition(camera.transform.position);

        private void FacePosition(Vector3 position)
        {
            transform.LookAt(position, Vector3.up);
            transform.rotation = Quaternion.LookRotation(-transform.forward, transform.up);
        }
    }
}
