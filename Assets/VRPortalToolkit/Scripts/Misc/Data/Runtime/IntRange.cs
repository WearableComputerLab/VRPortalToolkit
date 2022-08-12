using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Misc.Data
{
    [Serializable]
    public struct IntRange
    {
        public int min;

        public int max;

        public static readonly FloatRange MinMax = new FloatRange(float.MinValue, float.MaxValue);

        public IntRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"IntRange(min:{min}, max:{max})";
        }

        public bool Contains(float value)
        {
            return value >= min && value <= max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2Int(IntRange r)
        {
            return new Vector2Int(r.min, r.max);
        }
    }
}
