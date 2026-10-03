using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Съёмка: основной геймплей (кор). Здесь — только то, что нужно мете для карты и запуска сцены.
    [CreateAssetMenu(menuName = "RealityDirector/Rooms/Situation", fileName = "Situation_")]
    public class SituationRoomDefinition : RoomDefinition
    {
        [Header("Съёмка")]
        [Tooltip("Сцена, которая загружается при входе.")]
        public string scene = SceneFlow.Episode;
        [Tooltip("Роли для этой съёмки. Не имена: «инициатор — вспыльчивый».")]
        [TextArea(1, 3)] public string roleBrief;

        public override RoomType Type => RoomType.Situation;
    }
}
