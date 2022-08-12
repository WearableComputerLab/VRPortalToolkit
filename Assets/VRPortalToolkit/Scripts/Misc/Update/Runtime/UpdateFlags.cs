using System;

namespace Misc.Update
{
    [Flags]
    public enum UpdateFlags : int
    {
        Never = 0,
        FixedUpdate = 1 << 0,
        WaitForFixedUpdate = 1 << 1,
        Update = 1 << 2,
        NullUpdate = 1 << 3,
        LateUpdate = 1 << 4,
        WaitForEndOfFrame = 1 << 5,
        OnPreCull = 1 << 6,
        OnPreRender = 1 << 7,
        OnPostRender = 1 << 8,
        OnBeforeRender = 1 << 9,
        BeginCameraRendering = 1 << 10,
        BeginContextRendering = 1 << 11,
        BeginFrameRendering = 1 << 12,
        EndCameraRendering = 1 << 13,
        EndContextRendering = 1 << 14,
        EndFrameRendering = 1 << 15,
        Sources = 1 << 16,
        WaitForSeconds = 1 << 17,
        //OnEnabled = 1 << 18,
        //OnDisabled = 1 << 19,
    }
}