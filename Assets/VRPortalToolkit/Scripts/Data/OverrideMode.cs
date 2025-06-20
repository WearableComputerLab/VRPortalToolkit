namespace VRPortalToolkit.Data
{
    /// <summary>
    /// Specifies how the list of portals should be applied in portal operations.
    /// </summary>
    public enum OverrideMode : byte
    {
        /// <summary>No override.</summary>
        None = 0,
        /// <summary>Ignore the value.</summary>
        Ignore = 1,
        /// <summary>Replace the value.</summary>
        Replace = 2
    }
}