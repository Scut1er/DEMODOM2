using System;
using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Иконка узла (если у комнаты нет арта). Новые значения — только в конец (сериализуются числом).
    public enum MapNodeKind
    {
        Start,
        Scene,
        Confession,
        Conflict,
        Date,
        Challenge,
        Secret,
        Sponsor,
        Edit,
        Elimination,
        Party,
        Climax,
        Shop,
        Mystery
    }

    // Узел карты выпуска. Сохраняется в EpisodeState как есть: позиция, связи и id комнаты.
    [Serializable]
    public class MapNode
    {
        public string id;
        public RoomType type;
        public string roomId;
        public int layer;
        public float row;
        public Vector2 offset;
        public List<string> next = new List<string>();

        // Подставляется при загрузке по roomId.
        [NonSerialized] public RoomDefinition room;

        public string title => room != null ? room.title : "";
        public string subtitle => room != null ? room.subtitle : "";
        public string description => room != null ? room.description : "";
        public string goal => room != null ? room.goal : "";
        public MapNodeKind kind => room != null ? room.icon : MapNodeKind.Scene;
        public Color color => room != null ? room.color : new Color(0.3f, 0.28f, 0.34f, 1f);
        public Sprite art => room != null ? room.art : null;

        // Тон, который даёт вход в комнату (первый эффект «Тон» в onEnter) — для подписи на карте.
        public int toneGain => ToneEffect()?.value ?? 0;
        public ShowMood mood => ToneEffect()?.mood ?? ShowMood.Drama;

        Effect ToneEffect()
        {
            if (room == null || room.onEnter == null)
                return null;
            for (int i = 0; i < room.onEnter.Count; i++)
            {
                if (room.onEnter[i] != null && room.onEnter[i].type == EffectType.Tone && room.onEnter[i].value > 0)
                    return room.onEnter[i];
            }

            return null;
        }
    }

    // Карта выпуска: ряды (layer) слева направо, в последнем — монтаж.
    public class MapGraph
    {
        public readonly List<MapNode> nodes;
        public int Layers;
        public int Lanes;

        public MapGraph() : this(new List<MapNode>())
        {
        }

        public MapGraph(List<MapNode> nodes)
        {
            this.nodes = nodes;
        }

        public MapNode Find(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].id == id)
                    return nodes[i];
            }

            return null;
        }
    }
}
