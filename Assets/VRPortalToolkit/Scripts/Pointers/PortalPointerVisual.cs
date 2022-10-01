using Misc;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VRPortalToolkit.Data;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Pointers
{
    // TODO: Need to make a serializable object ppol

    public class PortalPointerVisual : MonoBehaviour
    {
        [SerializeField] private PortalPointer _portalPointer;
        public PortalPointer portalPointer
        {
            get => _portalPointer;
            set => _portalPointer = value;
        }

        [SerializeField] private GameObject _linePrefab;
        public GameObject linePrefab
        {
            get => _linePrefab;
            set => _linePrefab = value;
        }

        [SerializeField] private bool _stopAtContact = true;
        public bool stopAtContact
        {
            get => _stopAtContact;
            set => _stopAtContact = value;
        }

        /*[SerializeField] private int _poolCapacity = -1;
        public int poolCapacity
        {
            get => linePool.Count;
            set
            {
                _poolCapacity = value;
                if (linePool != null) linePool. = value;
            }
        }*/

        protected LinkedList<Transform> lines = new LinkedList<Transform>();
        protected ObjectPool<Transform> linePool;

        /*protected virtual void OnValidate()
        {
            poolCapacity = _poolCapacity;
        }*/

        protected virtual void Reset()
        {
            _portalPointer = GetComponentInParent<PortalPointer>();
            if (!portalPointer) portalPointer = GetComponentInChildren<PortalPointer>(true);
        }

        protected virtual void Awake()
        {
            linePool = new ObjectPool<Transform>(CreateLine, null, null, DestroyLine);
        }

        protected virtual Transform CreateLine()
        {
            GameObject lineObject;

            if (linePrefab) lineObject = Instantiate(linePrefab, transform.position, transform.rotation);
            else lineObject = new GameObject($"[{gameObject.name}] Line Renderer");

            lineObject.transform.SetParent(transform, false);

            return lineObject.transform;
        }

        protected virtual void DestroyLine(Transform line)
        {
            if (line) Destroy(line.gameObject);
        }

        protected virtual void LateUpdate()
        {
            Apply();
        }

        public virtual void Apply()
        {
            if (portalPointer)
            {
                // TODO: Could optimise by only creating line renderers when a new portal rocks up in the array...
                if (_stopAtContact && _portalPointer.TryGetHitInfo(out RaycastHit hitInfo, out int portalRayIndex))
                {
                    UpdateRenderersCount(portalRayIndex + 1);
                    ApplyPortalRaysToRenderers();

                    if (lines.Count > 0)
                    {
                        Transform last = lines.Last.Value.transform;
                        last.localScale = new Vector3(last.localScale.x, last.localScale.y, hitInfo.distance);
                    }
                }
                else
                {
                    UpdateRenderersCount(_portalPointer.portalRaysCount);
                    ApplyPortalRaysToRenderers();
                }
            }
        }

        protected void ApplyPortalRaysToRenderers()
        {
            int index = 0;
            PortalRay portalRay;

            bool usesLayers = false, usesTag = false;
            int layer = linePrefab.layer;
            string tag = linePrefab.tag;

            foreach (Transform line in lines)
            {
                if (index >= _portalPointer.portalRaysCount) return;

                portalRay = _portalPointer.GetPortalRay(index++);

                if (portalRay.fromPortal)
                {
                    if (usesLayers |= portalRay.fromPortal.usesLayers)
                        layer = portalRay.fromPortal.ModifyLayer(layer);

                    if (usesTag |= portalRay.fromPortal.usesTag)
                        tag = portalRay.fromPortal.ModifyTag(tag);
                }

                if (usesLayers) line.gameObject.layer = layer;
                if (usesTag) line.tag = tag;

                line.position = portalRay.origin;

                if (portalRay.direction.magnitude > 0f)
                    line.rotation = Quaternion.LookRotation(portalRay.direction);
                
                if (portalRay.localToWorldMatrix.ValidTRS())
                    line.localScale = new Vector3(portalRay.localToWorldMatrix.lossyScale.x * linePrefab.transform.localScale.x,
                        portalRay.localToWorldMatrix.lossyScale.y * linePrefab.transform.localScale.y, portalRay.direction.magnitude);
            }
        }

        protected void UpdateRenderersCount(int count)
        {
            if (lines.Count < count)
            {
                Transform line;

                do
                {
                    line = linePool.Get();
                    line.gameObject.SetActive(true);
                    lines.AddLast(line);
                } while (lines.Count < count);
            }
            else if (lines.Count > count)
            {
                Transform line;

                do
                {
                    line = lines.Last.Value;
                    line.gameObject.SetActive(false);
                    linePool.Release(line);
                    lines.RemoveLast();
                } while (lines.Count > count);
            }
        }
    }
}