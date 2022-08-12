using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Tasks
{
    public class WaitTask : Task
    {
        [Header("Yield Instruction Settings")]
        [SerializeField] private Mode _mode = Mode.WaitForEndOfFrame;
        public virtual Mode mode {
            get => _mode;
            set {
                if (_mode != value)
                {
                    Validate.UpdateField(this, nameof(_mode), _mode = value);
                    isDirty = true;
                }
            }
        }

        public enum Mode
        {
            Ignore = 0,
            WaitForFixedUpdate = 1,
            Null = 2,
            WaitUntil = 3,
            WaitWhile = 4,
            WaitForSeconds = 5,
            WaitForRealtime = 6,
            WaitForEndOfFrame = 7
        }

        [ShowIf(nameof(UsesSeconds))]
        [SerializeField] private float _seconds;
        public virtual float seconds {
            get => _seconds;
            set {
                if (_seconds != value)
                {
                    Validate.UpdateField(this, nameof(_seconds), _seconds = value);

                    if (UsesSeconds) isDirty = true;
                }
            }
        }

        [ShowIf(nameof(UsesValue))]
        [SerializeField] private bool _value;
        public virtual bool value {
            get => _value;
            set {
                if (_value != value)
                {
                    Validate.UpdateField(this, nameof(_value), _value = value);

                    if (UsesValue) isDirty = true;
                }
            }
        }

        protected object yieldInstruction;
        protected IEnumerator coroutine;
        protected bool isDirty = true;

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_mode), nameof(mode));
            Validate.FieldWithProperty(this, nameof(_seconds), nameof(seconds));
            Validate.FieldWithProperty(this, nameof(_value), nameof(value));
        }

        private bool UsesSeconds => mode == Mode.WaitForSeconds || mode == Mode.WaitForRealtime;

        private bool UsesValue => mode == Mode.WaitWhile || mode == Mode.WaitUntil;

        protected override void OnBegin()
        {
            if (coroutine != null) StopCoroutine(coroutine);

            base.OnBegin();

            StartCoroutine(coroutine = DelayedTryComplete());
        }

        protected override void OnCancel()
        {
            if (coroutine != null) StopCoroutine(coroutine);

            base.OnCancel();
        }

        protected override void OnComplete()
        {
            if (coroutine != null) StopCoroutine(coroutine);

            base.OnComplete();
        }

        protected virtual IEnumerator DelayedTryComplete()
        {
            if (isDirty)
            {
                switch (mode)
                {
                    case Mode.WaitForFixedUpdate:
                        yieldInstruction = new WaitForFixedUpdate();
                        break;
                    case Mode.WaitUntil:
                        yieldInstruction = new WaitUntil(WaitUntilPredicate);
                        break;
                    case Mode.WaitWhile:
                        yieldInstruction = new WaitWhile(WaitWhilePredicate);
                        break;
                    case Mode.WaitForSeconds:
                        yieldInstruction = new WaitForSeconds(seconds);
                        break;
                    case Mode.WaitForRealtime:
                        yieldInstruction = new WaitForSecondsRealtime(seconds);
                        break;
                    case Mode.WaitForEndOfFrame:
                        yieldInstruction = new WaitForEndOfFrame();
                        break;
                    default:
                        yieldInstruction = null;
                        break;
                }

                isDirty = false;
            }
            Debug.Log("B4");
            if (yieldInstruction != null || mode == Mode.Null)
                yield return yieldInstruction;
            Debug.Log("BAfter");

            TryComplete();
        }

        protected virtual bool WaitUntilPredicate() => value;

        protected virtual bool WaitWhilePredicate() => value;
    }
}
