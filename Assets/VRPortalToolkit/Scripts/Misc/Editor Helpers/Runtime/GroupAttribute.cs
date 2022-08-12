using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.EditorHelpers
{
    [System.Flags]
    public enum GroupMask
    {
        None = 0,
        Foldout = 1 << 0,
        BoldHeader = 1 << 1,
        LargeHeader = 1 << 2,
        FirstAsHeader = 1 << 3,
        IndentFields = 1 << 4,
        //SpaceBefore = 1 << 5, No longer in use
        //SpaceAfter = 1 << 6, No longer in use
    }

    public class GroupAttribute : PropertyAttribute
    {
        public readonly string groupName;

        public readonly GroupMask groupMask;

        public GroupAttribute(string groupName, GroupMask groupMask, int order = 10)
        {
            this.groupName = groupName;
            this.groupMask = groupMask;
            this.order = order;
        }
        public GroupAttribute(string groupName, int order = 10)
        {
            this.groupName = groupName;
            groupMask = GroupMask.BoldHeader | GroupMask.Foldout | GroupMask.IndentFields;
            this.order = order;
        }
    }
}
