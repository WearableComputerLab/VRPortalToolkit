using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using VRPortalToolkit.Physics;
using VRPortalToolkit.Portables;
using System.Collections.ObjectModel;
using UnityEngine.EventSystems;
using VRPortalToolkit.Utilities;
using Misc;

namespace VRPortalToolkit
{
    public static partial class PortalPhysics
    {
        private static List<Portal> _allPortals = new List<Portal>();
        public static IReadOnlyCollection<Portal> allPortals { get; } = new ReadOnlyCollection<Portal>(_allPortals);

        private static Dictionary<Transform, TrackedTransform> _trackedTransforms = new Dictionary<Transform, TrackedTransform>();
        //public static IReadOnlyCollection<Transform> trackedTransforms { get; } = new ReadOnlyCollection<Transform>(_trackedTransforms.Values);

        private static Dictionary<Vector3Int, List<Portal>> _portalsByUnit = new Dictionary<Vector3Int, List<Portal>>();
        private static ObjectPool<List<Portal>> _portalListPool = new ObjectPool<List<Portal>>(() => new List<Portal>(), null, (i) => i.Clear());

        private static float _unitSize = 1f;
        private static float _unitScale = 1f;
        private static float _unitOffset = -0.5f;

        private struct DefaultPortable : IPortable
        {
            public LayerMask portalLayerMask => defaultPortalLayerMask;

            private Transform _transform;

            public DefaultPortable(Transform transform) { _transform = transform; }

            public Vector3 GetOrigin() => _transform.position;

            public void Teleport(Portal portal)
            {
                //PortalPhysics.Te
            }
        }

        private static List<Portal> _currentPortals = new List<Portal>();

        private static Dictionary<Transform, TeleportListener> _teleportListeners = new Dictionary<Transform, TeleportListener>();

        public static Action lateFixedUpdate;

        // TODO: Might need to make this more complicated
        // Should it do it just on lateUpdate, or also for teleport called?
        public static bool autoSyncTransforms { get; set; } = true;
        public static LayerMask defaultPortalLayerMask { get; set; } = 1 << 3;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialise()
        {
            PlayerLoopSystem system = PlayerLoop.GetCurrentPlayerLoop();

            PlayerLoopSystem lateFixedUpdate = new PlayerLoopSystem()
            {
                updateDelegate = LateFixedUpdate,
                type = typeof(PortalPhysics)
            };

            if (system.InsertAfter<FixedUpdate.ScriptRunBehaviourFixedUpdate>(lateFixedUpdate, out PlayerLoopSystem newSystem))
                PlayerLoop.SetPlayerLoop(newSystem);
            else
                Debug.LogError("Portal physics failed to initialise!");
        }

        // TODO: Should sync transform changes
        private static void LateFixedUpdate()
        {
            lateFixedUpdate?.Invoke();

            UpdatePortalsByUnit();

            bool anyTeleports = false;

            foreach (TrackedTransform tracked in _trackedTransforms.Values)
                if (UpdateTrackedTransform(tracked)) anyTeleports = true;

            // Update portals
            foreach (Portal portal in _allPortals)
                if (portal) portal.previousWorldToLocalMatrix = portal.transform.worldToLocalMatrix;

            if (anyTeleports && autoSyncTransforms) UnityEngine.Physics.SyncTransforms();
        }

        private static void UpdatePortalsByUnit()
        {
            // Clear Dictionary
            foreach (List<Portal> list in _portalsByUnit.Values)
                _portalListPool.Release(list);

            _portalsByUnit.Clear();

            _unitSize = 10f;
            _unitScale = 1 / _unitSize;
            _unitOffset = -_unitSize * 0.5f;

            // Update Dictionary
            Bounds bounds;
            Vector3 min, max;
            Vector3Int index;
            List<Portal> portals;
            int x, y, z, maxX, maxY, maxZ;

            foreach (Portal portal in _allPortals)
            {
                if (!portal) continue;

                bounds = new Bounds(portal.transform.position, Vector3.zero);

                foreach (Collider collider in portal.colliders)
                {
                    // TODO: What about the previous bounds?
                    // Gonna need to do some complicated math for this one
                    // Need to figure out what these bounds would have been with the previous transformation

                    bounds.Encapsulate(collider.bounds);
                }

                // Now to add to every bounds
                min = bounds.min;
                max = bounds.max;

                maxX = GetMaxUnit(max.x);
                maxY = GetMaxUnit(max.y);
                maxZ = GetMaxUnit(max.z);

                for (x = GetMinUnit(max.x); x < maxX; x++)
                {
                    for (y = GetMinUnit(max.y); y < maxY; y++)
                    {
                        for (z = GetMinUnit(max.z); z < maxZ; z++)
                        {
                            index = new Vector3Int(x, y, z);

                            if (!_portalsByUnit.TryGetValue(index, out portals))
                                _portalsByUnit[index] = portals = _portalListPool.Get();

                            portals.Add(portal);
                        }
                    }
                }
            }
        }

