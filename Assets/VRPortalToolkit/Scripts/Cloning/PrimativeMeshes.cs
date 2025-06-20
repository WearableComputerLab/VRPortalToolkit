using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    /// <summary>
    /// Utility class for accessing and caching primitive mesh types.
    /// </summary>
    public static class PrimativeMeshes
    {
        /// <summary>
        /// Cached array of primitive meshes.
        /// </summary>
        private static Mesh[] primitiveMeshes;

        /// <summary>
        /// Gets a cached mesh for a specific primitive type.
        /// If the requested mesh type hasn't been cached yet, it will be created.
        /// </summary>
        /// <param name="type">The Unity primitive type to get the mesh for.</param>
        /// <returns>A shared mesh instance for the specified primitive type.</returns>
        public static Mesh Get(PrimitiveType type)
        {
            if (primitiveMeshes == null) primitiveMeshes = new Mesh[6];

            Mesh mesh = primitiveMeshes[(int)type];

            if (mesh == null) primitiveMeshes[(int)type] = mesh = CreatePrimitiveMesh(type);

            return mesh;
        }

        /// <summary>
        /// Creates a new mesh for a primitive type;
        /// </summary>
        /// <param name="type">The Unity primitive type to create a mesh for.</param>
        /// <returns>A new mesh instance for the specified primitive type.</returns>
        private static Mesh CreatePrimitiveMesh(PrimitiveType type)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(gameObject);

            return mesh;
        }
    }
}
