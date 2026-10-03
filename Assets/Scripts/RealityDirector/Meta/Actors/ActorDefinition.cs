using System.Collections.Generic;
using RealityDirector.NPC;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Участник шоу. Создавать: ПКМ → Create → RealityDirector → Character. Класть в Resources/Content/Characters.
    // Из ассета берутся хаб, выбор каста и участники в квартире (главная и скрытая черта).
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
        [Tooltip("Наряд (тело): body, body2, body3… → <наряд>_neutral, _happy, _mad, _sad, _scared в Resources/Art/Characters. Пусто — body. Нет позы — нейтральная этого наряда.")]
        public string bodyPrefix = "body";

        [Header("Черты (строки под портретом в хабе)")]
        [Tooltip("Видимые черты.")]
        public List<string> visibleTraits = new List<string>();
        [Tooltip("Строка про скрытую черту. Пусто — не показывать.")]
        public string hiddenTraitLabel = "скрытая черта: ???";

        [Header("Поведение в съёмке")]
        [Tooltip("Главная черта: по ней квартира выбирает реакции участника.")]
        public TraitId mainTrait;
        [Tooltip("Скрытая черта — игрок узнаёт её по ходу сезона.")]
        public HiddenTrait hiddenTrait;

        [Header("Кастинг")]
        [Tooltip("Порядок в списке участников и в касте выпуска (меньше — раньше).")]
        public int order;
        [Tooltip("Выключен — участника нет в хабе и в касте.")]
        public bool available = true;
    }
}
