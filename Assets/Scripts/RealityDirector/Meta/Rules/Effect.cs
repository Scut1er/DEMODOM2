using System;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Что делает эффект. Новые значения — только в конец (сериализуются числом).
    public enum EffectType
    {
        Budget,            // value — +/- ЕБ
        Tone,              // mood, value — очки тона сезона
        SetEpisodeFlag,    // key
        ClearEpisodeFlag,  // key
        SetSeasonFlag,     // key
        ClearSeasonFlag,   // key
        AddNarrativeTag,   // key
        AddTempCard,       // key — id карты продюсера, только на этот выпуск
        RemoveTempCard,    // key
        NextRoomModifier,  // key, value — модификатор следующей съёмки (квартира читает stress/anger/sadness/hostility/attraction/confidence)
        BroadcastModifier, // key, value — модификатор эфира: rating (десятые оценки), pay (ЕБ за эфир), sponsorPay (ЕБ к выплате спонсора)
        Cash,              // value — +/- УЕ выпуска
        AddDeckCard,       // key — id карты, в колоду навсегда
        RemoveDeckCard,    // key — id карты, из колоды навсегда
        SponsorReputation  // value — +/- репутация спонсоров (0–100)
    }

    [Serializable]
    public class Effect
    {
        public EffectType type;
        [Tooltip("Флаг, сюжетный тег или id карты — зависит от типа.")]
        public string key;
        [Tooltip("Число: ЕБ, УЕ или очки тона.")]
        public int value;
        [Tooltip("Только для Tone.")]
        public ShowMood mood;
    }
}
