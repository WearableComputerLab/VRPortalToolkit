using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Examples
{
    /// <summary>
    /// Controls a door that can smoothly open and close with animation.
    /// </summary>
    public class Door : MonoBehaviour
    {
        [SerializeField] private bool _isOpen;
        /// <summary>
        /// Whether the door is currently open.
        /// </summary>
        public bool isOpen
        {
            get => _isOpen;
            set => _isOpen = value;
        }

        /// <summary>
        /// Whether the door is currently closed (inverse of isOpen).
        /// </summary>
        public bool isClosed
        {
            get => !_isOpen;
            set => _isOpen = !value;
        }

        [SerializeField] private Transform _hingeTransform;
        /// <summary>
        /// The transform that will be animated when the door opens and closes.
        /// </summary>
        public Transform hingeTransform
        {
            get => _hingeTransform;
            set => _hingeTransform = value;
        }

        [SerializeField] private float _translateSpeed = 0.5f;
        /// <summary>
        /// The speed at which the door translates between positions, in units per second.
        /// </summary>
        public float translateSpeed
        {
            get => _translateSpeed;
            set => _translateSpeed = value;
        }

        [SerializeField] private float _rotateSpeed = 30f;
        /// <summary>
        /// The speed at which the door rotates between orientations, in degrees per second.
        /// </summary>
        public float rotateSpeed
        {
            get => _rotateSpeed;
            set => _rotateSpeed = value;
        }

        [Header("Open Pose")]
        [SerializeField] private Vector3 _openPosition;
        /// <summary>
        /// The local position of the door when fully open.
        /// </summary>
        public Vector3 openPosition
        {
            get => _openPosition;
            set => _openPosition = value;
        }

        [SerializeField] private Quaternion _openRotation;
        /// <summary>
        /// The local rotation of the door when fully open.
        /// </summary>
        public Quaternion openRotation
        {
            get => _openRotation;
            set => _openRotation = value;
        }

        [Header("Closed Pose")]
        [SerializeField] private Vector3 _closedPosition;
        /// <summary>
        /// The local position of the door when fully closed.
        /// </summary>
        public Vector3 closedPosition
        {
            get => _closedPosition;
            set => _closedPosition = value;
        }

        [SerializeField] private Quaternion _closedRotation;
        /// <summary>
        /// The local rotation of the door when fully closed.
        /// </summary>
        public Quaternion closedRotation
        {
            get => _closedRotation;
            set => _closedRotation = value;
        }

        protected void Update()
        {
            if (_hingeTransform)
            {
                if (_isOpen)
                {
                    _hingeTransform.localPosition = Vector3.MoveTowards(_hingeTransform.localPosition, _openPosition, _translateSpeed * Time.deltaTime);
                    _hingeTransform.localRotation = Quaternion.RotateTowards(_hingeTransform.localRotation, _openRotation, _rotateSpeed * Time.deltaTime);
                }
                else
                {
                    _hingeTransform.localPosition = Vector3.MoveTowards(_hingeTransform.localPosition, _closedPosition, _translateSpeed * Time.deltaTime);
                    _hingeTransform.localRotation = Quaternion.RotateTowards(_hingeTransform.localRotation, _closedRotation, _rotateSpeed * Time.deltaTime);
                }
            }
        }
    }
}
