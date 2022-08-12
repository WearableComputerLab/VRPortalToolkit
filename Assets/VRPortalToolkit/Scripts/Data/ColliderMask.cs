namespace VRPortalToolkit.Data
{
    [System.Flags]
    public enum ColliderMask
    {
        IgnoreColliders = 0,
        IncludeNonColliders = 1 << 0,
        IncludeNonTriggers = 1 << 1,
        IncludeTriggers = 1 << 2,
    }
}