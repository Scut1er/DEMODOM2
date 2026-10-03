using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Meta;
using RealityDirector.NPC;
using UnityEngine;

namespace RealityDirector.Capture
{
    // Ролик в памяти. Текстуры не сериализуются: после перезапуска домена остаётся запись в сейве без картинки.
    public class FootageClip
    {
        public string id;
        public string nodeId;
        public string title;
        public List<string> actorNames = new List<string>();
        public List<string> tags = new List<string>();
        public ShowMood mood;
        public CaptureGrade grade;
        public HiddenTrait exposed;
        public float duration;
        public float time;
        public Texture2D photo;
        public List<Texture2D> frames;
        public string sponsorCardId;

        public bool Framed => grade != CaptureGrade.Blank;
    }

    public static class FootageReel
    {
        static readonly Dictionary<string, FootageClip> All = new Dictionary<string, FootageClip>();

        public static FootageClip Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            All.TryGetValue(id, out FootageClip clip);
            return clip;
        }

        // Забирает текстуры у момента. Дальше Release момента их не трогает.
        public static FootageClip Adopt(CapturedMoment moment, string nodeId)
        {
            var clip = new FootageClip
            {
                id = "clip_" + System.Guid.NewGuid().ToString("N").Substring(0, 10),
                nodeId = nodeId ?? "",
                title = moment.Title,
                actorNames = moment.actorNames != null ? new List<string>(moment.actorNames) : new List<string>(),
                tags = moment.tags != null ? new List<string>(moment.tags) : new List<string>(),
                mood = moment.mood,
                grade = moment.grade,
                exposed = moment.exposed,
                duration = moment.duration,
                time = moment.time,
                photo = moment.photo,
                frames = moment.frames
            };
            moment.photo = null;
            moment.frames = null;
            Keep(clip.photo);
            if (clip.frames != null)
            {
                for (int i = 0; i < clip.frames.Count; i++)
                    Keep(clip.frames[i]);
            }

            All[clip.id] = clip;
            return clip;
        }

        public static FootageClip Resolve(FootageEntry entry)
        {
            if (entry == null)
                return null;
            var live = Get(entry.clipRef);
            if (live != null)
                return live;
            return new FootageClip
            {
                id = entry.clipRef,
                nodeId = entry.nodeId,
                title = string.IsNullOrEmpty(entry.description) ? "КАДР" : entry.description,
                actorNames = entry.actorIds != null ? new List<string>(entry.actorIds) : new List<string>(),
                tags = entry.tags != null ? new List<string>(entry.tags) : new List<string>(),
                mood = entry.mood,
                grade = (CaptureGrade)entry.quality,
                exposed = (HiddenTrait)entry.exposed,
                duration = entry.time
            };
        }

        public static FootageEntry Entry(FootageClip clip)
        {
            return new FootageEntry
            {
                id = clip.id,
                clipRef = clip.id,
                nodeId = clip.nodeId,
                roomId = clip.nodeId,
                actorIds = new List<string>(clip.actorNames),
                tags = new List<string>(clip.tags),
                mood = clip.mood,
                quality = (int)clip.grade,
                value = clip.grade == CaptureGrade.Cast ? 2 : clip.grade == CaptureGrade.Prop ? 1 : 0,
                time = clip.duration,
                description = clip.title,
                exposed = (int)clip.exposed,
                failed = clip.grade == CaptureGrade.Blank
            };
        }

        public static void ReleaseEpisode(EpisodeState episode)
        {
            if (episode == null || episode.footage == null)
                return;
            for (int i = 0; i < episode.footage.Count; i++)
                Release(Get(episode.footage[i].clipRef));
        }

        public static void ReleaseAll()
        {
            var ids = new List<string>(All.Keys);
            for (int i = 0; i < ids.Count; i++)
                Release(Get(ids[i]));
        }

        public static void Release(FootageClip clip)
        {
            if (clip == null)
                return;
            All.Remove(clip.id);
            if (clip.frames != null)
            {
                for (int i = 0; i < clip.frames.Count; i++)
                {
                    if (clip.frames[i] != null)
                        Object.Destroy(clip.frames[i]);
                }

                clip.frames = null;
            }

            clip.photo = null;
        }

        static void Keep(Texture2D texture)
        {
            if (texture != null)
                texture.hideFlags = HideFlags.HideAndDontSave;
        }
    }
}
