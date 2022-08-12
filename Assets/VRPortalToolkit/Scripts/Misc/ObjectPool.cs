using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc
{
    public class ObjectPool<T>
    {
        private int _capacity;
        public int capacity
        {
            get => _capacity;
            set
            {
                _capacity = value;

                if (_capacity > 0 && _pool.Count > _capacity)
                    Remove(_pool.Count - capacity);
            }
        }

        public Func<T> generator;

        public Action<T> onGet;

        public Action<T> onRelease;

        public Action<T> disposer;

        public int Count => _pool.Count;

        private Queue<T> _pool;

        public ObjectPool(Func<T> generator, Action<T> onGet = null, Action<T> onRelease = null, Action<T> disposer = null, int capacity = -1)
        {
            this.generator = generator;
            this.onGet = onGet;
            this.onRelease = onRelease;
            this.disposer = disposer;
            _capacity = capacity;

            if (this.capacity > 0)
                _pool = new Queue<T>(_capacity);
            else
                _pool = new Queue<T>();
        }

        public T Get()
        {
            T item;

            if (_pool.Count > 0)
                item = _pool.Dequeue();
            else
                item = generator.Invoke();

            onGet?.Invoke(item);

            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;

            onRelease?.Invoke(item);

            if (_capacity < 0 || _pool.Count < _capacity)
                _pool.Enqueue(item);
            else
                disposer?.Invoke(item);
        }

        public void Fill(int count = -1)
        {
            if (count <= 0) count = capacity;

            while (_pool.Count < capacity && count > 0)
            {
                count--;
                _pool.Enqueue(generator.Invoke());
            }
        }

        public void Clear() => Remove(_pool.Count);

        public void Remove(int count)
        {
            int newCount = _pool.Count - count;

            if (disposer == null)
            {
                while (_pool.Count > newCount)
                    _pool.Dequeue();

                return;
            }

            while (_pool.Count > newCount)
                disposer.Invoke(_pool.Dequeue());
        }
    }
}