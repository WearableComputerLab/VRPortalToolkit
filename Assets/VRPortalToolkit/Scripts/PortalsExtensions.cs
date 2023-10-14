using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit
{
    public static class PortalsExtensions
    {
        /// <summary>Returns the distance squared between a point and a point through the portals.</summary>
        public static float DistanceSqr<TPortal>(this IEnumerable<TPortal> portals, Vector3 from, Vector3 to) where TPortal : IPortal
        {
            portals.ModifyPoint(ref to);
            float x = from.x - to.x, y = from.y - to.y, z = from.z - to.z;
            return x * x + y * y + z * z;
        }

        /// <summary>Returns the distance between a point and a point through the portals.</summary>
        public static float Distance<TPortal>(this IEnumerable<TPortal> portals, Vector3 from, Vector3 to) where TPortal : IPortal
        {
            portals.ModifyPoint(ref to);
            return Vector3.Distance(from, to);
        }

        /// <summary>Modifies a Pose by travelling through the portals.</summary>
        public static bool ModifyPose<TPortal>(this IEnumerable<TPortal> portals, ref Pose pose) where TPortal : IPortal
        {
            bool modified = false;

            foreach (TPortal portal in portals)
            {
                if (portal != null)
                    modified |= portal.ModifyPose(ref pose);
            }

            return modified;
        }

        /// <summary>Returns a Pose after travelling through the portals.</summary>
        public static Pose ModifyPose<TPortal>(this IEnumerable<TPortal> portals, Pose pose) where TPortal : IPortal
        {
            portals.ModifyPose(ref pose);
            return pose;
        }

        /// <summary>Modifies a layermask by travelling through the portals.</summary>
        public static bool ModifyLayerMask<TPortal>(this IEnumerable<TPortal> portals, ref int layerMask) where TPortal : IPortal
        {
            bool modified = false;

            foreach (TPortal portal in portals)
            {
                if (portal != null)
                    modified |= portal.ModifyLayerMask(ref layerMask);
            }

            return modified;
        }

        /// <summary>Returns a layermask after travelling through the portals.</summary>
        public static int ModifyLayerMask<TPortal>(this IEnumerable<TPortal> portals, int layerMask) where TPortal : IPortal
        {
            portals.ModifyLayerMask(ref layerMask);
            return layerMask;
        }

        /// <summary>Modifies a layer by travelling through the portals.</summary>
        public static bool ModifyLayer<TPortal>(this IEnumerable<TPortal> portals, ref int layer) where TPortal : IPortal
        {
            bool modified = false;

            foreach (TPortal portal in portals)
            {
                if (portal != null)
                    modified |= portal.ModifyLayer(ref layer);
            }

            return modified;
        }

        /// <summary>Returns a layer after travelling through the portals.</summary>
        public static int ModifyLayer<TPortal>(this IEnumerable<TPortal> portals, int layer) where TPortal : IPortal
        {
            portals.ModifyLayer(ref layer);
            return layer;
        }

        /// <summary>Modifies a tag by travelling through the portals.</summary>
        public static bool ModifyTag<TPortal>(this IEnumerable<TPortal> portals, ref string tag) where TPortal : IPortal
        {
            bool modified = false;

            foreach (TPortal portal in portals)
            {
                if (portal != null)
                    modified |= portal.ModifyTag(ref tag);
            }

            return modified;
        }

        /// <summary>Returns a tag after travelling through the portals.</summary>
        public static string ModifyTag<TPortal>(this IEnumerable<TPortal> portals, string tag) where TPortal : IPortal
        {
            portals.ModifyTag(ref tag);
            return tag;
        }

        /// <summary>Modifies a matrix by travelling through the portals.</summary>
        public static bool ModifyMatrix<TPortal>(this IEnumerable<TPortal> portals, ref Matrix4x4 localToWorldMatrix) where TPortal : IPortal
        {
            bool modified = false;

            foreach (TPortal portal in portals)
            {
                if (portal != null)
                    modified |= portal.ModifyMatrix(ref localToWorldMatrix);
            }

            return modified;
        }

        /// <summary>Returns a matrix after travelling through the portals.</summary>
        public static Matrix4x4 ModifyMatrix<TPortal>(this IEnumerable<TPortal> portals, Matrix4x4 localToWorldMatrix) where TPortal : IPortal
        {
            portals.ModifyMatrix(ref localToWorldMatrix);
            return localToWorldMatrix;
        }

        /// <summary>Modifies a point by travelling through the portals.</summary>
        public static bool ModifyPoint<TPortal>(this IEnumerable<TPortal> portals, ref Vector3 point) where TPortal : IPortal
        {
            bool modified = false;

            foreach (TPortal portal in portals)
            {
                if (portal != null)
                    modified |= portal.ModifyPoint(ref point);
            }

            return modified;
        }

        /// <summary>Returns a point after travelling through the portals.</summary>
        public static Vector3 ModifyPoint<TPortal>(this IEnumerable<TPortal> portals, Vector3 point) where TPortal : IPortal
        {
            portals.ModifyPoint(ref point);
            return point;
        }

        /// <summary>Modifies a direction by travelling through the portals.</summary>
        public static bool ModifyDirection<TPortal>(this IEnumerable<TPortal> portals, ref Vector3 direction) where TPortal : IPortal
        {
            bool modified = false;

            foreach (TPortal portal in portals)
            {
                if (portal != null)
                    modified |= portal.ModifyDirection(ref direction);
            }

            return modified;
        }

        /// <summary>Returns a direction after travelling through the portals.</summary>
        public static Vector3 ModifyDirection<TPortal>(this IEnumerable<TPortal> portals, Vector3 direction) where TPortal : IPortal
        {
            portals.ModifyDirection(ref direction);
            return direction;
        }

        /// <summary>Modifies a direction by travelling through the portals.</summary>
        public static bool ModifyVector<TPortal>(this IEnumerable<TPortal> portals, ref Vector3 vector) where TPortal : IPortal
        {
            bool modified = false;

            foreach (TPortal portal in portals)
            {
                if (portal != null)
                    modified |= portal.ModifyVector(ref vector);
            }

            return modified;
        }

        /// <summary>Returns a vector after travelling through the portals.</summary>
        public static Vector3 ModifyVector<TPortal>(this IEnumerable<TPortal> portals, Vector3 vector) where TPortal : IPortal
        {
            portals.ModifyVector(ref vector);
            return vector;
        }

        /// <summary>Modifies a direction by travelling through the portals.</summary>
        public static bool ModifyRotation<TPortal>(this IEnumerable<TPortal> portals, ref Quaternion rotation) where TPortal : IPortal
        {
            bool modified = false;

            foreach (TPortal portal in portals)
            {
                if (portal != null)
                    modified |= portal.ModifyRotation(ref rotation);
            }

            return modified;
        }

        /// <summary>Returns a rotation after travelling through the portals.</summary>
        public static Quaternion ModifyRotation<TPortal>(this IEnumerable<TPortal> portals, Quaternion rotation) where TPortal : IPortal
        {
            portals.ModifyRotation(ref rotation);
            return rotation;
        }

        /// <summary>Modifies a transform by the portal.</summary>
        public static void ModifyTransform<TPortal>(this IEnumerable<TPortal> portals, Transform transform, bool includeScale = true) where TPortal : IPortal
        {
            if (transform)
            {
                Matrix4x4 localToWorld = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

                foreach (TPortal portal in portals)
                {
                    if (portal != null)
                        portal.ModifyMatrix(ref localToWorld);
                }

                transform.SetPositionAndRotation(localToWorld.GetColumn(3), localToWorld.rotation);

                if (includeScale)
                    transform.localScale = localToWorld.lossyScale;
            }
        }

        /// <summary>Modifies a transform by the portal.</summary>
        public static void ModifyRigidbody<TPortal>(this IEnumerable<TPortal> portals, Rigidbody rigidbody, bool includeScale = false) where TPortal : IPortal
        {
            if (rigidbody)
            {
                //Matrix4x4 localToWorld = Matrix4x4.TRS(rigidbody.transform.position, rigidbody.transform.rotation, rigidbody.transform.localScale);
                Matrix4x4 localToWorld = Matrix4x4.TRS(rigidbody.position, rigidbody.rotation, rigidbody.transform.localScale);
                Vector3 velocity = rigidbody.velocity, angularVelocity = rigidbody.angularVelocity;

                foreach (TPortal portal in portals)
                {
                    if (portal != null)
                    {
                        portal.ModifyMatrix(ref localToWorld);

                        portal.ModifyVector(ref velocity);
                        portal.ModifyVector(ref angularVelocity);
                    }
                }

                //rigidbody.transform.SetPositionAndRotation(localToWorld.GetColumn(3), localToWorld.rotation);
                rigidbody.position = localToWorld.GetColumn(3);
                rigidbody.rotation = localToWorld.rotation;

                if (includeScale)
                    rigidbody.transform.localScale = localToWorld.lossyScale;

                if (!rigidbody.isKinematic)
                {
                    rigidbody.velocity = velocity;
                    rigidbody.angularVelocity = angularVelocity;
                }
            }
        }

        /// <summary>
        /// Gets the difference in portals given an object that is "portals" away from another object that is "other" away
        /// </summary>
        public static IEnumerable<TPortal> Difference<TPortal>(this IEnumerable<TPortal> portals, IEnumerable<TPortal> other) where TPortal : IPortal
        {
            if (portals != null)
            {
                IEnumerator<TPortal> toEnumerator = other != null ? other.GetEnumerator() : null;

                if (toEnumerator != null && toEnumerator.MoveNext())
                {
                    foreach (TPortal portal in portals)
                    {
                        if (ReferenceEquals(portal, toEnumerator.Current))
                        {
                            if (!toEnumerator.MoveNext())
                                yield break;
                            break;
                        }

                        yield return portal;
                    }

                    do
                    {
                        yield return toEnumerator.Current;
                    } while (toEnumerator.MoveNext());
                }
                else
                {
                    foreach (TPortal portal in portals)
                        yield return portal;
                }
            }
            else if (other != null)
            {
                foreach (TPortal portal in other)
                    yield return portal;
            }
        }
    }
}
