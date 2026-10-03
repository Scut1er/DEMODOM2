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
        Cash,              // value — +/- нал выпуска
        AddDeckCard,       // key — id карты, в колоду навсегда
        RemoveDeckCard     // key — id карты, из колоды навсегда
    }

    [Serializable]
    public class Effect
    {
        public EffectType type;
        [Tooltip("Флаг, сюжетный тег или id карты — зависит от типа.")]
        public string key;
        [Tooltip("Число: кредиты, нал или очки тона.")]
        public int value;
        [Tooltip("Только для Tone.")]
        public ShowMood mood;
    }
}
