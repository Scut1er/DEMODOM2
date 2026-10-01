using System.Collections.Generic;
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
        public float rageSeconds;
        public bool ignite;
        [TextArea] public string jamNote;
    }
}
