using UnityEngine;

namespace RealityDirector.NPC
{
    public enum TraitId
    {
        Aggressive,
        Sentimental,
        Timid
    }

    public enum NpcActionId
    {
        Idle,
        Panic,
        SeekFight,
        Fight,
        Emote
    }

    [CreateAssetMenu(menuName = "RealityDirector/Trait", fileName = "Trait")]
    public class TraitDefinition : ScriptableObject
    {
        public TraitId traitId;
        public string displayName;
        public bool isHiddenDefault;
    }
}
