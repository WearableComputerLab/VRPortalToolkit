using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Events
{
    public abstract class ActionListenerBase
    {
        public abstract void Invoke(object[] args);

        public abstract bool Find(Delegate @delegate, object[] args);

        protected static bool DelegatesMatch(Delegate delegate1, Delegate delegate2)
        {
            if (delegate1 == null)
            {
                if (delegate2 != null)
                    return false;
            }
            else if (!delegate1.Equals(delegate2))
                return false;

            return true;
        }

        protected static bool ArgsMatch(object[] args1, object[] args2)
        {
            if (args1 == null || args1.Length == 0)
            {
                if (args2 == null || args2.Length == 0)
                    return true;
            }

            for (int i = 0; i < args1.Length; i++)
            {
                object arg = args1[i];

                if (arg == null)
                {
                    if (args2[i] != null)
                        return false;
                }
                else if (!arg.Equals(args2[i]))
                    return false;
            }

            return true;
        }
    }

    public class ActionListener : ActionListenerBase
    {
        public readonly UnityAction action;

        public ActionListener(UnityAction action)
        {
            this.action = action;
        }

        public override void Invoke(object[] args)
        {
            action.Invoke();
        }

        public override bool Find(Delegate @delegate, object[] args)
        {
            return DelegatesMatch(action, @delegate) && ArgsMatch(null, args);
        }
    }

    public class ActionListener<T> : ActionListenerBase
    {
        public readonly UnityAction<T> action;

        public readonly object[] args;

        public ActionListener(UnityAction<T> action)
        {
            this.action = action;
        }

        public ActionListener(UnityAction<T> action, T value)
        {
            this.action = action;
            args = new object[] { value };
        }

        public override void Invoke(object[] args)
        {
            if (this.args == null)
                action.Invoke((T)args[0]);
            else
                action.Invoke((T)this.args[0]);
        }

        public override bool Find(Delegate @delegate, object[] args)
        {
            return DelegatesMatch(action, @delegate) && ArgsMatch(this.args, args);
        }
    }

    public class ActionListener<T1, T2> : ActionListenerBase
    {
        public readonly UnityAction<T1, T2> action;

        public readonly object[] args;

        public ActionListener(UnityAction<T1, T2> action)
        {
            this.action = action;
        }

        public ActionListener(UnityAction<T1, T2> action, T1 value1, T2 value2)
        {
            this.action = action;
            args = new object[] { value1, value2 };
        }

        public override void Invoke(object[] args)
        {
            if (this.args == null)
                action.Invoke((T1)args[0], (T2)args[1]);
            else
                action.Invoke((T1)this.args[0], (T2)this.args[1]);
        }

        public override bool Find(Delegate @delegate, object[] args)
        {
            return DelegatesMatch(action, @delegate) && ArgsMatch(this.args, args);
        }
    }

    public class ActionListener<T1, T2, T3> : ActionListenerBase
    {
        public readonly UnityAction<T1, T2, T3> action;

        public readonly object[] args;

        public ActionListener(UnityAction<T1, T2, T3> action)
        {
            this.action = action;
        }

        public ActionListener(UnityAction<T1, T2, T3> action, T1 value1, T2 value2, T3 value3)
        {
            this.action = action;
            args = new object[] { value1, value2, value3 };
        }

        public override void Invoke(object[] args)
        {
            if (this.args == null)
                action.Invoke((T1)args[0], (T2)args[1], (T3)args[2]);
            else
                action.Invoke((T1)this.args[0], (T2)this.args[1], (T3)this.args[2]);
        }

        public override bool Find(Delegate @delegate, object[] args)
        {
            return DelegatesMatch(action, @delegate) && ArgsMatch(this.args, args);
        }
    }

    public class ActionListener<T1, T2, T3, T4> : ActionListenerBase
    {
        public readonly UnityAction<T1, T2, T3, T4> action;

        public readonly object[] args;

        public ActionListener(UnityAction<T1, T2, T3, T4> action)
        {
            this.action = action;
        }

        public ActionListener(UnityAction<T1, T2, T3, T4> action, T1 value1, T2 value2, T3 value3, T4 value4)
        {
            this.action = action;
            args = new object[] { value1, value2, value3, value4 };
        }

        public override void Invoke(object[] args)
        {
            if (this.args == null)
                action.Invoke((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]);
            else
                action.Invoke((T1)this.args[0], (T2)this.args[1], (T3)this.args[2], (T4)this.args[3]);
        }

        public override bool Find(Delegate @delegate, object[] args)
        {
            return DelegatesMatch(action, @delegate) && ArgsMatch(this.args, args);
        }
    }
}
