namespace Misc.Events
{
    public abstract class CachedMember : CachedProcess
    {
        private object _cachedTarget;
        protected object cachedTarget => _cachedTarget;

        private bool _cachedTargetIsDirty = true;
        protected void SetCachedIsDirty() => _cachedTargetIsDirty = true;

        public abstract bool isStatic { get; }

        public sealed override object Invoke(ref object target, object[] args)
        {
            if (!isStatic)
            {
                if (target != _cachedTarget)
                    _cachedTargetIsDirty = true;
                else if (target != null && !target.Equals(_cachedTarget))
                    _cachedTargetIsDirty = true;
            }

            if (_cachedTargetIsDirty)
            {
                _cachedTargetIsDirty = false;
                _cachedTarget = target;
                OnCached();
            }

            if (AllowInvoke(target))
                return MemberInvoke(ref target, args);

            return null;
        }

        protected abstract void OnCached();

        protected abstract object MemberInvoke(ref object target, object[] args);
    }
}
