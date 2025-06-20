using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Examples;

namespace VRPortalToolkit.Examples
{
    /// <summary>
    /// Manages a sequence of button press tasks for tutorials or challenges.
    /// Tracks progress through a series of buttons and updates a scoreboard.
    /// </summary>
    public class ButtonTask : MonoBehaviour
    {
        [SerializeField] private XRBaseInteractable[] _buttons;
        /// <summary>
        /// Array of interactable buttons to be pressed in sequence.
        /// </summary>
        public XRBaseInteractable[] buttons
        {
            get => _buttons;
            set => _buttons = value;
        }

        [SerializeField] private Transform _marker;
        /// <summary>
        /// Visual indicator that shows which button is the current target.
        /// </summary>
        public Transform marker
        {
            get => _marker;
            set => _marker = value;
        }

        [SerializeField] private Scoreboard _scoreboard;
        /// <summary>
        /// Optional scoreboard to track task completion time.
        /// </summary>
        public Scoreboard scoreboard
        {
            get => _scoreboard;
            set => _scoreboard = value;
        }

        private int _index = 0;
        private XRBaseInteractable _button;

        protected void OnEnable()
        {
            _index = 0;
            BeginButton(true);
        }

        protected void OnDisable()
        {
            _button?.firstSelectEntered?.RemoveListener(ButtonPressed);
            CancelButton();
        }

        /// <summary>
        /// Restarts the button task from the beginning.
        /// </summary>
        public void Restart()
        {
            CancelButton();
            _index = 0;
            BeginButton();
        }

        private void BeginButton(bool first = false)
        {
            if (_index < _buttons.Length)
            {
                if (!first) _scoreboard?.Begin();
                _button = _buttons[_index];
                _button?.firstSelectEntered?.AddListener(ButtonPressed);

                if (_marker && _button)
                {
                    _marker.transform.SetPositionAndRotation(_button.transform.position, _button.transform.rotation);
                    _marker?.gameObject.SetActive(true);
                }
            }
        }

        private void CancelButton()
        {
            _scoreboard?.Cancel();
            _button?.firstSelectEntered?.RemoveListener(ButtonPressed);
            _marker?.gameObject.SetActive(false);
        }

        // Used in the tutorial
        public void SkipButton()
        {
            CancelButton();

            if (_buttons.Length > 0)
                _index = (_index + 1) % _buttons.Length;
            else
                _index = 0;

            BeginButton(true);
        }

        private void CompleteButton()
        {
            _scoreboard?.Complete();
            _button?.firstSelectEntered?.RemoveListener(ButtonPressed);
            _marker?.gameObject.SetActive(false);
        }

        private void ButtonPressed(SelectEnterEventArgs _)
        {
            CompleteButton();

            if (_buttons.Length > 0)
                _index = (_index + 1) % _buttons.Length;
            else
                _index = 0;

            BeginButton();
        }
    }
}