        private static bool UpdateTrackedTransform(TrackedTransform tracked)
        {
            if (!tracked.transform) return false;

            Vector3 origin = tracked.portable.GetOrigin(), previousOrigin, dirVec;
            RaycastHit hitInfo;
            LayerMask layerMask;
            bool hitPortal, teleported = false;
            int recursions = 0;

            do
            {
                hitPortal = false;

                UpdateCurrentPortals(tracked.previousOrigin, origin);

                foreach (Portal portal in _currentPortals)
                {
                    previousOrigin = portal.transform.TransformPoint(portal.previousWorldToLocalMatrix.MultiplyPoint(tracked.previousOrigin));

                    if (previousOrigin != origin)
                    {
                        dirVec = origin - previousOrigin;
                        layerMask = tracked.portable.portalLayerMask;

                        foreach (Collider collider in portal.colliders)
                        {
                            // Is there a way to separately cast for their colliders? Not really rigid
                            if (collider && layerMask == (layerMask | (1 << collider.gameObject.layer))
                                && collider.Raycast(new Ray(previousOrigin, dirVec), out hitInfo, dirVec.magnitude))
                            {
                                previousOrigin = tracked.transform.InverseTransformPoint(hitInfo.point); // TODO: May need to shift this by a little (+ dirVec * 0.00001f)
                                Teleport(tracked.transform, portal);
                                origin = tracked.portable.GetOrigin();
                                previousOrigin = tracked.transform.TransformPoint(previousOrigin);
                                tracked.previousOrigin = previousOrigin + Vector3.ClampMagnitude(origin - previousOrigin, 0.0002f);
                                hitPortal = teleported = true;
                                recursions++;
                                break;
                            }
                        }
                    }

                    if (hitPortal) break;
                }

            } while (hitPortal && recursions < 16);

            // Update nodes
            tracked.previousOrigin = tracked.portable.GetOrigin();

            return teleported;
        }

        private static void UpdateCurrentPortals(Vector3 start, Vector3 end)
        {
            int x, y, z, minX, minY, minZ, maxX, maxY, maxZ;

            GetCoordinate(start.x, end.x, out minX, out maxX);
            GetCoordinate(start.y, end.y, out minY, out maxY);
            GetCoordinate(start.z, end.z, out minZ, out maxZ);

            Vector3Int index;
            List<Portal> foundPortals;

            _currentPortals.Clear();

            bool first = true;

            for (x = minX; x < maxX; x++)
            {
                for (y = minY; y < maxY; y++)
                {
                    for (z = minZ; z < maxZ; z++)
                    {
                        index = new Vector3Int(x, y, z);

                        if (_portalsByUnit.TryGetValue(index, out foundPortals))
                        {
                            if (first)
                            {
                                foreach (Portal portal in foundPortals)
                                    _currentPortals.Add(portal);

                                first = false;
                            }
                            else
                            {
                                foreach (Portal portal in foundPortals)
                                    if (!_currentPortals.Contains(portal)) _currentPortals.Add(portal);
                            }
                        }
                    }
                }
            }
        }

        private static void GetCoordinate(float start, float end, out int min, out int max)
        {
            if (start < end)
            {
                min = GetMinUnit(start);
                max = GetMaxUnit(end);
            }
            else
            {
                min = GetMinUnit(end);
                max = GetMaxUnit(start);
            }
        }

        private static int GetMinUnit(float x) => Mathf.FloorToInt((x + _unitOffset) * _unitScale);

        private static int GetMaxUnit(float x) => Mathf.CeilToInt((x + _unitOffset) * _unitScale);

        public static bool ForcePortalCheck(Transform transform)
        {
            if (_trackedTransforms.TryGetValue(transform, out TrackedTransform tracked))
            {
                UpdatePortalsByUnit();
                if (UpdateTrackedTransform(tracked) && autoSyncTransforms) UnityEngine.Physics.SyncTransforms();
                return true;
            }

            return false;
        }

        private static Portal GetPortal(RaycastHit hitInfo)
        {
            if (hitInfo.rigidbody)
                return hitInfo.rigidbody.GetComponent<Portal>();

            if (hitInfo.collider)
                return hitInfo.collider.GetComponent<Portal>();

            return null;
        }

        public static void RegisterPortal(Portal portal)
        {
            if (!portal) return;

            if (!_allPortals.Contains(portal))
            {
                portal.previousWorldToLocalMatrix = portal.transform.worldToLocalMatrix;
                _allPortals.Add(portal);
            }
        }

        public static void UnregisterPortal(Portal portal)
        {
            _allPortals.Remove(portal);
        }
        public static void TrackPortable(Transform transform)
        {
            if (transform) TrackPortable(transform, new DefaultPortable(transform));
        }

        public static void TrackPortable(Transform transform, IPortable portable)
        {
            if (!transform || portable == null) return;

            if (!_trackedTransforms.TryGetValue(transform, out TrackedTransform tracked))
                _trackedTransforms[transform] = tracked = new TrackedTransform() { transform = transform };

            tracked.portable = portable;

            tracked.previousOrigin = portable.GetOrigin();
        }

