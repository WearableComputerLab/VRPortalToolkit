using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.EditorHelpers
{
    [System.Flags]
    public enum FoldoutOptions
    {
        None = 0,
        Bold = 1 << 0,
        Large = 1 << 1,
    }

    public class FoldoutAttribute : PropertyAttribute
    {
        public readonly FoldoutOptions options;

        public FoldoutAttribute(FoldoutOptions options = FoldoutOptions.None, int order = 10)
        {
            this.options = options;
            this.order = order;
        }
    }
}
