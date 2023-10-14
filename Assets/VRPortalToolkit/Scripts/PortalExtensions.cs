using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit
{
    public static class PortalExtensions
    {
        /// <summary>Returns the distance squared between a point and a point through the portal.</summary>
        public static float DistanceSqr(this IPortal portal, Vector3 from, Vector3 to)
        {
            portal.ModifyPoint(ref from);
            float x = from.x - to.x, y = from.y - to.y, z = from.z - to.z;
            return x * x + y * y + z * z;
        }

        /// <summary>Returns the distance between a point and a point through the portal.</summary>
        public static float Distance(this IPortal portal, Vector3 from, Vector3 to)
        {
            portal.ModifyPoint(ref from);
            return Vector3.Distance(from, to);
        }

        /// <summary>Modifies a Pose by travelling through the portal.</summary>
        public static bool ModifyPose(this IPortal portal, ref Pose pose)
        {
            bool modified = portal.ModifyPoint(ref pose.position);
            modified |= portal.ModifyRotation(ref pose.rotation);
            return modified;
        }

        /// <summary>Returns a Pose after travelling through the portal.</summary>
        public static Pose ModifyPose(this IPortal portal, Pose pose)
        {
            portal.ModifyPose(ref pose);
            return pose;
        }

        /// <summary>Returns a layermask after travelling through the portal.</summary>
        public static int ModifyLayerMask(this IPortal portal, int layerMask)
        {
            portal.ModifyLayerMask(ref layerMask);
            return layerMask;
        }

        /// <summary>Returns a layer after travelling through the portal.</summary>
        public static int ModifyLayer(this IPortal portal, int layer)
        {
            portal.ModifyLayer(ref layer);
            return layer;
        }

        /// <summary>Returns a tag after travelling through the portal.</summary>
        public static string ModifyTag(this IPortal portal, string tag)
        {
            portal.ModifyTag(ref tag);
            return tag;
        }

        /// <summary>Returns a matrix after travelling through the portal.</summary>
        public static Matrix4x4 ModifyMatrix(this IPortal portal, Matrix4x4 localToWorldMatrix)
        {
            portal.ModifyMatrix(ref localToWorldMatrix);
            return localToWorldMatrix;
        }

        /// <summary>Returns a point after travelling through the portal.</summary>
        public static Vector3 ModifyPoint(this IPortal portal, Vector3 point)
        {
            portal.ModifyPoint(ref point);
            return point;
        }

        /// <summary>Returns a direction after travelling through the portal.</summary>
        public static Vector3 ModifyDirection(this IPortal portal, Vector3 direction)
        {
            portal.ModifyDirection(ref direction);
            return direction;
        }

        /// <summary>Returns a vector after travelling through the portal.</summary>
        public static Vector3 ModifyVector(this IPortal portal, Vector3 vector)
        {
            portal.ModifyVector(ref vector);
            return vector;
        }

        /// <summary>Returns a rotation after travelling through the portal.</summary>
        public static Quaternion ModifyRotation(this IPortal portal, Quaternion rotation)
        {
            portal.ModifyRotation(ref rotation);
            return rotation;
        }

        /// <summary>Modifies a transform by the portal.</summary>
        public static void ModifyTransform(this IPortal portal, Transform transform, bool includeScale = true)
        {
            if (transform)
            {
                Matrix4x4 localToWorld = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

                portal.ModifyMatrix(ref localToWorld);

                transform.SetPositionAndRotation(localToWorld.GetColumn(3), localToWorld.rotation);

                if (includeScale)
                    transform.localScale = localToWorld.lossyScale;
            }
        }

        /// <summary>Modifies a transform by the portal.</summary>
        public static void ModifyRigidbody(this IPortal portal, Rigidbody rigidbody, bool includeScale = true)
        {
            if (rigidbody)
            {
                //Matrix4x4 localToWorld = Matrix4x4.TRS(rigidbody.transform.position, rigidbody.transform.rotation, rigidbody.transform.localScale);
                Matrix4x4 localToWorld = Matrix4x4.TRS(rigidbody.position, rigidbody.rotation, rigidbody.transform.localScale);

                portal.ModifyMatrix(ref localToWorld);
                
                //rigidbody.transform.SetPositionAndRotation(localToWorld.GetColumn(3), localToWorld.rotation);
                rigidbody.position = localToWorld.GetColumn(3);
                rigidbody.rotation = localToWorld.rotation;

                if (!rigidbody.isKinematic)
                {
                    Vector3 velocity = rigidbody.velocity, angularVelocity = rigidbody.angularVelocity;

                    portal.ModifyVector(ref velocity);
                    portal.ModifyVector(ref angularVelocity);

                    rigidbody.velocity = velocity;
                    rigidbody.angularVelocity = angularVelocity;
                }
            }
        }
    }
}
