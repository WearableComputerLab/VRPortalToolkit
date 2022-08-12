using UnityEngine;
using UnityEngine.Events;

namespace Misc.Update
{
    public class UpdateAction : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.Update);
        public UpdateMask updateMask { get => _updateMask; set => updater.updateMask = _updateMask = value; }
        protected Updater updater = new Updater();

        public UnityEvent onEnabled;
        public UnityEvent onUpdate;
        public UnityEvent onDisabled;

        protected virtual void Awake()
        {
            updater.onInvoke = InvokeUpdate;
            updater.updateMask = _updateMask;
        }

        protected virtual void OnEnable()
        {
            onEnabled?.Invoke();
            updater.enabled = true;
        }

        protected virtual void OnDisable()
        {
            updater.enabled = false;
            onDisabled?.Invoke();
        }

        public virtual void InvokeUpdate()
        {
            onUpdate?.Invoke();
        }
    }
}
