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
        // Кто что делал в кадре: «тег|имя» (Crying|Кира, Fight|Злой, Kleptomaniac|Добряк). HellTube называет только их.
        public List<string> cues = new List<string>();

        public bool Framed => grade != CaptureGrade.Blank;

        public static string Cue(string tag, string name)
        {
            return tag + "|" + name;
        }

        public static void AddCue(List<string> cues, string tag, string name)
        {
            if (cues == null || string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(name))
                return;
            string cue = Cue(tag, name);
            if (!cues.Contains(cue))
                cues.Add(cue);
        }

        // Имена из подсказок с этим тегом, в порядке записи.
        public static List<string> Who(List<string> cues, string tag)
        {
            var names = new List<string>();
            if (cues == null)
                return names;
            string prefix = tag + "|";
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i] != null && cues[i].StartsWith(prefix))
                {
                    string name = cues[i].Substring(prefix.Length);
                    if (!names.Contains(name))
                        names.Add(name);
                }
            }

            return names;
        }

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
