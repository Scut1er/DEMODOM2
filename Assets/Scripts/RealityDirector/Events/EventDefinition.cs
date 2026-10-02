using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.NPC;
using UnityEngine;

namespace RealityDirector.Events
{
    public enum TargetType
    {
        Object,
        Actor,
        Global
    }

    [CreateAssetMenu(menuName = "RealityDirector/Event", fileName = "Event")]
    public class EventDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string hint;
        public TargetType targetType;
        public string requiredObjectId;
        public Color cardColor = Color.white;
        public Sprite cardArt;
        public List<string> tags = new List<string>();
        public List<ShowMood> moods = new List<ShowMood>();
        public float rageSeconds;
        public bool ignite;
        public int price;
        public bool starter;
        public bool limitTrait;
        public TraitId targetTrait;
        [TextArea] public string jamNote;
    }
}
