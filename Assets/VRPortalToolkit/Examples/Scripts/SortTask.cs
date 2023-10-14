using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Examples;

namespace VRPortalToolkit.Examples
{
    public class SortTask : MonoBehaviour
    {
        [SerializeField] private XRBaseInteractable _startButton;
        public XRBaseInteractable startButton
        {
            get => _startButton;
            set => _startButton = value;
        }

        [SerializeField] private OrbContainer[] _orbContainers;
        public OrbContainer[] orbContainers
        {
            get => _orbContainers;
            set => _orbContainers = value;
        }

        [SerializeField] private Scoreboard _scoreboard;
        public Scoreboard scoreboard
        {
            get => _scoreboard;
            set => _scoreboard = value;
        }

        [SerializeField] private float _floorHeight = 0.15f;
        public float floorHeight
        {
            get => _floorHeight;
            set => _floorHeight = value;
        }

        private readonly Dictionary<Transform, Vector3> _origins = new Dictionary<Transform, Vector3>();

        protected void OnEnable()
        {
            _startButton?.firstSelectEntered?.AddListener(ButtonPressed);
        }

        protected void OnDisable()
        {
            _startButton?.firstSelectEntered?.RemoveListener(ButtonPressed);
        }

        protected void Update()
        {
            if (_scoreboard && _scoreboard.isRunning)
            {
                foreach (OrbContainer container in _orbContainers)
                {
                    if (container == null) continue;

                    foreach (Transform orb in container.orbs)
                    {
                        if (!orb) continue;

                        if (orb.transform.position.y < floorHeight)
                            ResetOrb(orb);
                    }
                }

                TryComplete();
            }
        }

        private void TryComplete()
        {
            foreach (OrbContainer container in _orbContainers)
                if (!container || !container.isValid) return;

            DisableOrbs();

            _scoreboard.Complete();
        }

        private void DisableOrbs()
        {
            foreach (OrbContainer container in _orbContainers)
            {
                if (container == null) continue;

                foreach (Transform orb in container.orbs)
                {
                    if (!orb) continue;

                    ResetOrb(orb);
                    orb.gameObject.SetActive(false);
                }
            }
        }

        private void ButtonPressed(SelectEnterEventArgs _)
        {
            if (_scoreboard)
            {
                _scoreboard.Cancel();
                DisableOrbs();

                _origins.Clear();
                foreach (OrbContainer container in _orbContainers)
                {
                    if (container == null) continue;

                    foreach (Transform orb in container.orbs)
                    {
                        if (!orb) continue;

                        _origins.Add(orb, orb.transform.position);
                        orb.gameObject.SetActive(true);
                    }
                }

                _scoreboard.Begin();
            }
        }

        private void ResetOrb(Transform orb)
        {
            if (!orb) return;

            if (_origins.TryGetValue(orb, out Vector3 origin))
            {
                orb.position = origin;

                if (orb.TryGetComponent(out Rigidbody rigidbody))
                {
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                }
            }
        }
    }
}
