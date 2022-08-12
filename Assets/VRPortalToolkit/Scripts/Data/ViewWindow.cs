using UnityEngine;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Data
{
    /// <summary>
    /// Represents how a portal is seen by a camera (for the sake of ensuring it is worth rendering).
    /// </summary>
    public struct ViewWindow
    {
        public float xMin;

        public float xMax;

        public float yMin;

        public float yMax;

        public float zMin;

        public float zMax;

        /// <summary>
        /// A view through a window.
        /// </summary>
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
        /// A view through a window.
        /// </summary>
        /// <param name="min">The start min for all xyz.</param>
        /// <param name="max">The start max for all xyz.</param>
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
        /// A view through a window.
        /// </summary>
        /// <param name="min">The start min for all xyz.</param>
        /// <param name="max">The start max for all xyz.</param>
        public ViewWindow(float xMin, float xMax, float yMin, float yMax, float zMin, float zMax)
        {
            this.xMin = xMin;
            this.xMax = xMax;
            this.yMin = yMin;
            this.yMax = yMax;
            this.zMin = zMin;
            this.zMax = zMax;
        }

        public bool IsValid()
        {
            return xMin <= xMax && yMin <= yMax && zMin <= zMax;
        }

        public bool Contains(Vector2 screenPos)
            => xMin <= screenPos.x && screenPos.x <= xMax && yMin <= screenPos.y && screenPos.y <= yMax;

        public bool Contains(Vector3 screenPos)
            => xMin <= screenPos.x && screenPos.x <= xMax && yMin <= screenPos.y && screenPos.y <= yMax && zMin <= screenPos.z && screenPos.z <= zMax;

        /// <summary>
        /// Adds a point to be encased by this window.
        /// </summary>
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
        /// Returns true if this is visible through another portal.
        /// </summary>
        public bool IsVisibleThrough(ViewWindow outerWindow)
            => (zMax > outerWindow.zMin && xMax > outerWindow.xMin && xMin < outerWindow.xMax && yMax > outerWindow.yMin && yMin < outerWindow.yMax);

        public void ClampInside(ViewWindow outerWindow)
        {
            if (xMin < outerWindow.xMin) xMin = outerWindow.xMin;
            if (xMax > outerWindow.xMax) xMax = outerWindow.xMax;
            if (yMin < outerWindow.yMin) yMin = outerWindow.yMin;
            if (yMax > outerWindow.yMax) yMax = outerWindow.yMax;
        }

        // TODO: I don't care for this array...
        private static readonly Vector3[] boundCornerOffsets = {
            new Vector3 (1, 1, 1), new Vector3 (-1, 1, 1), new Vector3 (-1, -1, 1), new Vector3 (-1, -1, -1),
            new Vector3 (-1, 1, -1), new Vector3 (1, -1, -1), new Vector3 (1, 1, -1), new Vector3 (1, -1, 1),
        };

        public Rect GetRect() => Rect.MinMaxRect(xMin, yMin, xMax, yMax);

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

        /// <summary>Tries to get a window of this portal relative to a camera.</summary>
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

        public override string ToString()
        {
            return $"({xMin}<{xMax},{yMin}<{yMax},{zMin}<{zMax})";
        }
    }
}