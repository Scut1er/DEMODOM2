using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Главные числа сезона для дизайнера: сколько выпусков, какие карты, лимиты каста и монтажа.
    [CreateAssetMenu(menuName = "RealityDirector/Season Config", fileName = "SeasonConfig")]
    public class SeasonConfig : ScriptableObject
    {
        [Header("Сезон")]
        [Tooltip("Сколько выпусков в сезоне. Между выпусками — хаб.")]
        [Min(1)] public int episodes = 4;
        [Tooltip("Карта для каждого выпуска по порядку: элемент 0 — выпуск 1. Если выпусков больше — берётся последняя карта.")]
        public List<EpisodeMapConfig> maps = new List<EpisodeMapConfig>();
        [Tooltip("Бюджет в начале сезона.")]
        [Min(0)] public int startingBudget;

        [Header("Каст")]
        [Tooltip("Минимум участников в выпуске.")]
        [Min(1)] public int castMin = 2;
        [Tooltip("Максимум участников в выпуске (места ещё ограничивает уровень Кастинга).")]
        [Min(1)] public int castMax = 5;

        [Header("Монтаж")]
        [Tooltip("Сколько клипов может накопиться за выпуск.")]
        [Min(1)] public int footageLimit = 5;
        [Tooltip("Сколько клипов входит в финальный эфир.")]
        [Min(1)] public int finalCutSize = 3;

        public EpisodeMapConfig MapFor(int episodeIndex)
        {
            if (maps == null || maps.Count == 0)
                return null;
            var map = maps[Mathf.Clamp(episodeIndex, 0, maps.Count - 1)];
            return map != null ? map : maps[0];
        }

        public static SeasonConfig CreateDefault()
        {
            var c = CreateInstance<SeasonConfig>();
            c.name = "SeasonConfig";
            return c;
        }
    }
}