        public static void UntrackPortable(Transform transform, IPortable portable)
        {
            if (_trackedTransforms.TryGetValue(transform, out TrackedTransform tracked) && tracked.portable == portable)
                _trackedTransforms.Remove(transform);
        }

        public static void UntrackPortable(Transform transform)
        {
            _trackedTransforms.Remove(transform);
        }

        public static void IgnoreParentTeleport(Transform transform, bool ignore = true)
        {
            if (!transform) return;

            if (ignore)
                GetOrCreateTeleportListener(transform).ignoreParent = true;
            else if (_teleportListeners.TryGetValue(transform, out TeleportListener listener))
            {
                listener.ignoreParent = false;

                TryRemoveTeleportListener(listener);
            }
        }

        public static bool GetIgnoreParentTeleport(Transform transform)
        {
            if (transform && _teleportListeners.TryGetValue(transform, out TeleportListener listener))
                return listener.ignoreParent;

            return false;
        }

        public static void Teleport(Transform transform, Portal portal)
        {
            if (!transform || !portal) return;

            if (transform.gameObject.TryGetComponent(out IPortable portable))
                portable.Teleport(portal);
            else
                portal.Teleport(transform);
        }

        public static void ForceTeleport(Transform transform, Action teleport, Component source = null, Portal portal = null)
        {
            if (!transform) return;

            InvokePreTeleport(source, transform, transform, portal);

            if (teleport != null) teleport.Invoke();

            InvokePostTeleport(source, transform, transform, portal);

            if (_trackedTransforms.TryGetValue(transform, out TrackedTransform tracked))
                tracked.previousOrigin = tracked.portable.GetOrigin();
        }

        private static void InvokePreTeleport(Component source, Transform target, Transform transform, Portal fromPortal)
        {
            Teleportation teleportation = Teleportation.Get(source, target, transform, fromPortal);

            ExecuteEvents.Execute<ITeleportHandler>(transform.gameObject, null, (x, _) => x.OnPreTeleport(teleportation));

            if (_teleportListeners.TryGetValue(transform, out TeleportListener listener))
            {
                if (target != transform && listener.ignoreParent)
                    return;

                listener.onPreTeleport?.Invoke(teleportation);
            }

            Teleportation.Release(teleportation);

            for (int i = 0; i < transform.childCount; i++)
                InvokePreTeleport(source, target, transform.GetChild(i), fromPortal);
        }

        private static void InvokePostTeleport(Component source, Transform target, Transform transform, Portal fromPortal)
        {
            Teleportation teleportation = Teleportation.Get(source, target, transform, fromPortal);

            ExecuteEvents.Execute<ITeleportHandler>(transform.gameObject, null, (x, _) => x.OnPostTeleport(teleportation));

            if (_teleportListeners.TryGetValue(transform, out TeleportListener listener))
            {
                if (target != transform && listener.ignoreParent)
                    return;
                
                listener.onPostTeleport?.Invoke(teleportation);
            }

            Teleportation.Release(teleportation);

            for (int i = 0; i < transform.childCount; i++)
                InvokePostTeleport(source, target, transform.GetChild(i), fromPortal);
        }

        public static void AddPreTeleportListener(Transform transform, TeleportAction onPreTeleport)
        {
            if (!transform) return;

            GetOrCreateTeleportListener(transform).onPreTeleport += onPreTeleport;
        }

        public static void RemovePreTeleportListener(Transform transform, TeleportAction onPreTeleport)
        {
            if (!transform) return;

            if (_teleportListeners.TryGetValue(transform, out TeleportListener listener))
            {
                listener.onPreTeleport -= onPreTeleport;

                TryRemoveTeleportListener(listener);
            }
        }

        public static void AddPostTeleportListener(Transform transform, TeleportAction onPostTeleport)
        {
            if (!transform) return;

            GetOrCreateTeleportListener(transform).onPostTeleport += onPostTeleport;
        }

        public static void RemovePostTeleportListener(Transform transform, TeleportAction onPostTeleport)
        {
            if (!transform) return;

            if (_teleportListeners.TryGetValue(transform, out TeleportListener listener))
            {
                listener.onPostTeleport -= onPostTeleport;

                TryRemoveTeleportListener(listener);
            }
        }

        private static TeleportListener GetOrCreateTeleportListener(Transform transform)
        {
            if (!_teleportListeners.TryGetValue(transform, out TeleportListener listener))
                _teleportListeners[transform] = listener = new TeleportListener() { transform = transform };

            return listener;
        }

        private static void TryRemoveTeleportListener(TeleportListener listener)
        {
            if (listener == null) return;

            if (!listener.transform || (!listener.ignoreParent
                && listener.onPreTeleport == null && listener.onPostTeleport == null))
                _teleportListeners.Remove(listener.transform);
        }
    }
}