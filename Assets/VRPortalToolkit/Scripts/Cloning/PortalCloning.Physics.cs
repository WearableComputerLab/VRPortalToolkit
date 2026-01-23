using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    /// <summary>
    /// Physics-specific extensions for the PortalCloning class.
    /// Provides utilities for updating physics components of cloned objects.
    /// </summary>
    public static partial class PortalCloning
    {
        /// <summary>
        /// Updates a cloned Rigidbody to match its original counterpart.
        /// </summary>
        /// <param name="clone">The cloned Rigidbody to update.</param>
        /// <returns>True if the clone was successfully updated, false otherwise.</returns>
        public static bool UpdateRigidbody(Rigidbody clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Rigidbody> cloneInfo))
            {
                UpdateRigidbody(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates a cloned Rigidbody to match its original counterpart using the provided clone info.
        /// Properly transforms position, rotation, velocity, and angular velocity through portals.
        /// </summary>
        /// <param name="cloneInfo">The clone information containing the original and clone Rigidbody.</param>
        public static void UpdateRigidbody(this PortalCloneInfo<Rigidbody> cloneInfo)
        {
            Rigidbody original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                clone.isKinematic = original.isKinematic;
                clone.mass = original.mass;
                clone.linearDamping = original.linearDamping;
                clone.angularDamping = original.angularDamping;
                clone.useGravity = original.useGravity;
                clone.interpolation = original.interpolation;
                clone.collisionDetectionMode = original.collisionDetectionMode;
                clone.inertiaTensor = original.inertiaTensor;
                clone.inertiaTensorRotation = original.inertiaTensorRotation;

                Matrix4x4 localToWorld = Matrix4x4.TRS(original.position, original.rotation, original.transform.localScale);

                if (original.isKinematic)
                {
                    for (int i = 0; i < cloneInfo.PortalCount; i++)
                        cloneInfo.GetOriginalToClonePortal(i)?.ModifyMatrix(ref localToWorld);

                    clone.position = localToWorld.GetColumn(3);
                    clone.rotation = localToWorld.rotation;
                }
                else
                {
                    Vector3 velocity = original.linearVelocity, angularVelocity = original.angularVelocity;

                    for (int i = 0; i < cloneInfo.PortalCount; i++)
                    {
                        Portal portal = cloneInfo.GetOriginalToClonePortal(i);

                        if (portal != null)
                        {
                            portal.ModifyMatrix(ref localToWorld);
                            portal.ModifyVector(ref velocity);
                            portal.ModifyVector(ref angularVelocity);
                        }
                    }

                    clone.MovePosition(localToWorld.GetColumn(3));
                    clone.MoveRotation(localToWorld.rotation);

                    clone.linearVelocity = velocity;
                    clone.angularVelocity = angularVelocity;
                }

                clone.transform.localScale = localToWorld.lossyScale;
            }
        }

        /// <summary>
        /// Updates a cloned Collider to match its original counterpart.
        /// </summary>
        /// <param name="clone">The cloned Collider to update.</param>
        /// <returns>True if the clone was successfully updated, false otherwise.</returns>
        public static bool UpdateCollider(Collider clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Collider> cloneInfo))
            {
                UpdateCollider(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates a cloned Collider to match its original counterpart using the provided clone info.
        /// Handles specific collider types appropriately.
        /// </summary>
        /// <typeparam name="TCollider">The type of collider.</typeparam>
        /// <param name="cloneInfo">The clone information containing the original and clone Collider.</param>
        public static void UpdateCollider<TCollider>(this PortalCloneInfo<TCollider> cloneInfo) where TCollider : Collider
        {
            Collider original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                if (original is SphereCollider originalS && clone is SphereCollider cloneS)
                    UpdateSphereCollider(originalS, cloneS);
                if (original is BoxCollider originalB && clone is BoxCollider cloneB)
                    UpdateBoxCollider(originalB, cloneB);
                else if (original is CapsuleCollider originalC && clone is CapsuleCollider cloneC)
                    UpdateCapsuleCollider(originalC, cloneC);
                else if (original is MeshCollider originalM && clone is MeshCollider cloneM)
                    UpdateMeshCollider(originalM, cloneM);
                else if (original is CharacterController originalCC && clone is CharacterController cloneCC)
                    UpdateCharacterController(originalCC, cloneCC);
                else
                    UpdateCollider(original, clone);
            }
        }

        /// <summary>
        /// Updates a cloned SphereCollider to match its original counterpart.
        /// </summary>
        /// <param name="clone">The cloned SphereCollider to update.</param>
        /// <returns>True if the clone was successfully updated, false otherwise.</returns>
        public static bool UpdateCollider(SphereCollider clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<SphereCollider> cloneInfo))
            {
                UpdateCollider(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates a cloned SphereCollider to match its original counterpart using the provided clone info.
        /// </summary>
        /// <param name="cloneInfo">The clone information containing the original and clone SphereCollider.</param>
        public static void UpdateCollider(this PortalCloneInfo<SphereCollider> cloneInfo)
        {
            SphereCollider original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateSphereCollider(original, clone);
        }

        /// <summary>
        /// Updates a cloned BoxCollider to match its original counterpart.
        /// </summary>
        /// <param name="clone">The cloned BoxCollider to update.</param>
        /// <returns>True if the clone was successfully updated, false otherwise.</returns>
        public static bool UpdateCollider(BoxCollider clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<BoxCollider> cloneInfo))
            {
                UpdateCollider(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates a cloned BoxCollider to match its original counterpart using the provided clone info.
        /// </summary>
        /// <param name="cloneInfo">The clone information containing the original and clone BoxCollider.</param>
        public static void UpdateCollider(this PortalCloneInfo<BoxCollider> cloneInfo)
        {
            BoxCollider original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateBoxCollider(original, clone);
        }

        /// <summary>
        /// Updates a cloned CapsuleCollider to match its original counterpart.
        /// </summary>
        /// <param name="clone">The cloned CapsuleCollider to update.</param>
        /// <returns>True if the clone was successfully updated, false otherwise.</returns>
        public static bool UpdateCollider(CapsuleCollider clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<CapsuleCollider> cloneInfo))
            {
                UpdateCollider(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates a cloned CapsuleCollider to match its original counterpart using the provided clone info.
        /// </summary>
        /// <param name="cloneInfo">The clone information containing the original and clone CapsuleCollider.</param>
        public static void UpdateCollider(this PortalCloneInfo<CapsuleCollider> cloneInfo)
        {
            CapsuleCollider original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateCapsuleCollider(original, clone);
        }

        /// <summary>
        /// Updates a cloned MeshCollider to match its original counterpart.
        /// </summary>
        /// <param name="clone">The cloned MeshCollider to update.</param>
        /// <returns>True if the clone was successfully updated, false otherwise.</returns>
        public static bool UpdateCollider(MeshCollider clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<MeshCollider> cloneInfo))
            {
                UpdateCollider(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates a cloned MeshCollider to match its original counterpart using the provided clone info.
        /// </summary>
        /// <param name="cloneInfo">The clone information containing the original and clone MeshCollider.</param>
        public static void UpdateCollider(this PortalCloneInfo<MeshCollider> cloneInfo)
        {
            MeshCollider original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateMeshCollider(original, clone);
        }

        /// <summary>
        /// Updates a cloned CharacterController to match its original counterpart.
        /// </summary>
        /// <param name="clone">The cloned CharacterController to update.</param>
        /// <returns>True if the clone was successfully updated, false otherwise.</returns>
        public static bool UpdateCollider(CharacterController clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<CharacterController> cloneInfo))
            {
                UpdateCollider(cloneInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates a cloned CharacterController to match its original counterpart using the provided clone info.
        /// </summary>
        /// <param name="cloneInfo">The clone information containing the original and clone CharacterController.</param>
        public static void UpdateCollider(this PortalCloneInfo<CharacterController> cloneInfo)
        {
            CharacterController original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateCharacterController(original, clone);
        }

        private static void UpdateSphereCollider(SphereCollider original, SphereCollider clone)
        {
            clone.center = original.center;
            clone.radius = original.radius;

            UpdateCollider(original, clone);
        }

        private static void UpdateBoxCollider(BoxCollider original, BoxCollider clone)
        {
            clone.center = original.center;
            clone.size = original.size;

            UpdateCollider(original, clone);
        }

        private static void UpdateCapsuleCollider(CapsuleCollider original, CapsuleCollider clone)
        {
            UpdateCollider(original, clone);
        }

        private static void UpdateMeshCollider(MeshCollider original, MeshCollider clone)
        {
            clone.sharedMesh = original.sharedMesh;
            clone.convex = original.convex;

            UpdateCollider(original, clone);
        }

        private static void UpdateCharacterController(CharacterController original, CharacterController clone)
        {
            UpdateCollider(original, clone);
        }

        private static void UpdateCollider(Collider original, Collider clone)
        {
            clone.enabled = original.enabled;
            clone.isTrigger = original.isTrigger;
            clone.enabled = original.enabled;
            clone.contactOffset = original.contactOffset;
            //clone.hasModifiableContacts = original.hasModifiableContacts;
            clone.enabled = original.enabled;
            clone.sharedMaterial = original.sharedMaterial;
        }
    }
}
