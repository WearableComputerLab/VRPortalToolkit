using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace VRPortalToolkit.Examples
{
    /// <summary>
    /// Manages and displays scores and completion times for timed challenges.
    /// </summary>
    /// <remarks>
    public class Scoreboard : MonoBehaviour
    {
        private static readonly string DecimalFormat = "0.##";

        [SerializeField] private TMP_Text _text;
        /// <summary>
        /// Text component where the scoreboard will be displayed.
        /// </summary>
        public TMP_Text text
        {
            get => _text;
            set => _text = value;
        }

        [SerializeField] private int _count = 4;
        /// <summary>
        /// Maximum number of previous scores to display on the scoreboard.
        /// </summary>
        public int count
        {
            get => _count;
            set => _count = value;
        }

        [SerializeField] private string _unitLong = "Press";
        /// <summary>
        /// Long form of the unit name (e.g., "Press", "Button", "Task").
        /// </summary>
        public string unitLong
        {
            get => _unitLong;
            set => _unitLong = value;
        }

        [SerializeField] private string _unitShort = "p";
        /// <summary>
        /// Short form of the unit name for throughput display (e.g., "p", "b", "t").
        /// </summary>
        public string unitShort
        {
            get => _unitShort;
            set => _unitShort = value;
        }

        [SerializeField] private AudioClip _audioClipForBegan;
        /// <summary>
        /// Sound played when a task begins.
        /// </summary>
        public AudioClip audioClipForBegan
        {
            get => _audioClipForBegan;
            set => _audioClipForBegan = value;
        }

        [SerializeField] private AudioClip _audioClipForCompleted;
        /// <summary>
        /// Sound played when a task is completed successfully.
        /// </summary>
        public AudioClip audioClipForCompleted
        {
            get => _audioClipForCompleted;
            set => _audioClipForCompleted = value;
        }

        [SerializeField] private AudioClip _audioClipForCancelled;
        /// <summary>
        /// Sound played when a task is cancelled.
        /// </summary>
        public AudioClip audioClipForCancelled
        {
            get => _audioClipForCancelled;
            set => _audioClipForCancelled = value;
        }

        private AudioSource _audioSource;

        private readonly List<Score> _previous = new List<Score>();

        private Score _best;

        private int _index = 0;
        /// <summary>
        /// Current task index (increments with each completed task).
        /// </summary>
        public int index => _index;
        
        private float _startTime;

        private bool _isRunning = false;
        /// <summary>
        /// Whether a task is currently in progress.
        /// </summary>
        public bool isRunning => _isRunning;

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        /// <summary>
        /// Represents a completed task score with timing information.
        /// </summary>
        public readonly struct Score
        {
            /// <summary>
            /// Reference to the scoreboard that recorded this score.
            /// </summary>
            public readonly Scoreboard scoreboard;
            
            /// <summary>
            /// Index of this task in the sequence.
            /// </summary>
            public readonly int index;
            
            /// <summary>
            /// Time taken to complete the task in seconds.
            /// </summary>
            public readonly float time;
            
            /// <summary>
            /// Throughput rate (completions per minute).
            /// </summary>
            public readonly float throughput;

            internal Score(Scoreboard scoreboard, int index, float time)
            {
                this.scoreboard = scoreboard;
                this.index = index;
                this.time = time;

                if (time != 0f)
                    throughput = 1f / (time / 60f);
                else
                    throughput = 0f;
            }
        }

        /// <summary>
        /// Event triggered when a task is completed. Provides the score details.
        /// </summary>
        public UnityAction<Score> onCompleted;

        protected void Reset()
        {
            _text = GetComponentInChildren<TMP_Text>();
        }

        public void Start()
        {
            UpdateScoreboard();
        }

        /// <summary>
        /// Clears all scores and resets the scoreboard.
        /// </summary>
        public void Clear()
        {
            _isRunning = false;
            _best = default;
            _previous.Clear();
            UpdateScoreboard();
        }

        /// <summary>
        /// Begins timing a new task.
        /// </summary>
        public void Begin()
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _startTime = Time.time;
                PlaySound(_audioClipForBegan);
            }
        }

        /// <summary>
        /// Cancels the current task without recording a score.
        /// </summary>
        public void Cancel()
        {
            if (_isRunning)
            {
                _isRunning = false;
                PlaySound(_audioClipForCancelled);
            }
        }

        /// <summary>
        /// Completes the current task and records the score.
        /// </summary>
        public void Complete()
        {
            if (_isRunning)
            {
                Score score = new Score(this, ++_index, Time.time - _startTime);

                if (_best.scoreboard == null || score.time < _best.time)
                    _best = score;

                _previous.Add(score);

                while (_previous.Count > _count)
                    _previous.RemoveAt(0);

                UpdateScoreboard();

                _isRunning = false;
                PlaySound(_audioClipForCompleted);
                onCompleted?.Invoke(score);
            }
        }

        private void UpdateScoreboard()
        {
            if (_text)
            {
                for (int i = 0; i < _count - _previous.Count; i++)
                    AppendScore();

                foreach (Score score in _previous)
                {
                    _stringBuilder.Append(_unitLong);
                    _stringBuilder.Append(" ");
                    _stringBuilder.Append(score.index);
                    _stringBuilder.Append(":");
                    AppendScore(score);
                }

                _stringBuilder.AppendLine();

                _stringBuilder.Append("Best: ");
                AppendScore(_best);

                _text.text = _stringBuilder.ToString();
                _stringBuilder.Clear();
            }
        }

        private void AppendScore(Score score = default)
        {
            if (score.scoreboard != null)
                _stringBuilder.Append(score.time.ToString(DecimalFormat));
            else
                _stringBuilder.Append("---");

            _stringBuilder.Append("sec (");

            if (score.scoreboard != null)
                _stringBuilder.Append(score.throughput.ToString(DecimalFormat));
            else
                _stringBuilder.Append("---");

            _stringBuilder.Append(_unitShort);
            _stringBuilder.AppendLine("/min)");
        }

        private void PlaySound(AudioClip audioClip)
        {
            if (audioClip)
            {
                if (!_audioSource)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                    _audioSource.loop = false;
                    _audioSource.playOnAwake = false;
                }

                _audioSource.PlayOneShot(audioClip);
            }
        }
    }
}
