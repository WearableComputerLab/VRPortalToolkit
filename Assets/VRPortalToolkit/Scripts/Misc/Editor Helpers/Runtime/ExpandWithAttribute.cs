using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.EditorHelpers
{
    public class ExpandWithAttribute : PropertyAttribute
    {
        public readonly string fieldName;

        public readonly bool indent;

        public ExpandWithAttribute(string fieldName, bool indent = true, int order = 10)
        {
            this.fieldName = fieldName;
            this.indent = indent;
            this.order = order;
        }
    }
}
