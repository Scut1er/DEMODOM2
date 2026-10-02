using System;
using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
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
        Climax
    }

    [Serializable]
    public class MapNode
    {
        public string id;
        public string title;
        public string subtitle;
        [TextArea(2, 4)] public string description;
        [TextArea(1, 3)] public string goal;
        public MapNodeKind kind;
        [Tooltip("Колонка = номер серии (0 — первая).")]
        public int layer;
        [Tooltip("-1 верх, 0 середина, 1 низ. Можно дробно.")]
        public float row;
        public Vector2 offset;
        public Color color = new Color(0.3f, 0.28f, 0.34f, 1f);
        public Sprite art;
        [Tooltip("Куда можно пойти дальше.")]
        public List<string> next = new List<string>();

        [Header("Замок")]
        public CrewTrack lockTrack;
        [Tooltip("0 — без замка.")]
        public int lockLevel;

        [Header("Эффект при выборе")]
        public int budget;
        public ShowMood mood;
        public int toneGain;
    }

    // Карта сезона: колонка — серия, узел — тип выпуска. Правится в инспекторе.
    [CreateAssetMenu(menuName = "RealityDirector/Episode Map", fileName = "EpisodeMap")]
    public class EpisodeMap : ScriptableObject
    {
        public List<MapNode> nodes = new List<MapNode>();

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

        public int Layers
        {
            get
            {
                int max = -1;
                for (int i = 0; i < nodes.Count; i++)
                    max = Mathf.Max(max, nodes[i].layer);
                return max + 1;
            }
        }

        void OnValidate()
        {
            if (nodes.Count > 0 && Layers != Progression.SeasonLength)
                Debug.LogWarning(name + ": колонок " + Layers + ", а серий в сезоне " + Progression.SeasonLength + ".", this);
        }

        public static EpisodeMap CreateDefault()
        {
            var map = CreateInstance<EpisodeMap>();
            map.name = "EpisodeMap";
            map.nodes = new List<MapNode>
            {
                Node("start", "НАЧАЛО", "Холодное открытие", MapNodeKind.Start, 0, 0f, new Color(0.22f, 0.2f, 0.27f, 1f),
                    "Первый выпуск: знакомим зрителя с домом и героями. Одна карта, без спешки.",
                    "Снять первый хайлайт и понять, кто здесь кто.", ShowMood.Drama, 0, 0, "scene", "challenge"),
                Node("scene", "СЦЕНА", "Знакомство", MapNodeKind.Scene, 1, -1f, new Color(0.78f, 0.6f, 0.22f, 1f),
                    "Герои притираются друг к другу. Спокойный выпуск — хорош для семейного тона.",
                    "Поймать момент тепла или первую искру.", ShowMood.Family, 6, 0, "confession", "secret"),
                Node("challenge", "ИСПЫТАНИЕ", "Командный конкурс", MapNodeKind.Challenge, 1, 0.15f, new Color(0.28f, 0.42f, 0.82f, 1f),
                    "Соревнование выводит характеры наружу.",
                    "Снять, как кто-то проигрывает некрасиво.", ShowMood.Trash, 6, 0, "secret", "sponsor"),
                Node("confession", "ИСПОВЕДЬ", "Комната признаний", MapNodeKind.Confession, 2, -1f, new Color(0.2f, 0.6f, 0.52f, 1f),
                    "Герои говорят на камеру то, что не скажут в лицо.",
                    "Довести кого-то до слёз.", ShowMood.Drama, 8, 0, "conflict", "edit"),
                Node("secret", "СЕКРЕТ", "Утечка", MapNodeKind.Secret, 2, 0.1f, new Color(0.7f, 0.55f, 0.28f, 1f),
                    "Один секрет — и в доме уже никто никому не верит.",
                    "Снять реакцию на раскрытый секрет.", ShowMood.Drama, 6, 0, "conflict", "edit", "elimination"),
                Node("sponsor", "СПОНСОР", "Рекламная интеграция", MapNodeKind.Sponsor, 2, 1.1f, new Color(0.76f, 0.38f, 0.72f, 1f),
                    "Спонсор платит за выпуск, но зрители любят рекламу меньше драмы.",
                    "Отработать интеграцию и не растерять зрителей.", ShowMood.Family, 4, 60, "conflict", "elimination"),
                Node("conflict", "КОНФЛИКТ", "Ссора в доме", MapNodeKind.Conflict, 3, -1f, new Color(0.82f, 0.28f, 0.24f, 1f),
                    "Искры летят — идеальный момент для драки в кадре.",
                    "Снять драку или громкую ссору.", ShowMood.Trash, 8, 0, "date", "party"),
                Node("edit", "МОНТАЖ", "Перемонтаж", MapNodeKind.Edit, 3, 0.1f, new Color(0.42f, 0.4f, 0.48f, 1f),
                    "Сценаристы перекраивают сюжет: любой момент можно подать как скандал.",
                    "Собрать выпуск из самых острых кадров.", ShowMood.Drama, 10, 0, "date", "party"),
                Node("elimination", "ЭЛИМИНАЦИЯ", "Кто уходит?", MapNodeKind.Elimination, 3, 1.1f, new Color(0.38f, 0.38f, 0.44f, 1f),
                    "Голосование держит всех в напряжении. Нужен каст побольше.",
                    "Снять реакцию на вылет.", ShowMood.Trash, 10, 0, "party"),
                Node("date", "СВИДАНИЕ", "Романтический ужин", MapNodeKind.Date, 4, -1f, new Color(0.88f, 0.32f, 0.58f, 1f),
                    "Свидание под камерами — нежность или неловкость.",
                    "Поймать тёплый момент или провал свидания.", ShowMood.Family, 8, 0, "climax"),
                Node("party", "ВЕЧЕРИНКА", "Ночь у бассейна", MapNodeKind.Party, 4, 0.5f, new Color(0.52f, 0.28f, 0.78f, 1f),
                    "Музыка, бассейн и никаких тормозов.",
                    "Снять хаос, пока он не закончился.", ShowMood.Trash, 8, 0, "climax"),
                Node("climax", "КУЛЬМИНАЦИЯ", "Финал сезона", MapNodeKind.Climax, 5, 0.2f, new Color(0.92f, 0.72f, 0.28f, 1f),
                    "Последний выпуск. Всё, что накопилось за сезон, выходит наружу.",
                    "Снять лучший момент сезона.", ShowMood.Drama, 6, 0)
            };

            var edit = map.Find("edit");
            edit.lockTrack = CrewTrack.Writers;
            edit.lockLevel = 2;
            var elimination = map.Find("elimination");
            elimination.lockTrack = CrewTrack.Cast;
            elimination.lockLevel = 2;
            return map;
        }

        static MapNode Node(string id, string title, string subtitle, MapNodeKind kind, int layer, float row, Color color,
            string description, string goal, ShowMood mood, int toneGain, int budget, params string[] next)
        {
            return new MapNode
            {
                id = id,
                title = title,
                subtitle = subtitle,
                kind = kind,
                layer = layer,
                row = row,
                color = color,
                description = description,
                goal = goal,
                mood = mood,
                toneGain = toneGain,
                budget = budget,
                next = new List<string>(next)
            };
        }
    }
}
