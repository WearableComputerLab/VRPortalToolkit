using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Rendering;
using VRPortalToolkit.Rendering.Universal;

namespace VRPortalToolkit
{
    public static class PortalRenderStack
    {
        private static List<PortalRenderNode> _portalNodes = new List<PortalRenderNode>();

        /// <summary>
        /// Clears all nodes from the stack.
        /// </summary>
        public static void Clear()
        {
            _portalNodes.Clear();
        }

        /// <summary>
        /// Pushes a node onto the stack.
        /// </summary>
        /// <param name="node">The node to push.</param>
        public static void Push(PortalRenderNode node)
        {
            if (node != null) _portalNodes.Add(node);
        }

        /// <summary>
        /// Pops the top node from the stack.
        /// </summary>
        /// <returns>The removed node, or null if the stack is empty or contains only one node.</returns>
        public static PortalRenderNode Pop()
        {
            if (_portalNodes.Count > 1)
            {
                var removed = _portalNodes[_portalNodes.Count - 1];
                _portalNodes.RemoveAt(_portalNodes.Count - 1);
                return removed;
            }

            return null;
        }

        /// <summary>
        /// Gets the parent node of the current node.
        /// </summary>
        public static PortalRenderNode Parent
        {
            get
            {
                if (_portalNodes.Count > 1)
                    return _portalNodes[_portalNodes.Count - 2];

                return null;
            }
        }

        /// <summary>
        /// Gets the current (top) node on the stack.
        /// </summary>
        public static PortalRenderNode Current
        {
            get
            {
                if (_portalNodes.Count > 0)
                    return _portalNodes[_portalNodes.Count - 1];

                return null;
            }
        }
    }
}
