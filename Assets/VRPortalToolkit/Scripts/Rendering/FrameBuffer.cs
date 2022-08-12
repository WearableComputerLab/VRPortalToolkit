using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRPortalToolkit.Rendering
{
    public class FrameBuffer
    {
        private static FrameBuffer _current;
        public static FrameBuffer current => _current;

        private static Dictionary<Key, FrameBuffer> _bufferByCamera
            = new Dictionary<Key, FrameBuffer>();

        private Camera _camera;
        public Camera camera => _camera;

        private RenderTexture _texture;
        public RenderTexture texture => _texture;

        private RenderTargetIdentifier _identifier;
        public RenderTargetIdentifier identifier => _identifier;

        private Camera.MonoOrStereoscopicEye _eye;
        public Camera.MonoOrStereoscopicEye eye => _eye;

        /*private RenderTexture _secondaryTexture;
        public RenderTexture secondaryTexture => _secondaryTexture;*/

        private PortalRenderNode _rootNode;
        public PortalRenderNode rootNode
        {
            get => _rootNode;
            set
            {
                if (_rootNode != value)
                {
                    if (_rootNode != null) _rootNode.Dispose();

                    _rootNode = value;
                }
            }
        }

        private struct Key
        {
            public readonly Camera camera;
            public readonly Camera.MonoOrStereoscopicEye eye;

            public Key(Camera camera, Camera.MonoOrStereoscopicEye eye)
            {
                this.camera = camera;
                this.eye = eye;
            }

        }

        private FrameBuffer(Camera camera, Camera.MonoOrStereoscopicEye eye)
        {
            _camera = camera;
            _eye = eye;
        }

        public static void SetCurrent(Camera camera, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono)
        {
            _current = GetBuffer(camera, eye);
        }

        public static FrameBuffer GetBuffer(Camera camera, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono)
        {
            if (camera == null) return null;

            Key key = new Key(camera, eye);

            if (!_bufferByCamera.TryGetValue(key, out FrameBuffer buffer))
                _bufferByCamera[key] = buffer = new FrameBuffer(camera, eye);

            return buffer;
        }

        public static bool TryGetBuffer(Camera camera, out FrameBuffer buffer)
            => TryGetBuffer(camera, Camera.MonoOrStereoscopicEye.Mono, out buffer);
        public static bool TryGetBuffer(Camera camera, Camera.MonoOrStereoscopicEye eye, out FrameBuffer buffer)
            => _bufferByCamera.TryGetValue(new Key(camera, eye), out buffer);

        public static bool HasBuffer(Camera camera, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono) => _bufferByCamera.ContainsKey(new Key(camera, eye));

        public static void ClearBuffer(Camera camera, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono)
        {
            Key key = new Key(camera, eye);

            if (_bufferByCamera.TryGetValue(key, out FrameBuffer buffer))
            {
                buffer.ClearTexture();
                buffer.rootNode = null;
                _bufferByCamera.Remove(key);
            }
        }

        public static void ClearBuffers()
        {
            foreach (var pair in _bufferByCamera)
            {
                pair.Value.ClearTexture();
                pair.Value.rootNode = null;
            }

            _bufferByCamera.Clear();
        }

        public void UpdateTexture(RenderTextureDescriptor descriptor)
        {
            ClearTexture();
            _texture = RenderTexture.GetTemporary(descriptor);
            _identifier = new RenderTargetIdentifier(_texture, 0, CubemapFace.Unknown, -1);
        }

        public void ClearTexture()
        {
            if (_texture != null)
            {
                RenderTexture.ReleaseTemporary(_texture);
                _identifier = default(RenderTargetIdentifier);
                _texture = null;
            }
        }
    }
}
