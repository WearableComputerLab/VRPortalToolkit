using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc
{
    public class StateController : MonoBehaviour
    {
        [SerializeField] private StateRequester _stateRequester;
        public StateRequester stateRequester {
            get => _stateRequester;
            set {
                if (_stateRequester != value)
                {
                    if (Application.isPlaying)
                        RemoveRequesterListener(_stateRequester);

                    Validate.UpdateField(this, nameof(_stateRequester), _stateRequester = value);

                    if (Application.isPlaying)
                    {
                        AddRequesterListener(_stateRequester);
                        UpdateActive();
                    }
                }
            }
        }

        [SerializeField] private State _defaultState = State.Active;
        public State defaultState {
            get => _defaultState;
            set {
                if (_defaultState != value)
                {
                    Validate.UpdateField(this, nameof(_defaultState), _defaultState = value);
                    UpdateActive();
                }
            }
        }

        public enum State : byte
        {
            Inactive = 0,
            Active = 1,
            LastState = 2
        }

        [SerializeField] private Mode _stateMode = Mode.MostRequested;
        public Mode stateMode {
            get => _stateMode;
            set {
                if (_stateMode != value)
                {
                    Validate.UpdateField(this, nameof(_stateMode), _stateMode = value);
                    UpdateActive();
                }
            }
        }

        public enum Mode : byte
        {
            MostRequested = 0,
            PreferActive = 1,
            PreferInactive = 2,
            FirstSource = 3,
            LastSource = 4
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_stateRequester), nameof(stateRequester));
            Validate.FieldWithProperty(this, nameof(_defaultState), nameof(defaultState));
            Validate.FieldWithProperty(this, nameof(_stateMode), nameof(stateMode));
        }

        protected virtual void Reset()
        {
            _stateRequester = GetComponent<StateRequester>();

            if (!_stateRequester) _stateRequester = gameObject.AddComponent<StateRequester>();
        }

        protected virtual void Awake()
        {
            AddRequesterListener(_stateRequester);
            UpdateActive();
        }

        protected virtual void AddRequesterListener(StateRequester stateRequester)
        {
            if (stateRequester) stateRequester.modified.AddListener(UpdateActive);
        }

        protected virtual void RemoveRequesterListener(StateRequester stateRequester)
        {
            if (stateRequester) stateRequester.modified.RemoveListener(UpdateActive);
        }

        protected virtual void UpdateActive()
        {
            if (Application.isPlaying && _stateRequester)
            {
                switch (_stateMode)
                {
                    case Mode.PreferActive:

                        if (_stateRequester.activeRequestsCount > 0)
                            SetState(true);
                        else if (_stateRequester.deactiveRequestsCount > 0)
                            SetState(false);
                        else
                            SetDefault();
                        break;

                    case Mode.PreferInactive:

                        if (_stateRequester.deactiveRequestsCount > 0)
                            SetState(false);
                        else if (_stateRequester.activeRequestsCount > 0)
                            SetState(true);
                        else
                            SetDefault();
                        break;

                    case Mode.FirstSource:

                        if (_stateRequester.readOnlyRequests.Count > 0)
                            SetState(_stateRequester.readOnlyRequests[0].state);
                        else
                            SetDefault();
                        break;

                    case Mode.LastSource:

                        if (_stateRequester.readOnlyRequests.Count > 0)
                            SetState(_stateRequester.readOnlyRequests[_stateRequester.readOnlyRequests.Count - 1].state);
                        else
                            SetDefault();
                        break;

                    default:
                        float weight = _stateRequester.activeRequestsCount - _stateRequester.deactiveRequestsCount;

                        if (weight < 0)
                            SetState(false);
                        else if (weight > 0)
                            SetState(true);
                        else
                            SetDefault();
                        break;
                }
            }
        }

        protected virtual void SetState(bool value)
        {
            if (value != _stateRequester.gameObject.activeSelf)
                _stateRequester.gameObject.SetActive(value);
        }

        protected virtual void SetDefault()
        {
            if (defaultState == State.Inactive)
                SetState(false);
            else if (defaultState == State.Active)
                SetState(true);
        }
    }
}