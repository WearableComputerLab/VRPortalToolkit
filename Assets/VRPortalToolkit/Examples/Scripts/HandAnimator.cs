using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRPortalToolkit.Examples
{
    /// <summary>
    /// Animates hand models based on controller input values.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class HandAnimator : MonoBehaviour
    {
        /// <summary>
        /// Cached hash ID for the "Trigger" animator parameter.
        /// </summary>
        private static readonly int TriggerID = Animator.StringToHash("Trigger");
        
        /// <summary>
        /// Cached hash ID for the "Grip" animator parameter.
        /// </summary>
        private static readonly int GripID = Animator.StringToHash("Grip");

        [SerializeField] private InputActionProperty _triggerAction;
        /// <summary>
        /// Input action for the trigger button/axis that controls finger pointing animations.
        /// </summary>
        public InputActionProperty triggerAction
        {
            get => _triggerAction;
            set => _triggerAction = value;
        }

        [SerializeField] private InputActionProperty _gripAction;
        /// <summary>
        /// Input action for the grip button/axis that controls hand closing animations.
        /// </summary>
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
