using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit
{
    public interface IPortal
    {
        #region Portal Properties

        /// <summary>Returns true if ModifyMatrix and related functions should be used.</summary>
        bool usesTeleport { get; }

        /// <summary>Returns true if ModifyLayer and ModifyLayerMask should be used.</summary>
        bool usesLayers { get; }

        /// <summary>Returns true if modify tag should be used.</summary>
        bool usesTag { get; }

        #endregion

        #region Teleport Matrix

        /// <summary>Matrix where teleportMatrix * matrix = ModifyMatrix(matrix).</summary>
        Matrix4x4 teleportMatrix { get; }

        #endregion

        #region Physics Casting Functions

        /// <summary>Called before a PortalPhysics cast through this portal.</summary>
        void PreCast();

        /// <summary>Called after a PortalPhysics cast through this portal.</summary>
        void PostCast();

        #endregion

        #region Layers and Tag Functions

        /// <summary>Returns a layermask after travelling through the portal.</summary>
        bool ModifyLayerMask(ref int layerMask);

        /// <summary>Returns a layermask after travelling through the portal.</summary>
        bool ModifyLayer(ref int layer);

        bool ModifyTag(ref string tag);

        #endregion

        #region Teleport Functions

        /// <summary>Returns a matrix after travelling through the portal.</summary>
        bool ModifyMatrix(ref Matrix4x4 localToWorldMatrix);

        /// <summary>Returns a point after travelling through the portal.</summary>
        bool ModifyPoint(ref Vector3 point);

        /// <summary>Returns a direction after travelling through the portal.</summary>
        bool ModifyDirection(ref Vector3 direction);

        /// <summary>Returns a vector after travelling through the portal.</summary>
        bool ModifyVector(ref Vector3 vector);

        /// <summary>Returns a rotation after travelling through the portal.</summary>
        bool ModifyRotation(ref Quaternion rotation);

        #endregion
    }
}