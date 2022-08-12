using Misc.EditorHelpers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Misc.Observables
{
    public class IntegerInput : ObservableInteger
    {
        [Space]
        [SerializeField] private InputActionProperty _inputAction;
        public InputActionProperty inputAction
        {
            get => _inputAction;
            set
            {
                if (_inputAction != value)
                {
                    if (isActiveAndEnabled && Application.isPlaying)
                    {
                        RemoveInputListener(_inputAction);
                        Validate.UpdateField(this, nameof(_inputAction), _inputAction = value);
                        AddInputListener(_inputAction);
                    }
                    else
                        Validate.UpdateField(this, nameof(_inputAction), _inputAction = value);
                }
            }
        }

        public InputActionReference asadat;

        protected override void OnValidate()
        {
            base.OnValidate();

            Validate.FieldWithProperty(this, nameof(_inputAction), nameof(inputAction));
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            AddInputListener(inputAction);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            RemoveInputListener(inputAction);
        }

        protected virtual void AddInputListener(InputActionProperty input)
        {
            if (input != null && input.action != null)
            {
                input.action.started += ActionStarted;
                input.action.performed += ActionPerformed;
                input.action.canceled += ActionCancelled;

                if (input.reference == null) input.action.Enable();
            }
        }

        protected virtual void RemoveInputListener(InputActionProperty input)
        {
            if (input != null && input.action != null)
            {
                input.action.started -= ActionStarted;
                input.action.performed -= ActionPerformed;
                input.action.canceled -= ActionCancelled;

                if (input.reference == null) input.action.Disable();
            }
        }

        protected virtual void ActionStarted(InputAction.CallbackContext context)
        {
            currentValue = context.ReadValue<int>();
        }

        protected virtual void ActionPerformed(InputAction.CallbackContext context)
        {
            currentValue = context.ReadValue<int>();
        }

        protected virtual void ActionCancelled(InputAction.CallbackContext context)
        {
            currentValue = context.ReadValue<int>();
        }
    }
}