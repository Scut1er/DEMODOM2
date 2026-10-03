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
        public string cardId;
        public string title;
        public string blurb;
        public OfferKind kind;
        public int price;
        public int minReputation;
        public int payout;
        public int scoreHit;
    }

    // Покупка за нал и спонсорский контракт. Контракт не платит сразу.
    [CreateAssetMenu(menuName = "RealityDirector/Rooms/Marketing", fileName = "Marketing_")]
    public class MarketingRoomDefinition : RoomDefinition
    {
        public List<MarketingOffer> offers = new List<MarketingOffer>();

        public override RoomType Type => RoomType.Marketing;
    }
}
