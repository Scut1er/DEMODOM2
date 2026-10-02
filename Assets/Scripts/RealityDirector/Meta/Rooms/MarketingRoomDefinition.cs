using UnityEngine;

namespace RealityDirector.Meta
{
    // Маркетинг: покупки и спонсорские контракты. Пока открывает старый магазин карт; офферы — шаг Marketing room.
    [CreateAssetMenu(menuName = "RealityDirector/Rooms/Marketing", fileName = "Marketing_")]
    public class MarketingRoomDefinition : RoomDefinition
    {
        public override RoomType Type => RoomType.Marketing;
    }
}
