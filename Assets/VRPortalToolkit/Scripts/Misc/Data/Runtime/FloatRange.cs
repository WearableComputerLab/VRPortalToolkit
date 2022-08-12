using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Misc.Data
{
    [Serializable]
    public struct FloatRange
    {
        public float min;

        public float max;

        public static readonly FloatRange MinMax = new FloatRange(float.MinValue, float.MaxValue);

        public FloatRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"FloatRange(min:{min}, max:{max})";
        }

        public bool Contains(float value)
        {
            return value >= min && value <= max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator IntRange(FloatRange r)
        {
            return new IntRange((int)r.min, (int)r.max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FloatRange(IntRange r)
        {
            return new FloatRange(r.min, r.max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(FloatRange r)
        {
            return new Vector2(r.min, r.max);
        }
    }
}
