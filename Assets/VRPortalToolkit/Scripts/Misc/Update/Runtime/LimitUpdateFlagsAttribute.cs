using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Update
{
    public class LimitUpdateFlagsAttribute : Attribute
    {
        private UpdateFlags _flags = new UpdateFlags();
        public UpdateFlags flags => _flags;

        public LimitUpdateFlagsAttribute(UpdateFlags flags)
        {
            _flags = flags;
        }
    }
}
