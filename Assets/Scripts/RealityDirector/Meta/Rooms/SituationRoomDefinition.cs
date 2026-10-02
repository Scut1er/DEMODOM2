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

        public override RoomType Type => RoomType.Situation;
    }
}
