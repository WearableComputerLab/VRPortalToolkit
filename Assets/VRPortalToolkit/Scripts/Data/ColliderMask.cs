namespace VRPortalToolkit.Data
{
    /// <summary>
    /// Specifies which types of colliders to include in portal operations.
    /// </summary>
    [System.Flags]
    public enum ColliderMask
    {
        /// <summary>
        /// Ignore all colliders.
        /// </summary>
        IgnoreColliders = 0,
        /// <summary>
        /// Include non-collider objects.
        /// </summary>
        IncludeNonColliders = 1 << 0,
        /// <summary>
        /// Include non-trigger colliders.
        /// </summary>
        IncludeNonTriggers = 1 << 1,
        /// <summary>
        /// Include trigger colliders.
        /// </summary>
        IncludeTriggers = 1 << 2,
    }
}