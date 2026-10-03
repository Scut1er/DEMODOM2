using System;
using System.Collections.Generic;
using RealityDirector.Events;
using RealityDirector.NPC;

namespace RealityDirector.Cards
{
    // Что из данных карты игра действительно исполняет. Один список на всех:
    // Card Workshop помечает остальное «NOT RUNTIME SUPPORTED», стартовая колода и магазины
    // не выдают карту, которая на съёмке ничего не сделает.
    public static class CardRuntime
    {
        // Эффекты в комнате — CardStage.Apply.
        static readonly HashSet<CardEffectType> Room = new HashSet<CardEffectType>
        {
            CardEffectType.ChangeStat,
            CardEffectType.ChangeRelationship,
            CardEffectType.AddState,
            CardEffectType.WorldEvent,
            CardEffectType.MoveActor,
            CardEffectType.ChangeHighestNegative,
            CardEffectType.SpawnObject,
            CardEffectType.CreateAura,
            CardEffectType.TriggerZone,
            CardEffectType.NpcInteraction,
            CardEffectType.BehaviourWeights,
            CardEffectType.AddContext,
            CardEffectType.LowerPublicContext,
            CardEffectType.LockExit,
            CardEffectType.QuietRoom,
            CardEffectType.SponsorVisibility,
            CardEffectType.Check,
            CardEffectType.RevealSecret,
            CardEffectType.InviteActor,
            CardEffectType.InterruptChain,
            CardEffectType.RandomOutcome,
            CardEffectType.ForceMovement,
            CardEffectType.EventCandidate,
            CardEffectType.NextCaptureBonus
        };

        // Эффекты в руке и колоде — PitchFlow.DeckPlay.
        static readonly HashSet<CardEffectType> Deck = new HashSet<CardEffectType>
        {
            CardEffectType.ReturnHandCardToLibrary,
            CardEffectType.DrawRandom,
            CardEffectType.SearchLibrary,
            CardEffectType.RecoverUsedCard,
            CardEffectType.ProtectCard,
            CardEffectType.RetainCard,
            CardEffectType.ReduceCost,
            CardEffectType.DuplicateEffect,
            CardEffectType.MoveHandCardToUsed,
            CardEffectType.PeekLibrary,
            CardEffectType.ChooseOneToHand,
            CardEffectType.TakeSelectedIntoHand
        };

        // Эффекты, которым нужен поставленный объект (его создаёт «поставить объект» или аура).
        static readonly HashSet<CardEffectType> NeedProp = new HashSet<CardEffectType>
        {
            CardEffectType.NpcInteraction,
            CardEffectType.TriggerZone,
            CardEffectType.SponsorVisibility
        };

        // Старые карты без эффектов: у каждой своя постановка в CardStage.Legacy.
        static readonly HashSet<string> Legacy = new HashSet<string>
        {
            "provoke", "fridge_fire", "no_hot_water", "spoiled_food", "cut_wifi",
            "meditation_bell", "confession_cam", "sponsor_cola", "sponsor_energy"
        };

        // Реквизит, который квартира умеет рисовать и которым люди умеют пользоваться (StageProp).
        public static readonly HashSet<string> Props = new HashSet<string>
        {
            "AlcoholCrate", "RomanceSofa", "OpenMic", "GiftBox", "KaraokeMachine", "OilSpill",
            "RomanticSpeaker", "AnxietyLight", "HiddenCameraProp", "Spotlight", "UnattendedPhone",
            "RedButton", "Poster_Leviathan", "HellColaFridge", "Tripod", "ColaCan"
        };

        // Правила карты, которые читает игра. Остальные Workshop показывает предупреждением.
        static readonly HashSet<SpecialRule> Rules = new HashSet<SpecialRule>
        {
            SpecialRule.Retain,
            SpecialRule.Innate,
            SpecialRule.UsesProductionSlot
        };

        static readonly HashSet<CardLifetime> Lifetimes = new HashSet<CardLifetime>
        {
            CardLifetime.Permanent,
            CardLifetime.UntilBroadcast
        };

        public static bool Supports(CardEffectType type)
        {
            return Room.Contains(type) || Deck.Contains(type);
        }

        public static bool IsDeck(CardEffectType type)
        {
            return Deck.Contains(type);
        }

        public static bool Supports(SpecialRule rule)
        {
            return Rules.Contains(rule);
        }

        public static bool Supports(CardLifetime lifetime)
        {
            return Lifetimes.Contains(lifetime);
        }

        public static bool HasLegacy(string id)
        {
            return id != null && Legacy.Contains(id);
        }

        public static bool HasDeckEffect(EventDefinition def)
        {
            if (def == null || def.effects == null)
                return false;
            foreach (var e in def.effects)
            {
                if (e != null && Deck.Contains(e.type))
                    return true;
            }

            return false;
        }

