using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Core
{
    public class EpisodeContext
    {
        readonly List<Stamp> _stamps = new List<Stamp>();

        public struct Stamp
        {
            public float time;
            public string tag;
            public string sourceActorId;
            public string targetActorId;
            public bool hasLocus;
            public Vector2 locus;
        }

        public void Bind()
        {
            EventBus.Published += OnEvent;
        }

        public void Unbind()
        {
            EventBus.Published -= OnEvent;
        }

        public void Reset()
        {
            _stamps.Clear();
        }

        public bool Had(string tag)
        {
            for (int i = 0; i < _stamps.Count; i++)
            {
                if (_stamps[i].tag == tag)
                    return true;
            }

            return false;
        }

        public List<string> RecentTags(float window)
        {
            var list = new List<string>();
            float now = Time.time;
            for (int i = 0; i < _stamps.Count; i++)
            {
                if (now - _stamps[i].time > window)
                    continue;
                if (!list.Contains(_stamps[i].tag))
                    list.Add(_stamps[i].tag);
            }

            return list;
        }

        // События за окно с участниками и местом — камера берёт в ролик только то, что было в кадре.
        public List<Stamp> RecentStamps(float window)
        {
            var list = new List<Stamp>();
            float now = Time.time;
            for (int i = 0; i < _stamps.Count; i++)
            {
                if (now - _stamps[i].time <= window)
                    list.Add(_stamps[i]);
            }

            return list;
        }

        void OnEvent(WorldEvent worldEvent)
        {
            if (worldEvent.tags == null)
                return;

            float time = worldEvent.time > 0f ? worldEvent.time : Time.time;
            for (int i = 0; i < worldEvent.tags.Count; i++)
            {
                _stamps.Add(new Stamp
                {
                    time = time,
                    tag = worldEvent.tags[i],
                    sourceActorId = worldEvent.sourceActorId,
                    targetActorId = worldEvent.targetActorId,
                    hasLocus = worldEvent.hasLocus,
                    locus = worldEvent.locus
                });
            }
        }
    }
}
