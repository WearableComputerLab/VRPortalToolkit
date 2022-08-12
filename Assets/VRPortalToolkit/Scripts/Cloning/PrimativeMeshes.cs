using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    public static class PrimativeMeshes
    {
        private static Mesh[] primitiveMeshes;

        public static Mesh Get(PrimitiveType type)
        {
            if (primitiveMeshes == null) primitiveMeshes = new Mesh[6];

            Mesh mesh = primitiveMeshes[(int)type];

            if (mesh == null) primitiveMeshes[(int)type] = mesh = CreatePrimitiveMesh(type);

            return mesh;
        }

        private static Mesh CreatePrimitiveMesh(PrimitiveType type)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(gameObject);

            return mesh;
        }
    }
}
