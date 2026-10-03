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
        public float duration;
        public Texture2D photo;
        public List<Texture2D> frames = new List<Texture2D>();
        public Vector2 screenPoint;
        public ShowMood mood;
        public CaptureGrade grade;
        public HiddenTrait exposed;

        public bool Framed => grade != CaptureGrade.Blank;

        public Texture2D FrameAt(float clipTime)
        {
            if (frames == null || frames.Count == 0)
                return photo;
            if (frames.Count == 1 || duration <= 0f)
                return frames[0];
            float u = Mathf.Clamp01(clipTime / duration);
            int index = Mathf.Clamp(Mathf.FloorToInt(u * frames.Count), 0, frames.Count - 1);
            return frames[index];
        }

        public void Release()
        {
            if (frames != null)
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    if (frames[i] != null)
                        UnityEngine.Object.Destroy(frames[i]);
                }

                frames.Clear();
            }

            photo = null;
        }

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
