using RealityDirector.Core;
using RealityDirector.NPC;
using UnityEngine;

namespace RealityDirector.Events
{
    public class EventExecutor : MonoBehaviour
    {
        public void Play(EventDefinition def, Interactable targetObject, NPCController targetActor)
        {
            if (def == null)
                return;

            if (def.ignite && targetObject != null)
                targetObject.Ignite();

            if (def.rageSeconds > 0f && targetActor != null)
                targetActor.ApplyRage(def.rageSeconds);

            EventBus.Publish(new WorldEvent
            {
                eventId = def.id,
                tags = def.tags,
                targetActorId = targetActor != null ? targetActor.Id : null,
                targetObjectId = targetObject != null ? targetObject.Id : null,
                time = Time.time
            });
        }
    }
}
