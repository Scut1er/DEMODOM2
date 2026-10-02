using UnityEngine;

namespace RealityDirector.Meta
{
    // Магазин выпуска: разовые карты и спонсоры за нал. В колоду сезона не попадают.
    [CreateAssetMenu(menuName = "RealityDirector/Rooms/Marketing", fileName = "Marketing_")]
    public class MarketingRoomDefinition : RoomDefinition
    {
        public override RoomType Type => RoomType.Marketing;
    }
}
