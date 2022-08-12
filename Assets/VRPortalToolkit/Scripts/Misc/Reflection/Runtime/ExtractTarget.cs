using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Reflection
{
    [System.Serializable]
    public struct ExtractTarget
    {
        // TODO: Rename these
        public Object SourceObject;
        public string TargetName;

        public ExtractTarget(Object sourceObject, string targetName)
        {
            SourceObject = sourceObject;
            TargetName = targetName;
        }
    }
}
