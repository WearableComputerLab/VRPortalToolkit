using System;

namespace Misc.Reflection
{
    [Flags]
    public enum BindingMode
    {
        None = 0,
        IncludePublic = 1 << 0,
        IncludeNonPublic = 1 << 1,
        IncludeInstanced = 1 << 2,
        IncludeStatic = 1 << 3,
        IgnoreCase = 1 << 4
    }
}