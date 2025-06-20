namespace VRPortalToolkit.Data
{
    /// <summary>
    /// Represents a direction in 3D space for portal operations.
    /// </summary>
    [System.Flags]
    public enum Direction : sbyte
    {
        /// <summary>Left direction.</summary>
        Left = 1 << 1,
        /// <summary>Right direction.</summary>
        Right = 1 << 2,
        /// <summary>Down direction.</summary>
        Down = 1 << 3,
        /// <summary>Up direction.</summary>
        Up = 1 << 4,
        /// <summary>Back direction.</summary>
        Back = 1 << 5,
        /// <summary>Forward direction.</summary>
        Forward = 1 << 6
    }
}