using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    public static partial class PortalCloning
    {
        // TODO: Could do wheel colliders, could do terrain mesh
        // TODO: Could do joints
        public static bool UpdateRigidbody(Rigidbody clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Rigidbody> cloneInfo))
            {
                UpdateRigidbody(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateRigidbody(PortalCloneInfo<Rigidbody> cloneInfo)
        {
            Rigidbody original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                clone.isKinematic = original.isKinematic;
                clone.mass = original.mass;
                clone.drag = original.drag;
                clone.angularDrag = original.angularDrag;
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
                    Vector3 velocity = original.velocity, angularVelocity = original.angularVelocity;

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

                    clone.velocity = velocity;
                    clone.angularVelocity = angularVelocity;
                }

                clone.transform.localScale = localToWorld.lossyScale;
            }
        }

        public static bool UpdateCollider(Collider clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Collider> cloneInfo))
            {
                UpdateCollider(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateCollider<TCollider>(PortalCloneInfo<TCollider> cloneInfo) where TCollider : Collider
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

        public static bool UpdateCollider(SphereCollider clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<SphereCollider> cloneInfo))
            {
                UpdateCollider(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateCollider(PortalCloneInfo<SphereCollider> cloneInfo)
        {
            SphereCollider original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateSphereCollider(original, clone);
        }

        public static bool UpdateCollider(BoxCollider clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<BoxCollider> cloneInfo))
            {
                UpdateCollider(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateCollider(PortalCloneInfo<BoxCollider> cloneInfo)
        {
            BoxCollider original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateBoxCollider(original, clone);
        }

        public static bool UpdateCollider(CapsuleCollider clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<CapsuleCollider> cloneInfo))
            {
                UpdateCollider(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateCollider(PortalCloneInfo<CapsuleCollider> cloneInfo)
        {
            CapsuleCollider original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateCapsuleCollider(original, clone);
        }

        public static bool UpdateCollider(MeshCollider clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<MeshCollider> cloneInfo))
            {
                UpdateCollider(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateCollider(PortalCloneInfo<MeshCollider> cloneInfo)
        {
            MeshCollider original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateMeshCollider(original, clone);
        }


        public static bool UpdateCollider(CharacterController clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<CharacterController> cloneInfo))
            {
                UpdateCollider(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateCollider(PortalCloneInfo<CharacterController> cloneInfo)
        {
            CharacterController original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateCharacterController(original, clone);
        }

        private static void UpdateSphereCollider(SphereCollider original, SphereCollider clone)
        {
            UpdateCollider(original, clone);

            clone.center = original.center;
            clone.radius = original.radius;
        }

        private static void UpdateBoxCollider(BoxCollider original, BoxCollider clone)
        {
            UpdateCollider(original, clone);

            clone.center = original.center;
            clone.size = original.size;
            clone.size = original.size;
        }

        private static void UpdateCapsuleCollider(CapsuleCollider original, CapsuleCollider clone)
        {
            UpdateCollider(original, clone);

        }

        private static void UpdateMeshCollider(MeshCollider original, MeshCollider clone)
        {
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
