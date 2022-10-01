using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Physics
{
    public class ComponentHandler<TSource, TComponent> where TSource : Component where TComponent : Component
    {
        private bool _enabled;
        public bool enabled {
            get => _enabled;
            set {
                if (value != _enabled)
                {
                    // Clear invalid before actually enabling
                    if (value) ClearInvalid();

                    _enabled = value;

                    if (_enabled) OnEnable();
                    else OnDisable();
                }
            }
        }

        private bool _exitOnComponentDisabled = true;
        public bool exitOnComponentDisabled {
            get => _exitOnComponentDisabled;
            set {
                if (_exitOnComponentDisabled != value)
                {
                    _exitOnComponentDisabled = value;

                    if (_exitOnComponentDisabled)
                    {
                        foreach (TriggerInfo info in _triggeredComponents.Values)
                            if (!info.isActiveAndEnabled)
                            {
                                _componentCount--;
                                OnExitComponent(info.component);
                            }
                    }
                    else
                    {
                        foreach (TriggerInfo info in _triggeredComponents.Values)
                            if (!info.isActiveAndEnabled)
                            {
                                _componentCount++;
                                OnEnterComponent(info.component);
                            }
                    }
                }
            }
        }

        private bool _exitOnSourceDisabled = true;
        public bool exitOnSourceDisabled {
            get => _exitOnSourceDisabled;
            set {
                if (_exitOnSourceDisabled != value)
                {
                    _exitOnSourceDisabled = value;

                    if (_exitOnSourceDisabled)
                    {
                        foreach (SourceInfo info in _sources.Values)
                            if (!info.isActiveAndEnabled)
                            {
                                foreach (TComponent component in info.components)
                                    if (component != null) AddComponentSource(component);
                            }
                    }
                    else
                    {
                        foreach (SourceInfo info in _sources.Values)
                            if (!info.isActiveAndEnabled)
                            {
                                foreach (TComponent component in info.components)
                                    if (component != null) RemoveComponentSource(component);
                            }
                    }
                }
            }
        }

        private bool _exitOnComponentDestroyed = true;
        public bool exitOnComponentDestroyed {
            get => _exitOnComponentDestroyed;
            set {
                if (_exitOnComponentDestroyed != value)
                {
                    _exitOnComponentDestroyed = value;
                    ClearInvalid();
                }
            }
        }

        private bool _exitOnSourceDestroyed = true;
        public bool exitOnSourceDestroyed {
            get => _exitOnSourceDestroyed;
            set {
                if (_exitOnSourceDestroyed != value)
                {
                    _exitOnSourceDestroyed = value;
                    ClearInvalid();
                }
            }
        }

        private GetComponentsMode _getComponentsMode = GetComponentsMode.GetComponent;
        public GetComponentsMode getComponentsMode {
            get => _getComponentsMode;
            set {
                if (_getComponentsMode != value)
                {
                    _getComponentsMode = value;
                    Reset();
                }
            }
        }

        public UnityAction<TComponent> componentEntered;
        public UnityAction<TComponent> componentExited;

        public int sourceCount => _sources.Count;

        private int _componentCount = 0;
        public int componentCount => enabled ? _componentCount : 0;

        private Dictionary<TComponent, TriggerInfo> _triggeredComponents = new Dictionary<TComponent, TriggerInfo>();
        private Dictionary<TSource, SourceInfo> _sources = new Dictionary<TSource, SourceInfo>();

        private List<TComponent> _componentsToBeRemoved = new List<TComponent>();
        private List<TSource> _sourceToBeRemoved = new List<TSource>();

        private class TriggerInfo
        {
            public bool isActiveAndEnabled;

            public TComponent component;

            public int sourcesCount = 1;
        }

        private class SourceInfo
        {
            public bool isActiveAndEnabled;

            public TSource source;

            public TComponent[] components;
        }

        protected virtual void OnEnable()
        {
            foreach (TriggerInfo info in _triggeredComponents.Values)
            {
                if (!exitOnComponentDisabled || info.isActiveAndEnabled)
                    OnEnterComponent(info.component);
            }
        }

        protected virtual void OnDisable()
        {
            foreach (TriggerInfo info in _triggeredComponents.Values)
            {
                if (!exitOnComponentDisabled || info.isActiveAndEnabled)
                    OnExitComponent(info.component);
            }
        }

        public virtual void EnterSource(TSource source)
        {
            if (source && !_sources.ContainsKey(source))
            {
                switch (getComponentsMode)
                {
                    case GetComponentsMode.GetComponent:
                        AddComponent(source, source.GetComponent<TComponent>());
                        break;

                    case GetComponentsMode.GetComponentInChildren:
                        AddComponent(source, source.GetComponentInChildren<TComponent>(true));
                        break;

                    case GetComponentsMode.GetComponentInParent:
                        AddComponent(source, source.GetComponentInParent<TComponent>());
                        break;

                    case GetComponentsMode.GetComponents:
                        AddComponents(source, source.GetComponents<TComponent>());
                        break;

                    case GetComponentsMode.GetComponentsInChildren:
                        AddComponents(source, source.GetComponentsInChildren<TComponent>());
                        break;

                    case GetComponentsMode.GetComponentsInParent:
                        AddComponents(source, source.GetComponentsInParent<TComponent>());
                        break;
                }
            }
        }

        private void AddComponent(TSource source, TComponent component)
        {
            if (component && ComponentIsValid(component))
            {
                bool isActiveAndEnabled = SourceIsActiveAndEnabled(source);

                if (!exitOnComponentDisabled || isActiveAndEnabled)
                    AddComponentSource(component);

                _sources.Add(source, new SourceInfo
                {
                    source = source,
                    isActiveAndEnabled = isActiveAndEnabled,
                    components = new TComponent[1] { component }
                });
            }
        }

        private void AddComponents(TSource source, TComponent[] components)
        {
            if (components != null)
            {
                TComponent component;
                int count = 0;

                bool isActiveAndEnabled = SourceIsActiveAndEnabled(source);

                for (int i = 0; i < components.Length; i++)
                {
                    component = components[i];

                    if (component && ComponentIsValid(component))
                    {
                        count++;

                        if (!exitOnComponentDisabled || isActiveAndEnabled)
                            AddComponentSource(component);
                    }
                    else
                        components[i] = null;
                }

                if (count > 0) _sources.Add(source, new SourceInfo
                {
                    source = source,
                    isActiveAndEnabled = isActiveAndEnabled,
                    components = components
                });
            }
        }

        protected virtual bool ComponentIsValid(Component component) => true;

        private void AddComponentSource(TComponent component)
        {
            if (_triggeredComponents.TryGetValue(component, out TriggerInfo info))
                info.sourcesCount++;
            else
            {
                info = new TriggerInfo()
                {
                    component = component,
                    isActiveAndEnabled = ComponentIsActiveAndEnabled(component),
                    sourcesCount = 1
                };

                _triggeredComponents.Add(component, info);

            }

            if (info.sourcesCount == 1 && (!exitOnComponentDisabled || info.isActiveAndEnabled))
            {
                _componentCount++;

                if (enabled) OnEnterComponent(component);
            }
        }

        protected virtual void OnEnterComponent(TComponent other) => componentEntered?.Invoke(other);

        public virtual void ExitSource(TSource source)
        {
            if (_sources.TryGetValue(source, out SourceInfo info))
            {
                foreach (TComponent component in info.components)
                    RemoveComponentSource(component);

                _sources.Remove(source);
            }
        }

        private void RemoveComponentSource(TComponent component)
        {
            if (component != null && _triggeredComponents.TryGetValue(component, out TriggerInfo info))
            {
                info.sourcesCount--;

                if (info.sourcesCount <= 0)
                {
                    _triggeredComponents.Remove(component);

                    if (!exitOnComponentDisabled || info.isActiveAndEnabled)
                    {
                        _componentCount--;
                        if (enabled) OnExitComponent(component);
                    }
                }
            }
        }

        protected virtual void OnExitComponent(TComponent other) => componentExited?.Invoke(other);

        public virtual void ClearInvalid()
        {
            foreach (SourceInfo info in _sources.Values)
            {
                if (_exitOnSourceDestroyed && !info.source) _sourceToBeRemoved.Add(info.source);
                else if (exitOnSourceDisabled)
                {
                    if (info.isActiveAndEnabled != SourceIsActiveAndEnabled(info.source))
                    {
                        info.isActiveAndEnabled = !info.isActiveAndEnabled;

                        if (info.isActiveAndEnabled)
                        {
                            foreach (TComponent component in info.components)
                                if (component != null) AddComponentSource(component);
                        }
                        else
                        {
                            foreach (TComponent component in info.components)
                                if (component != null) RemoveComponentSource(component);
                        }
                    }
                }
            }

            foreach (TSource source in _sourceToBeRemoved)
                if (_sources.TryGetValue(source, out SourceInfo info))
                {
                    foreach (TComponent component in info.components)
                        RemoveComponentSource(component);
                }

            _sourceToBeRemoved.Clear();

            foreach (TriggerInfo info in _triggeredComponents.Values)
            {
                if (exitOnComponentDestroyed && !info.component) _componentsToBeRemoved.Add(info.component);
                else if (exitOnComponentDisabled)
                {
                    if (info.isActiveAndEnabled != ComponentIsActiveAndEnabled(info.component))
                    {
                        info.isActiveAndEnabled = !info.isActiveAndEnabled;

                        if (info.isActiveAndEnabled)
                        {
                            _componentCount++;
                            if (enabled) OnEnterComponent(info.component);
                        }
                        else
                        {
                            _componentCount--;
                            if (enabled) OnExitComponent(info.component);
                        }
                    }
                }
            }

            RemoveComponents(_componentsToBeRemoved);
            _componentsToBeRemoved.Clear();
        }

        private void RemoveComponents(List<TComponent> toBeRemoved)
        {
            if (toBeRemoved.Count > 0)
            {
                foreach (TComponent component in toBeRemoved)
                {
                    if (_triggeredComponents.TryGetValue(component, out TriggerInfo info))
                    {
                        _triggeredComponents.Remove(component);

                        if (!exitOnComponentDisabled || info.isActiveAndEnabled)
                        {
                            _componentCount--;
                            if (enabled) OnExitComponent(component);
                        }
                    }
                }
            }
        }

        protected virtual bool SourceIsActiveAndEnabled(TSource source) => IsActiveAndEnabled(source);

        protected virtual bool ComponentIsActiveAndEnabled(TComponent component) => IsActiveAndEnabled(component);

        private bool IsActiveAndEnabled(Component component)
        {
            if (component && component.gameObject && component.gameObject.activeSelf)
            {
                if (component is Behaviour behaviour)
                    return behaviour.enabled;

                if (component is Renderer renderer)
                    return renderer.enabled;

                if (component is Collider collider)
                    return collider.enabled;

                return true;
            }

            return false;
        }

        public virtual bool HasComponent(TComponent component)
        {
            if (!enabled) return false;

            if (exitOnComponentDisabled)
                return _triggeredComponents.ContainsKey(component);

            if (_triggeredComponents.TryGetValue(component, out TriggerInfo info))
                return info.isActiveAndEnabled;

            return false;
        }

        public virtual IEnumerable<TComponent> GetComponents()
        {
            if (enabled)
            {
                foreach (TriggerInfo info in _triggeredComponents.Values)
                {
                    if (exitOnComponentDisabled || info.isActiveAndEnabled)
                        yield return info.component;
                }
            }
        }

        public virtual bool HasSource(TSource source) => _sources.ContainsKey(source);

        public virtual IEnumerable<TSource> GetSources()
        {
            foreach (TSource source in _sources.Keys)
                yield return source;
        }

        public virtual void Clear()
        {
            _componentsToBeRemoved.Clear();

            foreach (TriggerInfo info in _triggeredComponents.Values)
                _componentsToBeRemoved.Add(info.component);

            RemoveComponents(_componentsToBeRemoved);
        }

        public virtual void Reset()
        {
            TSource[] sources = new TSource[_sources.Count];

            int index = 0;
            foreach (TSource source in _sources.Keys)
                sources[index++] = source;

            Clear();

            foreach (TSource source in sources)
                EnterSource(source);
        }
    }
}
