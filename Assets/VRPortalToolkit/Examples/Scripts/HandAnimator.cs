using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRPortalToolkit.Examples
{
    [RequireComponent(typeof(Animator))]
    public class HandAnimator : MonoBehaviour
    {
        private static readonly int TriggerID = Animator.StringToHash("Trigger");
        private static readonly int GripID = Animator.StringToHash("Grip");

        [SerializeField] private InputActionProperty _triggerAction;
        public InputActionProperty triggerAction
        {
            get => _triggerAction;
            set => _triggerAction = value;
        }

        [SerializeField] private InputActionProperty _gripAction;
        public InputActionProperty gripAction
        {
            get => _gripAction;
            set => _gripAction = value;
        }

        private Animator _animator;

        protected void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        protected void Update()
        {
            if (!_animator) return;

            if (triggerAction.action != null)
            {
                float triggerValue = triggerAction.action.ReadValue<float>();
                _animator.SetFloat(TriggerID, triggerValue);
            }

            if (gripAction.action != null)
            {
                float gripvalue = gripAction.action.ReadValue<float>();
                _animator.SetFloat(GripID, gripvalue);
            }
        }
    }
}
