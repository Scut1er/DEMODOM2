using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Meta
{
    public enum OfferKind
    {
        Purchase,
        Contract
    }

    [Serializable]
    public class MarketingOffer
    {
        [Tooltip("Стабильный id предложения (MKT_001): по нему помнится «куплено».")]
        public string id;
        [Tooltip("Карта, которую получает игрок (временная до эфира; у контракта — спонсорская карта).")]
        public string cardId;
        public string title;
        public string blurb;
        public OfferKind kind;
        public int price;
        public int minReputation;
        public int payout;
        public int scoreHit;
        [Tooltip("Флаг выпуска для покупки-бонуса (HellTokenPack, ExtraCaptureSlot, EnvDiscount…).")]
        public string flag;
        [Tooltip("Бренд контракта. Пусто — первые слова описания до точки.")]
        public string brand;
    }

    // Покупка за УЕ и спонсорский контракт. Контракт не платит сразу.
    [CreateAssetMenu(menuName = "RealityDirector/Rooms/Marketing", fileName = "Marketing_")]
    public class MarketingRoomDefinition : RoomDefinition
    {
        public List<MarketingOffer> offers = new List<MarketingOffer>();
        [Tooltip("Сколько покупок показать (случайные из списка, одни и те же для этой комнаты на карте). 0 — все.")]
        [Min(0)] public int purchasesShown;
        [Tooltip("Сколько контрактов показать. 0 — все.")]
        [Min(0)] public int contractsShown;

        public override RoomType Type => RoomType.Marketing;
    }
}
