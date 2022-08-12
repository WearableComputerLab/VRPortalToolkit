using UnityEngine;

namespace Misc.Update
{
    [System.Serializable]
    public struct EventSource
    {
        public Object SourceObject;
        public string EventName;

        public EventSource(Object sourceObject, string eventName)
        {
            SourceObject = sourceObject;
            EventName = eventName;
        }
    }
}
