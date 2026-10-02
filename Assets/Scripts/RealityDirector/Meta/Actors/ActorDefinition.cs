using System.Collections.Generic;
using RealityDirector.NPC;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Участник шоу. Создавать: ПКМ → Create → RealityDirector → Character. Класть в Resources/Content/Characters.
    // Сейчас из ассета берутся хаб и кастинг. В съёмке (квартира) пока живут только npc_zloi и npc_dobryak —
    // остальные появятся там после шага 8 (вместе с кором).
    [CreateAssetMenu(menuName = "RealityDirector/Character", fileName = "Character_")]
    public class ActorDefinition : ContentDefinition
    {
        [Header("Кто это")]
        public string displayName = "Новый участник";
        [Tooltip("Архетип одной фразой: «вспыльчивый бывший боксёр».")]
        public string archetype;
        [TextArea(2, 5)] public string bio;

        [Header("Арт")]
        [Tooltip("Портрет для хаба. Пусто — берётся голова из Resources/Art/Characters по artPrefix.")]
        public Sprite portrait;
        [Tooltip("Префикс файлов арта: zloi → zloi_happy, zloi_mad… в Resources/Art/Characters.")]
        public string artPrefix;

        [Header("Черты")]
        [Tooltip("Видимые черты — строки под портретом в хабе.")]
        public List<string> visibleTraits = new List<string>();
        [Tooltip("Главная черта для реакций в съёмке (кор).")]
        public TraitId mainTrait;
        [Tooltip("Скрытая черта — игрок узнаёт её по ходу сезона.")]
        public HiddenTrait hiddenTrait;
        [Tooltip("Что видит игрок, пока скрытая черта не раскрыта.")]
        public string hiddenTraitLabel = "скрытая черта: ???";

        [Header("Кастинг")]
        [Tooltip("Порядок в списке участников (меньше — выше).")]
        public int order;
        [Tooltip("Доступен с начала сезона.")]
        public bool available = true;
        [Tooltip("Шанс попасть в предложения кастинга (для будущего выбора каста).")]
        [Min(0f)] public float castingWeight = 1f;
    }
}
