using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.NPC
{
    public enum TraitId
    {
        Aggressive,
        Sentimental,
        Timid,
        Panicker,
        Jealous,
        Cowardly,
        Vain,
        Opportunist,
        Honest,
        Shy,
        Chaotic
    }

    public enum HiddenTrait
    {
        None,
        Prankster,
        Kleptomaniac,
        Singer
    }

    public enum NpcActionId
    {
        Idle,
        Panic,
        SeekFight,
        Fight,
        Emote,
        Roam
    }

    // Черта меняет чувствительность и склонности (GDD §7), а не запускает сцену сама.
    // Ассет в Resources/Content заменяет встроенную черту с тем же traitId.
    [CreateAssetMenu(menuName = "RealityDirector/Trait", fileName = "Trait")]
    public class TraitDefinition : ScriptableObject
    {
        public TraitId traitId;
        public string displayName;
        public bool isHiddenDefault;

        [Header("Чувствительность: множитель к изменениям эмоций (1 — обычная)")]
        [Tooltip("Насколько сильно растёт злость от реакций и злящих карт.")]
        public float angerGain = 1f;
        [Tooltip("Насколько сильно растёт стресс (огонь рядом, паника).")]
        public float stressGain = 1f;
        [Tooltip("Насколько сильно растёт грусть.")]
        public float sadnessGain = 1f;
        [Tooltip("Насколько сильно растёт влечение.")]
        public float attractionGain = 1f;

        [Header("Склонность в реакциях")]
        [Tooltip("Чаще лезет в драку: реакции «драка» получают бонус к весу.")]
        public bool fightProne;
        [Tooltip("Чаще паникует: реакции «паника» получают бонус к весу.")]
        public bool panicProne;

        [Header("Для игрока (карточка кандидата в касте)")]
        [Tooltip("Одна фраза: какой это человек в кадре. Пусто — встроенная фраза черты.")]
        [TextArea(1, 3)] public string shortDescription;
        [Tooltip("2–3 склонности простыми словами: «чужой флирт → злость», «легко плачет». "
                 + "Пусто — соберутся сами из чувствительности и правил реакций черты.")]
        public List<string> gameplayHints = new List<string>();
    }
}
