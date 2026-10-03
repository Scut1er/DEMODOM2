using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Meta
{
    [Serializable]
    public class EventChoice
    {
        public string label;
        [TextArea(1, 3)] public string resultText;
        public List<Effect> effects = new List<Effect>();
    }

    // Текстовый квест: картинка, текст, 2–3 выбора. В футаж не пишется.
    [CreateAssetMenu(menuName = "RealityDirector/Rooms/Event", fileName = "Event_")]
    public class EventRoomDefinition : RoomDefinition
    {
        [TextArea(3, 8)] public string body;
        public List<EventChoice> choices = new List<EventChoice>();

        public override RoomType Type => RoomType.Event;
    }
}
