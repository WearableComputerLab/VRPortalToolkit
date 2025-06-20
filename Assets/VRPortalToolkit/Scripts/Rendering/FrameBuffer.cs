using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRPortalToolkit.Rendering
{
    /// <summary>
    /// Manages render textures and associated data for "infinite" portal rendering effect. Simply put, the previous frame
    /// is stored so that it can be used again to given the illusion a pair of portals (or more) tunnel forever.
    /// </summary>
    public class FrameBuffer
    {
        private static FrameBuffer _current;
        /// <summary>
        /// Gets the current frame buffer.
        /// </summary>
        public static FrameBuffer current => _current;

        private static Dictionary<Key, FrameBuffer> _bufferByCamera
            = new Dictionary<Key, FrameBuffer>();

        private Camera _camera;
        /// <summary>
        /// Gets the camera associated with this frame buffer.
        /// </summary>
        public Camera camera => _camera;

        private RenderTexture _texture;
        /// <summary>
        /// Gets the render texture used by this frame buffer.
        /// </summary>
        public RenderTexture texture => _texture;

        private RenderTargetIdentifier _identifier;
        /// <summary>
        /// Gets the render target identifier for this frame buffer.
        /// </summary>
        public RenderTargetIdentifier identifier => _identifier;

        private Camera.MonoOrStereoscopicEye _eye;
        /// <summary>
        /// Gets the eye associated with this frame buffer.
        /// </summary>
        public Camera.MonoOrStereoscopicEye eye => _eye;

        /*private RenderTexture _secondaryTexture;
        public RenderTexture secondaryTexture => _secondaryTexture;*/

        private PortalRenderNode _rootNode;
        /// <summary>
        /// Gets or sets the root portal render node for this frame buffer.
        /// </summary>
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

        /// <summary>
        /// Key used to identify frame buffers in the dictionary.
        /// </summary>
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

        /// <summary>
        /// Sets the current frame buffer for the specified camera and eye.
        /// </summary>
        /// <param name="camera">The camera to use.</param>
        /// <param name="eye">The eye to use (defaults to Mono).</param>
        public static void SetCurrent(Camera camera, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono)
        {
            _current = GetBuffer(camera, eye);
        }

        /// <summary>
        /// Gets or creates a frame buffer for the specified camera and eye.
        /// </summary>
        /// <param name="camera">The camera to use.</param>
        /// <param name="eye">The eye to use (defaults to Mono).</param>
        /// <returns>The frame buffer for the specified camera and eye.</returns>
        public static FrameBuffer GetBuffer(Camera camera, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono)
        {
            if (camera == null) return null;

            Key key = new Key(camera, eye);

            if (!_bufferByCamera.TryGetValue(key, out FrameBuffer buffer))
                _bufferByCamera[key] = buffer = new FrameBuffer(camera, eye);

            return buffer;
        }

        /// <summary>
        /// Tries to get an existing frame buffer for the specified camera.
        /// </summary>
        /// <param name="camera">The camera to look for.</param>
        /// <param name="buffer">Output parameter for the found buffer.</param>
        /// <returns>True if a buffer was found, false otherwise.</returns>
        public static bool TryGetBuffer(Camera camera, out FrameBuffer buffer)
            => TryGetBuffer(camera, Camera.MonoOrStereoscopicEye.Mono, out buffer);
        
        /// <summary>
        /// Tries to get an existing frame buffer for the specified camera and eye.
        /// </summary>
        /// <param name="camera">The camera to look for.</param>
        /// <param name="eye">The eye to look for.</param>
        /// <param name="buffer">Output parameter for the found buffer.</param>
        /// <returns>True if a buffer was found, false otherwise.</returns>
        public static bool TryGetBuffer(Camera camera, Camera.MonoOrStereoscopicEye eye, out FrameBuffer buffer)
            => _bufferByCamera.TryGetValue(new Key(camera, eye), out buffer);

        /// <summary>
        /// Checks if a frame buffer exists for the specified camera and eye.
        /// </summary>
        /// <param name="camera">The camera to check.</param>
        /// <param name="eye">The eye to check (defaults to Mono).</param>
        /// <returns>True if a buffer exists, false otherwise.</returns>
        public static bool HasBuffer(Camera camera, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono) => _bufferByCamera.ContainsKey(new Key(camera, eye));

        /// <summary>
        /// Clears and removes the frame buffer for the specified camera and eye.
        /// </summary>
        /// <param name="camera">The camera to clear the buffer for.</param>
        /// <param name="eye">The eye to clear the buffer for (defaults to Mono).</param>
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

        /// <summary>
        /// Clears and removes all frame buffers.
        /// </summary>
        public static void ClearBuffers()
        {
            foreach (var pair in _bufferByCamera)
            {
                pair.Value.ClearTexture();
                pair.Value.rootNode = null;
            }

            _bufferByCamera.Clear();
        }

        /// <summary>
        /// Updates the render texture for this frame buffer with the specified descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor for the new render texture.</param>
        public void UpdateTexture(RenderTextureDescriptor descriptor)
        {
            ClearTexture();
            _texture = RenderTexture.GetTemporary(descriptor);
            _identifier = new RenderTargetIdentifier(_texture, 0, CubemapFace.Unknown, -1);
        }

        /// <summary>
        /// Clears and releases the render texture used by this frame buffer.
        /// </summary>
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
