using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using VRPortalToolkit.XRI;

namespace VRPortalToolkit.Examples
{
    public class PortalManager : MonoBehaviour
    {
        [SerializeField] private List<Transform> _portalPairs = new List<Transform>();
        public List<Transform> portalPairs
        {
            get => _portalPairs;
            set => _portalPairs = value;
        }

        [SerializeField] private XRBaseInteractor _leftInteractor;
        public XRBaseInteractor leftInteractor
        {
            get => _leftInteractor;
            set => _leftInteractor = value;
        }

        [SerializeField] private InputActionProperty _leftSpawnAction;
        public InputActionProperty leftSpawnAction
        {
            get => _leftSpawnAction;
            set => _leftSpawnAction = value;
        }

        [SerializeField] private Transform _leftOffset;
        public Transform leftOffset
        {
            get => _leftOffset;
            set => _leftOffset = value;
        }

        [SerializeField] private XRBaseInteractor _rightInteractor;
        public XRBaseInteractor rightInteractor
        {
            get => _rightInteractor;
            set => _rightInteractor = value;
        }

        [SerializeField] private InputActionProperty _rightSpawnAction;
        public InputActionProperty rightSpawnAction
        {
            get => _rightSpawnAction;
            set => _rightSpawnAction = value;
        }

        [SerializeField] private Transform _rightOffset;
        public Transform rightOffset
        {
            get => _rightOffset;
            set => _rightOffset = value;
        }

        public UnityAction<Transform> portalSpawned;

        // Portal parents are changed when interactables are being held, so this is just a quick fix to that
        private readonly Dictionary<XRPortalInteractable, Transform> _portalsToRoot = new Dictionary<XRPortalInteractable, Transform>(); 

        protected void OnEnable()
        {
            if (_leftSpawnAction.action != null)
            {
                _leftSpawnAction.EnableDirectAction();
                _leftSpawnAction.action.started += SpawnLeftPortal;
            }

            if (_rightSpawnAction.action != null)
            {
                _rightSpawnAction.EnableDirectAction();
                _rightSpawnAction.action.started += SpawnRightPortal;
            }
        }

        protected void OnDisable()
        {
            if (_leftSpawnAction.action != null)
                _leftSpawnAction.action.started -= SpawnLeftPortal;

            if (_rightSpawnAction.action != null)
                _rightSpawnAction.action.started -= SpawnRightPortal;
        }

        private void SpawnLeftPortal(InputAction.CallbackContext _) => SpawnPortal(_leftInteractor, _leftOffset);

        private void SpawnRightPortal(InputAction.CallbackContext _) => SpawnPortal(_rightInteractor, _rightOffset);

        private void SpawnPortal(XRBaseInteractor interactor, Transform offset)
        {
            if (!interactor) return;

            if (!interactor.hasSelection)
            {
                if (!offset) offset = interactor.transform;

                if (TryGetPortalPair(out Transform portalPair))
                {
                    XRPortalInteractable entry = portalPair.GetComponentInChildren<XRPortalInteractable>();

                    if (!entry) return;
                    _portalsToRoot[entry] = portalPair;

                    AdaptivePortal entrySize = entry.GetComponent<AdaptivePortal>();

                    if (entrySize)
                    {
                        UpdateOffset(entrySize);
                        UpdateOffset(entrySize.connected);

                        entry.transform.SetPositionAndRotation(offset.TransformPoint(
                            new Vector3(entrySize.maintainBounds.center.x, -entrySize.maintainBounds.yMin)), offset.rotation);
                    }
                    else
                        entry.transform.SetPositionAndRotation(offset.position, offset.rotation);

                    if (entry.connected)
                    {
                        _portalsToRoot[entry.connected] = portalPair;
                        entry.connected.transform.SetPositionAndRotation(entry.transform.position, entry.transform.rotation);
                    }

                    entry.transform.Rotate(Vector3.up, 180f);

                    portalPair.gameObject.SetActive(true);

                    interactor.interactionManager?.SelectEnter((IXRSelectInteractor)interactor, entry);

                    portalSpawned?.Invoke(portalPair);
                }
            }
            else
            {
                XRPortalInteractable entry = interactor.interactablesSelected[0] as XRPortalInteractable;

                // Unspawn portal
                if (entry && _portalsToRoot.TryGetValue(entry, out Transform portalPair))
                {
                    _portalsToRoot.Remove(entry);
                    entry.interactionManager?.CancelInteractableSelection((IXRSelectInteractable)entry);
                    
                    if (entry.connected != null)
                    {
                        _portalsToRoot.Remove(entry.connected);
                        entry.connected.interactionManager?.CancelInteractableSelection((IXRSelectInteractable)entry.connected);
                    }

                    if (portalPair) portalPair.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateOffset(AdaptivePortal portalSize)
        {
            if (portalSize && portalSize.offset)
            {
                portalSize.offset.localScale = new Vector3(portalSize.maintainBounds.width * 0.1f, portalSize.maintainBounds.height * 0.1f, portalSize.offset.localScale.z);
                portalSize.offset.localPosition = new Vector3(portalSize.maintainBounds.center.x, portalSize.maintainBounds.yMin - portalSize.offset.transform.localScale.y, 0f);
            }
        }

        private bool TryGetPortalPair(out Transform portalPair)
        {
            if (_portalPairs != null)
            {
                // Search for an inactive pair
                for (int i = 0; i < _portalPairs.Count; i++)
                {
                    portalPair = _portalPairs[i];

                    if (portalPair && !portalPair.gameObject.activeSelf)
                    {
                        SwapBack(i);
                        return true;
                    }
                }

                // Otherwise recycle first active on in the list
                for (int i = 0; i < _portalPairs.Count; i++)
                {
                    portalPair = _portalPairs[i];

                    if (portalPair)
                    {
                        portalPair.gameObject.SetActive(false);
                        SwapBack(i);
                        return true;
                    }
                }
            }

            portalPair = null;
            return false;
        }

        private void SwapBack(int index)
        {
            Transform pair = _portalPairs[index];
            _portalPairs.RemoveAt(index);
            _portalPairs.Add(pair);
        }
    }
}
