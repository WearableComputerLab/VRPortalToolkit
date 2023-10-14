using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit
{
    [ExecuteInEditMode]
    public class ExpandPortal : MonoBehaviour
    {
        [SerializeField] private bool _doubleSided = false;
        public bool doubleSided { get => _doubleSided; set => _doubleSided = value; }

        [SerializeField] private Bounds _bounds = new Bounds(Vector3.zero, Vector3.one * 0.5f);
        public Bounds bounds { get => _bounds; set => _bounds = value; }

        // Temporarily store
        private float _localScaleZ;
        private Matrix4x4 _worldToLocal;

        protected virtual void OnDrawGizmosSelected()
        {
            float previous = transform.localScale.z;
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, _localScaleZ);

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, previous);
        }

        protected virtual void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;

            PortalRendering.onPreRender += OnPortalPreRender;
            PortalRendering.onPostRender += OnPortalPostRender;
        }

        protected virtual void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            PortalRendering.onPreRender -= OnPortalPreRender;
            PortalRendering.onPostRender -= OnPortalPostRender;
        }

        protected virtual void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            _localScaleZ = transform.localScale.z;
            _worldToLocal = transform.worldToLocalMatrix;

            TryExpand(camera, camera.transform.position);
        }

        protected virtual void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            Flatten(camera, camera.transform.position);

            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, _localScaleZ);
        }

        protected virtual void OnPortalPreRender(PortalRenderNode renderNode)
        {
            TryExpand(renderNode.camera, renderNode.localToWorldMatrix.GetColumn(3));
        }

        protected virtual void OnPortalPostRender(PortalRenderNode renderNode)
        {
            TryExpand(renderNode.camera, renderNode.parent.localToWorldMatrix.GetColumn(3));
        }

        protected virtual void TryExpand(Camera camera, Vector3 position)
        {
            if (camera.cameraType != CameraType.Preview && camera.cameraType != CameraType.SceneView)
            {
                float cameraWidth = GetCameraWidth(camera) * 0.5f,
                    longest = Mathf.Sqrt(cameraWidth * cameraWidth + camera.nearClipPlane * camera.nearClipPlane);

                Vector3 expand = _worldToLocal.MultiplyPoint(transform.position + transform.up + transform.right + transform.forward);
                Bounds newBounds = bounds;
                newBounds.Expand(expand * longest);

                if (newBounds.Contains(_worldToLocal.MultiplyPoint(position)))
                {
                    if (doubleSided && !isFacingCamera(camera, position))
                        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, -longest * expand.z);
                    else
                        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, longest * expand.z);

                    return;
                }
            }

            // Else
            Flatten(camera, position);
        }

        private float GetCameraWidth(Camera camera)
        {
            return Vector3.Distance(camera.ViewportToWorldPoint(new Vector3(0f, 0f, camera.nearClipPlane), Camera.MonoOrStereoscopicEye.Left),
                camera.ViewportToWorldPoint(new Vector3(1f, 1f, camera.nearClipPlane), Camera.MonoOrStereoscopicEye.Right));
        }

        private void Flatten(Camera camera, Vector3 position)
        {
            if (doubleSided && !isFacingCamera(camera, position))
                transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, -float.Epsilon);
            else
                transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, float.Epsilon);
        }

        private bool isFacingCamera(Camera camera, Vector3 position)
            => Vector3.Dot(position - transform.position, transform.forward) > 0f;
    }
}
