using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.EditorHelpers
{
    public class ConversionAttribute : PropertyAttribute
    {
        public readonly string from = "from";

        public readonly string arrow = ">>";

        public readonly string to = "to";

        // TODO: Change this to be an enum
        // have options:
        // ShowLabel
        // ShowFromLabel
        // ShowToLabel 
        public readonly bool showLabel = true;

        public ConversionAttribute(string from, string to, int order = 10)
        {
            this.from = from;
            this.to = to;
            this.order = order;
        }

        public ConversionAttribute(string from, string arrow, string to, int order = 10)
        {
            this.from = from;
            this.arrow = arrow;
            this.to = to;
            this.order = order;
        }

        public ConversionAttribute(int order = 10)
        {
            this.order = order;
        }

        public ConversionAttribute(bool showLabel, int order = 10)
        {
            this.order = order;
            this.showLabel = showLabel;
        }

        public ConversionAttribute(string from, string to, bool showLabel, int order = 10)
        {
            this.from = from;
            this.to = to;
            this.showLabel = showLabel;
            this.order = order;
        }

        public ConversionAttribute(string from, string arrow, string to, bool showLabel, int order = 10)
        {
            this.from = from;
            this.arrow = arrow;
            this.to = to;
            this.order = order;
            this.showLabel = showLabel;
        }
    }
}
