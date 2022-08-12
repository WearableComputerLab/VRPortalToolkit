using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc
{
    public class ActionRemapper<TSource> where TSource : class
    {
        private static ObjectPool<Node> _nodePool = new ObjectPool<Node>(() => new Node());

        private class Node//<TSource>
        {
            public TSource source;

            public ActionRemapper<TSource> map;

            public virtual void OnInvoke()
            {
                if (map != null) map.onInvoke?.Invoke(source);
            }
        }

        public Action<TSource> onInvoke { get; set; }

        public Action<Action, TSource> addListener { get; set; }
        public Action<Action, TSource> removeListener { get; set; }

        private bool _isListening = false;
        public bool isListening => _isListening;
        private List<Node> _nodes = new List<Node>();

        public IReadOnlyList<TSource> sources => new ReadonlyRemappedList<Node, TSource>(_nodes, (i) => i.source, 0, _nodes.Count);

        public void StartListening()
        {
            if (!_isListening)
            {
                _isListening = true;

                foreach (Node node in _nodes)
                    addListener?.Invoke(node.OnInvoke, node.source);
            }
        }

        public void StopListening()
        {
            if (_isListening)
            {
                _isListening = false;

                foreach (Node node in _nodes)
                    removeListener?.Invoke(node.OnInvoke, node.source);
            }
        }

        public void AddSource(TSource source)
        {
            Node node = _nodePool.Get();
            node.source = source;
            node.map = this;
            _nodes.Add(node);

            if (_isListening) addListener?.Invoke(node.OnInvoke, node.source);
        }

        public bool RemoveSource(TSource source)
        {
            Node node;

            for (int i = 0; i < _nodes.Count; i++)
            {
                node = _nodes[i];

                if (node == source)
                {
                    if (_isListening) removeListener?.Invoke(node.OnInvoke, node.source);

                    _nodePool.Release(node);
                    node.source = null;
                    node.map = null;
                    _nodes.RemoveAt(i);

                    return true;
                }
            }

            return false;
        }
    }
}
