using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.NPC
{
    [Serializable]
    public class ReactionRule
    {
        public string eventTag;
        public TraitId requiredTrait;
        public bool requireTargetSelf;
        public bool requireRage;
        public NpcActionId action;
        public string emote;
        public int priority;
    }

    [CreateAssetMenu(menuName = "RealityDirector/Reaction Rule Set", fileName = "ReactionRuleSet")]
    public class ReactionRuleSet : ScriptableObject
    {
        public TraitId trait;
        public List<ReactionRule> rules = new List<ReactionRule>();
    }
}
