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
        public TraitDefinition Panicker;
        public ReactionRuleSet AggressiveRules;
        public ReactionRuleSet SentimentalRules;
        public ReactionRuleSet PanickerRules;
        public EventDefinition Provoke;
        public EventDefinition FridgeFire;
        public EventDefinition NoHotWater;
        public EventDefinition OpenBathroom;
        public EventDefinition OpenBedroom;
        public EventDefinition SpoiledFood;
        public EventDefinition CutWifi;
        public EventDefinition MeditationBell;
        public EventDefinition Confession;
        public EventDefinition SponsorCola;
        public EventDefinition SponsorEnergy;
        public EventDefinition SponsorShip;
        public EventDefinition Guest;
        public EventDefinition Vote;
        public EventDefinition[] All;
        readonly List<TraitDefinition> _traits = new List<TraitDefinition>();
        readonly List<ReactionRuleSet> _sets = new List<ReactionRuleSet>();

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
            content.Panicker = Trait("trait_panicker", TraitId.Panicker, "паникер");

            content.AggressiveRules = Rules(TraitId.Aggressive,
                Rule(MomentTags.Fire, TraitId.Aggressive, false, true, NpcActionId.SeekFight, "!!!", 20),
                Rule(MomentTags.Fire, TraitId.Aggressive, false, false, NpcActionId.Emote, "горит?!", 12),
                Rule(MomentTags.Conflict, TraitId.Aggressive, true, false, NpcActionId.Emote, "злость", 8),
                Rule(MomentTags.Conflict, TraitId.Aggressive, false, false, NpcActionId.Emote, "злость", 4));

            content.SentimentalRules = Rules(TraitId.Sentimental,
                Rule(MomentTags.Fire, TraitId.Sentimental, false, false, NpcActionId.Panic, "слёзы", 20),
                Rule(MomentTags.Crying, TraitId.Sentimental, true, false, NpcActionId.Panic, "слёзы", 20),
                Rule(MomentTags.Misery, TraitId.Sentimental, false, false, NpcActionId.Emote, "брр, холодно", 5),
                Rule(MomentTags.Warmth, TraitId.Sentimental, false, false, NpcActionId.Emote, "уют", 5));

            content.PanickerRules = Rules(TraitId.Panicker,
                Rule(MomentTags.Fire, TraitId.Panicker, false, false, NpcActionId.Panic, "ааа", 20),
                Rule(MomentTags.Misery, TraitId.Panicker, false, false, NpcActionId.Emote, "брр, холодно", 5),
                Rule(MomentTags.Warmth, TraitId.Panicker, false, false, NpcActionId.Emote, "уют", 5));

            content.Provoke = Event("provoke", "Разозлить", "клик по Злому", TargetType.Actor, null,
                new Color(0.62f, 0.16f, 0.16f, 1f), 90f, false, IllustratedArt.IconAnger, MomentTags.Conflict);
            content.FridgeFire = Event("fridge_fire", "Поджог", "клик по холодильнику", TargetType.Object, "fridge",
                new Color(0.72f, 0.32f, 0.12f, 1f), 0f, true, IllustratedArt.IconFire, MomentTags.Fire, MomentTags.Chaos);
            content.NoHotWater = Event("no_hot_water", "Нет воды", "сразу на весь дом", TargetType.Global, null,
                new Color(0.16f, 0.32f, 0.5f, 1f), 0f, false, IllustratedArt.IconWater, MomentTags.Misery);
            content.OpenBathroom = Event("open_bathroom", "Ванная", "клик по заколоченной двери", TargetType.Object, "bath_door",
                new Color(0.45f, 0.3f, 0.16f, 1f), 0f, false, IllustratedArt.IconDoor);
            content.OpenBedroom = Event("open_bedroom", "Спальня", "клик по заколоченной двери", TargetType.Object, "bed_door",
                new Color(0.38f, 0.24f, 0.32f, 1f), 0f, false, IllustratedArt.IconDoor, MomentTags.Warmth);

            content.Provoke.starter = true;
            content.Provoke.limitTrait = true;
            content.Provoke.targetTrait = TraitId.Aggressive;
            content.FridgeFire.starter = true;
            content.NoHotWater.starter = true;
            content.OpenBathroom.starter = true;

            content.SpoiledFood = Event("spoiled_food", "Тухлятина", "сразу на весь дом", TargetType.Global, null,
                new Color(0.42f, 0.38f, 0.16f, 1f), 0f, false, IllustratedArt.IconWater, MomentTags.Misery);
            content.CutWifi = Event("cut_wifi", "Нет сети", "сразу на весь дом", TargetType.Global, null,
                new Color(0.28f, 0.22f, 0.38f, 1f), 0f, false, IllustratedArt.IconAnger, MomentTags.Conflict);
            content.MeditationBell = Event("meditation_bell", "Колокол", "сразу на весь дом", TargetType.Global, null,
                new Color(0.2f, 0.42f, 0.28f, 1f), 0f, false, IllustratedArt.IconFamily, MomentTags.Warmth);
            content.Confession = Event("confession_cam", "Исповедь", "клик по Добряку", TargetType.Actor, null,
                new Color(0.2f, 0.38f, 0.55f, 1f), 0f, false, IllustratedArt.IconTear, MomentTags.Crying);
            content.Confession.limitTrait = true;
            content.Confession.targetTrait = TraitId.Panicker;

            content.SpoiledFood.price = 120;
            content.CutWifi.price = 110;
            content.MeditationBell.price = 140;
            content.Confession.price = 160;
            content.OpenBedroom.price = 100;
            content.SpoiledFood.runPrice = 35;
            content.CutWifi.runPrice = 30;
            content.MeditationBell.runPrice = 40;

            content.SponsorCola = Sponsor("sponsor_cola", "Банка колы", new Color(0.75f, 0.12f, 0.14f, 1f),
                IllustratedArt.IconWater, 45, 110, 2);
            content.SponsorEnergy = Sponsor("sponsor_energy", "Энергетик", new Color(0.85f, 0.55f, 0.1f, 1f),
                IllustratedArt.IconAnger, 25, 70, 1);
            content.SponsorShip = Sponsor("sponsor_ship", "Верфь Инферно", new Color(0.35f, 0.12f, 0.18f, 1f),
                IllustratedArt.IconAnger, 0, 200, 3);
            content.Guest = Event("invite_guest", "Гость", "сразу на весь дом", TargetType.Global, null,
                new Color(0.48f, 0.22f, 0.28f, 1f), 0f, false, IllustratedArt.IconAnger, MomentTags.Conflict, MomentTags.Chaos);
            content.Vote = Event("night_vote", "Голосование", "сразу на весь дом", TargetType.Global, null,
                new Color(0.32f, 0.18f, 0.42f, 1f), 0f, false, IllustratedArt.IconTear, MomentTags.Conflict);
            content.Guest.price = 150;
            content.Vote.price = 180;

            Stamp(content.Provoke, ShowMood.Trash, ShowMood.Drama);
            Stamp(content.FridgeFire, ShowMood.Trash);
            Stamp(content.NoHotWater, ShowMood.Drama);
            Stamp(content.OpenBathroom, ShowMood.Family);
            Stamp(content.OpenBedroom, ShowMood.Family);
            Stamp(content.SpoiledFood, ShowMood.Drama);
            Stamp(content.CutWifi, ShowMood.Trash);
            Stamp(content.MeditationBell, ShowMood.Family);
            Stamp(content.Confession, ShowMood.Drama);
            Stamp(content.Guest, ShowMood.Trash, ShowMood.Drama);
            Stamp(content.Vote, ShowMood.Drama);

            // Ассеты из Resources/Content/Cards переопределяют встроенные карты и добавляют новые (мета, CardLibrary).
            content.All = Meta.CardLibrary.Merge(new[]
            {
                content.Provoke, content.FridgeFire, content.NoHotWater, content.OpenBathroom, content.OpenBedroom,
                content.SpoiledFood, content.CutWifi, content.MeditationBell, content.Confession,
                content.SponsorCola, content.SponsorEnergy, content.SponsorShip,
                content.Guest, content.Vote
            });
            // Ассет карты затирает поля, которых в нём ещё нет. Цены нала и спонсоров возвращаем, если пусто.
            KeepRun(content.All, "spoiled_food", 35);
            KeepRun(content.All, "cut_wifi", 30);
            KeepRun(content.All, "meditation_bell", 40);
            KeepSponsor(content.All, "sponsor_cola", 45, 110, 2);
            KeepSponsor(content.All, "sponsor_energy", 25, 70, 1);
            KeepSponsor(content.All, "sponsor_ship", 0, 200, 3);
            Cat(content.All, "provoke", "Provocation");
            Cat(content.All, "cut_wifi", "Provocation");
            Cat(content.All, "fridge_fire", "Environment");
            Cat(content.All, "spoiled_food", "Environment");
            Cat(content.All, "no_hot_water", "Environment");
            Cat(content.All, "open_bathroom", "Environment");
            Cat(content.All, "open_bedroom", "Environment");
            Cat(content.All, "meditation_bell", "Social");
            Cat(content.All, "invite_guest", "Social");
            Cat(content.All, "confession_cam", "Confession");
            Cat(content.All, "night_vote", "Reveal");
            return content;
        }

        public EventDefinition Find(string id)
        {
            if (All == null)
                return null;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i] != null && All[i].id == id)
                    return All[i];
            }

            return null;
        }

        public TraitDefinition TraitOf(TraitId id)
        {
            var known = Known(id);
            if (known != null)
                return known;
            string name = id == TraitId.Jealous ? "ревнивый"
                : id == TraitId.Cowardly ? "трус"
                : id == TraitId.Vain ? "тщеславный"
                : id == TraitId.Opportunist ? "оппортунист"
                : id == TraitId.Honest ? "честный"
                : id == TraitId.Shy ? "застенчивый"
                : id == TraitId.Chaotic ? "хаотичный"
                : id == TraitId.Timid ? "робкий"
                : id.ToString();
            var trait = Trait("trait_" + id, id, name);
            _traits.Add(trait);
            return trait;
        }

        public ReactionRuleSet RulesFor(TraitId id)
        {
            if (id == TraitId.Aggressive)
                return AggressiveRules;
            if (id == TraitId.Sentimental)
                return SentimentalRules;
            if (id == TraitId.Panicker)
                return PanickerRules;
            for (int i = 0; i < _sets.Count; i++)
            {
                if (_sets[i].trait == id)
                    return _sets[i];
            }

            bool fight = id == TraitId.Jealous || id == TraitId.Chaotic || id == TraitId.Vain;
            bool panic = id == TraitId.Cowardly || id == TraitId.Shy || id == TraitId.Timid;
            var source = fight ? AggressiveRules : panic ? PanickerRules : SentimentalRules;
            var set = Rules(id);
            for (int i = 0; i < source.rules.Count; i++)
            {
                var rule = source.rules[i];
                set.rules.Add(new ReactionRule
                {
                    eventTag = rule.eventTag,
                    requiredTrait = id,
                    requireTargetSelf = rule.requireTargetSelf,
                    requireRage = rule.requireRage,
                    action = rule.action,
                    emote = rule.emote,
                    priority = rule.priority
                });
            }

            _sets.Add(set);
            return set;
        }

        TraitDefinition Known(TraitId id)
        {
            if (id == TraitId.Aggressive)
                return Aggressive;
            if (id == TraitId.Sentimental)
                return Sentimental;
            if (id == TraitId.Panicker)
                return Panicker;
            for (int i = 0; i < _traits.Count; i++)
            {
                if (_traits[i].traitId == id)
                    return _traits[i];
            }

            return null;
        }

        public List<string> StarterIds()
        {
            var ids = new List<string>();
            if (All == null)
                return ids;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i] != null && All[i].starter)
                    ids.Add(All[i].id);
            }

            return ids;
        }

        public void DestroyAssets()
        {
            Object.Destroy(Aggressive);
            Object.Destroy(Sentimental);
            Object.Destroy(Panicker);
            Object.Destroy(AggressiveRules);
            Object.Destroy(SentimentalRules);
            Object.Destroy(PanickerRules);
            for (int i = 0; i < _traits.Count; i++)
                Object.Destroy(_traits[i]);
            for (int i = 0; i < _sets.Count; i++)
                Object.Destroy(_sets[i]);
            if (All == null)
                return;
            for (int i = 0; i < All.Length; i++)
                Object.Destroy(All[i]);
        }

        static void Stamp(EventDefinition def, params ShowMood[] moods)
        {
            def.moods.AddRange(moods);
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

        static void KeepRun(EventDefinition[] all, string id, int price)
        {
            var def = Find(all, id);
            if (def != null && def.runPrice <= 0)
                def.runPrice = price;
        }

        static void KeepSponsor(EventDefinition[] all, string id, int price, int pay, int hit)
        {
            var def = Find(all, id);
            if (def == null)
                return;
            if (!def.sponsor)
            {
                def.runPrice = price;
                def.sponsor = true;
            }

            if (def.sponsorPay <= 0)
                def.sponsorPay = pay;
            if (def.sponsorScoreHit <= 0)
                def.sponsorScoreHit = hit;
        }

        static void Cat(EventDefinition[] all, string id, string category)
        {
            var def = Find(all, id);
            if (def != null && string.IsNullOrEmpty(def.category))
                def.category = category;
        }

        static EventDefinition Find(EventDefinition[] all, string id)
        {
            if (all == null)
                return null;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].id == id)
                    return all[i];
            }

            return null;
        }

        static EventDefinition Sponsor(string id, string title, Color color, Sprite art, int runPrice, int pay, int hit)
        {
            var def = Event(id, title, "сыграй и сними кадр, в эфир — только через монтаж", TargetType.Global, null, color, 0f, false, art);
            def.runPrice = runPrice;
            def.sponsor = true;
            def.sponsorPay = pay;
            def.sponsorScoreHit = hit;
            return def;
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
