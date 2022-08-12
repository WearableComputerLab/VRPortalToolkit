using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Data
{
    public class TreeNode<T> : IEnumerable<T> where T : TreeNode<T>
    {
        private T _parent;
        public T parent
        {
            get => _parent;
            set
            {
                if (_parent == value) return;

                if (_parent != null && _parent._children != null)
                    _parent._children.Remove((T)this);

                _parent = value;

                if (_parent != null)
                {
                    if (_parent._children == null)
                        _parent._children = new List<T>(1) { (T)this };
                    else
                        _parent._children.Add((T)this);
                }
            }
        }

        private List<T> _children;

        public IEnumerable<T> children
        {
            set
            {
                if (_children != null)
                {
                    foreach (T child in _children)
                        child._parent = null;

                    _children.Clear();
                }
                else 
                    _children = new List<T>();

                if (value != null)
                    foreach (T child in value)
                        _children.Add(child);
            }
            get
            {
                if (_children != null)
                    foreach (T child in _children)
                        yield return child;
            }
        }

        public int childrenCount => _children != null ? _children.Count : 0;

        public void AddChild(T node)
        {
            if (node != null)
                node.parent = (T)this;
        }

        public void RemoveChild(T node)
        {
            if (node._parent == this)
                node.parent = null;
        }

        public void RemoveChildAt(int index)
        {
            T child = _children[index];
            child.parent = null;
            _children.RemoveAt(index);
        }

        public T GetChildAt(int index)
        {
            if (_children != null) return _children[index];

            return null;
        }

        public int IndexOfChild(T node)
        {
            if (_children != null) return _children.IndexOf(node);

            return -1;
        }

        public IEnumerable<T> GetDepthFirst()
        {
            foreach (T child in children)
                foreach (T childTree in child.GetDepthFirst())
                    yield return childTree;

            yield return (T)this;
        }

        public IEnumerable<T> GetBreadthFirst()
        {
            foreach (T node in this) yield return node;
        }

        /// Depth First
        public IEnumerator<T> GetEnumerator()
        {
            yield return (T)this;
            
            foreach (T child in children)
                foreach (T childTree in child)
                    yield return childTree;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}