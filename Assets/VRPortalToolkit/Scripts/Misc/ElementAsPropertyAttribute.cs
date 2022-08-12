using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc
{
    public class ElementAsPropertyAttribute : PropertyAttribute
    {
        private string _propertyName;
        public string propertyName { get => _propertyName; set => _propertyName = value; }

        private string _key;
        public string key { get => _key; set => _key = value; }

        public ElementAsPropertyAttribute(string key)
        {
            _key = key;
        }

        public ElementAsPropertyAttribute(string key, string propertyName)
        {
            _propertyName = propertyName;
            _key = key;
        }
    }
}
