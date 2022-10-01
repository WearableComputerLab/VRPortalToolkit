using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit
{
    [ExecuteInEditMode]
    public class PortalExpander : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        public Transform target { get => _target; set => _target = value; }

        [SerializeField] private float _scale = 1f;
        public float scale { get => _scale; set => _scale = value; }

        [SerializeField] private bool _doubleSided = false;
        public bool doubleSided { get => _doubleSided; set => _doubleSided = value; }

        [SerializeField] private Bounds _bounds = new Bounds(Vector3.zero, Vector3.one);
        public Bounds bounds { get => _bounds; set => _bounds = value; }

        [Header("Events")]
        public UnityEvent<Camera> expanded = new UnityEvent<Camera>();
        public UnityEvent<Camera> flattened = new UnityEvent<Camera>();

        protected virtual void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.blue;

            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        protected virtual void OnEnable()
        {
            //Camera.onPreCull += OnCameraPreCull;
            //Camera.onPostRender += OnCameraPostRender;

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;

            PortalRenderer.onPreRender += OnPortalPreRender;
            PortalRenderer.onPostRender += OnPortalPostRender;
        }

        protected virtual void OnDisable()
        {
            //Camera.onPreCull -= OnCameraPreCull;
            //Camera.onPostRender -= OnCameraPostRender;

            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            PortalRenderer.onPreRender -= OnPortalPreRender;
            PortalRenderer.onPostRender -= OnPortalPostRender;
        }

        protected virtual void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera) => CheckCamera(camera, camera.transform.position);

        protected virtual void OnEndCameraRendering(ScriptableRenderContext context, Camera camera) => Flatten(camera, camera.transform.position);

        protected virtual void OnCameraPreCull(Camera camera) => CheckCamera(camera, camera.transform.position);

        protected virtual void OnCameraPostRender(Camera camera) => Flatten(camera, camera.transform.position);

        protected virtual void OnPortalPreRender(Camera camera, PortalRenderNode renderNode)
        {
            CheckCamera(camera, renderNode.localToWorldMatrix.GetColumn(3));
        }

        protected virtual void OnPortalPostRender(Camera camera, PortalRenderNode renderNode)
        {
            CheckCamera(camera, renderNode.parent.localToWorldMatrix.GetColumn(3));
        }

        //bool isExpanded = false;
        protected virtual void CheckCamera(Camera camera, Vector3 position)
        {
            if (camera.cameraType != CameraType.Preview && camera.cameraType != CameraType.SceneView)
            {
                float cameraWidth = GetCameraWidth(camera) * 0.5f;
                float longest = Mathf.Sqrt(cameraWidth * cameraWidth + camera.nearClipPlane * camera.nearClipPlane);

                Bounds newBounds = bounds;
                newBounds.Expand(longest);

                if (newBounds.Contains(transform.InverseTransformPoint(position)))
                {
                    //isExpanded = true;
                    expanded?.Invoke(camera);

                    if (target)
                    {
                        if (doubleSided && !isFacingCamera(camera, position))
                            target.localScale = new Vector3(transform.localScale.x, transform.localScale.y, -longest * scale);
                        else
                            target.localScale = new Vector3(transform.localScale.x, transform.localScale.y, longest * scale);
                    }

                    return;
                }
            }

            // Else
            Flatten(camera, position);
        }

        protected virtual float GetCameraWidth(Camera camera)
        {
            //if (camera.stereoEnabled)
            //{
            return Vector3.Distance(camera.ViewportToWorldPoint(new Vector3(0f, 0f, camera.nearClipPlane), Camera.MonoOrStereoscopicEye.Left),
                camera.ViewportToWorldPoint(new Vector3(1f, 1f, camera.nearClipPlane), Camera.MonoOrStereoscopicEye.Right));
            //}
            //else
            //    return Vector3.Distance(camera.ViewportToWorldPoint(new Vector3(0f, 0f, camera.nearClipPlane)),
            //        camera.ViewportToWorldPoint(new Vector3(1f, 1f, camera.nearClipPlane)));
        }

        protected virtual void Flatten(Camera camera, Vector3 position)
        {
            //isExpanded = false;
            flattened?.Invoke(camera);

            if (target)
            {
                if (doubleSided && !isFacingCamera(camera, position))
                    target.localScale = new Vector3(target.localScale.x, target.localScale.y, -float.Epsilon * Mathf.Sign(scale));
                else
                    target.localScale = new Vector3(target.localScale.x, target.localScale.y, float.Epsilon * Mathf.Sign(scale));
            }
        }

        protected virtual bool isFacingCamera(Camera camera, Vector3 position)
            => Vector3.Dot(position - transform.position, transform.forward) > 0f;
    }
}
