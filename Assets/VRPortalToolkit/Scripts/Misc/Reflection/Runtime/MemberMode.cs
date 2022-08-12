using System;

namespace Misc.Reflection
{
    [Flags]
    public enum MemberMode
    {
        None = 0,
        IncludeFields = 1 << 0,
        IncludeProperties = 1 << 1,
        IncludeMethods = 1 << 2
    }
}