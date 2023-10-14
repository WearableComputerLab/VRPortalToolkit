using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.XRI
{
    /// <summary>
    /// This class is designed to force the TeleportationProvider to invoke PortalPhysics teleportations,
    /// while also forcing PortalPhysics to invoke TeleportationProvider teleportations.
    /// </summary>
    public class PortalTeleportationPrivider : TeleportationProvider
    {
        private bool _isTeleporting;


        private static FieldInfo _timeStartedField;
        private float GetStartTime()
        {
            if (_timeStartedField == null)
            {
                _timeStartedField = typeof(TeleportationProvider).GetField("m_TimeStarted", BindingFlags.NonPublic | BindingFlags.Instance);

                if (_timeStartedField == null)
                    Debug.LogError("\"m_TimeStarted\" field could not be found!");
            }

            if (_timeStartedField != null)
                return (float)_timeStartedField.GetValue(this);

            return default;
        }

        protected override void Update()
        {
            if (!validRequest || (system.busy && locomotionPhase == LocomotionPhase.Idle) || (delayTime > 0 && Time.time - GetStartTime() < delayTime))
            {
                base.Update();
                return;
            }
            
            _isTeleporting = true;
            PortalPhysics.ForceTeleport(transform, base.Update);
            _isTeleporting = false;
        }

        protected virtual void OnEnable()
        {
            PortalPhysics.AddPreTeleportListener(transform, OnPreTeleport);
            PortalPhysics.AddPostTeleportListener(transform, OnPostTeleport);
        }

        protected virtual void OnDisable()
        {
            PortalPhysics.RemovePreTeleportListener(transform, OnPreTeleport);
            PortalPhysics.RemovePostTeleportListener(transform, OnPostTeleport);
        }

        private void OnPreTeleport(Teleportation args)
        {
            if (!_isTeleporting)
                BeginLocomotion();
        }

        private void OnPostTeleport(Teleportation args)
        {
            if (!_isTeleporting)
                EndLocomotion();
        }
    }
}
