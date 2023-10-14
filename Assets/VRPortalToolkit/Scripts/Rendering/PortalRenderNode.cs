using Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Data;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering
{
    // Note: matrices are not calculated if id = -1 (flagged for fake rendering)

    public sealed class PortalRenderNode : IEnumerable<PortalRenderNode>, IDisposable
    {
        private static ObjectPool<PortalRenderNode> _nodePool = new ObjectPool<PortalRenderNode>(() => new PortalRenderNode(false));
        private static ObjectPool<PortalRenderNode> _stereoNodePool = new ObjectPool<PortalRenderNode>(() => new PortalRenderNode(true));

        public static PortalRenderNode Get(Camera camera)
        {
            PortalRenderNode node = _nodePool.Get();
            node.cullingWindow = node.window = new ViewWindow(1f, 1f, 0f);
            node.camera = camera;

            node._portal = null;
            node._renderers.Clear();
            node._parent = null;
            node._depth = 0;
            node._root = node;
            node._children.Clear();
            node._isValid = false;
            node._root._isDirty = true;
            node._isValid = false;

            return node;
        }

        public static PortalRenderNode GetStereo(Camera camera, IPortalRenderer renderer, ViewWindow leftWindow, ViewWindow rightWindow)
        {
            PortalRenderNode node = _stereoNodePool.Get();
            node.cullingWindow = node.window = new ViewWindow(1f, 1f, 0f);
            node.camera = camera;

            node._portal = null;
            node._renderers.Clear();
            node._parent = null;
            node._depth = 0;
            node._root = node;
            node._children.Clear();
            node._isValid = false;
            node._root._isDirty = true;
            node._isValid = false;

            node._windows[0] = node.window;
            node._windows[1] = node.window;

            return node;
        }

        public PortalRenderNode GetOrAddChild(IPortalRenderer renderer)
        {
            if (_isStereo)
            {
                if (((1 << renderer.layer) & cullingMask) == 0) return null;

                bool leftValid = renderer.TryGetWindow(this, localToWorldMatrix.GetColumn(3), GetStereoViewMatrix(0), root.GetStereoProjectionMatrix(0), out ViewWindow leftWindow),
                    rightValid = renderer.TryGetWindow(this, localToWorldMatrix.GetColumn(3), GetStereoViewMatrix(1), root.GetStereoProjectionMatrix(1), out ViewWindow rightWindow);

                if ((!leftValid || !leftWindow.IsVisibleThrough(cullingWindow)) && (!rightValid || !rightWindow.IsVisibleThrough(cullingWindow)))
                    return null;

                return GetOrAddChild(renderer, leftWindow, rightWindow);
            }
            else
            {
                if (((1 << renderer.layer) & cullingMask) == 0) return null;

                if (renderer.TryGetWindow(this, localToWorldMatrix.GetColumn(3), worldToCameraMatrix, root.projectionMatrix, out ViewWindow window) && window.IsVisibleThrough(cullingWindow))
                    return GetOrAddChild(renderer, window);
            }

            return null;
        }

        public PortalRenderNode GetOrAddChild(IPortalRenderer renderer, ViewWindow window)
        {
            if (renderer == null || renderer.portal == null) return null;

            if (TryGetChild(renderer.portal, out PortalRenderNode child))
            {
                // Already exists
                child._renderers.Add(renderer);
                child.cullingWindow = child.window = ViewWindow.Combine(child.window, window);
            }
            else
            {
                // First one to be added
                child = isStereo ? _stereoNodePool.Get() : _nodePool.Get();
                child._children.Clear();

                child.cullingWindow = child.window = window;
                child._renderers.Add(renderer);
                child._portal = renderer?.portal;
                child.camera = camera;

                child._parent = this;
                _children.Add(child);

                child._depth = depth + 1;
                child._root = _root;
            }

            child._isValid = false;
            child._root._isDirty = true;
            child.cullingWindow.ClampInside(cullingWindow);

            return child;
        }

        public PortalRenderNode GetOrAddChild(IPortalRenderer renderer, ViewWindow leftWindow, ViewWindow rightWindow)
        {
            if (renderer == null || renderer.portal == null) return null;

            ViewWindow window = ViewWindow.Combine(leftWindow, rightWindow);

            PortalRenderNode child = GetOrAddChild(renderer, window);

            if (child.isStereo)
            {
                child._windows[0] = leftWindow;
                child._windows[1] = rightWindow;
            }

            return child;
        }

        private bool TryGetChild(IPortal portal, out PortalRenderNode child)
        {
            foreach (PortalRenderNode other in _children)
            {
                if (other.portal == portal)
                {
                    child = other;
                    return true;
                }
            }

            child = null;
            return false;
        }

        public static void Release(PortalRenderNode node)
        {
            node._parent = null;

            foreach (PortalRenderNode child in node._children)
                Release(child);

            node._children.Clear();

            if (node.isStereo)
                _stereoNodePool.Release(node);
            else
                _nodePool.Release(node);

            node._portal = null;
            node._renderers.Clear();
        }

        /// <summary>The portal to track.</summary>
        //private IPortalRenderer _renderer;_
        private readonly List<IPortalRenderer> _renderers = new List<IPortalRenderer>(1);
        public IEnumerable<IPortalRenderer> renderers => _renderers;
        public IPortalRenderer renderer => _renderers.Count > 0 ? _renderers[0] : null;

        private IPortal _portal;
        public IPortal portal => _portal;


        private PortalRenderNode _root;
        public PortalRenderNode root => _root;

        private bool _isDirty;

        // Recalculate count and indices if needed
        private void UpdateIndices()
        {
            if (_root != null && _root._isDirty)
            {
                _root._isDirty = false;

                foreach (PortalRenderNode node in _root.GetPostorderDepthFirst())
                {
                    node._totalChildCount = node.childCount;
                    node._validChildCount = 0;
                    node._totalValidChildCount = 0;

                    foreach (PortalRenderNode child in node.children)
                    {
                        if (child._isValid) node._validChildCount++;

                        node._totalChildCount += child.totalChildCount;
                        node._totalValidChildCount += child.totalValidChildCount;
                    }

                    node._totalValidChildCount += node._validChildCount;
                }

                int index = 0, validIndex = 0, invalidIndex = -1;

                foreach (PortalRenderNode child in _root)
                {
                    child._index = index++;

                    if (child.isValid || child == _root) child._validIndex = validIndex++;
                    else child._validIndex = invalidIndex--;
                }
            }
        }

        //private int _id;
        //public int id => _id;

        private int _index;
        public int index
        {
            get
            {
                UpdateIndices();
                return _index;
            }
        }

        private int _validIndex;
        public int validIndex
        {
            get
            {
                UpdateIndices();
                return _validIndex;
            }
        }

        private int _validChildCount;
        public int validChildCount
        {
            get
            {
                UpdateIndices();
                return _validChildCount;
            }
        }

        private int _totalValidChildCount;
        public int totalValidChildCount
        {
            get
            {
                UpdateIndices();
                return _totalValidChildCount;
            }
        }

        public int childCount => _children.Count;

        private int _totalChildCount;
        public int totalChildCount
        {
            get
            {
                UpdateIndices();
                return _totalChildCount;
            }
        }

        public int totalInvalidChildCount => totalChildCount - totalValidChildCount;
        public int invalidChildCount => childCount - validChildCount;

        private int _depth;
        public int depth => _depth;

        public int cullingMask;

        private bool _isStereo;
        public bool isStereo => _isStereo;

        private bool _isValid;
        public bool isValid
        {
            get => _isValid;
            set
            {
                if (_isValid != value)
                {
                    if (_root != null) _root._isDirty = true;
                    _isValid = value;
                }
            }
        }

        public Camera camera;

        public Matrix4x4 connectedTeleportMatrix;

        public Matrix4x4 teleportMatrix;

        public Matrix4x4 localToWorldMatrix;

        public Matrix4x4 worldToCameraMatrix;

        public Matrix4x4 projectionMatrix;

        private readonly Matrix4x4[] _viewMatrices;

        private readonly Matrix4x4[] _projectionMatrices;

        /// <summary>The viewport window this portal can be seen through.</summary>
        public ViewWindow cullingWindow;

        public ViewWindow window;

        private readonly ViewWindow[] _windows;

        private PortalRenderNode _parent;
        public PortalRenderNode parent => _parent;

        private readonly List<PortalRenderNode> _children = new List<PortalRenderNode>(2);

        public IEnumerable<PortalRenderNode> children
        {
            get
            {
                foreach (PortalRenderNode child in _children)
                    yield return child;
            }
        }

        private PortalRenderNode(bool isStereo)
        {
            _isStereo = isStereo;

            if (isStereo)
            {
                _viewMatrices = new Matrix4x4[2];
                _projectionMatrices = new Matrix4x4[2];
                _windows = new ViewWindow[2];
            }
        }

        public Matrix4x4 GetStereoViewMatrix(int index)
        {
            if (index < 0 || index > 1) throw new IndexOutOfRangeException();

            if (_viewMatrices != null) return _viewMatrices[index];

            return worldToCameraMatrix;
        }

        public void SetStereoViewMatrix(int index, Matrix4x4 view)
        {
            if (index < 0 || index > 1) throw new IndexOutOfRangeException();

            if (_viewMatrices != null) _viewMatrices[index] = view;
        }

        public Matrix4x4 GetStereoProjectionMatrix(int index)
        {
            if (index < 0 || index > 1) throw new IndexOutOfRangeException();

            if (_projectionMatrices != null) return _projectionMatrices[index];

            return projectionMatrix;
        }

        public void SetStereoProjectionMatrix(int index, Matrix4x4 proj)
        {
            if (index < 0 || index > 1) throw new IndexOutOfRangeException();

            if (_projectionMatrices != null) _projectionMatrices[index] = proj;
        }

        public void SetStereoViewAndProjection(Matrix4x4 leftView, Matrix4x4 leftProj, Matrix4x4 rightView, Matrix4x4 rightProj)
        {
            if (_viewMatrices != null)
            {
                _viewMatrices[0] = leftView;
                _viewMatrices[1] = rightView;
            }

            if (_projectionMatrices != null)
            {
                _projectionMatrices[0] = leftProj;
                _projectionMatrices[1] = rightProj;
            }
        }

        public ViewWindow GetStereoWindow(int index)
        {
            if (_windows != null) return _windows[index];

            return window;
        }

        public override string ToString()
        {
            string parentToString = "NULL";
            if (parent != null) parentToString = parent.index.ToString();

            return $"PortalRenderNode(index: {index}, depth: {depth}, isValid: {isValid}, validIndex: {validIndex}, isStero: {isStereo}, parentIndex: {parentToString})";
        }

        public void SetStereoWindow(int index, ViewWindow window)
        {
            if (_windows != null)

                _windows[index] = window;
        }

        public void SetStereoWindows(ViewWindow leftWindow, ViewWindow rightWindow)
        {
            if (_windows != null)
            {
                _windows[0] = leftWindow;
                _windows[1] = rightWindow;
            }
        }

        public void Dispose()
        {
            Release(this);
        }

        public PortalRenderNode GetChild(int index)
        {
            if (_children != null) return _children[index];

            return null;
        }

        public int IndexOfChild(PortalRenderNode node)
        {
            if (_children != null) return _children.IndexOf(node);

            return -1;
        }

        public IEnumerable<PortalRenderNode> GetPostorderDepthFirst()
        {
            foreach (PortalRenderNode child in children)
                foreach (PortalRenderNode childTree in child.GetPostorderDepthFirst())
                    yield return childTree;

            yield return this;
        }
        private IEnumerable<PortalRenderNode> GetPreorderDepthFirst()
        {
            yield return this;

            foreach (PortalRenderNode child in children)
                foreach (PortalRenderNode childTree in child.GetPreorderDepthFirst())
                    yield return childTree;

        }

        public IEnumerable<PortalRenderNode> GetBreadthFirst()
        {
            Queue<PortalRenderNode> queue = new Queue<PortalRenderNode>(totalChildCount + 1);
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                yield return queue.Dequeue();

                foreach (PortalRenderNode child in _children)
                    yield return child;
            }
        }


        public IEnumerator<PortalRenderNode> GetEnumerator()
        {
            yield return this;

            foreach (PortalRenderNode child in children)
                foreach (PortalRenderNode childTree in child)
                    yield return childTree;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void SortChildren(IComparer<PortalRenderNode> comparer, bool recursive = false)
        {
            _children.Sort(comparer);

            if (recursive)
            {
                foreach (PortalRenderNode child in _children)
                    child.SortChildren(comparer, recursive);
            }

            _root._isDirty = true;
        }

        // This node will be used for rendering, so need to calculate matrices
        public void ComputeMaskAndMatrices()
        {
            if (_parent != null && portal != null)
            {
                if (_portal.usesLayers)
                    cullingMask = _portal.ModifyLayerMask(_parent.cullingMask);
                else
                    cullingMask = _parent.cullingMask;

                if (_portal.usesTeleport)
                {
                    Matrix4x4 teleport = _portal.teleportMatrix, connectedTeleport = _portal.connected.teleportMatrix;

                    teleportMatrix = teleport * _parent.teleportMatrix;
                    connectedTeleportMatrix = connectedTeleport * _parent.connectedTeleportMatrix;
                    localToWorldMatrix = teleport * _parent.localToWorldMatrix;
                    worldToCameraMatrix = _parent.worldToCameraMatrix * connectedTeleport;

                    if (_isStereo)
                    {
                        _viewMatrices[0] = _parent._viewMatrices[0] * connectedTeleport;
                        _viewMatrices[1] = _parent._viewMatrices[1] * connectedTeleport;
                    }
                }
                else
                {
                    teleportMatrix = _parent.teleportMatrix;
                    connectedTeleportMatrix = _parent.connectedTeleportMatrix;
                    localToWorldMatrix = _parent.localToWorldMatrix;
                    worldToCameraMatrix = _parent.worldToCameraMatrix;

                    if (_isStereo)
                    {
                        _viewMatrices[0] = _parent._viewMatrices[0];
                        _viewMatrices[1] = _parent._viewMatrices[1];
                    }
                }

                // Set projection matric to default
                projectionMatrix = _root.projectionMatrix;

                if (_isStereo)
                {
                    _projectionMatrices[0] = _root._projectionMatrices[0];
                    _projectionMatrices[1] = _root._projectionMatrices[1];
                }

                // Clip the projection matrix by the plane
                foreach (IPortalRenderer renderer in _renderers)
                {
                    if (renderer.TryGetClippingPlane(_parent, out Vector3 clippingCentre, out Vector3 clippingNormal))
                    {
                        projectionMatrix = CameraUtility.CalculateObliqueMatrix(_parent.worldToCameraMatrix, projectionMatrix, clippingCentre, clippingNormal);

                        if (_isStereo)
                        {
                            _projectionMatrices[0] = CameraUtility.CalculateObliqueMatrix(_parent._viewMatrices[0], _projectionMatrices[0], clippingCentre, clippingNormal);
                            _projectionMatrices[1] = CameraUtility.CalculateObliqueMatrix(_parent._viewMatrices[1], _projectionMatrices[1], clippingCentre, clippingNormal);
                        }

                        break;
                    }
                }

                // Scissor the projection matrix
                Rect rect = cullingWindow.GetRect();

                projectionMatrix = CameraUtility.CalculateScissorMatrix(projectionMatrix, rect);

                if (_isStereo)
                {
                    _projectionMatrices[0] = CameraUtility.CalculateScissorMatrix(_projectionMatrices[0], rect);
                    _projectionMatrices[1] = CameraUtility.CalculateScissorMatrix(_projectionMatrices[1], rect);
                }
            }
        }
    }
}