using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.NPC;
using UnityEngine;

namespace RealityDirector.Capture
{
    public enum CaptureGrade
    {
        Blank,
        Prop,
        Cast
    }

    public class CapturedMoment
    {
        public List<string> actorNames = new List<string>();
        public List<string> tags = new List<string>();
        public float time;
        public Texture2D photo;
        public Vector2 screenPoint;
        public ShowMood mood;
        public CaptureGrade grade;
        public HiddenTrait exposed;

        public bool Framed => grade != CaptureGrade.Blank;

        public string Title
        {
            get
            {
                if (grade == CaptureGrade.Blank)
                    return "ПУСТО";
                if (tags.Contains(MomentTags.Fight))
                    return "ДРАКА";
                if (tags.Contains(MomentTags.Crying))
                    return "СЛЁЗЫ";
                if (mood == ShowMood.Family)
                    return "СЕМЬЯ";
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
