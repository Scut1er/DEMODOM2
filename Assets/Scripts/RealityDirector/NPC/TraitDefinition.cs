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

    [CreateAssetMenu(menuName = "RealityDirector/Trait", fileName = "Trait")]
    public class TraitDefinition : ScriptableObject
    {
        public TraitId traitId;
        public string displayName;
        public bool isHiddenDefault;
    }
}
