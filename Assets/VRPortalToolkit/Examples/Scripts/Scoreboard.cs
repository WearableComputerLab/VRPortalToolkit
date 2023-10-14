using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace VRPortalToolkit.Examples
{
    public class Scoreboard : MonoBehaviour
    {
        private static readonly string DecimalFormat = "0.##";

        [SerializeField] private TMP_Text _text;
        public TMP_Text text
        {
            get => _text;
            set => _text = value;
        }

        [SerializeField] private int _count = 4;
        public int count
        {
            get => _count;
            set => _count = value;
        }

        [SerializeField] private string _unitLong = "Press";
        public string unitLong
        {
            get => _unitLong;
            set => _unitLong = value;
        }

        [SerializeField] private string _unitShort = "p";
        public string unitShort
        {
            get => _unitShort;
            set => _unitShort = value;
        }

        [SerializeField] private AudioClip _audioClipForBegan;
        public AudioClip audioClipForBegan
        {
            get => _audioClipForBegan;
            set => _audioClipForBegan = value;
        }

        [SerializeField] private AudioClip _audioClipForCompleted;
        public AudioClip audioClipForCompleted
        {
            get => _audioClipForCompleted;
            set => _audioClipForCompleted = value;
        }

        [SerializeField] private AudioClip _audioClipForCancelled;
        public AudioClip audioClipForCancelled
        {
            get => _audioClipForCancelled;
            set => _audioClipForCancelled = value;
        }

        private AudioSource _audioSource;

        private readonly List<Score> _previous = new List<Score>();

        private Score _best;

        private int _index = 0;
        public int index => _index;
        
        private float _startTime;

        private bool _isRunning = false;
        public bool isRunning => _isRunning;

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        public readonly struct Score
        {
            public readonly Scoreboard scoreboard;
            public readonly int index;
            public readonly float time;
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

        public UnityAction<Score> onCompleted;

        protected void Reset()
        {
            _text = GetComponentInChildren<TMP_Text>();
        }

        public void Start()
        {
            UpdateScoreboard();
        }

        public void Clear()
        {
            _isRunning = false;
            _best = default;
            _previous.Clear();
            UpdateScoreboard();
        }

        public void Begin()
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _startTime = Time.time;
                PlaySound(_audioClipForBegan);
            }
        }

        public void Cancel()
        {
            if (_isRunning)
            {
                _isRunning = false;
                PlaySound(_audioClipForCancelled);
            }
        }

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
