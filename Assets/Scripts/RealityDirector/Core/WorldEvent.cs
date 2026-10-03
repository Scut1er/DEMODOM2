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
        public bool hasLocus;
        public UnityEngine.Vector2 locus;
        public float time;
        public int depth;
    }
}
