using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.EditorHelpers
{
    public class ShowIfAttribute : PropertyAttribute
    {
		public readonly string memberName;
		public readonly object value;

		public ShowIfAttribute(string memberName) : this(memberName, true) { }

		public ShowIfAttribute(string memberName, object value, int order = 0)
		{
			this.memberName = memberName;
			this.value = value;
			this.order = order;
		}
	}
}
