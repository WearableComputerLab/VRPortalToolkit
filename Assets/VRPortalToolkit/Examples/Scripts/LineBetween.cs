using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Examples
{
    /// <summary>
    /// Creates a line between two transform positions using a LineRenderer.
    /// </summary>
    [DefaultExecutionOrder(300)]
    [RequireComponent(typeof(LineRenderer))]
    public class LineBetween : MonoBehaviour
    {
        [SerializeField] private Transform _from;
        /// <summary>
        /// The transform marking the starting point of the line.
        /// </summary>
        public Transform from
        {
            get => _from;
            set => _from = value;
        }

        [SerializeField] private float _fromOffset = 0f;
        /// <summary>
        /// Distance offset from the starting transform position, moving toward the end transform.
        /// </summary>
        public float fromOffset
        {
            get => _fromOffset;
            set => _fromOffset = value;
        }

        [SerializeField] private Transform _to;
        /// <summary>
        /// The transform marking the ending point of the line.
        /// </summary>
        public Transform to
        {
            get => _to;
            set => _to = value;
        }

        [SerializeField] private float _toOffset = 0f;
        /// <summary>
        /// Distance offset from the ending transform position, moving toward the start transform.
        /// </summary>
        public float toOffset
        {
            get => _toOffset;
            set => _toOffset = value;
        }

        private LineRenderer _lineRenderer;

        protected void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        protected void LateUpdate()
        {
            if (!_lineRenderer || !from || !to) return;

            if (_lineRenderer.positionCount != 2) _lineRenderer.positionCount = 2;

            _lineRenderer.SetPosition(0, Vector3.MoveTowards(_from.position, _to.position, _fromOffset));
            _lineRenderer.SetPosition(1, Vector3.MoveTowards(_to.position, _from.position, _toOffset));
        }
    }
}
