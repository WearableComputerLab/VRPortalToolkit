using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VRPortalToolkit.Examples;

namespace VRPortalToolkit.Examples
{
    [RequireComponent(typeof(Door))]
    public class ScoreDoor : MonoBehaviour
    {
        private Door _door;
        public Door door => _door;

        [SerializeField] private Scoreboard _scoreboard;
        public Scoreboard scoreboard
        {
            get => _scoreboard;
            set => _scoreboard = value;
        }

        [SerializeField] private float _requiredTime = 0.1f;
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
