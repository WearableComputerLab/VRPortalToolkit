using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Data;
using VRPortalToolkit.Portables;

namespace VRPortalToolkit.Physics
{
    internal class TeleportListener// : TreeNode<TeleportListener>
    {
        public Transform transform;
        
        public bool ignoreParent;

        public TeleportAction onPreTeleport;

        public TeleportAction onPostTeleport;
    }
}