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
    /// <summary>
    /// Allows players to spawn and despawn portal pairs using VR controllers and handles portal positioning and orientation.
    /// </summary>
    public class PortalManager : MonoBehaviour
    {
        [SerializeField] private List<Transform> _portalPairs = new List<Transform>();
        /// <summary>
        /// List of portal pair transforms that can be spawned. Portal pairs are recycled when depleted.
        /// </summary>
        public List<Transform> portalPairs
        {
            get => _portalPairs;
            set => _portalPairs = value;
        }

        [SerializeField] private XRBaseInteractor _leftInteractor;
        /// <summary>
        /// The left hand interactor used to grab and place portals.
        /// </summary>
        public XRBaseInteractor leftInteractor
        {
            get => _leftInteractor;
            set => _leftInteractor = value;
        }

        [SerializeField] private InputActionProperty _leftSpawnAction;
        /// <summary>
        /// Input action that triggers portal spawning from the left controller.
        /// </summary>
        public InputActionProperty leftSpawnAction
        {
            get => _leftSpawnAction;
            set => _leftSpawnAction = value;
        }

        [SerializeField] private Transform _leftOffset;
        /// <summary>
        /// Transform that defines the spawn position and orientation offset for the left controller.
        /// If null, the interactor's transform is used directly.
        /// </summary>
        public Transform leftOffset
        {
            get => _leftOffset;
            set => _leftOffset = value;
        }

        [SerializeField] private XRBaseInteractor _rightInteractor;
        /// <summary>
        /// The right hand interactor used to grab and place portals.
        /// </summary>
        public XRBaseInteractor rightInteractor
        {
            get => _rightInteractor;
            set => _rightInteractor = value;
        }

        [SerializeField] private InputActionProperty _rightSpawnAction;
        /// <summary>
        /// Input action that triggers portal spawning from the right controller.
        /// </summary>
        public InputActionProperty rightSpawnAction
        {
            get => _rightSpawnAction;
            set => _rightSpawnAction = value;
        }

        [SerializeField] private Transform _rightOffset;
        /// <summary>
        /// Transform that defines the spawn position and orientation offset for the right controller.
        /// If null, the interactor's transform is used directly.
        /// </summary>
        public Transform rightOffset
        {
            get => _rightOffset;
            set => _rightOffset = value;
        }

        /// <summary>
        /// Event triggered when a portal is successfully spawned.
        /// Provides the Transform of the spawned portal pair.
        /// </summary>
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

        /// <summary>
        /// Event handler for left controller spawn action.
        /// Calls SpawnPortal with the left interactor and offset.
        /// </summary>
        /// <param name="_">Input action callback context (unused)</param>
        private void SpawnLeftPortal(InputAction.CallbackContext _) => SpawnPortal(_leftInteractor, _leftOffset);

        /// <summary>
        /// Event handler for right controller spawn action.
        /// Calls SpawnPortal with the right interactor and offset.
        /// </summary>
        /// <param name="_">Input action callback context (unused)</param>
        private void SpawnRightPortal(InputAction.CallbackContext _) => SpawnPortal(_rightInteractor, _rightOffset);

        /// <summary>
        /// Handles the logic for spawning or despawning a portal.
        /// If the interactor is not selecting anything, a portal will be spawned.
        /// If the interactor is selecting a portal, the portal will be despawned.
        /// </summary>
        /// <param name="interactor">The interactor initiating the action</param>
        /// <param name="offset">Transform defining position and orientation offset (optional)</param>
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

        /// <summary>
        /// Updates the offset transform of an AdaptivePortal based on its maintained bounds.
        /// This adjusts the visual handle or grip position of the portal.
        /// </summary>
        /// <param name="portalSize">The AdaptivePortal component to update</param>
        private void UpdateOffset(AdaptivePortal portalSize)
        {
            if (portalSize && portalSize.offset)
            {
                portalSize.offset.localScale = new Vector3(portalSize.maintainBounds.width * 0.1f, portalSize.maintainBounds.height * 0.1f, portalSize.offset.localScale.z);
                portalSize.offset.localPosition = new Vector3(portalSize.maintainBounds.center.x, portalSize.maintainBounds.yMin - portalSize.offset.transform.localScale.y, 0f);
            }
        }

        /// <summary>
        /// Attempts to get an available portal pair from the pool.
        /// First looks for inactive portal pairs, then recycles an active one if necessary.
        /// </summary>
        /// <param name="portalPair">The output portal pair transform if found</param>
        /// <returns>True if a portal pair was found, false otherwise</returns>
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

                // Otherwise recycle first active one in the list
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

        /// <summary>
        /// Moves a portal pair from its current position in the list to the end.
        /// This implements a basic recycling system for portal pairs, prioritizing
        /// those at the beginning of the list for reuse.
        /// </summary>
        /// <param name="index">The index of the portal pair to move to the end</param>
        private void SwapBack(int index)
        {
            Transform pair = _portalPairs[index];
            _portalPairs.RemoveAt(index);
            _portalPairs.Add(pair);
        }
    }
}
