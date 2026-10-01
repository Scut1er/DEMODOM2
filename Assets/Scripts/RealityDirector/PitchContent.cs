using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Events;
using RealityDirector.NPC;
using RealityDirector.Util;
using UnityEngine;

namespace RealityDirector
{
    public class PitchContent
    {
        public TraitDefinition Aggressive;
        public TraitDefinition Sentimental;
        public ReactionRuleSet AggressiveRules;
        public ReactionRuleSet SentimentalRules;
        public EventDefinition Provoke;
        public EventDefinition FridgeFire;
        public EventDefinition NoHotWater;
        public EventDefinition OpenBathroom;
        public EventDefinition[] Hand;

        // Jam EventCatalog, не спавнить в питче:
        // fridge_fire, no_hot_water, provoke, cut_wifi, spoiled_food, broken_ac,
        // love_triangle_rumor, hidden_mic, swap_beds, steal_phone, slam_door,
        // fake_pregnancy_news, invite_ex, alcohol_in_coffee, power_outage,
        // release_rat, glue_on_chair, confession_cam, meditation_bell, double_date_challenge

        public static PitchContent Create()
        {
            var content = new PitchContent();
            content.Aggressive = Trait("trait_aggressive", TraitId.Aggressive, "агрессивный");
            content.Sentimental = Trait("trait_sentimental", TraitId.Sentimental, "сентиментальный");

            content.AggressiveRules = Rules(TraitId.Aggressive,
                Rule(MomentTags.Fire, TraitId.Aggressive, false, true, NpcActionId.SeekFight, "!!!", 20),
                Rule(MomentTags.Conflict, TraitId.Aggressive, true, false, NpcActionId.Emote, "злость", 5));

            content.SentimentalRules = Rules(TraitId.Sentimental,
                Rule(MomentTags.Fire, TraitId.Sentimental, false, false, NpcActionId.Panic, "!", 20),
                Rule(MomentTags.Misery, TraitId.Sentimental, false, false, NpcActionId.Emote, "брр, холодно", 5));

            content.Provoke = Event("provoke", "Разозлить", "клик по Злому", TargetType.Actor, null,
                new Color(0.62f, 0.16f, 0.16f, 1f), 90f, false, IllustratedArt.IconAnger, MomentTags.Conflict);
            content.FridgeFire = Event("fridge_fire", "Поджог", "клик по холодильнику", TargetType.Object, "fridge",
                new Color(0.72f, 0.32f, 0.12f, 1f), 0f, true, IllustratedArt.IconFire, MomentTags.Fire, MomentTags.Chaos);
            content.NoHotWater = Event("no_hot_water", "Нет воды", "сразу на весь дом", TargetType.Global, null,
                new Color(0.16f, 0.32f, 0.5f, 1f), 0f, false, IllustratedArt.IconWater, MomentTags.Misery);
            content.OpenBathroom = Event("open_bathroom", "Ванная", "клик по заколоченной двери", TargetType.Object, "bath_door",
                new Color(0.45f, 0.3f, 0.16f, 1f), 0f, false, IllustratedArt.IconDoor);

            content.Hand = new[] { content.Provoke, content.FridgeFire, content.NoHotWater, content.OpenBathroom };
            return content;
        }

        public void DestroyAssets()
        {
            Object.Destroy(Aggressive);
            Object.Destroy(Sentimental);
            Object.Destroy(AggressiveRules);
            Object.Destroy(SentimentalRules);
            Object.Destroy(Provoke);
            Object.Destroy(FridgeFire);
            Object.Destroy(NoHotWater);
            Object.Destroy(OpenBathroom);
        }

        static TraitDefinition Trait(string id, TraitId traitId, string displayName)
        {
            var trait = ScriptableObject.CreateInstance<TraitDefinition>();
            trait.name = id;
            trait.traitId = traitId;
            trait.displayName = displayName;
            return trait;
        }

        static ReactionRuleSet Rules(TraitId trait, params ReactionRule[] rules)
        {
            var set = ScriptableObject.CreateInstance<ReactionRuleSet>();
            set.trait = trait;
            set.rules = new List<ReactionRule>(rules);
            return set;
        }

        static ReactionRule Rule(string tag, TraitId trait, bool self, bool rage, NpcActionId action, string emote, int priority)
        {
            return new ReactionRule
            {
                eventTag = tag,
                requiredTrait = trait,
                requireTargetSelf = self,
                requireRage = rage,
                action = action,
                emote = emote,
                priority = priority
            };
        }

        static EventDefinition Event(string id, string title, string hint, TargetType target, string objectId, Color color, float rage, bool ignite, Sprite art, params string[] tags)
        {
            var def = ScriptableObject.CreateInstance<EventDefinition>();
            def.name = id;
            def.id = id;
            def.displayName = title;
            def.hint = hint;
            def.targetType = target;
            def.requiredObjectId = objectId;
            def.cardColor = color;
            def.cardArt = art;
            def.rageSeconds = rage;
            def.ignite = ignite;
            def.tags = new List<string>(tags);
            return def;
        }
    }
}
