using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Data
{
    public class PortalTrace : IEnumerable<Portal>, IEnumerable, IReadOnlyCollection<Portal>, IReadOnlyList<Portal>
    {
        public int Capacity { get => _startToEnd.Capacity; set => _startToEnd.Capacity = value; }

        public int Count => _startToEnd.Count;

        public Portal this[int index] => GetPortal(index);

        private List<Portal> _startToEnd;
        
        public PortalTrace()
        {
            _startToEnd = new List<Portal>();
        }

        public PortalTrace(int capacity)
        {
            _startToEnd = new List<Portal>(capacity);
        }

        public PortalTrace(IEnumerable<Portal> startToEndPortals)
        {
            _startToEnd = new List<Portal>();
            AddEndTeleports(startToEndPortals);
        }

        public void Clear() => _startToEnd.Clear();

        public void AddStartTeleports(IEnumerable<Portal> portals)
        {
            foreach (Portal portal in portals)
                AddStartTeleport(portal);
        }

        public void AddStartTeleport(Portal portal)
        {
            if (portal && portal.connectedPortal)
            {
                if (_startToEnd.Count > 0 && _startToEnd[0] == portal)
                    _startToEnd.RemoveAt(0);
                else
                    _startToEnd.Insert(0, portal.connectedPortal);
            }
        }

        public void AddEndTeleports(IEnumerable<Portal> portals)
        {
            foreach (Portal portal in portals)
                AddEndTeleport(portal);
        }

        public void AddEndTeleport(Portal portal)
        {
            if (portal && portal.connectedPortal)
            {
                if (_startToEnd.Count > 0 && _startToEnd[_startToEnd.Count - 1] == portal.connectedPortal)
                    _startToEnd.RemoveAt(_startToEnd.Count - 1);
                else
                    _startToEnd.Add(portal);
            }
        }

        public Portal GetPortal(int index) => _startToEnd[index];

        public Portal GetUndoPortal(int index) => _startToEnd[_startToEnd.Count - index - 1]?.connectedPortal;

        public IEnumerable<Portal> GetPortals()
        {
            for (int i = 0; i < _startToEnd.Count; i++)
                yield return _startToEnd[i];
        }

        public IEnumerable<Portal> GetUndoPortals()
        {
            for (int i = 1; i <= _startToEnd.Count; i++)
                yield return _startToEnd[_startToEnd.Count - i]?.connectedPortal;
        }

        public void ApplyPortals(Transform target)
        {
            if (target)
            {
                Matrix4x4 localToWorld = Matrix4x4.TRS(target.position, target.rotation, target.localScale);

                foreach (Portal portal in GetPortals())
                    if (portal) portal.ModifyMatrix(ref localToWorld);

                target.SetPositionAndRotation(localToWorld.GetColumn(3), localToWorld.rotation);
                target.localScale = localToWorld.lossyScale;
            }
        }
        public void ApplyUndoPortals(Transform target)
        {
            if (target)
            {
                Matrix4x4 localToWorld = Matrix4x4.TRS(target.position, target.rotation, target.localScale);

                foreach (Portal portal in GetUndoPortals())
                    if (portal) portal.ModifyMatrix(ref localToWorld);

                target.SetPositionAndRotation(localToWorld.GetColumn(3), localToWorld.rotation);
                target.localScale = localToWorld.lossyScale;
            }
        }

        public void TeleportPortals(Transform target)
        {
            if (target)
            {
                foreach (Portal portal in GetPortals())
                    if (portal) portal.Teleport(target);
            }
        }

        public void TeleportUndoPortals(Transform target)
        {
            if (target)
            {
                foreach (Portal portal in GetUndoPortals())
                    if (portal) portal.Teleport(target);
            }
        }

        public void TeleportDifference(Transform target, PortalRay[] portalRays)
            => TeleportDifference(target, GetPortals(portalRays, portalRays != null ? portalRays.Length : 0));

        public void TeleportDifference(Transform target, PortalRay[] portalRays, int portalRaysCount)
            => TeleportDifference(target, GetPortals(portalRays, portalRaysCount));

        public void TeleportDifference(Transform target, IEnumerable<Portal> newStartToEndPortals)
        {
            IEnumerator<Portal> newEnumator = newStartToEndPortals.GetEnumerator();

            if (newEnumator.MoveNext())
            {
                for (int i = 0; i < _startToEnd.Count; i++)
                {
                    Portal portal = _startToEnd[i];

                    if (portal)
                    {
                        if (newEnumator.Current != portal)
                        {
                            // Undo any left over portals
                            BackTrackPortals(target, i);

                            break; // Break, still have new portals to apply
                        }

                        if (!newEnumator.MoveNext())
                        {
                            // Undo any left over portals
                            BackTrackPortals(target, i + 1);

                            return; // Return, no new portals to apply
                        }
                    }
                    else
                    {
                        // TODO: This portal has been destroyed, so what do we do?
                    }
                }

                // Apply the new portals
                do
                {
                    if (newEnumator.Current)
                        PortalPhysics.Teleport(target, newEnumator.Current);
                }
                while (newEnumator.MoveNext());
            }
            else
            {
                // Undo all teleportations
                BackTrackPortals(target, 0);
            }
        }

        private void BackTrackPortals(Transform target, int newCount)
        {
            // Need to unteleport
            for (int i = _startToEnd.Count - 1; i >= newCount; i--)
            {
                Portal portal = _startToEnd[i];

                if (portal && portal.connectedPortal)
                    PortalPhysics.Teleport(target, portal.connectedPortal);
            }

            // Just incase
            //while (sourceToTarget.Count > i)
            //    sourceToTarget.RemoveAt(sourceToTarget.Count - 1);
        }

        private IEnumerable<Portal> GetPortals(PortalRay[] portalRays, int count)
        {
            if (portalRays != null)
            {
                int actualCount = count > portalRays.Length ? portalRays.Length : count;

                for (int i = 0; i < actualCount; i++)
                {
                    Portal portal = portalRays[i].fromPortal;

                    if (portal != null) yield return portal;
                }
            }
        }

        public IEnumerator<Portal> GetEnumerator() => GetPortals().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetPortals().GetEnumerator();
    }
}
