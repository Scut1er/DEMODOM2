using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Capture
{
    public class CapturedMoment
    {
        public List<string> actorNames = new List<string>();
        public List<string> tags = new List<string>();
        public float time;
        public Texture2D photo;
        public Vector2 screenPoint;

        public string Title
        {
            get
            {
                if (tags.Contains(MomentTags.Fight))
                    return "ДРАКА";
                if (tags.Contains(MomentTags.Fire))
                    return "ОГОНЬ";
                if (tags.Contains(MomentTags.Conflict))
                    return "КОНФЛИКТ";
                if (tags.Contains(MomentTags.Misery))
                    return "БЫТ";
                return "КАДР";
            }
        }
    }
}
