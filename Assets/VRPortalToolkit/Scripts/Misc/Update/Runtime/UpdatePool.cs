using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Update
{
    [ExecuteInEditMode]
    public class UpdatePool : MonoBehaviour
    {
        private static UpdatePool _current;
        private static UpdatePool current
        {
            get
            {
                if (!_current)
                {
                    _current = FindObjectOfType<UpdatePool>();

                    if (!_current)
                    {
                        _current = new GameObject("Update Pool").AddComponent<UpdatePool>();
                        _current.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    }
                }

                return _current;
            }
            set => _current = value;
        }

        private UnityAction _onFixedUpdate;
        private UnityAction _onWaitForFixedUpdate;
        private bool _waitForFixedUpdate;
        private UnityAction _onUpdate;
        private UnityAction _onNullUpdate;
        private bool _nullUpdate;
        private UnityAction _onLateUpdate;
        private UnityAction _onWaitForEndOfFrame;
        private bool _waitForEndFrame;

        private Dictionary<UnityAction, List<KeyValuePair<float, Coroutine>>> _waitForSeconds;

        public static void FixedUpdateAdd(UnityAction action)
            => current._onFixedUpdate += action;

        public static void FixedUpdateRemove(UnityAction action)
            => current._onFixedUpdate -= action;

        public static void WaitForFixedUpdateAdd(UnityAction action)
        {
            current._onWaitForFixedUpdate += action;
            if (!current._waitForFixedUpdate) current.StartCoroutine(current.YieldWaitForFixedUpdate());
        }

        public static void WaitForFixedUpdateRemove(UnityAction action)
            => current._onWaitForFixedUpdate -= action;

        public static void UpdateAdd(UnityAction action)
            => current._onUpdate += action;

        public static void UpdateRemove(UnityAction action)
            => current._onUpdate -= action;

        public static void NullUpdateAdd(UnityAction action)
        {
            current._onUpdate += action;
            if (!current._nullUpdate) current.StartCoroutine(current.YieldNullUpdate());
        }

        public static void NullUpdateRemove(UnityAction action)
            => current._onUpdate -= action;

        public static void LateUpdateAdd(UnityAction action)
            => current._onLateUpdate += action;

        public static void LateUpdateRemove(UnityAction action)
            => current._onLateUpdate -= action;

        public static void WaitForEndOfFrameAdd(UnityAction action)
        {
            current._onWaitForEndOfFrame += action;
            if (!current._waitForEndFrame) current.StartCoroutine(current.YieldWaitForEndOfFrame());
        }

        public static void WaitForEndOfFrameRemove(UnityAction action)
            => current._onWaitForEndOfFrame -= action;

        protected class WFSContainer
        {
            public readonly UnityAction Action;

            public readonly float Seconds;

            public WFSContainer(UnityAction action, float seconds)
            {
                Action = action;
                Seconds = seconds;
            }

            public IEnumerator GetEnumerator()
            {
                WaitForSeconds wait = new WaitForSeconds(Seconds);

                while (Application.isPlaying)
                {
                    yield return wait;

                    Action.Invoke();
                }
            }
        }

        public static void WaitForSecondsAdd(UnityAction action, float seconds)
        {
            if (action == null) return;

            if (current._waitForSeconds == null)
                _current._waitForSeconds = new Dictionary<UnityAction, List<KeyValuePair<float, Coroutine>>>();

            WFSContainer container = new WFSContainer(action, seconds);

            KeyValuePair<float, Coroutine> pair = new KeyValuePair<float, Coroutine>(seconds, _current.StartCoroutine(container.GetEnumerator()));

            if (current._waitForSeconds.TryGetValue(action, out List<KeyValuePair<float, Coroutine>> list))
                list.Add(pair);
            else
                current._waitForSeconds[action] = new List<KeyValuePair<float, Coroutine>>(1) { pair };
        }

        public static void WaitForSecondsRemove(UnityAction action, float seconds)
        {
            if (action == null || current._waitForSeconds == null) return;

            if (_current._waitForSeconds.TryGetValue(action, out List<KeyValuePair<float, Coroutine>> list))
            {
                KeyValuePair<float, Coroutine> pair;

                for (int i = 0; i < list.Count; i++)
                {
                    pair = list[i];

                    if (pair.Key == seconds)
                    {
                        list.RemoveAt(i);
                        _current.StopCoroutine(pair.Value);

                        return;
                    }
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (_onFixedUpdate != null) _onFixedUpdate.Invoke();
        }

        protected virtual void Update()
        {
            if (_onUpdate != null) _onUpdate.Invoke();
        }

        protected virtual void LateUpdate()
        {
            if (_onLateUpdate != null) _onLateUpdate.Invoke();
        }
        private IEnumerator YieldWaitForFixedUpdate()
        {
            WaitForFixedUpdate waiter = new WaitForFixedUpdate();
            _waitForFixedUpdate = true;

            yield return waiter;

            while (_onWaitForFixedUpdate != null)
            {
                _onWaitForFixedUpdate.Invoke();

                yield return waiter;
            }

            _waitForFixedUpdate = false;
        }

        private IEnumerator YieldNullUpdate()
        {
            _nullUpdate = true;

            yield return null;

            while (_onNullUpdate != null)
            {
                _onNullUpdate.Invoke();

                yield return null;
            }

            _nullUpdate = false;
        }

        private IEnumerator YieldWaitForEndOfFrame()
        {
            WaitForEndOfFrame waiter = new WaitForEndOfFrame();
            _waitForEndFrame = true;

            yield return waiter;

            while (_onWaitForEndOfFrame != null)
            {
                _onWaitForEndOfFrame.Invoke();

                yield return waiter;
            }

            _waitForEndFrame = false;
        }
    }
}