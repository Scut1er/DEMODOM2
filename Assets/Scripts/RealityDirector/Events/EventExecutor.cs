using RealityDirector.Cards;
using RealityDirector.Core;
using RealityDirector.NPC;
using UnityEngine;

namespace RealityDirector.Events
{
    public class EventExecutor : MonoBehaviour
    {
        // Постановка карты на площадке: кубики, эффекты из таблицы, реквизит, действия людей.
        public CardStage Stage;

        public void Play(EventDefinition def, Interactable targetObject, NPCController targetActor, Vector2? point = null)
        {
            if (def == null)
                return;

            if (def.ignite && targetObject != null)
                targetObject.Ignite();

            if (def.rageSeconds > 0f && targetActor != null)
                targetActor.ApplyRage(def.rageSeconds);

            bool placed = targetObject != null || point.HasValue;
            Vector2 locus = targetObject != null ? (Vector2)targetObject.transform.position : point ?? Vector2.zero;
            EventBus.Publish(new WorldEvent
            {
                eventId = def.id,
                tags = def.tags,
                targetActorId = targetActor != null ? targetActor.Id : null,
                targetObjectId = targetObject != null ? targetObject.Id : null,
                hasLocus = placed,
                locus = locus,
                time = Time.time
            });

            if (Stage != null)
                Stage.Play(def, targetObject, targetActor, point);
        }
    }
}
