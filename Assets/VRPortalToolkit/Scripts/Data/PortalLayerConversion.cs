using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit
{
    [System.Serializable]
    public struct PortalLayerConversion
    {
        [SerializeField] public int outside;

        [SerializeField] public int between;

        [SerializeField] public int inside;

        public PortalLayerConversion(int outside, int between, int inside)
        {
            this.outside = outside;
            this.between = between;
            this.inside = inside;
        }

        public override bool Equals(object obj)
        {
            return obj is PortalLayerConversion conversion &&
                   outside == conversion.outside &&
                   between == conversion.between &&
                   inside == conversion.inside;
        }

        public override int GetHashCode()
        {
            int hashCode = -1936129480;
            hashCode = hashCode * -1521134295 + outside.GetHashCode();
            hashCode = hashCode * -1521134295 + between.GetHashCode();
            hashCode = hashCode * -1521134295 + inside.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(PortalLayerConversion x, PortalLayerConversion y) => x.Equals(y);

        public static bool operator !=(PortalLayerConversion x, PortalLayerConversion y) => !x.Equals(y);
    }
}
