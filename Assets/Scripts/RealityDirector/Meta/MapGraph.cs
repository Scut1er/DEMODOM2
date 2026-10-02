using System;
using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Тип узла карты сезона.
    public enum MapNodeType
    {
        Filming,
        RandomEvent,
        Shop,
        Editing
    }

    // Вид узла — для иконки и внешнего вида. Новые значения — только в конец (сериализуются числом).
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

    [Serializable]
    public class MapNode
    {
        [HideInInspector] public string id;
        public MapNodeType type;
        public string title;
        public string subtitle;
        [TextArea(2, 4)] public string description;
        [TextArea(1, 3)] public string goal;
        public MapNodeKind kind;
        public Color color = new Color(0.3f, 0.28f, 0.34f, 1f);
        public Sprite art;
        [Tooltip("Вес при случайном выборе вкуса съёмки.")]
        public float weight = 1f;

        [Header("Замок")]
        public CrewTrack lockTrack;
        [Tooltip("0 — без замка.")]
        public int lockLevel;

        [Header("Эффект при выборе")]
        public int budget;
        public ShowMood mood;
        public int toneGain;

        // Заполняет генератор.
        [HideInInspector] public int layer;
        [HideInInspector] public float row;
        [HideInInspector] public Vector2 offset;
        [HideInInspector] public List<string> next = new List<string>();

        public MapNode Clone()
        {
            var copy = (MapNode)MemberwiseClone();
            copy.next = new List<string>();
            return copy;
        }
    }

    // Сгенерированная карта сезона: ряды (layer) слева направо, в последнем — финал.
    public class MapGraph
    {
        public readonly List<MapNode> nodes = new List<MapNode>();
        public int Layers;
        public int Lanes;

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
