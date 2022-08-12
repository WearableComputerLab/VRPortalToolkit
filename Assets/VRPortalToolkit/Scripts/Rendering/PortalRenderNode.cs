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

        public static PortalRenderNode Get(PortalRenderer renderer, ViewWindow window, PortalRenderNode parent = null)
        {
            PortalRenderNode child = _nodePool.Get();
            child.cullingWindow = child.window = window;
            child._renderer = renderer;

            Update(parent, child);

            return child;
        }

        public static PortalRenderNode GetStereo(PortalRenderer renderer, ViewWindow leftWindow, ViewWindow rightWindow, PortalRenderNode parent = null)
        {
            PortalRenderNode child = _stereoNodePool.Get();
            child.cullingWindow = child.window = ViewWindow.Combine(leftWindow, rightWindow);
            child._renderer = renderer;

            child._windows[0] = leftWindow;
            child._windows[1] = rightWindow;

            Update(parent, child);

            return child;
        }

        private static void Update(PortalRenderNode parent, PortalRenderNode child)
        {
            child._children.Clear();

            if (parent == null)
            {
                child._parent = null;
                child._depth = 0;
                child._root = child;
            }
            else
            {
                child._parent = parent;
                parent._children.Add(child);

                child._depth = parent.depth + 1;
                child._root = parent._root;
                child.cullingWindow.ClampInside(parent.cullingWindow);
            }

            child._isValid = false;

            child._root._isDirty = true;
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
        }

        /// <summary>The portal to track.</summary>
        private PortalRenderer _renderer;
        public PortalRenderer renderer => _renderer;

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

        public Matrix4x4 connectedTeleportMatrix;

        public Matrix4x4 teleportMatrix;

        public Matrix4x4 localToWorldMatrix;

        public Matrix4x4 worldToCameraMatrix;

        public Matrix4x4 projectionMatrix;

        private Matrix4x4[] _viewMatrices;

        private Matrix4x4[] _projectionMatrices;

        /// <summary>The viewport window this portal can be seen through.</summary>
        public ViewWindow cullingWindow;

        public ViewWindow window;

        private ViewWindow[] _windows;

        private PortalRenderNode _parent;
        public PortalRenderNode parent => _parent; 

        private List<PortalRenderNode> _children = new List<PortalRenderNode>(2);

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

        /*public void SetStereoWindow(int index, ViewWindow window)
        {
            if (_windows == null) _windows = new ViewWindow[2];

            _windows[index] = window;
        }

        public void SetStereoWindows(ViewWindow leftWindow, ViewWindow rightWindow)
        {
            if (_windows == null) _windows = new ViewWindow[2];
            {
                _windows[0] = leftWindow;
                _windows[1] = rightWindow;
            }
        }*/

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

        // This node will be used for rendering, so need to calculate matrices
        public void ComputeMaskAndMatrices()
        {
            if (_parent != null && renderer != null)
            {
                if (_renderer.portal.usesLayers)
                    cullingMask = _renderer.portal.ModifyLayerMask(_parent.cullingMask);
                else
                    cullingMask = _parent.cullingMask;

                if (_renderer.portal.usesTeleport)
                {
                    Matrix4x4 teleport = _renderer.portal.teleportMatrix, connectedTeleport = _renderer.portal.connectedPortal.teleportMatrix;

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
                if (_renderer.TryGetClippingPlane(null, _parent, out Vector3 clippingCentre, out Vector3 clippingNormal))
                {
                    projectionMatrix = CameraUtility.CalculateObliqueMatrix(_parent.worldToCameraMatrix, projectionMatrix,
                        _parent.localToWorldMatrix.GetColumn(3), clippingCentre, clippingNormal);

                    if (_isStereo)
                    {
                        _projectionMatrices[0] = CameraUtility.CalculateObliqueMatrix(_parent._viewMatrices[0], _projectionMatrices[0],
                            _parent.localToWorldMatrix.GetColumn(3), clippingCentre, clippingNormal);
                        _projectionMatrices[1] = CameraUtility.CalculateObliqueMatrix(_parent._viewMatrices[1], _projectionMatrices[1],
                            _parent.localToWorldMatrix.GetColumn(3), clippingCentre, clippingNormal);
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