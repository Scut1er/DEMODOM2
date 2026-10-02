using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Настройки генерации карты сезона (как в Slay the Spire). Правится в инспекторе.
    [CreateAssetMenu(menuName = "RealityDirector/Season Map Config", fileName = "SeasonMapConfig")]
    public class SeasonMapConfig : ScriptableObject
    {
        [Header("Размер карты")]
        [Tooltip("Рядов (шагов) слева направо, включая финал. Первый ряд — всегда съёмка.")]
        [Range(3, 20)] public int floors = 8;
        [Tooltip("Дорожек по вертикали — максимум узлов в одном ряду.")]
        [Range(2, 7)] public int lanes = 4;
        [Tooltip("Сколько путей прокладывается. Больше путей — больше узлов и развилок.")]
        [Range(1, 8)] public int paths = 4;
        [Tooltip("0 — новая карта каждый сезон. Любое другое число — всегда одна и та же карта (удобно для теста).")]
        public int seed;

        [Header("Шансы типов узлов (веса)")]
        public float filmingWeight = 60f;
        public float randomEventWeight = 25f;
        public float shopWeight = 15f;

        [Header("Правила")]
        [Tooltip("С какого ряда (0 — старт) могут появляться событие и магазин. Монтаж — всегда только финал.")]
        public int minSpecialFloor = 1;
        [Tooltip("Ряд, где все узлы — магазин. -1 — без гарантии.")]
        public int guaranteedShopFloor = -1;
        [Tooltip("Магазин не идёт сразу за магазином.")]
        public bool noRepeatSpecial = true;
        [Tooltip("Ряд перед финалом — только съёмки.")]
        public bool filmingBeforeFinale = true;

        [Header("Содержимое узлов")]
        [Tooltip("Старт — единственный узел первого ряда (съёмка).")]
        public MapNode opening;
        [Tooltip("Финал — единственный узел последнего ряда (монтаж сезона).")]
        public MapNode finale;
        [Tooltip("Варианты съёмки — выбираются случайно (по весу) для каждого узла-съёмки.")]
        public List<MapNode> filmingFlavors = new List<MapNode>();
        public MapNode shop;
        public MapNode randomEvent;

        public static SeasonMapConfig CreateDefault()
        {
            var c = CreateInstance<SeasonMapConfig>();
            c.name = "SeasonMapConfig";
            c.opening = Node(MapNodeType.Filming, "НАЧАЛО", "Холодное открытие", MapNodeKind.Start, new Color(0.22f, 0.2f, 0.27f, 1f),
                "Первый выпуск: знакомим зрителя с домом и героями. Одна карта, без спешки.",
                "Снять первый хайлайт и понять, кто здесь кто.", ShowMood.Drama, 0, 0);
            c.finale = Node(MapNodeType.Editing, "ФИНАЛ", "Монтаж сезона", MapNodeKind.Climax, new Color(0.92f, 0.72f, 0.28f, 1f),
                "Собираем сезон из отснятого и выпускаем в эфир. Монтаж пока не реализован — шаг засчитается, и сезон завершится.",
                "Собрать лучший сезон из отснятого.", ShowMood.Drama, 0, 0);

            c.filmingFlavors = new List<MapNode>
            {
                Node(MapNodeType.Filming, "СЦЕНА", "Знакомство", MapNodeKind.Scene, new Color(0.78f, 0.6f, 0.22f, 1f),
                    "Герои притираются друг к другу. Спокойный выпуск — хорош для семейного тона.",
                    "Поймать момент тепла или первую искру.", ShowMood.Family, 6, 0),
                Node(MapNodeType.Filming, "ИСПЫТАНИЕ", "Командный конкурс", MapNodeKind.Challenge, new Color(0.28f, 0.42f, 0.82f, 1f),
                    "Соревнование выводит характеры наружу.",
                    "Снять, как кто-то проигрывает некрасиво.", ShowMood.Trash, 6, 0),
                Node(MapNodeType.Filming, "ИСПОВЕДЬ", "Комната признаний", MapNodeKind.Confession, new Color(0.2f, 0.6f, 0.52f, 1f),
                    "Герои говорят на камеру то, что не скажут в лицо.",
                    "Довести кого-то до слёз.", ShowMood.Drama, 8, 0),
                Node(MapNodeType.Filming, "СЕКРЕТ", "Утечка", MapNodeKind.Secret, new Color(0.7f, 0.55f, 0.28f, 1f),
                    "Один секрет — и в доме уже никто никому не верит.",
                    "Снять реакцию на раскрытый секрет.", ShowMood.Drama, 6, 0),
                Node(MapNodeType.Filming, "СПОНСОР", "Рекламная интеграция", MapNodeKind.Sponsor, new Color(0.76f, 0.38f, 0.72f, 1f),
                    "Спонсор платит за выпуск, но зрители любят рекламу меньше драмы.",
                    "Отработать интеграцию и не растерять зрителей.", ShowMood.Family, 4, 60),
                Node(MapNodeType.Filming, "КОНФЛИКТ", "Ссора в доме", MapNodeKind.Conflict, new Color(0.82f, 0.28f, 0.24f, 1f),
                    "Искры летят — идеальный момент для драки в кадре.",
                    "Снять драку или громкую ссору.", ShowMood.Trash, 8, 0),
                Node(MapNodeType.Filming, "СВИДАНИЕ", "Романтический ужин", MapNodeKind.Date, new Color(0.88f, 0.32f, 0.58f, 1f),
                    "Свидание под камерами — нежность или неловкость.",
                    "Поймать тёплый момент или провал свидания.", ShowMood.Family, 8, 0),
                Node(MapNodeType.Filming, "ВЕЧЕРИНКА", "Ночь у бассейна", MapNodeKind.Party, new Color(0.52f, 0.28f, 0.78f, 1f),
                    "Музыка, бассейн и никаких тормозов.",
                    "Снять хаос, пока он не закончился.", ShowMood.Trash, 8, 0)
            };

            var elimination = Node(MapNodeType.Filming, "ЭЛИМИНАЦИЯ", "Кто уходит?", MapNodeKind.Elimination, new Color(0.38f, 0.38f, 0.44f, 1f),
                "Голосование держит всех в напряжении. Нужен каст побольше.",
                "Снять реакцию на вылет.", ShowMood.Trash, 10, 0);
            elimination.lockTrack = CrewTrack.Cast;
            elimination.lockLevel = 2;
            elimination.weight = 0.6f;
            c.filmingFlavors.Add(elimination);

            c.shop = Node(MapNodeType.Shop, "МАГАЗИН", "Закупка ивентов", MapNodeKind.Shop, new Color(0.25f, 0.55f, 0.35f, 1f),
                "Здесь можно купить новые карты ивентов за бюджет шоу.", "", ShowMood.Drama, 0, 0);
            c.randomEvent = Node(MapNodeType.RandomEvent, "СОБЫТИЕ", "Что-то случилось", MapNodeKind.Mystery, new Color(0.45f, 0.3f, 0.6f, 1f),
                "Случайное событие за кадром. Пока не реализовано — шаг просто засчитается.", "", ShowMood.Drama, 0, 0);
            return c;
        }

        static MapNode Node(MapNodeType type, string title, string subtitle, MapNodeKind kind, Color color,
            string description, string goal, ShowMood mood, int toneGain, int budget)
        {
            return new MapNode
            {
                type = type,
                title = title,
                subtitle = subtitle,
                kind = kind,
                color = color,
                description = description,
                goal = goal,
                mood = mood,
                toneGain = toneGain,
                budget = budget
            };
        }
    }
}
