using System;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Что делает эффект. Новые значения — только в конец (сериализуются числом).
    public enum EffectType
    {
        Budget,            // value — +/- кредиты
        Tone,              // mood, value — очки тона сезона
        SetEpisodeFlag,    // key
        ClearEpisodeFlag,  // key
        SetSeasonFlag,     // key
        ClearSeasonFlag,   // key
        AddNarrativeTag,   // key
        AddTempCard,       // key — id карты продюсера, только на этот выпуск
        RemoveTempCard,    // key
        NextRoomModifier,  // key, value — модификатор для следующей съёмки
        BroadcastModifier  // key, value — модификатор эфира (рейтинг, просмотры...)
    }

    [Serializable]
    public class Effect
    {
        public EffectType type;
        [Tooltip("Флаг, тег, id карты или ключ модификатора — зависит от типа.")]
        public string key;
        [Tooltip("Число: кредиты, очки тона, сила модификатора.")]
        public int value;
        [Tooltip("Только для Tone.")]
        public ShowMood mood;
    }
}