        // Все эффекты карты — колодные: в комнате она ничего не ставит и не показывает.
        public static bool OnlyDeck(EventDefinition def)
        {
            if (def == null || def.effects == null || def.effects.Count == 0)
                return def != null && def.category == "DeckManagement";
            foreach (var e in def.effects)
            {
                if (e != null && !Deck.Contains(e.type))
                    return false;
            }

            return true;
        }

        static bool Has(EventDefinition def, CardEffectType type)
        {
            if (def.effects == null)
                return false;
            foreach (var e in def.effects)
            {
                if (e != null && e.type == type)
                    return true;
            }

            return false;
        }

        // Почему карта на съёмке ничего не сделает (или сделает не всё). Пусто — карта рабочая.
        public static List<string> Problems(EventDefinition def)
        {
            var list = new List<string>();
            if (def == null)
            {
                list.Add("карты нет");
                return list;
            }

            bool any = def.effects != null && def.effects.Count > 0;
            if (any)
            {
                foreach (var e in def.effects)
                {
                    if (e == null)
                        continue;
                    if (!Supports(e.type))
                        list.Add("эффект «" + e.type + "» игра не исполняет");
                    if (NeedProp.Contains(e.type) && !Has(def, CardEffectType.SpawnObject) && !Has(def, CardEffectType.CreateAura))
                        list.Add("«" + e.type + "» без «поставить объект» — нечем пользоваться");
                }

                bool spawns = Has(def, CardEffectType.SpawnObject);
                string kind = !string.IsNullOrEmpty(def.environmentId) ? def.environmentId : def.id;
                if (spawns && !Props.Contains(kind))
                    list.Add("объект «" + kind + "» квартира не рисует (environmentId)");
            }
            else if (!HasLegacy(def.id) && !def.ignite && def.rageSeconds <= 0f
                     && (def.tags == null || def.tags.Count == 0) && string.IsNullOrEmpty(def.requiredObjectId))
            {
                list.Add("нет ни эффектов, ни тегов — карта ничего не делает");
            }

            return list;
        }

        // Карта может попасть игроку: всё, что в ней написано, игра исполняет.
        public static bool Playable(EventDefinition def)
        {
            return Problems(def).Count == 0;
        }

        // Условие «только если» из таблицы: черта (Shy, Jealous… или по-русски), состояние (Drunk…),
        // рейтинг («низкий»/«высокий» — по уверенности), романтика. Несколько — через / или запятую: хватит одного.
        public static bool Holds(string onlyIf, NPCController npc, EventDefinition def)
        {
            if (string.IsNullOrEmpty(onlyIf) || npc == null)
                return true;
            bool known = false;
            foreach (var raw in onlyIf.Split('/', ','))
            {
                string t = raw.Trim();
                if (t.Length == 0)
                    continue;
                string s = t.ToLowerInvariant();
                var trait = npc.Trait != null ? npc.Trait.traitId : TraitId.Sentimental;
                if (s.Contains("shy") || s.Contains("anxious") || s.Contains("застенч") || s.Contains("тревож"))
                {
                    known = true;
                    if (trait == TraitId.Shy || trait == TraitId.Timid || trait == TraitId.Panicker || trait == TraitId.Cowardly)
                        return true;
                    continue;
                }

                if (Enum.TryParse(t, true, out TraitId id))
                {
                    known = true;
                    if (trait == id)
                        return true;
                    continue;
                }

                if (npc.Trait != null && !string.IsNullOrEmpty(npc.Trait.displayName) && npc.Trait.displayName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (npc.HasState(t))
                    return true;
                if (s.Contains("низк"))
                {
                    known = true;
                    if (npc.Confidence < 50)
                        return true;
                    continue;
                }

                if (s.Contains("высок"))
                {
                    known = true;
                    if (npc.Confidence >= 50)
                        return true;
                    continue;
                }

                if (s.Contains("романт"))
                {
                    known = true;
                    if (npc.Attraction >= 15 || (def != null && def.tags != null && def.tags.Contains("Romance")))
                        return true;
                    continue;
                }

                if (Enum.TryParse(t, true, out TraitId _) || t == NPCController.StateDrunk || t == NPCController.StateSuspicious
                    || t == NPCController.StateWet || t == NPCController.StateTrapped || t == NPCController.StateAmplified)
                    known = true;
            }

            // Непонятный текст («по контексту», «при входе») не блокирует эффект.
            return !known;
        }

        // Правила и время жизни, которые игра пока не читает (карта работает, но без этого).
        public static List<string> Ignored(EventDefinition def)
        {
            var list = new List<string>();
            if (def == null)
                return list;
            if (def.specialRules != null)
            {
                foreach (var r in def.specialRules)
                {
                    if (!Supports(r))
                        list.Add("правило «" + r + "»");
                }
            }

            if (!Supports(def.lifetime))
                list.Add("жизнь карты «" + def.lifetime + "»");
            return list;
        }
    }
}
