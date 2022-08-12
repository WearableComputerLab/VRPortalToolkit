using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.EditorHelpers
{
    [System.Flags]
    public enum GroupObjectMask
    {
        None = 0,
        Foldout = 1 << 0,
        BoldHeader = 1 << 1,
        LargeHeader = 1 << 2,
        FirstAsHeader = 1 << 3,
        FirstReplaceLabel = FirstAsHeader | (1 << 4),
        FirstAddNameToLabel = FirstAsHeader | (1 << 5),
        AddNameToFieldLabels = 1 << 6,
        IndentFields = 1 << 7,
        SpaceBefore = 1 << 8, // Do I need this? Feels like a decorator could do this?
        SpaceAfter = 1 << 9, // Do I need this?
    }

    public class GroupObjectAttribute : PropertyAttribute
    {
        public readonly GroupObjectMask groupMask;

        public GroupObjectAttribute(GroupObjectMask groupMask = GroupObjectMask.BoldHeader | GroupObjectMask.Foldout | GroupObjectMask.IndentFields, int order = 10)
        {
            this.groupMask = groupMask;
            this.order = order;
        }
    }
}
