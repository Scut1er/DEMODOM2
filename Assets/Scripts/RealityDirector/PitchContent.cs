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
            content.LoadAuthored();
            content.Aggressive = content.TraitOf(TraitId.Aggressive);
            content.Sentimental = content.TraitOf(TraitId.Sentimental);
            content.Panicker = content.TraitOf(TraitId.Panicker);
            content.AggressiveRules = content.RulesFor(TraitId.Aggressive);
            content.SentimentalRules = content.RulesFor(TraitId.Sentimental);
            content.PanickerRules = content.RulesFor(TraitId.Panicker);

            content.Provoke = Event("provoke", "Разозлить", "клик по агрессивному", TargetType.Actor, null,
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
            content.Confession = Event("confession_cam", "Исповедь", "клик по паникёру", TargetType.Actor, null,
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
            content.Guest.cost = 3;
            content.Vote.price = 180;
            content.Vote.cost = 3;
            content.SponsorCola.cost = 2;
            content.SponsorEnergy.cost = 1;
            content.SponsorShip.cost = 3;

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

        // Черты и реакции: ассеты дизайнера в Resources/Content (TraitDefinition, ReactionRuleSet) важнее встроенных.
        readonly List<TraitDefinition> _authoredTraits = new List<TraitDefinition>();
        readonly List<ReactionRuleSet> _authoredSets = new List<ReactionRuleSet>();

        void LoadAuthored()
        {
            _authoredTraits.AddRange(Resources.LoadAll<TraitDefinition>("Content"));
            _authoredSets.AddRange(Resources.LoadAll<ReactionRuleSet>("Content"));
        }

        public TraitDefinition TraitOf(TraitId id)
        {
            for (int i = 0; i < _authoredTraits.Count; i++)
            {
                if (_authoredTraits[i] != null && _authoredTraits[i].traitId == id)
                    return _authoredTraits[i];
            }

            for (int i = 0; i < _traits.Count; i++)
            {
                if (_traits[i].traitId == id)
                    return _traits[i];
            }

            var trait = BuiltInTrait(id);
            _traits.Add(trait);
            return trait;
        }

        public ReactionRuleSet RulesFor(TraitId id)
        {
            for (int i = 0; i < _authoredSets.Count; i++)
            {
                if (_authoredSets[i] != null && _authoredSets[i].trait == id)
                    return _authoredSets[i];
            }

            for (int i = 0; i < _sets.Count; i++)
            {
                if (_sets[i].trait == id)
                    return _sets[i];
            }

            var set = Rules(id, BuiltInRules(id));
            _sets.Add(set);
            return set;
        }

        // Все одиннадцать черт — для редакторских подсказок («кто как отреагирует на карту»).
        public List<ReactionRuleSet> AllRuleSets()
        {
            var list = new List<ReactionRuleSet>();
            foreach (TraitId id in System.Enum.GetValues(typeof(TraitId)))
                list.Add(RulesFor(id));
            return list;
        }

        // Встроенные черты (GDD §7): чувствительность и склонности. Ассет с тем же traitId их заменяет.
        static TraitDefinition BuiltInTrait(TraitId id)
        {
            switch (id)
            {
                case TraitId.Aggressive: return Trait(id, "агрессивный", anger: 1.3f, fight: true);
                case TraitId.Sentimental: return Trait(id, "сентиментальный", sadness: 1.4f);
                case TraitId.Timid: return Trait(id, "робкий", stress: 1.3f);
                case TraitId.Panicker: return Trait(id, "паникер", stress: 1.4f, panic: true);
                case TraitId.Jealous: return Trait(id, "ревнивый", anger: 1.2f, fight: true);
                case TraitId.Cowardly: return Trait(id, "трус", stress: 1.5f, panic: true);
                case TraitId.Vain: return Trait(id, "тщеславный", attraction: 1.2f);
                case TraitId.Opportunist: return Trait(id, "оппортунист");
                case TraitId.Honest: return Trait(id, "честный");
                case TraitId.Shy: return Trait(id, "застенчивый", stress: 1.3f, panic: true);
                default: return Trait(id, "хаотичный", anger: 1.1f, fight: true);
            }
        }

        // Встроенные реакции: у каждой черты свои, на теги, которые реально дают карты и поведение участников.
        // Злость/стресс/грусть/влечение — сдвиг эмоций при реакции. Ассет ReactionRuleSet с той же чертой их заменяет.
        static ReactionRule[] BuiltInRules(TraitId id)
        {
            const NpcActionId Emote = NpcActionId.Emote;
            const NpcActionId Panic = NpcActionId.Panic;
            const NpcActionId Fight = NpcActionId.SeekFight;
            switch (id)
            {
                case TraitId.Aggressive:
                    return new[]
                    {
                        R(MomentTags.Fire, Fight, "!!!", 20, anger: 18, rage: true),
                        R(MomentTags.Fire, Emote, "горит?!", 12, anger: 10, stress: 6),
                        R(MomentTags.Conflict, Emote, "злость", 8, anger: 14, self: true),
                        R(MomentTags.Conflict, Emote, "злость", 4, anger: 8),
                        R("Humiliation", Fight, "ну всё!", 16, anger: 20, self: true),
                        R(MomentTags.Fight, Emote, "врежь ему!", 6, anger: 8, others: true),
                        R(MomentTags.Crying, Emote, "ой, всё", 3, anger: 5, others: true)
                    };
                case TraitId.Sentimental:
                    return new[]
                    {
                        R(MomentTags.Fire, Panic, "слёзы", 20),
                        R(MomentTags.Crying, Panic, "слёзы", 20, self: true),
                        R(MomentTags.Crying, Emote, "бедняжка…", 7, sadness: 12, others: true),
                        R(MomentTags.Fight, Emote, "не надо!", 8, stress: 10, sadness: 10, others: true),
                        R("Confession", Panic, "слёзы", 14, sadness: 16, self: true),
                        R(MomentTags.Misery, Emote, "брр, холодно", 5, sadness: 6),
                        R(MomentTags.Warmth, Emote, "уют", 5, stress: -6, attraction: 6)
                    };
                case TraitId.Panicker:
                    return new[]
                    {
                        R(MomentTags.Fire, Panic, "ааа", 20),
                        R(MomentTags.Fight, Panic, "ааа, драка!", 14, stress: 14, others: true),
                        R(MomentTags.Chaos, Emote, "что происходит?!", 6, stress: 10),
                        R(MomentTags.Conflict, Emote, "только не ссорьтесь", 5, stress: 8),
                        R(MomentTags.Misery, Emote, "брр, холодно", 5, stress: 4),
                        R(MomentTags.Warmth, Emote, "уют", 5, stress: -6)
                    };
                case TraitId.Jealous:
                    return new[]
                    {
                        R(MomentTags.Hug, Fight, "ревную!!!", 22, anger: 20, rage: true, others: true),
                        R(MomentTags.Hug, Emote, "а это что?!", 12, anger: 16, others: true),
                        R("Romance", Emote, "это что ещё?", 12, anger: 14),
                        R("Flirt", Emote, "я всё вижу", 12, anger: 14, others: true),
                        R("Jealousy", Emote, "злость", 14, anger: 18),
                        R("Secret", Emote, "я всё вижу", 8, anger: 10),
                        R(MomentTags.Warmth, Emote, "ну-ну", 4, anger: 6, others: true),
                        R(MomentTags.Fire, Fight, "!!!", 20, anger: 18, rage: true),
                        R(MomentTags.Fire, Emote, "кто это сделал?!", 12, anger: 10, stress: 6),
                        R(MomentTags.Conflict, Emote, "злость", 8, anger: 12, self: true)
                    };
                case TraitId.Vain:
                    return new[]
                    {
                        R("Public", Emote, "мой ракурс!", 10, attraction: 8),
                        R("Camera", Emote, "мой ракурс!", 10, attraction: 8),
                        R("Attention", Emote, "все на меня", 8, attraction: 6),
                        R(MomentTags.Fire, Panic, "мои волосы!", 16, stress: 12),
                        R(MomentTags.Conflict, Emote, "как ты смеешь", 10, anger: 14, self: true),
                        R("Humiliation", Emote, "как ты смеешь", 14, anger: 18, self: true),
                        R(MomentTags.Hug, Emote, "а на меня смотрят?", 6, anger: 6, others: true),
                        R(MomentTags.Crying, Emote, "только не в кадре", 4, anger: 4, others: true),
                        R(MomentTags.Fight, Emote, "фу, грубо", 5, stress: 6, others: true)
                    };
                case TraitId.Cowardly:
                    return new[]
                    {
                        R(MomentTags.Fire, Panic, "спасите!", 20),
                        R(MomentTags.Fight, Panic, "я ни при чём!", 16, stress: 15, others: true),
                        R(MomentTags.Conflict, Panic, "не бейте!", 14, stress: 14, self: true),
                        R(MomentTags.Conflict, Emote, "я лучше отойду", 5, stress: 8),
                        R(MomentTags.Chaos, Panic, "бежим!", 10, stress: 10),
                        R("Pressure", Emote, "ой, нет", 8, stress: 10)
                    };
                case TraitId.Shy:
                    return new[]
                    {
                        R("Public", Panic, "не смотрите…", 14, stress: 14),
                        R("Camera", Panic, "не снимайте…", 12, stress: 12),
                        R(MomentTags.Fire, Panic, "ой…", 18),
                        R(MomentTags.Conflict, Emote, "я тихо…", 6, stress: 8),
                        R(MomentTags.Hug, Emote, "ой…", 8, stress: 6, attraction: 10, self: true),
                        R("Private", Emote, "так лучше", 5, stress: -8),
                        R(MomentTags.Warmth, Emote, "уют", 5, stress: -6)
                    };
                case TraitId.Timid:
                    return new[]
                    {
                        R(MomentTags.Fire, Panic, "мамочки", 18),
                        R(MomentTags.Fight, Panic, "уйду я", 12, stress: 12, others: true),
                        R(MomentTags.Conflict, Emote, "может, не надо?", 6, stress: 8),
                        R("Pressure", Emote, "я не могу", 8, stress: 10, sadness: 4),
                        R(MomentTags.Misery, Emote, "брр", 5, sadness: 6)
                    };
                case TraitId.Chaotic:
                    return new[]
                    {
                        R(MomentTags.Fire, Fight, "!!!", 20, anger: 16, rage: true),
                        R(MomentTags.Fire, Emote, "ха-ха, горит!", 14, anger: 4, attraction: 4),
                        R(MomentTags.Chaos, Emote, "ещё!", 8, anger: 6),
                        R(MomentTags.Fight, Fight, "я тоже!", 14, anger: 16, others: true),
                        R(MomentTags.Conflict, Emote, "погнали", 6, anger: 10),
                        R("Party", Emote, "тусим!", 8, attraction: 6),
                        R("Alcohol", Emote, "наливай", 8, anger: 4, attraction: 6)
                    };
                case TraitId.Opportunist:
                    return new[]
                    {
                        R(MomentTags.Fight, Emote, "снимайте!", 10, attraction: 4, others: true),
                        R(MomentTags.Fire, Emote, "контент!", 10, stress: 4),
                        R(MomentTags.Sponsor, Emote, "реклама? беру", 8, attraction: 4),
                        R(MomentTags.Crying, Emote, "плачь в камеру", 6, others: true),
                        R("Secret", Emote, "чем заплатишь?", 8),
                        R("Betrayal", Emote, "сделка есть сделка", 8, anger: 4)
                    };
                case TraitId.Honest:
                    return new[]
                    {
                        R("Secret", Emote, "это нечестно", 10, anger: 10),
                        R("Betrayal", Emote, "это нечестно", 12, anger: 14),
                        R("Theft", Emote, "верни!", 10, anger: 12),
                        R(MomentTags.Crying, Emote, "держись", 8, sadness: 6, others: true),
                        R(MomentTags.Fire, Panic, "надо тушить!", 16, stress: 12),
                        R(MomentTags.Conflict, Emote, "давайте честно", 6, anger: 6)
                    };
                default:
                    return new ReactionRule[0];
            }
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

        // Удаляет только созданное в памяти. Ассеты дизайнера (черты, реакции из Resources) не трогает.
        public void DestroyAssets(bool immediate = false)
        {
            for (int i = 0; i < _traits.Count; i++)
                Kill(_traits[i], immediate);
            for (int i = 0; i < _sets.Count; i++)
                Kill(_sets[i], immediate);
            _traits.Clear();
            _sets.Clear();
            if (All == null)
                return;
            for (int i = 0; i < All.Length; i++)
                Kill(All[i], immediate);
        }

        static void Kill(Object obj, bool immediate)
        {
            if (obj == null)
                return;
            if (immediate)
                Object.DestroyImmediate(obj);
            else
                Object.Destroy(obj);
        }

        static void Stamp(EventDefinition def, params ShowMood[] moods)
        {
            def.moods.AddRange(moods);
        }

        static TraitDefinition Trait(TraitId traitId, string displayName, float anger = 1f, float stress = 1f, float sadness = 1f,
            float attraction = 1f, bool fight = false, bool panic = false)
        {
            var trait = ScriptableObject.CreateInstance<TraitDefinition>();
            trait.name = "trait_" + traitId.ToString().ToLowerInvariant();
            trait.traitId = traitId;
            trait.displayName = displayName;
            trait.angerGain = anger;
            trait.stressGain = stress;
            trait.sadnessGain = sadness;
            trait.attractionGain = attraction;
            trait.fightProne = fight;
            trait.panicProne = panic;
            return trait;
        }

        static ReactionRuleSet Rules(TraitId trait, params ReactionRule[] rules)
        {
            var set = ScriptableObject.CreateInstance<ReactionRuleSet>();
            set.name = "rules_" + trait.ToString().ToLowerInvariant();
            set.trait = trait;
            set.rules = new List<ReactionRule>(rules);
            return set;
        }

        static ReactionRule R(string tag, NpcActionId action, string emote, int priority, int anger = 0, int stress = 0,
            int sadness = 0, int attraction = 0, bool self = false, bool rage = false, bool others = false)
        {
            return new ReactionRule
            {
                eventTag = tag,
                requireTargetSelf = self,
                requireRage = rage,
                othersOnly = others,
                action = action,
                emote = emote,
                priority = priority,
                anger = anger,
                stress = stress,
                sadness = sadness,
                attraction = attraction
            };
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
