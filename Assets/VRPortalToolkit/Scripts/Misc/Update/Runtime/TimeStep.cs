using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Update
{
    public class TimeStep
    {
        private TimeUnit _unit;
        public TimeUnit unit {
            get => _unit;
            set {
                if (_unit != value)
                {
                    _unit = value;
                    isDirty = true;
                }
            }
        }

        protected bool isDirty = true;

        protected double lastTime = 0;

        public float UpdateStep(TimeUnit unit)
        {
            this.unit = unit;
            return UpdateStep();
        }

        public float UpdateStep()
        {
            switch (_unit)
            {
                case TimeUnit.One:
                    return 1f;
                case TimeUnit.Time:
                    return GetStep(Time.timeAsDouble);
                case TimeUnit.TimeScaled:
                    return GetStep(Time.timeAsDouble) * Time.timeScale;
                default:
                    return 0f;
            }
        }

        protected virtual float GetStep(double time)
        {
            if (isDirty)
            {
                lastTime = time;
                isDirty = false;
                return 0f;
            }
            else
            {
                float current = (float)(time - lastTime);
                lastTime = time;
                return current;
            }
        }
    }
}