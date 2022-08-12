using System;
using System.Reflection;

namespace Misc.Events
{
    public abstract class CachedField : CachedProcess
    {
        private FieldInfo _field;
        public FieldInfo field {
            get => _field;
            set => _field = value;
        }

        public CachedField() { }

        public CachedField(FieldInfo field) : base()
        {
            this.field = field;
        }

        public override string GetName()
        {
            if (field != null)
                return field.DeclaringType.Name + "." + field.Name;

            return "CachedField";
        }
    }

    public class CachedGetField : CachedField
    {
        public CachedGetField() : base() { }

        public CachedGetField(FieldInfo field) : base(field) { }

        public override object Invoke(ref object obj, object[] args)
        {
            if (AllowInvoke(obj) && field != null)
            {
                object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

                if (actualArgs != null && actualArgs.Length != 0)
                    throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 0");

                object returnValue = field.GetValue(obj);

                if (isLast) return returnValue;

                return next.Invoke(ref returnValue, args);
            }

            return null;
        }
    }
    public class CachedSetField : CachedField
    {
        public CachedSetField() : base() { }

        public CachedSetField(FieldInfo field) : base(field) { }

        public override object Invoke(ref object obj, object[] args)
        {
            if (AllowInvoke(obj) && field != null)
            {
                object[] actualArgs = hasOverrideArgs ? overrideArgs : args;

                if (isLast)
                {
                    ThrowOnInvalidLength(actualArgs, 1);

                    field.SetValue(obj, actualArgs[0]);
                    return null;
                }

                ThrowOnInvalidLength(actualArgs, 0);

                object nextValue = field.GetValue(obj);

                object returnValue = next.Invoke(ref nextValue, args);

                if (!field.IsInitOnly)
                    field.SetValue(obj, nextValue);

                return returnValue;
            }

            return null;
        }
    }
}
