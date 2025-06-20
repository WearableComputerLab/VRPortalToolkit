using Misc;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit
{
    /// <summary>
    /// Manages render textures for portal rendering, providing a pooling system to reduce memory allocations.
    /// </summary>
    public class RenderPortalsBuffer
    {
        private static ObjectPool<RenderPortalsBuffer> _pool = new ObjectPool<RenderPortalsBuffer>(() => new RenderPortalsBuffer());

        private static Dictionary<PortalRenderNode, RenderPortalsBuffer> _bufferByCamera = new Dictionary<PortalRenderNode, RenderPortalsBuffer>();

        private PortalRenderNode _renderNode;
        /// <summary>
        /// Gets the portal render node associated with this buffer.
        /// </summary>
        public PortalRenderNode renderNode => _renderNode;

        private RenderTexture _texture;
        /// <summary>
        /// Gets the render texture used by this buffer.
        /// </summary>
        public RenderTexture texture => _texture;

        private RenderPortalsBuffer() { }

        /// <summary>
        /// Gets or creates a buffer for the specified render node.
        /// </summary>
        /// <param name="renderNode">The render node to get a buffer for.</param>
        /// <returns>The buffer associated with the render node.</returns>
        public static RenderPortalsBuffer GetBuffer(PortalRenderNode renderNode)
        {
            if (renderNode == null) return null;

            if (!_bufferByCamera.TryGetValue(renderNode, out RenderPortalsBuffer buffer))
            {
                _bufferByCamera[renderNode] = buffer = _pool.Get();
                buffer._renderNode = renderNode;
            }

            return buffer;
        }

        /// <summary>
        /// Tries to get an existing buffer for the specified render node.
        /// </summary>
        /// <param name="renderNode">The render node to look for.</param>
        /// <param name="buffer">Output parameter for the found buffer.</param>
        /// <returns>True if a buffer was found, false otherwise.</returns>
        public static bool TryGetBuffer(PortalRenderNode renderNode, out RenderPortalsBuffer buffer)
            => _bufferByCamera.TryGetValue(renderNode, out buffer);

        /// <summary>
        /// Checks if a buffer exists for the specified render node.
        /// </summary>
        /// <param name="renderNode">The render node to check.</param>
        /// <returns>True if a buffer exists, false otherwise.</returns>
        public static bool HasBuffer(PortalRenderNode renderNode) => _bufferByCamera.ContainsKey(renderNode);
        
        /// <summary>
        /// Clears and releases the buffer for the specified render node.
        /// </summary>
        /// <param name="renderNode">The render node to clear the buffer for.</param>
        public static void ClearBuffer(PortalRenderNode renderNode)
        {
            if (_bufferByCamera.TryGetValue(renderNode, out RenderPortalsBuffer buffer))
            {
                buffer.ClearTexture();
                _bufferByCamera.Remove(renderNode);
                _pool.Release(buffer);
            }
        }

        /// <summary>
        /// Clears and releases all buffers.
        /// </summary>
        public static void ClearBuffers()
        {
            foreach (var pair in _bufferByCamera)
            {
                pair.Value.ClearTexture();
                _pool.Release(pair.Value);
            }

            _bufferByCamera.Clear();
        }

        /// <summary>
        /// Updates the render texture for this buffer with the specified descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor for the new render texture.</param>
        public void UpdateTexture(RenderTextureDescriptor descriptor)
        {
            ClearTexture();
            _texture = RenderTexture.GetTemporary(descriptor);
        }

        /// <summary>
        /// Clears and releases the render texture used by this buffer.
        /// </summary>
        public void ClearTexture()
        {
            if (_texture != null)
            {
                RenderTexture.ReleaseTemporary(_texture);
                _texture = null;
            }
        }
    }
}
