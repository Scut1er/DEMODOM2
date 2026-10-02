using UnityEngine;

namespace RealityDirector.Meta
{
    // Монтаж — всегда последняя комната выпуска. Выбор и порядок клипов — шаг Montage.
    [CreateAssetMenu(menuName = "RealityDirector/Rooms/Montage", fileName = "Montage_")]
    public class MontageRoomDefinition : RoomDefinition
    {
        public override RoomType Type => RoomType.Montage;
    }
}
