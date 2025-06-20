using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Data
{
    /// <summary>
    /// Tracks a sequence of portals that an object has passed through.
    /// </summary>
    /// <remarks>
    /// The PortalTrace class maintains an ordered list of portals that an object has passed through,
    /// allowing for precise tracking of the object's path through connected portals.
    /// This is useful for teleporting objects correctly when they move through multiple portals.
    /// </remarks>
    public class PortalTrace : IEnumerable<Portal>, IEnumerable, IReadOnlyCollection<Portal>, IReadOnlyList<Portal>
    {
        /// <summary>
        /// Gets or sets the capacity of the internal list storing the portals.
        /// </summary>
        public int Capacity { get => _startToEnd.Capacity; set => _startToEnd.Capacity = value; }

        /// <summary>
        /// Gets the number of portals in the trace.
        /// </summary>
        public int Count => _startToEnd.Count;

        /// <summary>
        /// Gets the portal at the specified index in the trace.
        /// </summary>
        /// <param name="index">The zero-based index of the portal to get.</param>
        /// <returns>The portal at the specified index.</returns>
        public Portal this[int index] => GetPortal(index);

        private List<Portal> _startToEnd;
        
        /// <summary>
        /// Initializes a new instance of the PortalTrace class with default capacity.
        /// </summary>
        public PortalTrace()
        {
            _startToEnd = new List<Portal>();
        }

        /// <summary>
        /// Initializes a new instance of the PortalTrace class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the internal list.</param>
        public PortalTrace(int capacity)
        {
            _startToEnd = new List<Portal>(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the PortalTrace class with the specified portals.
        /// </summary>
        /// <param name="startToEndPortals">The collection of portals to initialize the trace with.</param>
        public PortalTrace(IEnumerable<Portal> startToEndPortals)
        {
            _startToEnd = new List<Portal>();
            AddEndTeleports(startToEndPortals);
        }

        /// <summary>
        /// Clears all portals from the trace.
        /// </summary>
        public void Clear() => _startToEnd.Clear();

        /// <summary>
        /// Adds multiple portals to the beginning of the trace.
        /// </summary>
        /// <param name="portals">The collection of portals to add to the beginning of the trace.</param>
        public void AddStartTeleports(IEnumerable<Portal> portals)
        {
            foreach (Portal portal in portals)
                AddStartTeleport(portal);
        }

        /// <summary>
        /// Adds a portal to the beginning of the trace.
        /// </summary>
        /// <param name="portal">The portal to add to the beginning of the trace.</param>
        /// <remarks>
        /// When adding a portal to the start of the trace, the connected portal is actually stored.
        /// If the first portal in the trace is already the same as the one being added, it will be removed
        /// (cancelling out the teleportation).
        /// </remarks>
        public void AddStartTeleport(Portal portal)
        {
            if (portal && portal.connected)
            {
                // Remove destroyed
                while (_startToEnd.Count > 0 && !_startToEnd[0])
                    _startToEnd.RemoveAt(0);

                if (_startToEnd.Count > 0 && _startToEnd[0] == portal)
                    _startToEnd.RemoveAt(0);
                else
                    _startToEnd.Insert(0, portal.connected);
            }
        }

        /// <summary>
        /// Adds multiple portals to the end of the trace.
        /// </summary>
        /// <param name="portals">The collection of portals to add to the end of the trace.</param>
        public void AddEndTeleports(IEnumerable<Portal> portals)
        {
            foreach (Portal portal in portals)
                AddEndTeleport(portal);
        }

        /// <summary>
        /// Adds a portal to the end of the trace.
        /// </summary>
        /// <param name="portal">The portal to add to the end of the trace.</param>
        /// <remarks>
        /// If the last portal in the trace is already the connected portal of the one being added,
        /// it will be removed (cancelling out the teleportation).
        /// </remarks>
        public void AddEndTeleport(Portal portal)
        {
            if (portal && portal.connected)
            {
                // Remove destroyed
                while (_startToEnd.Count > 0 && !_startToEnd[_startToEnd.Count - 1])
                    _startToEnd.RemoveAt(_startToEnd.Count - 1);

                if (_startToEnd.Count > 0 && _startToEnd[_startToEnd.Count - 1] == portal.connected)
                    _startToEnd.RemoveAt(_startToEnd.Count - 1);
                else
                    _startToEnd.Add(portal);
            }
        }

        /// <summary>
        /// Gets the portal at the specified index in the trace.
        /// </summary>
        /// <param name="index">The zero-based index of the portal to get.</param>
        /// <returns>The portal at the specified index.</returns>
        public Portal GetPortal(int index) => _startToEnd[index];

        /// <summary>
        /// Gets the undo portal at the specified index in the trace.
        /// </summary>
        /// <param name="index">The zero-based index of the undo portal to get.</param>
        /// <returns>The connected portal of the portal at the reverse index.</returns>
        /// <remarks>
        /// This method returns the connected portal of the portal at (Count - index - 1),
        /// which is useful for undoing teleportations in reverse order.
        /// </remarks>
        public Portal GetUndoPortal(int index) => _startToEnd[_startToEnd.Count - index - 1]?.connected;

        /// <summary>
        /// Gets all portals in the trace in order from start to end.
        /// </summary>
        /// <returns>An enumerable collection of portals from start to end.</returns>
        public IEnumerable<Portal> GetPortals()
        {
            for (int i = 0; i < _startToEnd.Count; i++)
                yield return _startToEnd[i];
        }

        /// <summary>
        /// Gets all undo portals in the trace in order from end to start.
        /// </summary>
        /// <returns>An enumerable collection of undo portals from end to start.</returns>
        /// <remarks>
        /// This method returns the connected portals of all portals in the trace in reverse order,
        /// which is useful for undoing teleportations.
        /// </remarks>
        public IEnumerable<Portal> GetUndoPortals()
        {
            for (int i = 1; i <= _startToEnd.Count; i++)
                yield return _startToEnd[_startToEnd.Count - i]?.connected;
        }

        /*public void ApplyPortals(Transform target)
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
        }*/

        /// <summary>
        /// Teleports the target through all portals in the trace in order from start to end.
        /// </summary>
        /// <param name="target">The transform to teleport.</param>
        public void TeleportPortals(Transform target)
        {
            if (target)
            {
                foreach (Portal portal in GetPortals())
                    if (portal) portal.Teleport(target);
            }
        }

        /// <summary>
        /// Teleports the target through all undo portals in the trace in order from end to start.
        /// </summary>
        /// <param name="target">The transform to teleport.</param>
        public void TeleportUndoPortals(Transform target)
        {
            if (target)
            {
                foreach (Portal portal in GetUndoPortals())
                    if (portal) portal.Teleport(target);
            }
        }

        /// <summary>
        /// Teleports the target through the difference between the current trace and the specified portal rays.
        /// </summary>
        /// <param name="target">The transform to teleport.</param>
        /// <param name="portalRays">The array of portal rays to compare against.</param>
        public void TeleportDifference(Transform target, PortalRay[] portalRays)
            => TeleportDifference(target, GetPortals(portalRays, portalRays != null ? portalRays.Length : 0));

        /// <summary>
        /// Teleports the target through the difference between the current trace and the specified portal rays, up to the specified count.
        /// </summary>
        /// <param name="target">The transform to teleport.</param>
        /// <param name="portalRays">The array of portal rays to compare against.</param>
        /// <param name="portalRaysCount">The number of portal rays to consider.</param>
        public void TeleportDifference(Transform target, PortalRay[] portalRays, int portalRaysCount)
            => TeleportDifference(target, GetPortals(portalRays, portalRaysCount));

        /// <summary>
        /// Teleports the target through the difference between the current trace and the specified portals.
        /// </summary>
        /// <param name="target">The transform to teleport.</param>
        /// <param name="newStartToEndPortals">The new collection of portals to compare against.</param>
        /// <remarks>
        /// This method compares the current portal trace with the new list of portals, and teleports the target
        /// through only the portals that are different. This is useful for efficiently updating an object's position
        /// when the set of portals it should be teleported through changes.
        /// </remarks>
        public void TeleportDifference(Transform target, IEnumerable<Portal> newStartToEndPortals)
        {
            if (newStartToEndPortals == null)
            {
                BackTrackPortals(target, 0);
                return;
            }

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
                        _startToEnd.RemoveAt(i--);
                        // TODO: This portal has been destroyed, there is nothing much that can be done
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

        /// <summary>
        /// Undoes portal teleportations from the end of the trace to the specified new count.
        /// </summary>
        /// <param name="target">The transform to teleport.</param>
        /// <param name="newCount">The new count of portals to keep.</param>
        /// <remarks>
        /// This method teleports the target through the connected portals of the portals at the end of the trace,
        /// effectively undoing the teleportations in reverse order.
        /// </remarks>
        private void BackTrackPortals(Transform target, int newCount)
        {
            // Need to unteleport
            for (int i = _startToEnd.Count - 1; i >= newCount; i--)
            {
                Portal portal = _startToEnd[i];

                if (portal && portal.connected)
                    PortalPhysics.Teleport(target, portal.connected);
                else
                    _startToEnd.RemoveAt(i++);
            }

            // Just incase
            //while (sourceToTarget.Count > i)
            //    sourceToTarget.RemoveAt(sourceToTarget.Count - 1);
        }

        /// <summary>
        /// Extracts portals from an array of portal rays up to the specified count.
        /// </summary>
        /// <param name="portalRays">The array of portal rays to extract portals from.</param>
        /// <param name="count">The maximum number of portal rays to process.</param>
        /// <returns>An enumerable collection of portals from the portal rays.</returns>
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

        /// <summary>
        /// Returns an enumerator that iterates through the portals in the trace.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the portals in the trace.</returns>
        public IEnumerator<Portal> GetEnumerator() => GetPortals().GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the portals in the trace.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the portals in the trace.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetPortals().GetEnumerator();
    }
}
