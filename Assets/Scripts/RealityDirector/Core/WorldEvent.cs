using System.Collections.Generic;

namespace RealityDirector.Core
{
    public struct WorldEvent
    {
        public string eventId;
        public List<string> tags;
        public string sourceActorId;
        public string targetActorId;
        public string targetObjectId;
        public float time;
    }
}
