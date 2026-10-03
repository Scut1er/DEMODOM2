using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Форма карты выпуска (как в Slay the Spire) и откуда брать комнаты. Правится в инспекторе.
    [CreateAssetMenu(menuName = "RealityDirector/Episode Map Config", fileName = "EpisodeMap_")]
    public class EpisodeMapConfig : ScriptableObject
    {
        [Header("Размер карты")]
        [Tooltip("Рядов (шагов) слева направо, включая старт и монтаж.")]
        [Min(2)] public int floors = 6;
        [Tooltip("Дорожек по вертикали — максимум комнат в одном ряду.")]
        [Min(1)] public int lanes = 3;
        [Tooltip("Сколько путей прокладывается. Больше путей — больше комнат и развилок.")]
        [Min(1)] public int paths = 3;
        [Tooltip("0 — новая карта каждый выпуск. Любое другое число — всегда одна и та же карта (удобно для теста).")]
        public int seed;

        [Header("Шансы типов комнат в средних рядах (веса)")]
        [Min(0f)] public float situationWeight = 60f;
        [Min(0f)] public float eventWeight = 25f;
        [Min(0f)] public float marketingWeight = 15f;

        [Header("Правила")]
        [Tooltip("С какого ряда (0 — старт) могут появляться событие и маркетинг.")]
        [Min(0)] public int minSpecialFloor = 1;
        [Tooltip("Ряд, где все комнаты — маркетинг. -1 — без гарантии.")]
        public int guaranteedMarketingFloor = -1;
        [Tooltip("Ряд, где все комнаты — события. -1 — без гарантии. Вместе с рядом маркетинга и «съёмкой перед монтажом» "
                 + "гарантирует первый выпуск: 2 съёмки, событие, маркетинг, монтаж — на любом пути.")]
        public int guaranteedEventFloor = -1;
        [Tooltip("Событие не идёт сразу за событием, маркетинг — за маркетингом.")]
        public bool noRepeatSpecial = true;
        [Tooltip("Ряд перед монтажом — только съёмки.")]
        public bool situationBeforeMontage = true;

        [Header("Комнаты")]
        [Tooltip("Первая комната выпуска (единственная в первом ряду). Пусто — случайная съёмка из пула.")]
        public SituationRoomDefinition opening;
        [Tooltip("Монтаж — всегда последний ряд. Пусто — первый монтаж из пула.")]
        public MontageRoomDefinition montage;
        [Tooltip("Из каких комнат собирать карту. Пусто — все комнаты из Resources/Content.")]
        public List<RoomDefinition> pool = new List<RoomDefinition>();

        public static EpisodeMapConfig CreateDefault()
        {
            var c = CreateInstance<EpisodeMapConfig>();
            c.name = "EpisodeMap_Default";
            return c;
        }
    }
}
