using System;
using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Events;
using RealityDirector.Meta;
using RealityDirector.NPC;
using RealityDirector.UI;
using UnityEngine;

namespace RealityDirector.Cards
{
    // Постановка съёмки (SituationRoomDefinition): с чего начинается комната — кто где стоит, кто на взводе,
    // кто кому враг, что уже стоит в квартире, где «без камер». Одна квартира — разные съёмки.
    public partial class CardStage
    {
        public void ApplySetup(SituationRoomDefinition def, Func<string, EventDefinition> findCard, Action openBed, Action openBath)
        {
            if (def == null)
                return;
            if (def.openBedroom)
                openBed?.Invoke();
            if (def.openBathroom)
                openBath?.Invoke();

            var cast = Alive();
            var summary = new List<string>();
            var perRoom = new Dictionary<Room, int>();
            if (def.actors != null)
            {
                foreach (var a in def.actors)
                {
                    if (a == null)
                        continue;
                    foreach (var npc in Targets(a, cast))
                    {
                        if (a.room != HouseRoom.Any)
                        {
                            var room = ToRoom(a.room);
                            perRoom.TryGetValue(room, out int n);
                            perRoom[room] = n + 1;
                            Vector2 spot = HouseMap.Center(room) + new Vector2((n % 2 == 0 ? -1f : 1f) * (0.55f + 0.5f * (n / 2)), -0.35f);
                            spot = HouseMap.Clamp(room, spot);
                            npc.transform.position = spot;
                            npc.Home = spot;
                        }

                        Add(npc, a);
                        string mood = Mood(a);
                        if (mood != null && !summary.Contains(npc.DisplayName + " " + mood))
                            summary.Add(npc.DisplayName + " " + mood);
                        if (a.states != null)
                        {
                            foreach (var s in a.states)
                            {
                                if (!string.IsNullOrEmpty(s))
                                    npc.AddState(s, 0f);
                            }
                        }
                    }
                }
            }

            if (cast.Count >= 2 && (def.hostility != 0 || def.trust != 0))
            {
                var a = cast[0];
                var b = cast[1];
                a.Rival = b;
                b.Rival = a;
                a.ShiftBond(def.hostility, def.trust);
                b.ShiftBond(def.hostility, def.trust);
                bool war = def.hostility > def.trust;
                _fx.Link(a, b, war ? "Bolt" : "Handshake", war ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.85f, 0.4f), 4f);
                summary.Add(war ? "между " + a.DisplayName + " и " + b.DisplayName + " вражда" : a.DisplayName + " и " + b.DisplayName + " доверяют друг другу");
            }

            if (def.props != null)
            {
                foreach (var p in def.props)
                {
                    var card = p != null && findCard != null ? findCard(p.cardId) : null;
                    if (card == null)
                        continue;
                    var room = p.room == HouseRoom.Any ? Room.Living : ToRoom(p.room);
                    var prop = Place(card, HouseMap.RandomPoint(room));
                    if (prop != null)
                    {
                        prop.Dressing = true;
                        summary.Add(StageProp.LabelOf(prop.Kind) + " — " + HouseMap.Name(room).ToLowerInvariant());
                    }
                }
            }

            Zones(def.privateRooms, "Private", summary);
            Zones(def.pressureRooms, "Pressure", summary);

            // Стартовые события — участники реагируют по своим чертам (те же правила, что у карт).
            if (def.startEvents != null && def.startEvents.Count > 0 && cast.Count > 0)
            {
                var tags = new List<string>();
                foreach (var t in def.startEvents)
                {
                    if (!string.IsNullOrEmpty(t))
                        tags.Add(t.Trim());
                }

                if (tags.Count > 0)
                {
                    StartCoroutine(Later(1.2f, () =>
                    {
                        var live = Alive();
                        if (live.Count == 0)
                            return;
                        EventBus.Publish(new WorldEvent
                        {
                            eventId = "setup_" + def.Id,
                            tags = tags,
                            sourceActorId = live[0].Id,
                            targetActorId = live.Count > 1 ? live[1].Id : null,
                            locus = live[0].transform.position,
                            time = Time.time
                        });
                        EscalateAll();
                    }));
                    var names = new List<string>();
                    foreach (var t in tags)
                        names.Add(CardBrief.TagName(t));
                    summary.Add("на старте: " + string.Join(", ", names));
                }
            }

