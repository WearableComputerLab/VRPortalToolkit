using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering
{
    /// <summary>
    /// Provides algorithms for building portal rendering trees using different traversal methods.
    /// </summary>
    public class PortalAlgorithms
    {
        /// <summary>
        /// Node used in the predictive portal traversal algorithm.
        /// </summary>
        public struct PredictiveNode : IComparable<PredictiveNode>
        {
            private readonly float significance;
            private readonly bool hasPattern;
            private readonly bool focused;

            /// <summary>
            /// The render node associated with this predictive node.
            /// </summary>
            public readonly PortalRenderNode renderNode;

            /// <summary>
            /// Creates a new predictive node with the specified render node.
            /// </summary>
            /// <param name="renderNode">The render node to associate with this predictive node.</param>
            /// <param name="focused">Whether this node is in the camera's focus area.</param>
            public PredictiveNode(PortalRenderNode renderNode, bool focused = false)
            {
                this.renderNode = renderNode;
                this.focused = focused;

                /*significance = (renderNode.cullingWindow.xMax - renderNode.cullingWindow.xMin)
                    * (renderNode.cullingWindow.yMax - renderNode.cullingWindow.yMin);

                if (renderNode.cullingWindow.zMin != 0)
                    significance /= renderNode.cullingWindow.zMin;*/

                significance = renderNode.cullingWindow.zMin;

                hasPattern = false;

                if (renderNode.depth >= 2)
                {
                    PortalRenderNode parent = renderNode.parent;

                    while (parent != null)
                    {
                        if (parent.portal == renderNode.portal) // TODO: Different culling will make this different
                        {
                            hasPattern = true;
                            break;
                        }

                        parent = parent.parent;
                    }
                }
            }

            /// <summary>
            /// Compares this node with another for sorting purposes.
            /// </summary>
            /// <param name="other">The other node to compare with.</param>
            /// <returns>A value indicating the relative ordering.</returns>
            public int CompareTo(PredictiveNode other)
            {
                if (hasPattern != other.hasPattern)
                    return hasPattern ? -1 : 1;

                //if (renderNode.depth != other.renderNode.depth)
                //    return renderNode.depth.CompareTo(other.renderNode.depth);

                if (focused != other.focused)
                    return focused ? 1 : -1;

                //return renderNode.window.zMin.CompareTo(other.renderNode.window.zMin);
                return other.significance.CompareTo(significance);
            }
        }

        /// <summary>
        /// Node used in the breadth-first portal traversal algorithm.
        /// </summary>
        public struct BreadthFirstNode : IComparable<BreadthFirstNode>
        {
            /// <summary>
            /// The render node associated with this breadth-first node.
            /// </summary>
            public readonly PortalRenderNode renderNode;

            /// <summary>
            /// Creates a new breadth-first node with the specified render node.
            /// </summary>
            /// <param name="renderNode">The render node to associate with this breadth-first node.</param>
            /// <param name="focused">Whether this node is in the camera's focus area (unused).</param>
            public BreadthFirstNode(PortalRenderNode renderNode, bool focused = false)
            {
                this.renderNode = renderNode;
            }

            /// <summary>
            /// Compares this node with another for sorting purposes.
            /// </summary>
            /// <param name="other">The other node to compare with.</param>
            /// <returns>A value indicating the relative ordering.</returns>
            public int CompareTo(BreadthFirstNode other)
            {
                return other.renderNode.depth.CompareTo(renderNode.depth);
            }
        }

        private static List<PortalRenderNode> _requiredRenderNodes = new List<PortalRenderNode>();
        private static List<PredictiveNode> _predictiveNodes = new List<PredictiveNode>();
        private static List<BreadthFirstNode> _breadthFirstNodes = new List<BreadthFirstNode>();

        /// <summary>
        /// Gets a portal rendering tree using the default breadth-first algorithm.
        /// </summary>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="minDepth">The minimum recursion depth to render.</param>
        /// <param name="maxDepth">The maximum recursion depth to render.</param>
        /// <param name="maxRenders">The maximum number of portals to render.</param>
        /// <param name="portals">The available portals to traverse.</param>
        /// <returns>The root node of the portal rendering tree.</returns>
        public static PortalRenderNode GetTree(Camera camera, int minDepth, int maxDepth, int maxRenders, IEnumerable<IPortalRenderer> portals)
            => GetTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, camera.cullingMask, minDepth, maxDepth, maxRenders, portals);

        /// <summary>
        /// Gets a portal rendering tree using the breadth-first algorithm with custom matrices.
        /// </summary>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="localToWorld">The local to world matrix.</param>
        /// <param name="view">The view matrix.</param>
        /// <param name="proj">The projection matrix.</param>
        /// <param name="layerMask">The layer mask for culling.</param>
        /// <param name="minDepth">The minimum recursion depth to render.</param>
        /// <param name="maxDepth">The maximum recursion depth to render.</param>
        /// <param name="maxRenders">The maximum number of portals to render.</param>
        /// <param name="visiblePortals">The available portals to traverse.</param>
        /// <returns>The root node of the portal rendering tree.</returns>
        public static PortalRenderNode GetTree(Camera camera, Matrix4x4 localToWorld, Matrix4x4 view, Matrix4x4 proj, int layerMask, int minDepth, int maxDepth, int maxRenders, IEnumerable<IPortalRenderer> visiblePortals)
        {
            PortalRenderNode root = GetRoot(camera, localToWorld, view, proj, layerMask);

            GetTree(root, visiblePortals, minDepth, maxDepth, maxRenders);

            return root;
        }

        /// <summary>
        /// Gets a portal rendering tree for stereo rendering using the breadth-first algorithm.
        /// </summary>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="minDepth">The minimum recursion depth to render.</param>
        /// <param name="maxDepth">The maximum recursion depth to render.</param>
        /// <param name="maxRenders">The maximum number of portals to render.</param>
        /// <param name="portals">The available portals to traverse.</param>
        /// <returns>The root node of the portal rendering tree.</returns>
        public static PortalRenderNode GetStereoTree(Camera camera, int minDepth, int maxDepth, int maxRenders, IEnumerable<IPortalRenderer> portals)
            => GetStereoTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, camera.cullingMask, camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left),
                camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), minDepth, maxDepth, maxRenders, portals);

        /// <summary>
        /// Gets a portal rendering tree for stereo rendering using the breadth-first algorithm with custom matrices.
        /// </summary>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="localToWorld">The local to world matrix.</param>
        /// <param name="cullView">The view matrix for culling.</param>
        /// <param name="cullProj">The projection matrix for culling.</param>
        /// <param name="layerMask">The layer mask for culling.</param>
        /// <param name="leftView">The view matrix for the left eye.</param>
        /// <param name="leftProj">The projection matrix for the left eye.</param>
        /// <param name="rightView">The view matrix for the right eye.</param>
        /// <param name="rightProj">The projection matrix for the right eye.</param>
        /// <param name="minDepth">The minimum recursion depth to render.</param>
        /// <param name="maxDepth">The maximum recursion depth to render.</param>
        /// <param name="maxRenders">The maximum number of portals to render.</param>
        /// <param name="visiblePortals">The available portals to traverse.</param>
        /// <returns>The root node of the portal rendering tree.</returns>
        public static PortalRenderNode GetStereoTree(Camera camera, Matrix4x4 localToWorld, Matrix4x4 cullView, Matrix4x4 cullProj, int layerMask, Matrix4x4 leftView, Matrix4x4 leftProj, Matrix4x4 rightView, Matrix4x4 rightProj, int minDepth, int maxDepth, int maxRenders, IEnumerable<IPortalRenderer> visiblePortals)
        {
            PortalRenderNode root = GetStereoRoot(camera, localToWorld, cullView, cullProj, layerMask, leftView, leftProj, rightView, rightProj);

            GetTree(root, visiblePortals, minDepth, maxDepth, maxRenders);

            return root;
        }

        private static void GetTree(PortalRenderNode root, IEnumerable<IPortalRenderer> visiblePortals, int minDepth, int maxDepth, int maxRenders)
        {
            _breadthFirstNodes.Clear();

            int count = 0;

            CreateChildrenNodes(root, visiblePortals);

            if (minDepth > 0)
                AddChildrenToRequiredNodes(root);
            else
                AddChildrenToBreadthFirstNodes(root);

            while (_requiredRenderNodes.Count > 0)
            {
                PortalRenderNode next = _requiredRenderNodes[_requiredRenderNodes.Count - 1];

                // Nodes now need to be checked by max renders and max depth
                if (next.depth > minDepth) break;

                _requiredRenderNodes.RemoveAt(_requiredRenderNodes.Count - 1);

                next.ComputeMaskAndMatrices();
                next.isValid = true;
                count++;

                CreateChildrenNodes(next, PortalRendering.GetVisiblePortalRenderers(next.portal));

                if (next.depth < minDepth)
                    AddChildrenToRequiredNodes(next);
                else if (next.depth < maxDepth && count < maxRenders)
                    AddChildrenToBreadthFirstNodes(next);
            }

            // Anything after this point is an extra, but not required
            while (_breadthFirstNodes.Count > 0 && count < maxRenders)
            {
                BreadthFirstNode next = _breadthFirstNodes[_breadthFirstNodes.Count - 1];
                _breadthFirstNodes.RemoveAt(_breadthFirstNodes.Count - 1);

                next.renderNode.ComputeMaskAndMatrices();
                next.renderNode.isValid = true;
                count++;

                CreateChildrenNodes(next.renderNode, PortalRendering.GetVisiblePortalRenderers(next.renderNode.portal));

                if (next.renderNode.depth < maxDepth && count < maxRenders)
                    AddChildrenToBreadthFirstNodes(next.renderNode);
            }
        }

        /// <summary>
        /// Gets a portal rendering tree using the predictive algorithm.
        /// </summary>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="maxDepth">The maximum recursion depth to render.</param>
        /// <param name="maxRenders">The maximum number of portals to render.</param>
        /// <param name="portals">The available portals to traverse.</param>
        /// <param name="focus">Optional focus point in viewport space to prioritize.</param>
        /// <returns>The root node of the portal rendering tree.</returns>
        public static PortalRenderNode GetSmartTree(Camera camera, int maxDepth, int maxRenders, IEnumerable<IPortalRenderer> portals, Vector2? focus = null)
            => GetPredictiveTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, camera.cullingMask, 0, maxDepth, maxRenders, portals, focus);

        /// <summary>
        /// Gets a portal rendering tree using the predictive algorithm with custom matrices.
        /// </summary>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="localToWorld">The local to world matrix.</param>
        /// <param name="view">The view matrix.</param>
        /// <param name="proj">The projection matrix.</param>
        /// <param name="layerMask">The layer mask for culling.</param>
        /// <param name="minDepth">The minimum recursion depth to render.</param>
        /// <param name="maxDepth">The maximum recursion depth to render.</param>
        /// <param name="maxRenders">The maximum number of portals to render.</param>
        /// <param name="visiblePortals">The available portals to traverse.</param>
        /// <param name="focus">Optional focus point in viewport space to prioritize.</param>
        /// <returns>The root node of the portal rendering tree.</returns>
        public static PortalRenderNode GetPredictiveTree(Camera camera, Matrix4x4 localToWorld, Matrix4x4 view, Matrix4x4 proj, int layerMask, int minDepth, int maxDepth, int maxRenders, IEnumerable<IPortalRenderer> visiblePortals, Vector2? focus = null)
        {
            PortalRenderNode root = GetRoot(camera, localToWorld, view, proj, layerMask);

            GetPredictiveTree(root, visiblePortals, minDepth, maxDepth, maxRenders, focus);

            return root;
        }

        /// <summary>
        /// Gets a portal rendering tree for stereo rendering using the predictive algorithm.
        /// </summary>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="maxDepth">The maximum recursion depth to render.</param>
        /// <param name="maxRenders">The maximum number of portals to render.</param>
        /// <param name="portals">The available portals to traverse.</param>
        /// <param name="focus">Optional focus point in viewport space to prioritize.</param>
        /// <returns>The root node of the portal rendering tree.</returns>
        public static PortalRenderNode GetSmartStereoTree(Camera camera, int maxDepth, int maxRenders, IEnumerable<IPortalRenderer> portals, Vector2? focus = null)
            => GetPredictiveStereoTree(camera, camera.transform.localToWorldMatrix, camera.worldToCameraMatrix, camera.projectionMatrix, camera.cullingMask, camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left),
                camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right), camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), 0, maxDepth, maxRenders, portals, focus);

        /// <summary>
        /// Gets a portal rendering tree for stereo rendering using the predictive algorithm with custom matrices.
        /// </summary>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="localToWorld">The local to world matrix.</param>
        /// <param name="cullView">The view matrix for culling.</param>
        /// <param name="cullProj">The projection matrix for culling.</param>
        /// <param name="layerMask">The layer mask for culling.</param>
        /// <param name="leftView">The view matrix for the left eye.</param>
        /// <param name="leftProj">The projection matrix for the left eye.</param>
        /// <param name="rightView">The view matrix for the right eye.</param>
        /// <param name="rightProj">The projection matrix for the right eye.</param>
        /// <param name="minDepth">The minimum recursion depth to render.</param>
        /// <param name="maxDepth">The maximum recursion depth to render.</param>
        /// <param name="maxRenders">The maximum number of portals to render.</param>
        /// <param name="visiblePortals">The available portals to traverse.</param>
        /// <param name="focus">Optional focus point in viewport space to prioritize.</param>
        /// <returns>The root node of the portal rendering tree.</returns>
        public static PortalRenderNode GetPredictiveStereoTree(Camera camera, Matrix4x4 localToWorld, Matrix4x4 cullView, Matrix4x4 cullProj, int layerMask, Matrix4x4 leftView, Matrix4x4 leftProj, Matrix4x4 rightView, Matrix4x4 rightProj, int minDepth, int maxDepth, int maxRenders, IEnumerable<IPortalRenderer> visiblePortals, Vector2? focus = null)
        {
            PortalRenderNode root = GetStereoRoot(camera, localToWorld, cullView, cullProj, layerMask, leftView, leftProj, rightView, rightProj);

            GetPredictiveTree(root, visiblePortals, minDepth, maxDepth, maxRenders, focus);

            return root;
        }

        private static void GetPredictiveTree(PortalRenderNode root, IEnumerable<IPortalRenderer> visiblePortals, int minDepth, int maxDepth, int maxRenders, Vector2? focus = null)
        {
            _predictiveNodes.Clear();

            int count = 0;

            CreateChildrenNodes(root, visiblePortals);

            if (minDepth > 0)
                AddChildrenToRequiredNodes(root);
            else
                AddChildrenToPredictiveNodes(root, focus);

            while (_requiredRenderNodes.Count > 0)
            {
                PortalRenderNode next = _requiredRenderNodes[_requiredRenderNodes.Count - 1];

                // Nodes now need to be checked by max renders and max depth
                if (next.depth > minDepth) break;

                _requiredRenderNodes.RemoveAt(_requiredRenderNodes.Count - 1);

                next.ComputeMaskAndMatrices();
                next.isValid = true;
                count++;

                //if (next.depth == 1)
                //    CreateChildrenNodes(next, PortalRendering.GetAllPortalRenderers());
                //else
                CreateChildrenNodes(next, PortalRendering.GetVisiblePortalRenderers(next.portal));

                //if (next.depth < minDepth)
                //    AddChildrenToRequiredNodes(next);
                //else if (next.depth < maxDepth && count < maxRenders)
                AddChildrenToPredictiveNodes(next, focus);
            }

            // Anything after this point is an extra, but not required
            while (_predictiveNodes.Count > 0 && count < maxRenders)
            {
                PredictiveNode next = _predictiveNodes[_predictiveNodes.Count - 1];
                _predictiveNodes.RemoveAt(_predictiveNodes.Count - 1);

                next.renderNode.ComputeMaskAndMatrices();
                next.renderNode.isValid = true;
                count++;

                //if (next.renderNode.depth == 1)
                //    CreateChildrenNodes(next.renderNode, PortalRendering.GetAllPortalRenderers());
                //else
                CreateChildrenNodes(next.renderNode, PortalRendering.GetVisiblePortalRenderers(next.renderNode.portal));

                if (next.renderNode.depth < maxDepth && count < maxRenders)
                    AddChildrenToPredictiveNodes(next.renderNode, focus);
            }
        }

        private static PortalRenderNode GetStereoRoot(Camera camera, Matrix4x4 localToWorld, Matrix4x4 cullView, Matrix4x4 cullProj, int layerMask, Matrix4x4 leftView, Matrix4x4 leftProj, Matrix4x4 rightView, Matrix4x4 rightProj)
        {
            PortalRenderNode root = PortalRenderNode.GetStereo(camera, null, new ViewWindow(1f, 1f, 0f), new ViewWindow(1f, 1f, 0f));
            root.isValid = true;

            root.connectedTeleportMatrix = root.teleportMatrix = Matrix4x4.identity;
            root.localToWorldMatrix = localToWorld;
            root.worldToCameraMatrix = cullView;
            root.projectionMatrix = cullProj;
            root.cullingMask = layerMask;

            // Set Stereo
            root.SetStereoViewAndProjection(leftView, leftProj, rightView, rightProj);
            return root;
        }

        private static PortalRenderNode GetRoot(Camera camera, Matrix4x4 localToWorld, Matrix4x4 view, Matrix4x4 proj, int layerMask)
        {
            PortalRenderNode root = PortalRenderNode.Get(camera);
            root.isValid = true;

            root.connectedTeleportMatrix = root.teleportMatrix = Matrix4x4.identity;
            root.localToWorldMatrix = localToWorld;
            root.worldToCameraMatrix = view;
            root.projectionMatrix = proj;
            root.cullingMask = layerMask;
            return root;
        }

        private static void CreateChildrenNodes(PortalRenderNode parent, IEnumerable<IPortalRenderer> visiblePortals)
        {
            foreach (IPortalRenderer renderer in visiblePortals)
                parent.GetOrAddChild(renderer);
        }

        private static void AddChildrenToBreadthFirstNodes(PortalRenderNode parent)
        {
            foreach (PortalRenderNode child in parent.children)
            {
                BreadthFirstNode potentialNode = new BreadthFirstNode(child);

                AddSorted(_breadthFirstNodes, potentialNode);
            }
        }

        private static void AddChildrenToRequiredNodes(PortalRenderNode parent)
        {
            foreach (PortalRenderNode child in parent.children)
                _requiredRenderNodes.Add(child);
        }

        private static void AddChildrenToPredictiveNodes(PortalRenderNode parent, Vector2? focus)
        {
            foreach (PortalRenderNode child in parent.children)
            {
                PredictiveNode potentialNode = new PredictiveNode(child, focus != null ? child.cullingWindow.Contains(focus.Value) : false);

                AddSorted(_predictiveNodes, potentialNode);
            }
        }

        private static void AddSorted<T>(List<T> list, T item) where T : IComparable<T>
        {
            if (list.Count == 0)
                list.Add(item);
            else if (item.CompareTo(list[list.Count - 1]) >= 0)
                list.Add(item);
            else if (item.CompareTo(list[0]) <= 0)
                list.Insert(0, item);
            else
            {
                int index = list.BinarySearch(item);

                if (index < 0) index = ~index;

                list.Insert(index, item);
            }
        }
    }
}
