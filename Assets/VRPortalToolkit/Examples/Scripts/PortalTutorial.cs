using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Examples;

namespace VRPortalToolkit.Examples
{
    public class PortalTutorial : MonoBehaviour
    {
        [SerializeField] private Transform _head;
        public Transform head
        {
            get => _head;
            set => _head = value;
        }

        [SerializeField] private ButtonTask _buttonTask;
        public ButtonTask buttonTask
        {
            get => _buttonTask;
            set => _buttonTask = value;
        }

        [SerializeField] private GameObject _portals;
        public GameObject portals
        {
            get => _portals;
            set => _portals = value;
        }

        [SerializeField] private GameObject _moveHere;
        public GameObject moveHere
        {
            get => _moveHere;
            set => _moveHere = value;
        }

        [SerializeField] private float _moveHereThreshold = 0.15f;
        public float moveHereThreshold
        {
            get => _moveHereThreshold;
            set => _moveHereThreshold = value;
        }

        [SerializeField] private GameObject _createPortal;
        public GameObject createPortal
        {
            get => _createPortal;
            set => _createPortal = value;
        }

        [SerializeField] private GameObject _portalhere;
        public GameObject portalhere
        {
            get => _portalhere;
            set => _portalhere = value;
        }

        [SerializeField] private PortalManager _portalManager;
        public PortalManager portalManager
        {
            get => _portalManager;
            set => _portalManager = value;
        }

        [SerializeField] private float _portalHereThreshold = 1f;
        public float portalHereThreshold
        {
            get => _portalHereThreshold;
            set => _portalHereThreshold = value;
        }

        [SerializeField] private ScoreDoor _door;
        public ScoreDoor door
        {
            get => _door;
            set => _door = value;
        }

        [SerializeField] private float _waitTime = 8f;
        public float waitTime
        {
            get => _waitTime;
            set => _waitTime = value;
        }

        [SerializeField] private GameObject _worldGrab;
        public GameObject worldGrab
        {
            get => _worldGrab;
            set => _worldGrab = value;
        }

        private int _count = 0;
        private bool _portalSpawned = false;
        private float _placeTime = 0f;

        private State _state;
        private enum State
        {
            TeleportTask,
            MoveHere,
            PortalTask,
            CreatePortal,
            PortalHere,
            WorldGrab,
            Complete,
        }
        protected virtual void Start()
        {
            Restart();
        }

        protected virtual void OnEnable()
        {
            if (_buttonTask && _buttonTask.scoreboard) _buttonTask.scoreboard.onCompleted += OnButtonTaskCompleted;
            if (_portalManager) _portalManager.portalSpawned += OnPortalSpawned;
        }

        protected virtual void Update()
        {
            switch (_state)
            {
                case State.TeleportTask:
                    if (_count >= 3)
                    {
                        _state = State.MoveHere;
                        if (_buttonTask) _buttonTask.enabled = false;
                        moveHere?.gameObject.SetActive(true);
                    }
                    break;
                case State.MoveHere:
                    if (!_head || !_moveHere || Vector2.Distance(new Vector2(_head.position.x, _head.position.z),
                        new Vector2(_moveHere.transform.position.x, _moveHere.transform.position.z)) < _moveHereThreshold)
                    {
                        _count = 0;
                        _state = State.PortalTask;
                        if (_buttonTask)
                        {
                            _buttonTask.enabled = true;
                            _buttonTask.SkipButton();
                        }
                        _portals?.SetActive(true);
                        _moveHere?.SetActive(false);
                    }
                    break;
                case State.PortalTask:
                    if (_count >= 5)
                    {
                        _state = State.CreatePortal;
                        if (_buttonTask) _buttonTask.enabled = false;
                        _portals?.SetActive(false);
                        _createPortal?.SetActive(true);
                        if (_portalManager) _portalManager.enabled = true;
                        _portalSpawned = false;
                    }
                    break;
                case State.CreatePortal:
                    if (_portalSpawned)
                    {
                        _createPortal?.SetActive(false);
                        _count = 0;
                        if (_buttonTask)
                        {
                            _buttonTask.enabled = true;
                            _buttonTask.SkipButton();
                        }
                        _state = State.PortalHere;
                        _portalhere?.SetActive(true);
                    }
                    break;
                case State.PortalHere:
                    if (PortalWithinThreshold())
                    {
                        _count = 0;
                        _state = State.WorldGrab;
                        _portalhere?.SetActive(false);
                        _worldGrab?.SetActive(true);
                        if (_door) _door.enabled = true;
                        _placeTime = Time.time;
                    }
                    break;
                case State.WorldGrab:
                    if (_door && _door.door ? _door.door.isClosed : false)
                        _placeTime = Time.time;

                    if (_placeTime + _waitTime < Time.time)
                    {
                        _state = State.Complete;
                        _worldGrab?.SetActive(false);
                    }
                    break;
            }
        }

        protected virtual void OnDisable()
        {
            if (_buttonTask && _buttonTask.scoreboard) _buttonTask.scoreboard.onCompleted -= OnButtonTaskCompleted;
            if (_portalManager) _portalManager.portalSpawned -= OnPortalSpawned;
        }

        private bool PortalWithinThreshold()
        {
            if (!_portalhere) return true;

            if (!_portalManager) return false;

            foreach (Transform pairs in _portalManager.portalPairs)
            {
                if (!pairs) continue;

                for (int i = 0; i < pairs.childCount; i++)
                {
                    Transform child = pairs.GetChild(i);

                    if (child && child.gameObject.activeInHierarchy && Vector2.Distance(new Vector2(child.position.x, child.position.z),
                        new Vector2(_portalhere.transform.position.x, _portalhere.transform.position.z)) < _portalHereThreshold)
                        return true;
                }
            }

            return false;
        }

        public void Restart()
        {
            _count = 0;
            if (_buttonTask) _buttonTask.enabled = true;
            _portals?.SetActive(false);
            _moveHere?.SetActive(false);
            _createPortal?.SetActive(false);
            _portalhere?.SetActive(false);
            _worldGrab?.SetActive(false);

            if (_portalManager)
            {
                _portalManager.enabled = false;

                foreach (Transform pair in _portalManager.portalPairs)
                    pair?.gameObject.SetActive(false);
            }

            if (_door)
            {
                if (_door.door) _door.door.isClosed = true;
                _door.enabled = false;
            }
        }

        private void OnButtonTaskCompleted(Scoreboard.Score _) => _count++;
        private void OnPortalSpawned(Transform portal) => _portalSpawned = true;
    }
}
