
namespace VRPortalToolkit.Data
{
    [System.Flags]
    public enum Direction : sbyte
    {
        Left = 1 << 1,
        Right = 1 << 2,
        Down = 1 << 3,
        Up = 1 << 4,
        Back = 1 << 5,
        Forward = 1 << 6
    }
}