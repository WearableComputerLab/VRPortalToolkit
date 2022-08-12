using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc
{
    [System.Flags]
    public enum TransformProperty
    {
        Ignore = 0,
        Position = 1 << 0,
        Rotation = 1 << 1,
        Scale = 1 << 2,
        PositionAndRotation = Position | Rotation
    }
}
