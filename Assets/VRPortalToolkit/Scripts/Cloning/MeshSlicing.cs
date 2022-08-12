using EzySlice;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    public static class MeshSlicing
    {
        public static bool Slice(Vector3[] vertices, Vector3[] uv, Vector3[] normals, Vector4[] tangents, int[][] triangles, int vertsCount, int submeshCount, int[] triangleCount, UnityEngine.Plane[] cuttingPlanes, int cuttingPlanesCount, int crossIndex, Rect uvRect, out Mesh newMesh, out bool hasInside)
        {
            if (vertices == null || vertsCount > vertices.Length || vertsCount <= 0
                || triangles == null || submeshCount > triangles.Length || submeshCount <= 0
                || cuttingPlanes == null || cuttingPlanesCount > cuttingPlanes.Length || cuttingPlanesCount <= 0)
            {
                newMesh = null;
                hasInside = cuttingPlanes == null || cuttingPlanes.Length == 0 || cuttingPlanesCount == 0;
                return false;
            }

            bool sliced = false;

            // each submesh will be sliced and placed in its own array structure
            List<Triangle>[] slices = new List<Triangle>[submeshCount];
            // the cross section hull is common across all submeshes
            List<Vector3>[] crossHulls = new List<Vector3>[cuttingPlanesCount];

            for (int i = 0; i < cuttingPlanesCount; i++)
                crossHulls[i] = new List<Vector3>();

            // we reuse this object for all intersection tests
            IntersectionResult result = new IntersectionResult();

            // see if we would like to split the mesh using uv, normals and tangents
            bool genUV = uv != null && uv.Length <= vertsCount;
            bool genNorm = normals != null && normals.Length <= vertsCount;
            bool genTan = tangents != null && tangents.Length <= vertsCount;

            int[] indices;
            int indicesCount, upperHullCount, interHullCount, planeIndex, meshTriangleCount, index, i0, i1, i2;
            List<Triangle> mesh;
            UnityEngine.Plane cuttingPlane;
            EzySlice.Plane plane;
            List<Vector3> crossHull;
            Triangle newTri;

            // iterate over all the submeshes individually. vertices and indices
            // are all shared within the submesh
            for (int submesh = 0; submesh < submeshCount; submesh++)
            {
                indices = triangles[submesh];
                indicesCount = triangleCount != null ? triangleCount[submesh] : indices.Length;

                if (indicesCount > indices.Length || indicesCount <= 0)
                    continue;

                slices[submesh] = mesh = new List<Triangle>();
                cuttingPlane = cuttingPlanes[0];
                plane = new EzySlice.Plane(cuttingPlane.normal, -cuttingPlane.distance);
                crossHull = crossHulls[0];

                // loop through all the mesh vertices, generating upper and lower hulls
                // and all intersection points
                for (index = 0; index < indicesCount; index += 3)
                {
                    i0 = indices[index + 0];
                    i1 = indices[index + 1];
                    i2 = indices[index + 2];

                    newTri = new Triangle(vertices[i0], vertices[i1], vertices[i2]);

                    // generate UV if available
                    if (genUV) newTri.SetUV(uv[i0], uv[i1], uv[i2]);

                    // generate normals if available
                    if (genNorm) newTri.SetNormal(normals[i0], normals[i1], normals[i2]);

                    // generate tangents if available
                    if (genTan) newTri.SetTangent(tangents[i0], tangents[i1], tangents[i2]);

                    // slice this particular triangle with the provided
                    if (newTri.Split(plane, result))
                    {
                        sliced = true;

                        upperHullCount = result.upperHullCount;
                        interHullCount = result.intersectionPointCount;

                        for (int i = 0; i < upperHullCount; i++)
                            mesh.Add(result.upperHull[i]);

                        for (int i = 0; i < interHullCount; i++)
                            crossHull.Add(result.intersectionPoints[i]);
                    }
                    else if (plane.SideOf(vertices[i0]) != SideOfPlane.DOWN)
                        mesh.Add(newTri);
                }

                for (planeIndex = 1; planeIndex < cuttingPlanesCount; planeIndex++)
                {
                    // Continue slicing for the remainding planes
                    cuttingPlane = cuttingPlanes[planeIndex];
                    plane = new EzySlice.Plane(cuttingPlane.normal, -cuttingPlane.distance);
                    crossHull = crossHulls[planeIndex];

                    // Remove all the uncessessary triangles
                    meshTriangleCount = mesh.Count;

                    for (index = 0; index < meshTriangleCount; index++)
                    {
                        newTri = mesh[index];

                        if (newTri.Split(plane, result))
                        {
                            sliced = true;

                            mesh.RemoveAt(index);
                            meshTriangleCount--;
                            index--;

                            upperHullCount = result.upperHullCount;
                            interHullCount = result.intersectionPointCount;

                            for (int i = 0; i < upperHullCount; i++)
                                mesh.Add(result.upperHull[i]);

                            for (int i = 0; i < interHullCount; i++)
                                crossHull.Add(result.intersectionPoints[i]);
                        }
                        else if (plane.SideOf(newTri.positionA) == SideOfPlane.DOWN)
                        {
                            mesh.RemoveAt(index);
                            meshTriangleCount--;
                            index--;
                        }
                    }
                }
            }

            if (sliced)
            {
                hasInside = true;
                Triangle triangle;
                List<Triangle>[] crossSections = new List<Triangle>[cuttingPlanesCount];
                List<Triangle> crossSection;
                TextureRegion region = new TextureRegion(uvRect.xMin, uvRect.yMin, uvRect.xMax, uvRect.yMax);

                // get the total amount of upper, lower and intersection counts
                int newTriangleCount = 0, crossSectionsCount = 0;
                foreach (List<Triangle> subMesh in slices)
                    newTriangleCount += subMesh.Count;

                int otherPlaneIndex, crossSectionIndex, crossSectionCount;
                for (planeIndex = 0; planeIndex < cuttingPlanesCount; planeIndex++)
                {
                    cuttingPlane = cuttingPlanes[planeIndex];
                    plane = new EzySlice.Plane(cuttingPlane.normal, -cuttingPlane.distance);

                    crossSections[planeIndex] = crossSection = null;//CreateFrom(crossHulls[planeIndex], plane.normal, region);

                    if (crossSection != null)
                    {
                        // TODO: This doesnt actually work for multiple slices, but I'm too lazy at the moment to figure out a better one
                        // Also dont know know if the order of these indices matter
                        for (otherPlaneIndex = 0; otherPlaneIndex < cuttingPlanesCount; otherPlaneIndex++)
                        {
                            if (planeIndex == otherPlaneIndex) continue;

                            crossSectionCount = crossSection.Count;

                            for (crossSectionIndex = 0; crossSectionIndex < crossSectionCount; crossSectionIndex++)
                            {
                                triangle = crossSection[crossSectionIndex];

                                if (triangle.Split(plane, result))
                                {
                                    crossSection.RemoveAt(crossSectionIndex);
                                    crossSectionIndex--;
                                    crossSectionCount--;

                                    upperHullCount = result.upperHullCount;

                                    for (int i = 0; i < upperHullCount; i++)
                                        crossSection.Add(result.upperHull[i]);
                                }
                                else if (plane.SideOf(triangle.positionA) == SideOfPlane.DOWN)
                                {
                                    crossSection.RemoveAt(crossSectionIndex);
                                    crossSectionIndex--;
                                    crossSectionCount--;
                                }
                            }
                        }

                        crossSectionsCount += crossSection.Count;
                    }
                }

                newMesh = CreateHull(slices, newTriangleCount, genUV, genNorm, genTan, crossSections, crossSectionsCount, crossIndex);
                return true;
            }

            // no slicing occured, just return null to signify
            newMesh = null;

            foreach (List<Triangle> subMesh in slices)
            {
                if (subMesh.Count > 0)
                {
                    hasInside = true;
                    return false;
                }
            }

            hasInside = true;
            return false;
        }

        /**
         * Generate a single Mesh HULL of either the UPPER or LOWER hulls. 
         */
        private static Mesh CreateHull(List<Triangle>[] meshes, int meshesTriangleCount, bool hasUV, bool hasNormal, bool hasTangent, List<Triangle>[] crossSections, int crossSectionsTriangleCount, int crossIndex)
        {
            if (meshesTriangleCount <= 0)
                return null;

            int submeshCount = meshes.Length;
            int crossCount = crossSections != null ? crossSectionsTriangleCount : 0;

            Mesh newMesh = new Mesh();
            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            int arrayLen = (meshesTriangleCount + crossCount) * 3;

            // vertices and uv's are common for all submeshes
            Vector3[] newVertices = new Vector3[arrayLen];
            Vector2[] newUvs = hasUV ? new Vector2[arrayLen] : null;
            Vector3[] newNormals = hasNormal ? new Vector3[arrayLen] : null;
            Vector4[] newTangents = hasTangent ? new Vector4[arrayLen] : null;

            // each index refers to our submesh triangles
            List<int[]> triangles = new List<int[]>(submeshCount);

            int vIndex = 0;

            // first we generate all our vertices, uv's and triangles
            for (int submesh = 0; submesh < submeshCount; submesh++)
            {
                // pick the hull we will be playing around with
                List<Triangle> hull = meshes[submesh];

                if (hull != null)
                {
                    int hullCount = hull.Count;

                    int[] indices = new int[hullCount * 3];

                    // fill our mesh arrays
                    for (int i = 0, triIndex = 0; i < hullCount; i++, triIndex += 3)
                    {
                        Triangle newTri = hull[i];

                        int i0 = vIndex + 0;
                        int i1 = vIndex + 1;
                        int i2 = vIndex + 2;

                        // add the vertices
                        newVertices[i0] = newTri.positionA;
                        newVertices[i1] = newTri.positionB;
                        newVertices[i2] = newTri.positionC;

                        // add the UV coordinates if any
                        if (hasUV)
                        {
                            newUvs[i0] = newTri.uvA;
                            newUvs[i1] = newTri.uvB;
                            newUvs[i2] = newTri.uvC;
                        }

                        // add the Normals if any
                        if (hasNormal)
                        {
                            newNormals[i0] = newTri.normalA;
                            newNormals[i1] = newTri.normalB;
                            newNormals[i2] = newTri.normalC;
                        }

                        // add the Tangents if any
                        if (hasTangent)
                        {
                            newTangents[i0] = newTri.tangentA;
                            newTangents[i1] = newTri.tangentB;
                            newTangents[i2] = newTri.tangentC;
                        }

                        // triangles are returned in clocwise order from the
                        // intersector, no need to sort these
                        indices[triIndex] = i0;
                        indices[triIndex + 1] = i1;
                        indices[triIndex + 2] = i2;

                        vIndex += 3;
                    }

                    // add triangles to the index for later generation
                    triangles.Add(indices);
                }
            }

            // generate the cross section required for this particular hull
            if (crossSections != null && crossCount > 0)
            {
                int crossSectionCount, triIndex = 0;
                int[] crossIndices = new int[crossCount * 3];
                List<Triangle> crossSection;

                for (int crossSectionIndex = 0; crossSectionIndex < crossSections.Length; crossSectionIndex++)
                {
                    crossSection = crossSections[crossSectionIndex];

                    if (crossSection != null)
                    {
                        crossSectionCount = crossSection.Count;
                        for (int i = 0; i < crossSectionCount; i++, triIndex += 3)
                        {
                            Triangle newTri = crossSection[i];

                            int i0 = vIndex + 0;
                            int i1 = vIndex + 1;
                            int i2 = vIndex + 2;

                            // add the vertices
                            newVertices[i0] = newTri.positionA;
                            newVertices[i1] = newTri.positionB;
                            newVertices[i2] = newTri.positionC;

                            // add the UV coordinates if any
                            if (hasUV)
                            {
                                newUvs[i0] = newTri.uvA;
                                newUvs[i1] = newTri.uvB;
                                newUvs[i2] = newTri.uvC;
                            }

                            // add the Normals if any
                            if (hasNormal)
                            {
                                newNormals[i0] = -newTri.normalA;
                                newNormals[i1] = -newTri.normalB;
                                newNormals[i2] = -newTri.normalC;
                            }

                            // add the Tangents if any
                            if (hasTangent)
                            {
                                newTangents[i0] = newTri.tangentA;
                                newTangents[i1] = newTri.tangentB;
                                newTangents[i2] = newTri.tangentC;
                            }

                            // add triangles in clockwise for upper
                            // and reversed for lower hulls, to ensure the mesh
                            // is facing the right direction=
                            crossIndices[triIndex] = i0;
                            crossIndices[triIndex + 1] = i1;
                            crossIndices[triIndex + 2] = i2;

                            vIndex += 3;
                        }
                    }
                }

                // add triangles to the index for later generation
                if (triangles.Count <= crossIndex)
                    triangles.Add(crossIndices);
                else
                {
                    // otherwise, we need to merge the triangles for the provided subsection
                    int[] prevTriangles = triangles[crossIndex];
                    int[] merged = new int[prevTriangles.Length + crossIndices.Length];

                    System.Array.Copy(prevTriangles, merged, prevTriangles.Length);
                    System.Array.Copy(crossIndices, 0, merged, prevTriangles.Length, crossIndices.Length);

                    // replace the previous array with the new merged array
                    triangles[crossIndex] = merged;
                }
            }

            int totalTriangles = triangles.Count;

            newMesh.subMeshCount = totalTriangles;
            // fill the mesh structure
            newMesh.vertices = newVertices;

            if (hasUV) newMesh.uv = newUvs;

            if (hasNormal) newMesh.normals = newNormals;

            if (hasTangent) newMesh.tangents = newTangents;

            // add the submeshes
            for (int i = 0; i < totalTriangles; i++)
                newMesh.SetTriangles(triangles[i], i, false);

            return newMesh;
        }

        /**
         * Generate Two Meshes (an upper and lower) cross section from a set of intersection
         * points and a plane normal. Intersection Points do not have to be in order.
         */
        private static List<Triangle> CreateFrom(List<Vector3> intPoints, Vector3 planeNormal, TextureRegion region)
        {
            if (Triangulator.MonotoneChain(intPoints, planeNormal, out List<Triangle> tris, region))
                return tris;

            return null;
        }
    }
}
