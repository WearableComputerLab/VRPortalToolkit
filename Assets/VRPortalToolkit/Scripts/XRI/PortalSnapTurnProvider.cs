using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.XRI
{
    public class PortalSnapTurnProvider : ActionBasedSnapTurnProvider
    {
        private static FieldInfo _timeStartedField;
        private void UpdateTimeStartedField()
        {
            if (_timeStartedField == null)
            {
                _timeStartedField = typeof(SnapTurnProviderBase).GetField("m_TimeStarted", BindingFlags.NonPublic | BindingFlags.Instance);
                if (_timeStartedField == null) Debug.LogError("\"m_TimeStarted\" field could not be found!");
            }
        }
        private float GetTimeStarted()
        {
            UpdateTimeStartedField();
            return (float)_timeStartedField?.GetValue(this);
        }
        private void SetTimeStarted(float time)
        {
            UpdateTimeStartedField();
            _timeStartedField?.SetValue(this, time);
        }

        private static FieldInfo _currentTurnAmountField;
        private void UpdateCurrentTurnAmountField()
        {
            if (_currentTurnAmountField == null)
            {
                _currentTurnAmountField = typeof(SnapTurnProviderBase).GetField("m_CurrentTurnAmount", BindingFlags.NonPublic | BindingFlags.Instance);
                if (_currentTurnAmountField == null) Debug.LogError("\"m_CurrentTurnAmount\" field could not be found!");
            }
        }
        private float GetCurrentTurnAmount()
        {
            UpdateCurrentTurnAmountField();
            return (float)_currentTurnAmountField?.GetValue(this);
        }
        private void SetCurrentTurnAmount(float time)
        {
            UpdateCurrentTurnAmountField();
            _currentTurnAmountField?.SetValue(this, time);
        }

        // Copied from original
        protected virtual new void Update()
        {
            float timeStarted = GetTimeStarted();
            if (timeStarted > 0f && (timeStarted + debounceTime < Time.time))
            {
                SetTimeStarted(0f);
                return;
            }

            if (locomotionPhase == LocomotionPhase.Done)
                locomotionPhase = LocomotionPhase.Idle;

            var input = ReadInput();
            var amount = GetTurnAmount(input);
            if (Mathf.Abs(amount) > 0f || locomotionPhase == LocomotionPhase.Started)
                StartTurn(amount);
            else if (Mathf.Approximately(GetCurrentTurnAmount(), 0f) && locomotionPhase == LocomotionPhase.Moving)
                locomotionPhase = LocomotionPhase.Done;

            if (locomotionPhase != LocomotionPhase.Moving)
                return;

            float currentTurnAmount = GetCurrentTurnAmount();
            if (Mathf.Abs(currentTurnAmount) > 0f && BeginLocomotion())
            {
                if (!system.xrOrigin)
                {
                    locomotionPhase = LocomotionPhase.Done;
                    SetCurrentTurnAmount(0f);
                    return;
                }

                PortalPhysics.ForceTeleport(system.xrOrigin.transform, () =>
                {
                    var xrOrigin = system.xrOrigin;
                    if (xrOrigin != null)
                    {
                        xrOrigin.RotateAroundCameraUsingOriginUp(GetCurrentTurnAmount());
                    }
                    else
                    {
                        locomotionPhase = LocomotionPhase.Done;
                    }
                    SetCurrentTurnAmount(0f);
                    EndLocomotion();

                    if (Mathf.Approximately(amount, 0f))
                        locomotionPhase = LocomotionPhase.Done;
                }, this);
            }
        }
    }
}
