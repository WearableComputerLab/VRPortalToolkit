using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering.Universal
{
    public class DrawBlankPortalsPass : PortalRenderPass
    {
        private static MaterialPropertyBlock propertyBlock;

        public Material material { get; set; }

        public DrawBlankPortalsPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                PortalRenderNode parentNode = PortalPassStack.Current.renderNode;

                bool hasFrameBuffer = FrameBuffer.current != null && FrameBuffer.current.texture;

                PortalPassStack.Current.SetViewAndProjectionMatrices(cmd);

                cmd.SetGlobalInt(PropertyID.PortalStencilRef, PortalPassStack.Current.stateBlock.stencilReference);
                if (hasFrameBuffer) propertyBlock.SetTexture(PropertyID.MainTex, FrameBuffer.current.texture);

                foreach (PortalRenderNode renderNode in parentNode.children)
                {
                    if (!renderNode.isValid)
                    {
                        if (hasFrameBuffer && TryFindAncestorNode(renderNode, FrameBuffer.current.rootNode, out PortalRenderNode originalNode))
                        {
                            Material material = renderNode.overrides.portalStereo ? renderNode.overrides.portalStereo : this.material;

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

                            foreach (IPortalRenderer renderer in renderNode.renderers)
                                renderer.Render(renderNode, cmd, material, propertyBlock);
                        }
                        else
                        {
                            foreach (IPortalRenderer renderer in renderNode.renderers)
                                renderer.RenderDefault(renderNode, cmd);
                        }
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
                if (!TryGetChildWithPortal(current, originalNode.portal, out current)) // TODO: clipping might make this different
                {
                    // Try cycling back
                    if (current != null && current.portal == originalNode.portal) // TODO: clipping might make this different
                        current = lastValid;
                    else
                    {
                        nextNode = null;
                        return false;
                    }
                }

                if (current.portal == target.portal) // TODO: clipping might make this different
                    lastValid = current;

                if (current.parent.portal == target.portal && current.portal != null)
                    nextNode = current;
            }

            return nextNode != null;
        }

        private bool TryGetChildWithPortal(PortalRenderNode parent, IPortal portal, out PortalRenderNode child)
        {
            if (parent != null)
            {
                foreach (PortalRenderNode childNode in parent.children)
                {
                    if (childNode.portal == portal)
                    {
                        child = childNode;
                        return true;
                    }
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
            if (node.portal.connected != null)
            {
                // These haven't been calculated because the node is invalid
                Matrix4x4 localToWorld = node.portal.teleportMatrix * node.parent.localToWorldMatrix;
                view = view * node.portal.connected.teleportMatrix;

                // TODO: Needs to get the window of all portals?
                node.renderer.TryGetWindow(node, localToWorld.GetColumn(3), view, proj, out ViewWindow window);
                return window;
            }

            return default;
        }

        private IEnumerable<PortalRenderNode> GetLoop(PortalRenderNode current, IPortal portal)
        {
            if (current != null && current.parent != null && current.parent.parent != null)
            {
                if (current.parent.portal != portal)
                    foreach (PortalRenderNode parent in GetLoop(current.parent, portal))
                        yield return parent;

                yield return current.parent;
            }
        }
    }
}
