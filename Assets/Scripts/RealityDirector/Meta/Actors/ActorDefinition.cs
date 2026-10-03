using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Участник шоу. Создавать: ПКМ → Create → RealityDirector → Character. Класть в Resources/Content/Characters.
    // Сейчас из ассета берутся хаб и каст выпуска. В съёмке (квартира) пока живут только npc_zloi и npc_dobryak —
    // их поведение задано в коде квартиры.
    [CreateAssetMenu(menuName = "RealityDirector/Character", fileName = "Character_")]
    public class ActorDefinition : ContentDefinition
    {
        [Tooltip("Имя в хабе.")]
        public string displayName = "Новый участник";

        [Header("Арт")]
        [Tooltip("Портрет для хаба. Пусто — берётся голова из Resources/Art/Characters по artPrefix.")]
        public Sprite portrait;
        [Tooltip("Префикс файлов арта: zloi → zloi_happy, zloi_mad… в Resources/Art/Characters.")]
        public string artPrefix;

        [Header("Черты (строки под портретом в хабе)")]
        [Tooltip("Видимые черты.")]
        public List<string> visibleTraits = new List<string>();
        [Tooltip("Строка про скрытую черту. Пусто — не показывать.")]
        public string hiddenTraitLabel = "скрытая черта: ???";

        [Header("Кастинг")]
        [Tooltip("Порядок в списке участников и в касте выпуска (меньше — раньше).")]
        public int order;
        [Tooltip("Выключен — участника нет в хабе и в касте.")]
        public bool available = true;
    }
}
