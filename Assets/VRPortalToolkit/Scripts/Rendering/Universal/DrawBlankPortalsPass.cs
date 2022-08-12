using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering.Universal
{
    public class DrawBlankPortalsPass : PortalRenderPass
    {
        private static MaterialPropertyBlock propertyBlock;

        public DrawBlankPortalsPass(PortalRenderFeature feature) : base(feature)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            if (!feature.portalStereo)
            {
                Debug.LogError(nameof(DrawTexturePortalsPass) + " requires feature.portalStereo!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                PortalRenderNode parentNode = feature.currentGroup.renderNode;

                bool hasFrameBuffer = FrameBuffer.current != null && FrameBuffer.current.texture;

                feature.currentGroup.SetViewAndProjectionMatrices(cmd);

                cmd.SetGlobalInt(PropertyID.PortalStencilRef, feature.currentGroup.stateBlock.stencilReference);
                feature.portalStereo.SetTexture(PropertyID.MainTex, FrameBuffer.current.texture);

                foreach (PortalRenderNode renderNode in parentNode.children)
                {
                    if (!renderNode.isValid && renderNode.renderer)
                    {
                        if (hasFrameBuffer && TryFindAncestorNode(renderNode, FrameBuffer.current.rootNode, out PortalRenderNode originalNode))
                        {
                            if (renderNode.isStereo)
                            {
                                UpdateScaleAndTranslation(GetWindow(parentNode.GetStereoViewMatrix(0), parentNode.GetStereoProjectionMatrix(0), renderNode),
                                    originalNode.GetStereoWindow(0), PropertyID.MainTex_ST);
                                UpdateScaleAndTranslation(GetWindow(parentNode.GetStereoViewMatrix(1), parentNode.GetStereoProjectionMatrix(1), renderNode),
                                    originalNode.GetStereoWindow(1), PropertyID.MainTex_ST_2);
                            }
                            else
                                UpdateScaleAndTranslation(GetWindow(parentNode.worldToCameraMatrix, parentNode.projectionMatrix, renderNode),
                                    originalNode.window, PropertyID.MainTex_ST);

                            // This is the old way, that didnt take perspective into account

                            //if (renderNode.isStereo)
                            //{
                            //    UpdateScaleAndTranslation(renderNode.GetStereoWindow(0), originalNext.parent.GetStereoWindow(0), feature.portalStereo, PropertyID.MainTex_ST);
                            //    UpdateScaleAndTranslation(renderNode.GetStereoWindow(1), originalNext.parent.GetStereoWindow(1), feature.portalStereo, PropertyID.MainTex_ST_2);
                            //}
                            //else
                            //    UpdateScaleAndTranslation(renderNode.window, originalNext.parent.window, feature.portalStereo, PropertyID.MainTex_ST);

                            renderNode.renderer.Render(renderingData.cameraData.camera, renderNode, cmd, feature.portalStereo, propertyBlock);
                        }
                        else
                            renderNode.renderer.RenderDefault(renderingData.cameraData.camera, renderNode, cmd);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }

        // for bellow, parent window should instead be window, and window should be a new calculated window
        private static void UpdateScaleAndTranslation(ViewWindow window, ViewWindow parentWindow, int scaleTranslateID)
        {
            if (parentWindow.xMin < 0f) parentWindow.xMin = 0f;
            if (parentWindow.yMin < 0f) parentWindow.yMin = 0f;
            if (parentWindow.xMax > 1f) parentWindow.xMax = 1f;
            if (parentWindow.yMax > 1f) parentWindow.yMax = 1f;

            // Get current position
            Vector2 size = new Vector2(window.xMax - window.xMin, window.yMax - window.yMin),
                parentSize = new Vector2(parentWindow.xMax - parentWindow.xMin, parentWindow.yMax - parentWindow.yMin),
                finalTiling = new Vector2(parentSize.x / size.x, parentSize.y / size.y);

            propertyBlock.SetVector(scaleTranslateID, new Vector4(
                finalTiling.x, finalTiling.y,
                (parentWindow.xMin + parentWindow.xMax - finalTiling.x * (window.xMin + window.xMax)) * 0.5f,
                (parentWindow.yMin + parentWindow.yMax - finalTiling.y * (window.yMin + window.yMax)) * 0.5f
            ));
        }

        protected virtual bool TryFindAncestorNode(PortalRenderNode target, PortalRenderNode root, out PortalRenderNode nextNode)
        {
            PortalRenderNode current = root;
            PortalRenderNode lastValid = null;
            nextNode = null;

            foreach (PortalRenderNode originalNode in GetPath(target))
            {
                if (!TryGetChildWithRenderer(current, originalNode.renderer, out current))
                {
                    // Try cycling back
                    if (current != null && current.renderer == originalNode.renderer)
                        current = lastValid;
                    else
                    {
                        nextNode = null;
                        return false;
                    }
                }

                if (current.renderer == target.renderer)
                    lastValid = current;

                if (current.parent.renderer == target.renderer && current.renderer)
                    nextNode = current;
            }

            return nextNode != null;
        }

        private bool TryGetChildWithRenderer(PortalRenderNode parent, PortalRenderer renderer, out PortalRenderNode child)
        {
            foreach (PortalRenderNode childNode in parent.children)
            {
                if (childNode.renderer == renderer)
                {
                    child = childNode;
                    return true;
                }
            }

            child = null;
            return false;
        }

        private IEnumerable<PortalRenderNode> GetPath(PortalRenderNode node)
        {
            if (node != null && node.parent != null)
            {
                foreach (PortalRenderNode parent in GetPath(node.parent))
                    yield return parent;

                yield return node;
            }
        }

        // Assumes view and proj are from the parent, not the node
        private ViewWindow GetWindow(Matrix4x4 view, Matrix4x4 proj, PortalRenderNode node)
        {
            // These haven't been calculated because the node is invalid
            Matrix4x4 localToWorld = node.renderer.portal.teleportMatrix * node.parent.localToWorldMatrix;
            view = view * node.renderer.portal.connectedPortal.teleportMatrix;

            node.renderer.TryGetWindow(localToWorld, view, proj, out ViewWindow window);
            return window;
        }

        private IEnumerable<PortalRenderNode> GetLoop(PortalRenderNode current, PortalRenderer renderer)
        {
            if (current != null && current.parent != null && current.parent.parent != null)
            {
                if (current.parent.renderer != renderer)
                    foreach (PortalRenderNode parent in GetLoop(current.parent, renderer))
                        yield return parent;

                yield return current.parent;
            }
        }
    }
}
