using UnityEngine;

namespace RealityDirector.Meta
{
    // Текстовое событие. Выборы и последствия — следующий шаг (Event room).
    [CreateAssetMenu(menuName = "RealityDirector/Rooms/Event", fileName = "Event_")]
    public class EventRoomDefinition : RoomDefinition
    {
        public override RoomType Type => RoomType.Event;
    }
}
