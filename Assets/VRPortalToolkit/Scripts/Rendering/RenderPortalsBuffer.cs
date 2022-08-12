using Misc;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit
{
    public class RenderPortalsBuffer
    {
        private static ObjectPool<RenderPortalsBuffer> _pool = new ObjectPool<RenderPortalsBuffer>(() => new RenderPortalsBuffer());

        private static Dictionary<PortalRenderNode, RenderPortalsBuffer> _bufferByCamera = new Dictionary<PortalRenderNode, RenderPortalsBuffer>();

        private PortalRenderNode _renderNode;
        public PortalRenderNode renderNode => _renderNode;

        private RenderTexture _texture;
        public RenderTexture texture => _texture;

        private RenderPortalsBuffer() { }

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

        public static bool TryGetBuffer(PortalRenderNode renderNode, out RenderPortalsBuffer buffer)
            => _bufferByCamera.TryGetValue(renderNode, out buffer);

        public static bool HasBuffer(PortalRenderNode renderNode) => _bufferByCamera.ContainsKey(renderNode);
        
        public static void ClearBuffer(PortalRenderNode renderNode)
        {
            if (_bufferByCamera.TryGetValue(renderNode, out RenderPortalsBuffer buffer))
            {
                buffer.ClearTexture();
                _bufferByCamera.Remove(renderNode);
                _pool.Release(buffer);
            }
        }

        public static void ClearBuffers()
        {
            foreach (var pair in _bufferByCamera)
            {
                pair.Value.ClearTexture();
                _pool.Release(pair.Value);
            }

            _bufferByCamera.Clear();
        }

        public void UpdateTexture(RenderTextureDescriptor descriptor)
        {
            ClearTexture();
            _texture = RenderTexture.GetTemporary(descriptor);
        }

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
