using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VRPortalToolkit.Examples;

namespace VRPortalToolkit.Examples
{
    /// <summary>
    /// Controls a door that opens when a task is completed within a required time.
    /// </summary>
    [RequireComponent(typeof(Door))]
    public class ScoreDoor : MonoBehaviour
    {
        private Door _door;
        /// <summary>
        /// The Door component that will be controlled by this ScoreDoor.
        /// </summary>
        public Door door => _door;

        [SerializeField] private Scoreboard _scoreboard;
        /// <summary>
        /// The Scoreboard that tracks task completion times.
        /// </summary>
        public Scoreboard scoreboard
        {
            get => _scoreboard;
            set => _scoreboard = value;
        }

        [SerializeField] private float _requiredTime = 0.1f;
        /// <summary>
        /// The maximum time (in seconds) allowed to complete the task and open the door.
        /// If the task is completed faster than this time, the door will open.
        /// </summary>
        public float requiredTime
        {
            get => _requiredTime;
            set => _requiredTime = value;
        }

        protected void Reset()
        {
            _scoreboard = GetComponentInChildren<Scoreboard>();
        }

        protected void Awake()
        {
            _door = GetComponent<Door>();
        }

        protected void OnEnable()
        {
            if (_scoreboard != null)
                _scoreboard.onCompleted += OnScoreboardCompleted;
        }

        protected void OnDisable()
        {
            if (_scoreboard != null)
                _scoreboard.onCompleted -= OnScoreboardCompleted;
        }

        private void OnScoreboardCompleted(Scoreboard.Score score)
        {
            if (score.time <= _requiredTime)
                if (_door) _door.isOpen = true;
        }
    }
}
