using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.NPC
{
    // Событие + черта → реакция и сдвиг эмоций (GDD 0.6: WorldEvent + Trait + Emotion → Reaction → state change).
    [Serializable]
    public class ReactionRule
    {
        [Tooltip("Тег события (Fire, Conflict, Crying, Hug, Fight…), на который реагирует участник.")]
        public string eventTag;
        [Tooltip("Только если событие направлено на этого участника (карта по нему).")]
        public bool requireTargetSelf;
        [Tooltip("Только если участник под злящей картой.")]
        public bool requireRage;
        [Tooltip("Только если событие про других: участник не источник и не цель (ревность к чужим объятиям).")]
        public bool othersOnly;
        public NpcActionId action;
        public string emote;
        [Tooltip("Вес: из подходящих правил побеждает самое весомое (плюс склонности черты и эмоции).")]
        public int priority;

        [Header("Сдвиг эмоций (умножается на чувствительность черты). Всё 0 — по действию: паника +стресс, драка +злость")]
        public int anger;
        public int stress;
        public int sadness;
        public int attraction;
    }

    // Реакции одной черты. Ассет в Resources/Content заменяет встроенный набор с той же чертой —
    // новые реакции добавляются данными, без правки NPCController.
    [CreateAssetMenu(menuName = "RealityDirector/Reaction Rule Set", fileName = "ReactionRuleSet")]
    public class ReactionRuleSet : ScriptableObject
    {
        public TraitId trait;
        public List<ReactionRule> rules = new List<ReactionRule>();
    }
}
