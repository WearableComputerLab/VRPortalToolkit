using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Rendering
{
    public static class PortalCameraPlane
    {
        private static Dictionary<Camera, PortalPlanes> cameraPlanes = new Dictionary<Camera, PortalPlanes>();

        private class PortalPlanes
        {
            public Camera camera { get; protected set; }

            //private bool sorted = false;

            //private Vector3 lastCameraPosition;

            private List<PortalPlane> planes = new List<PortalPlane>();

            public void AddPlane(PortalPlane plane)
            {
                if (plane.renderer)
                {
                    int index = planes.FindIndex(i => i.renderer == plane.renderer);

                    if (index >= 0)
                        planes[index] = plane;
                    else
                        planes.Add(plane);

                    //sorted = false;
                }
            }

            public void RemovePlane(PortalPlane plane)
            {
                if (plane.renderer)
                {
                    int index = planes.FindIndex(i => i.renderer == plane.renderer);

                    if (index >= 0)
                        planes[index] = plane;
                    else
                        planes.Add(plane);

                    //sorted = false;
                }
            }
        }

        private class PortalPlane
        {
            public PortalRenderer renderer;

            public Vector3 position;

            public Vector3 normal;

            public PortalPlane(PortalRenderer renderer, Vector3 position, Vector3 normal)
            {
                this.renderer = renderer;
                this.position = position;
                this.normal = normal;
            }
        }

        public static void AddPlane(Camera camera, PortalRenderer renderer, Vector3 position, Vector3 normal)
        {
            //cameraPlanes[camera] = new PortalPlane(renderer, position, normal);
        }


        public static void ClearPlane(Camera camera)
        {
            cameraPlanes.Remove(camera);
        }

        public static bool HasPlane(Camera camera)
        {
            //if (cameraPlanes.TryGetValue(camera, out PortalPlane plane))
            //    return plane.renderer;

            return false;
        }

        public static bool TryGetPlane(Camera camera, out PortalRenderer renderer, out Vector3 position, out Vector3 normal)
        {
            //if (cameraPlanes.TryGetValue(camera, out PortalPlane plane) && plane.renderer)
            //{
            //    renderer = plane.renderer;
            //    position = plane.position;
            //    normal = plane.normal;
            //    return true;
            //}

            renderer = null;
            position = Vector3.zero;
            normal = Vector3.forward;
            return false;
        }
    }
}
