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
    /// <summary>
    /// Makes an object face toward the active camera or portal render view.
    /// </summary>
    /// <remarks>
    /// This component ensures that the attached GameObject always faces the current
    /// active camera, including when rendering through portals. It's useful for UI elements,
    /// billboards, or any object that should always face the viewer regardless of their position
    /// or if they're viewing through a portal.
    /// </remarks>
    public class FaceCamera : MonoBehaviour
    {
        /// <summary>
        /// Registers event handlers when this component is enabled.
        /// </summary>
        protected void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            PortalRendering.onPreRender += OnPortalPreRender;
            PortalRendering.onPostRender += OnPortalPostRender;
        }

        /// <summary>
        /// Removes event handlers when this component is disabled.
        /// </summary>
        protected void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            PortalRendering.onPreRender -= OnPortalPreRender;
            PortalRendering.onPostRender -= OnPortalPostRender;
        }

        /// <summary>
        /// Called just before a portal is rendered, making the object face the portal camera.
        /// </summary>
        /// <param name="renderNode">The portal render node that is about to be rendered</param>
        private void OnPortalPreRender(PortalRenderNode renderNode) =>
            FacePosition(renderNode.localToWorldMatrix.GetColumn(3));

        /// <summary>
        /// Called after a portal has been rendered, restoring the object to face the parent camera.
        /// </summary>
        /// <param name="renderNode">The portal render node that was rendered</param>
        private void OnPortalPostRender(PortalRenderNode renderNode) =>
            FacePosition(renderNode.parent.localToWorldMatrix.GetColumn(3));

        /// <summary>
        /// Called when a camera begins rendering, making the object face the camera.
        /// </summary>
        /// <param name="context">The render context</param>
        /// <param name="camera">The camera that is rendering</param>
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera) =>
            FacePosition(camera.transform.position);

        /// <summary>
        /// Rotates the object to face a specific position in world space.
        /// The rotation is inverted so the object faces toward rather than away from the position.
        /// </summary>
        /// <param name="position">The position to face</param>
        private void FacePosition(Vector3 position)
        {
            transform.LookAt(position, Vector3.up);
            transform.rotation = Quaternion.LookRotation(-transform.forward, transform.up);
        }
    }
}
