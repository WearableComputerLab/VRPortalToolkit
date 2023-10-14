using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Examples
{
    public class Door : MonoBehaviour
    {
        [SerializeField] private bool _isOpen;
        public bool isOpen
        {
            get => _isOpen;
            set => _isOpen = value;
        }

        public bool isClosed
        {
            get => !_isOpen;
            set => _isOpen = !value;
        }

        [SerializeField] private Transform _hingeTransform;
        public Transform hingeTransform
        {
            get => _hingeTransform;
            set => _hingeTransform = value;
        }

        [SerializeField] private float _translateSpeed = 0.5f;
        public float translateSpeed
        {
            get => _translateSpeed;
            set => _translateSpeed = value;
        }

        [SerializeField] private float _rotateSpeed = 30f;
        public float rotateSpeed
        {
            get => _rotateSpeed;
            set => _rotateSpeed = value;
        }

        [Header("Open Pose")]
        [SerializeField] private Vector3 _openPosition;
        public Vector3 openPosition
        {
            get => _openPosition;
            set => _openPosition = value;
        }

        [SerializeField] private Quaternion _openRotation;
        public Quaternion openRotation
        {
            get => _openRotation;
            set => _openRotation = value;
        }

        [Header("Closed Pose")]
        [SerializeField] private Vector3 _closedPosition;
        public Vector3 closedPosition
        {
            get => _closedPosition;
            set => _closedPosition = value;
        }

        [SerializeField] private Quaternion _closedRotation;
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