            // Плашка постановки: название съёмки, задача и что уже не так в доме.
            var lines = new List<string>();
            lines.Add(string.IsNullOrEmpty(def.goal) ? def.subtitle : def.goal);
            for (int i = 0; i < summary.Count && i < 4; i++)
                lines.Add(summary[i]);
            _fx.Slate("СЪЁМКА: " + (def.title ?? "").ToUpperInvariant(), lines, UiKit.Gold, 6.5f);
        }

        static IEnumerable<NPCController> Targets(SituationActorSetup a, List<NPCController> cast)
        {
            if (a.castSlot < 0)
                return cast;
            return a.castSlot < cast.Count ? new[] { cast[a.castSlot] } : new NPCController[0];
        }

        static void Add(NPCController npc, SituationActorSetup a)
        {
            npc.Stress = Mathf.Clamp(npc.Stress + a.stress, 0, 100);
            npc.Anger = Mathf.Clamp(npc.Anger + a.anger, 0, 100);
            npc.Sadness = Mathf.Clamp(npc.Sadness + a.sadness, 0, 100);
            npc.Attraction = Mathf.Clamp(npc.Attraction + a.attraction, 0, 100);
            npc.Confidence = Mathf.Clamp(npc.Confidence + a.confidence, 0, 100);
            npc.SelfControl = Mathf.Clamp(npc.SelfControl + a.selfControl, 0, 100);
        }

        static string Mood(SituationActorSetup a)
        {
            int top = Mathf.Max(Mathf.Max(a.anger, a.stress), Mathf.Max(a.sadness, a.attraction));
            if (top < 10)
                return a.stress <= -10 ? "спокоен" : null;
            if (top == a.anger)
                return "на взводе";
            if (top == a.stress)
                return "нервничает";
            if (top == a.sadness)
                return "грустит";
            return "к кому-то тянется";
        }

        void Zones(List<HouseRoom> rooms, string key, List<string> summary)
        {
            if (rooms == null)
                return;
            foreach (var r in rooms)
            {
                if (r == HouseRoom.Any)
                    continue;
                var room = ToRoom(r);
                foreach (var npc in Alive())
                {
                    if (RoomAt(npc.transform.position) != room)
                        continue;
                    if (key == "Private")
                    {
                        bool shy = npc.Trait != null && (npc.Trait.traitId == TraitId.Shy || npc.Trait.traitId == TraitId.Timid || npc.Trait.traitId == TraitId.Panicker);
                        npc.Stress = Mathf.Clamp(npc.Stress - (shy ? 15 : 5), 0, 100);
                    }
                    else
                    {
                        npc.Stress = Mathf.Clamp(npc.Stress + 8, 0, 100);
                    }
                }

                var shade = _fx.Shade(HouseMap.Whole(room), key == "Private" ? new Color(0.6f, 0.4f, 1f, 0.07f) : new Color(1f, 0.35f, 0.25f, 0.06f), 2, 9999f);
                _junk.Add(shade.gameObject);
                summary.Add(HouseMap.Name(room).ToLowerInvariant() + (key == "Private" ? " — без камер" : " — под давлением"));
            }
        }

        static Room ToRoom(HouseRoom r)
        {
            switch (r)
            {
                case HouseRoom.Kitchen: return Room.Kitchen;
                case HouseRoom.Bedroom: return Room.Bedroom;
                case HouseRoom.Bathroom: return Room.Bathroom;
                default: return Room.Living;
            }
        }

        // Объект карты окружения без розыгрыша: аура, действия людей, ловушка, спонсор — по эффектам самой карты.
        public StageProp Place(EventDefinition def, Vector2 at)
        {
            if (def == null)
                return null;
            var room = RoomAt(at);
            if (room == Room.None)
                room = Room.Living;
            bool spawn = HasEffect(def, CardEffectType.SpawnObject);
            var prop = Spawn(def, at, room, !spawn);
            if (prop == null)
                return null;
            foreach (var e in def.effects ?? new List<CardEffect>())
            {
                if (e == null)
                    continue;
                switch (e.type)
                {
                    case CardEffectType.CreateAura:
                        prop.EnableAura();
                        break;
                    case CardEffectType.NpcInteraction:
                    case CardEffectType.EventCandidate:
                    case CardEffectType.BehaviourWeights:
                        prop.EnableUse();
                        break;
                    case CardEffectType.TriggerZone:
                        prop.EnableTrap();
                        break;
                    case CardEffectType.SponsorVisibility:
                        prop.Sponsor = true;
                        break;
                }
            }

            return prop;
        }
    }
}
