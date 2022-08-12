using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc
{
    public class SingleStateController : StateGroupController
    {
        [SerializeField] public SortingOrder _sortingOrder = SortingOrder.LastModified;
        public SortingOrder sortingOrder {
            get => _sortingOrder;
            set {
                if (_sortingOrder != value)
                {
                    Validate.UpdateField(this, nameof(_sortingOrder), _sortingOrder = value);
                    if (Application.isPlaying)
                    {
                        SortRequests();
                        UpdateActive();
                    }
                }
            }
        }

        public enum SortingOrder : byte
        {
            None = 0,
            LastModified = 1,
            MostRequests = 2,
            ActiveRequests = 3,
            InactiveRequests = 4
        }

        [SerializeField] public State _defaultState = State.DontAllowNone;
        public State defaultState {
            get => _defaultState;
            set {
                if (_defaultState != value)
                {
                    Validate.UpdateField(this, nameof(_defaultState), _defaultState = value);
                    if (Application.isPlaying)
                    {
                        UpdateMinimum();
                        UpdateActive();
                    }
                }
            }
        }

        public enum State : byte
        {
            Inactive = 0,
            Active = 1,
            DontAllowNone = 2
        }

        [SerializeField] public Preference _preference = Preference.ActiveThenFirst;
        public Preference preference {
            get => _preference;
            set {
                if (_preference != value)
                {
                    Validate.UpdateField(this, nameof(_preference), _preference = value);
                    if (Application.isPlaying) UpdateActive();
                }
            }
        }

        public enum Preference : byte
        {
            First = 0,
            ActiveThenFirst = 1,
            Last = 2,
            ActiveThenLast = 3,
        }

        [SerializeField] public Mode _stateMode = Mode.MostRequested;
        public Mode stateMode {
            get => _stateMode;
            set {
                if (_stateMode != value)
                {
                    Validate.UpdateField(this, nameof(_stateMode), stateMode = value);
                    if (Application.isPlaying) UpdateActive();
                }
            }
        }

        public enum Mode : byte
        {
            MostRequested = 0,
            PreferActive = 1,
            PreferDeactive = 2,
            FirstValid = 3,
            LastValid = 4
        }

        protected int minimum = int.MinValue;

        protected override void OnValidate()
        {
            base.OnValidate();

            Validate.FieldWithProperty(this, nameof(_sortingOrder), nameof(sortingOrder));
            Validate.FieldWithProperty(this, nameof(_stateMode), nameof(stateMode));
            Validate.FieldWithProperty(this, nameof(_preference), nameof(preference));
            Validate.FieldWithProperty(this, nameof(_stateMode), nameof(stateMode));
        }

        public override void Awake()
        {
            UpdateMinimum();
            base.Awake();
        }

        protected override void RequesterModified(StateRequester requester)
        {
            if (sortingOrder == SortingOrder.LastModified)
            {
                requesters.Remove(requester);
                requesters.Insert(0, requester);
            }
            else
                SortRequests();

            base.RequesterModified(requester);
        }

        protected override void UpdateActive()
        {
            if (requesters.Count > 0)
            {
                StateRequester current = null;

                switch (_stateMode)
                {
                    case Mode.PreferActive:
                    {
                        int currentCount = 0;

                        foreach (StateRequester requester in requesters)
                        {
                            if (!requester) continue;

                            if (requester.activeRequestsCount - requester.deactiveRequestsCount < minimum) continue;

                            if (!current || requester.activeRequestsCount > currentCount
                                || (requester.activeRequestsCount == currentCount && (requester.deactiveRequestsCount < current.deactiveRequestsCount
                                || (requester.deactiveRequestsCount == current.deactiveRequestsCount && requester == GetPreferred(current, requester)))))
                            {
                                current = requester;
                                currentCount = requester.activeRequestsCount;
                            }
                        }
                        break;
                    }
                    case Mode.PreferDeactive:
                    {
                        int currentCount = 0;

                        foreach (StateRequester requester in requesters)
                        {
                            if (!requester) continue;

                            if (requester.activeRequestsCount - requester.deactiveRequestsCount < minimum) continue;

                            if (!current || requester.deactiveRequestsCount > currentCount
                                || (requester.activeRequestsCount == currentCount && (requester.deactiveRequestsCount < current.deactiveRequestsCount
                                || (requester.deactiveRequestsCount == current.deactiveRequestsCount && requester == GetPreferred(current, requester)))))
                            {
                                current = requester;
                                currentCount = requester.activeRequestsCount;
                            }
                        }
                        break;
                    }
                    case Mode.FirstValid:
                    {
                        int weight;

                        foreach (StateRequester requester in requesters)
                        {
                            if (!requester) continue;

                            weight = requester.activeRequestsCount - requester.deactiveRequestsCount;

                            if (weight < minimum) continue;

                            if (requester.activeRequestsCount - requester.deactiveRequestsCount > 0)
                            {
                                current = requester;
                                break;
                            }
                        }
                        break;
                    }
                    case Mode.LastValid:
                    {
                        StateRequester requester;

                        for (int i = requesters.Count - 1; i >= 0; i++)
                        {
                            if (!(requester = requesters[i])) continue;

                            if (requester.activeRequestsCount - requester.deactiveRequestsCount < minimum) continue;

                            if (requester.activeRequestsCount - requester.deactiveRequestsCount > 0)
                            {
                                current = requester;
                                break;
                            }
                        }
                        break;
                    }
                    default:
                    {
                        int currentWeight = 0, weight;

                        foreach (StateRequester requester in requesters)
                        {
                            if (!requester) continue;

                            weight = requester.activeRequestsCount - requester.deactiveRequestsCount;

                            if (weight < minimum) continue;

                            if (!current || weight > currentWeight || (weight == currentWeight && GetPreferred(current, requester) == requester))
                            {
                                current = requester;
                                currentWeight = weight;
                            }
                        }
                        break;
                    }
                }

                if (_defaultState == State.DontAllowNone && current == null) current = GetDefault();

                foreach (StateRequester requester in requesters)
                    if (requester != null) SetState(requester, false);

                if (current) SetState(current, true);
            }
        }

        protected virtual void SortRequests()
        {
            switch (_sortingOrder)
            {
                case SortingOrder.MostRequests:
                    requesters.Sort((i, j) => i.requestsCount.CompareTo(j.requestsCount));
                    break;

                case SortingOrder.ActiveRequests:
                    requesters.Sort((i, j) =>
                    {
                        int order = i.activeRequestsCount.CompareTo(j.activeRequestsCount);

                        if (order != 0) return order;
                        
                        return i.deactiveRequestsCount.CompareTo(j.deactiveRequestsCount);
                    });
                    break;

                case SortingOrder.InactiveRequests:
                    requesters.Sort((i, j) =>
                    {
                        int order = i.deactiveRequestsCount.CompareTo(j.deactiveRequestsCount);

                        if (order != 0) return order;

                        return i.activeRequestsCount.CompareTo(j.activeRequestsCount);
                    });
                    break;

                default:
                    // Do Nothing
                    // Also SortingOrder.LastModified is not done here
                    break;
            }
        }

        protected virtual void UpdateMinimum()
        {
            switch (_defaultState)
            {
                case State.Inactive:
                    minimum = 1;
                    break;

                case State.Active:
                    minimum = 0;
                    break;

                default:
                    minimum = int.MinValue;
                    break;
            }
        }

        protected virtual void SetState(StateRequester requester, bool value)
        {
            if (value != requester.gameObject.activeSelf)
                requester.gameObject.SetActive(value);
        }

        protected virtual StateRequester GetPreferred(StateRequester firstRequest, StateRequester secondRequester)
        {
            switch (_preference)
            {
                case Preference.ActiveThenFirst:
                    if (!firstRequest.gameObject.activeSelf && secondRequester.gameObject.activeSelf)
                        return secondRequester;
                    return firstRequest;

                case Preference.Last:
                    return secondRequester;

                case Preference.ActiveThenLast:
                    if (!secondRequester.gameObject.activeSelf && firstRequest.gameObject.activeSelf)
                        return secondRequester;
                    return secondRequester;

                default: // First
                    return firstRequest;
            }
        }

        protected virtual StateRequester GetDefault()
        {
            if (requesters.Count > 0)
            {
                if (requesters.Count == 1) return requesters[0];

                switch (_preference)
                {
                    case Preference.ActiveThenFirst:
                    {
                        foreach (StateRequester requester in requesters)
                            if (requester && requester.gameObject.activeSelf) return requester;

                        return requesters[0];
                    }

                    case Preference.Last:
                        return requesters[requesters.Count - 1];

                    case Preference.ActiveThenLast:
                    {
                        StateRequester requester;

                        for (int i = requesters.Count - 1; i >= 0; i++)
                            if ((requester = requesters[i]) && requester.gameObject.activeSelf) return requester;

                        return requesters[requesters.Count - 1];
                    }

                    default: // First
                        return requesters[0];
                }
            }

            return null;
        }
    }
}