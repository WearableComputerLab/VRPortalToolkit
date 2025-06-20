using UnityEngine;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Data
{
    /// <summary>
    /// Represents a portal as seen by a camera, used to determine portal visibility and rendering bounds.
    /// </summary>
    public struct ViewWindow
    {
        /// <summary>
        /// The minimum X value of the window.
        /// </summary>
        public float xMin;

        /// <summary>
        /// The maximum X value of the window.
        /// </summary>
        public float xMax;

        /// <summary>
        /// The minimum Y value of the window.
        /// </summary>
        public float yMin;

        /// <summary>
        /// The maximum Y value of the window.
        /// </summary>
        public float yMax;

        /// <summary>
        /// The minimum Z value of the window.
        /// </summary>
        public float zMin;

        /// <summary>
        /// The maximum Z value of the window.
        /// </summary>
        public float zMax;

        /// <summary>
        /// Creates a view through a window with the given width, height, and depth.
        /// </summary>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        /// <param name="depth">The depth of the window.</param>
        public ViewWindow(float width, float height, float depth)
        {
            this.xMin = 0f;
            this.xMax = width;
            this.yMin = 0f;
            this.yMax = height;
            this.zMin = depth;
            this.zMax = depth;
        }

        /// <summary>
        /// Creates a view through a window with the same min and max for all axes.
        /// </summary>
        /// <param name="min">The minimum value for all axes.</param>
        /// <param name="max">The maximum value for all axes.</param>
        public ViewWindow(float min, float max)
        {
            this.xMin = min;
            this.xMax = max;
            this.yMin = min;
            this.yMax = max;
            this.zMin = min;
            this.zMax = max;
        }

        /// <summary>
        /// Creates a window with explicit min and max for each axis.
        /// </summary>
        /// <param name="xMin">Minimum X.</param>
        /// <param name="xMax">Maximum X.</param>
        /// <param name="yMin">Minimum Y.</param>
        /// <param name="yMax">Maximum Y.</param>
        /// <param name="zMin">Minimum Z.</param>
        /// <param name="zMax">Maximum Z.</param>
        public ViewWindow(float xMin, float xMax, float yMin, float yMax, float zMin, float zMax)
        {
            this.xMin = xMin;
            this.xMax = xMax;
            this.yMin = yMin;
            this.yMax = yMax;
            this.zMin = zMin;
            this.zMax = zMax;
        }

        /// <summary>
        /// Returns true if the window is valid (min <= max for all axes).
        /// </summary>
        public bool IsValid()
        {
            return xMin <= xMax && yMin <= yMax && zMin <= zMax;
        }

        /// <summary>
        /// Returns true if the given 2D screen position is inside the window.
        /// </summary>
        /// <param name="screenPos">The 2D screen position.</param>
        public bool Contains(Vector2 screenPos)
            => xMin <= screenPos.x && screenPos.x <= xMax && yMin <= screenPos.y && screenPos.y <= yMax;

        /// <summary>
        /// Returns true if the given 3D screen position is inside the window.
        /// </summary>
        /// <param name="screenPos">The 3D screen position.</param>
        public bool Contains(Vector3 screenPos)
            => xMin <= screenPos.x && screenPos.x <= xMax && yMin <= screenPos.y && screenPos.y <= yMax && zMin <= screenPos.z && screenPos.z <= zMax;

        /// <summary>
        /// Expands the window to include the given point.
        /// </summary>
        /// <param name="point">The point to include.</param>
        public void AddPoint(Vector3 point)
        {
            xMin = Mathf.Min(xMin, point.x);
            xMax = Mathf.Max(xMax, point.x);
            yMin = Mathf.Min(yMin, point.y);
            yMax = Mathf.Max(yMax, point.y);
            zMin = Mathf.Min(zMin, point.z);
            zMax = Mathf.Max(zMax, point.z);
        }

        /// <summary>
        /// Returns true if this window is visible through another window.
        /// </summary>
        /// <param name="outerWindow">The outer window to check against.</param>
        public bool IsVisibleThrough(ViewWindow outerWindow)
            => (zMax >= outerWindow.zMin && xMax > outerWindow.xMin && xMin < outerWindow.xMax && yMax > outerWindow.yMin && yMin < outerWindow.yMax);

        /// <summary>
        /// Clamps this window inside another window.
        /// </summary>
        /// <param name="outerWindow">The window to clamp inside.</param>
        public void ClampInside(ViewWindow outerWindow)
        {
            if (xMin < outerWindow.xMin) xMin = outerWindow.xMin;
            if (xMax > outerWindow.xMax) xMax = outerWindow.xMax;
            if (yMin < outerWindow.yMin) yMin = outerWindow.yMin;
            if (yMax > outerWindow.yMax) yMax = outerWindow.yMax;
        }

        private static readonly Vector3[] boundCornerOffsets = {
            new Vector3 (1, 1, 1), new Vector3 (-1, 1, 1), new Vector3 (-1, -1, 1), new Vector3 (-1, -1, -1),
            new Vector3 (-1, 1, -1), new Vector3 (1, -1, -1), new Vector3 (1, 1, -1), new Vector3 (1, -1, 1),
        };

        /// <summary>
        /// Gets the 2D rectangle representation of this window.
        /// </summary>
        public Rect GetRect() => Rect.MinMaxRect(xMin, yMin, xMax, yMax);

        /// <summary>
        /// Combines two windows into one that contains both.
        /// </summary>
        /// <param name="windowA">The first window.</param>
        /// <param name="windowB">The second window.</param>
        /// <returns>The combined window.</returns>
        public static ViewWindow Combine(ViewWindow windowA, ViewWindow windowB)
        {
            if (windowA.IsValid())
            {
                if (windowB.IsValid())
                {
                    if (windowB.xMin < windowA.xMin) windowA.xMin = windowB.xMin;
                    if (windowB.yMin < windowA.yMin) windowA.yMin = windowB.yMin;
                    if (windowB.zMin < windowA.zMin) windowA.zMin = windowB.zMin;
                    if (windowB.xMax > windowA.xMax) windowA.xMax = windowB.xMax;
                    if (windowB.yMax > windowA.yMax) windowA.yMax = windowB.yMax;
                    if (windowB.zMax > windowA.zMax) windowA.zMax = windowB.zMax;
                }
                
                return windowA;
            }

            return windowB;
        }

        /// <summary>
        /// Tries to get a window of this portal relative to a camera.
        /// </summary>
        /// <param name="camera">The camera to use.</param>
        /// <param name="localBounds">The local bounds of the portal.</param>
        /// <param name="localToWorld">The local to world matrix.</param>
        /// <returns>The calculated view window.</returns>
        public static ViewWindow GetWindow(Camera camera, Bounds localBounds, Matrix4x4 localToWorld)
        {
            ViewWindow window = new ViewWindow(float.MaxValue, float.MinValue);

            Vector3 corner;

            for (int i = 0; i < 8; i++)
            {
                // Local space
                corner = localBounds.center + Vector3.Scale(localBounds.extents, boundCornerOffsets[i]);
                
                // World space
                corner = localToWorld.MultiplyPoint(corner);

                // Viewport space
                corner = camera.WorldToViewportPoint(corner);
                
                if (corner.z <= 0)
                {
                    // If point is behind camera, it gets flipped to the opposite side
                    // So clamp to opposite edge to correct for this
                    //corner.x = 0.5f - corner.x;
                    //corner.y = 0.5f - corner.y;

                    // Alternate idea, if behind the camera, just assume max
                    window.xMin = float.MinValue;
                    window.xMax = float.MaxValue;
                    window.yMin = float.MinValue;
                    window.yMax = float.MaxValue;
                    window.zMin = Mathf.Min(window.zMin, corner.z);
                    window.zMax = Mathf.Max(window.zMax, corner.z);
                }

                // Update bounds with new corner point
                window.AddPoint(corner);
            }

            return window;
        }

        /// <summary>
        /// Tries to get a window of this portal relative to a custom view/projection.
        /// </summary>
        /// <param name="view">The view matrix.</param>
        /// <param name="proj">The projection matrix.</param>
        /// <param name="localBounds">The local bounds of the portal.</param>
        /// <param name="localToWorld">The local to world matrix.</param>
        /// <returns>The calculated view window.</returns>
        public static ViewWindow GetWindow(Matrix4x4 view, Matrix4x4 proj, Bounds localBounds, Matrix4x4 localToWorld)
        {
            ViewWindow window = new ViewWindow(float.MaxValue, float.MinValue);

            Vector3 corner;

            for (int i = 0; i < 8; i++)
            {
                // Local space
                corner = localBounds.center + Vector3.Scale(localBounds.extents, boundCornerOffsets[i]);

                // World space
                corner = localToWorld.MultiplyPoint(corner);
                
                // Viewport space
                corner = CameraUtility.WorldToViewportPoint(view, proj, corner);

                if (corner.z <= 0f)
                {
                    // If point is behind camera, it gets flipped to the opposite side
                    // So clamp to opposite edge to correct for this
                    //corner.x = 0.5f - corner.x;
                    //corner.y = 0.5f - corner.y;

                    // Alternate idea, if behind the camera, just assume max
                    window.xMin = float.MinValue;
                    window.xMax = float.MaxValue;
                    window.yMin = float.MinValue;
                    window.yMax = float.MaxValue;
                    window.zMin = Mathf.Min(window.zMin, corner.z);
                    window.zMax = Mathf.Max(window.zMax, corner.z);
                }

                // Update bounds with new corner point
                window.AddPoint(corner);
            }

            return window;
        }

        /// <summary>
        /// Returns a string representation of the window.
        /// </summary>
        public override string ToString()
        {
            return $"({xMin}<{xMax},{yMin}<{yMax},{zMin}<{zMax})";
        }
    }
}