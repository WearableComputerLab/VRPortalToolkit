using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Tasks
{
    public class AudioTask : Task
    {
        [Header("Audio Settings")]
        [SerializeField] private UpdateMask _checkCompleteOn = new UpdateMask(UpdateFlags.Update);
        public UpdateMask checkCompleteOn => _checkCompleteOn;
        protected Updater updater = new Updater();

        [SerializeField] private AudioSource _audioSource;
        public virtual AudioSource audioSource {
            get => _audioSource;
            set => _audioSource = value;
        }
        public void ClearAudioSource() => audioSource = null;
        protected AudioSource _actualAudioSource;

        [SerializeField] private AudioMode _audioSourceMode = (AudioMode)~0L;
        public virtual AudioMode audioSourceMode {
            get => _audioSourceMode;
            set => _audioSourceMode = value;
        }

        [System.Flags]
        public enum AudioMode
        {
            None = 0,
            StartAudioOnBegin = 1 << 0,
            StopAudioOnCancel = 1 << 1,
            StopAudioOnComplete = 1 << 2
        }

        [SerializeField] private AudioClip _optionalOneShot;
        public virtual AudioClip optionalOneShot {
            get => _optionalOneShot;
            set => _optionalOneShot = value;
        }
        public void ClearAudioClip() => optionalOneShot = null;

        protected virtual void Reset()
        {
            audioSource = GetComponentInChildren<AudioSource>(true);
            if (!audioSource)
            {
                audioSource = GetComponentInParent<AudioSource>();
                if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        protected virtual void Awake()
        {
            updater.updateMask = _checkCompleteOn;
            updater.onInvoke = ForceCheckAudioIsComplete;
        }

        protected virtual void OnValidate()
        {
            updater.updateMask = _checkCompleteOn;
        }

        protected override void OnBegin()
        {
            _actualAudioSource = audioSource;

            if (audioSourceMode.HasFlag(AudioMode.StartAudioOnBegin))
            {
                if (_actualAudioSource)
                {
                    if (optionalOneShot)
                        _actualAudioSource.PlayOneShot(optionalOneShot);
                    else
                        _actualAudioSource.Play();
                }
            }

            base.OnBegin();

            updater.enabled = true;
        }

        protected override void OnCancel()
        {
            updater.enabled = false;

            if (audioSourceMode.HasFlag(AudioMode.StopAudioOnCancel) && _actualAudioSource)
                _actualAudioSource.Stop();

            _actualAudioSource = null;

            base.OnCancel();
        }

        protected override void OnComplete()
        {
            updater.enabled = false;

            if (audioSourceMode.HasFlag(AudioMode.StopAudioOnComplete) && _actualAudioSource)
                _actualAudioSource.Stop();

            _actualAudioSource = null;

            base.OnComplete();
        }

        public virtual void CheckAudioIsComplete()
        {
            if (isRunning) ForceCheckAudioIsComplete();
        }

        protected virtual void ForceCheckAudioIsComplete()
        {
            if (!_actualAudioSource || !_actualAudioSource.isPlaying)
                TryComplete();
        }
    }
}
