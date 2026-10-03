using System.Collections.Generic;
using System.Text;
using RealityDirector.Events;
using RealityDirector.NPC;
using UnityEngine;

namespace RealityDirector.Cards
{
    // Карта объясняет себя — по тем же данным, которые исполняет CardStage: эффекты, кубики,
    // условия, реквизит, колода. Плюс прогноз реакции каста (без точных формул и без скрытых черт).
    // Читают подсказка карты в руке (PitchUi) и дизайнерские окна (CardInsight, Card Workshop).
    public static class CardBrief
    {
        public enum Level
        {
            None,
            Low,
            Medium,
            High
        }

        public class Forecast
        {
            public NPCController actor;
            public Level level;
            public float score;
            public readonly List<string> reasons = new List<string>();
        }

        public const string Gold = "#F2D14A";
        public const string Red = "#FF6A4A";
        public const string Muted = "#A89F96";

        // ---------- Что делает ----------

        public static List<string> What(EventDefinition def)
        {
            var lines = new List<string>();
            if (def == null)
                return lines;
            if (def.id == "CARD_REVEAL_005")
            {
                lines.Add("Чужой телефон на виду. Любопытные и ревнивые полезут.");
                return lines;
            }
            var effects = def.effects ?? new List<CardEffect>();
            var dice = def.diceEffects ?? new List<DiceEffect>();
            var usedDice = new HashSet<DiceEffect>();

            // Эмоции — по получателю: «Цель: злость +1d6, стресс +1d4».
            var byWho = new List<(string who, List<string> parts)>();
            foreach (var e in effects)
            {
                if (e == null || e.type != CardEffectType.ChangeStat)
                    continue;
                var die = FindDie(dice, usedDice, DiceSubject.Stat, e.stat, null);
                string part = StageFx.StatName(e.stat) + " " + Amount(def, e.amount, die, e.stat);
                if (!string.IsNullOrEmpty(e.onlyIf))
                    part += " (если " + Condition(e.onlyIf) + ")";
                Add(byWho, Who(e.receiver, def), part);
            }

            // Кубики эмоций без своего эффекта всё равно двигают цель.
            foreach (var d in dice)
            {
                if (d == null || d.subject != DiceSubject.Stat || usedDice.Contains(d))
                    continue;
                usedDice.Add(d);
                string part = StageFx.StatName(d.stat) + " " + (d.lower ? "−" : "+") + Dice.Notation(d);
                if (!string.IsNullOrEmpty(d.onlyIf))
                    part += " (если " + Condition(d.onlyIf) + ")";
                Add(byWho, def.PlayTarget == TargetType.Actor ? "Цель" : "Все рядом", part);
            }

            foreach (var (who, parts) in byWho)
            {
                bool anger = false;
                bool stress = false;
                bool onlyHeat = parts.Count > 0;
                for (int i = 0; i < parts.Count; i++)
                {
                    string part = parts[i];
                    bool up = part.Contains("+") || part.Contains("↑");
                    if (part.StartsWith("злость ") && up)
                        anger = true;
                    else if (part.StartsWith("стресс ") && up)
                        stress = true;
                    else
                        onlyHeat = false;
                }

                if (onlyHeat && anger && stress)
                    lines.Add("Злится и заводится. Насколько — как ляжет.");
                else
                    lines.Add(who + ": " + string.Join(", ", parts));
            }

            var rel = new List<string>();
            foreach (var e in effects)
            {
                if (e == null || e.type != CardEffectType.ChangeRelationship)
                    continue;
                var die = FindDie(dice, usedDice, DiceSubject.Relationship, null, e.axis);
                bool down = e.amount < 0f || (die != null ? die.lower : false);
                string size = die != null ? Dice.Notation(die) : e.amount != 0f ? Mathf.Abs(Mathf.RoundToInt(e.amount)).ToString() : "";
                rel.Add(AxisName(e.axis) + " " + (down ? "−" : "+") + size);
            }

            if (rel.Count > 0)
                lines.Add("Пара: " + string.Join(", ", rel));

            bool spawned = false;
            foreach (var e in effects)
            {
                if (e == null)
                    continue;
                string line = Line(def, e, dice, usedDice, ref spawned);
                if (!string.IsNullOrEmpty(line) && !lines.Contains(line))
                    lines.Add(line);
            }

            // Кубики-проверки, которые не забрал ни один эффект.
            foreach (var d in dice)
            {
                if (d == null || d.subject != DiceSubject.Check || usedDice.Contains(d))
                    continue;
                if (Has(def, CardEffectType.SpawnObject))
                    continue; // бросает реквизит, когда им пользуются — уже сказано в строке объекта
                lines.Add("Бросок " + Dice.Notation(d) + ": " + CheckName(d.check));
            }

            if (effects.Count == 0)
                Legacy(def, lines);
            return lines;
        }

        static void Add(List<(string who, List<string> parts)> list, string who, string part)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].who == who)
                {
                    list[i].parts.Add(part);
                    return;
                }
            }

            list.Add((who, new List<string> { part }));
        }

        static DiceEffect FindDie(List<DiceEffect> dice, HashSet<DiceEffect> used, DiceSubject subject, ActorStat? stat, RelationshipAxis? axis)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (var d in dice)
                {
                    if (d == null || used.Contains(d) || d.subject != subject)
                        continue;
                    if (pass == 0 && stat.HasValue && d.stat != stat.Value)
                        continue;
                    if (pass == 0 && axis.HasValue && d.axis != axis.Value)
                        continue;
                    used.Add(d);
                    return d;
                }
            }

            return null;
        }

        static string Amount(EventDefinition def, float amount, DiceEffect die, ActorStat stat)
        {
            if (amount != 0f)
                return (amount < 0f ? "−" : "+") + Mathf.Abs(Mathf.RoundToInt(amount));
            if (die != null)
                return (die.lower ? "−" : "+") + Dice.Notation(die);
            return Calming(def, stat) ? "↓" : "↑";
        }

        static bool Calming(EventDefinition def, ActorStat stat)
        {
            bool negative = stat == ActorStat.Anger || stat == ActorStat.Stress || stat == ActorStat.Sadness;
            if (negative && (def.category == "Control" || (def.tags != null && (def.tags.Contains("Calm") || def.tags.Contains("Safe") || def.tags.Contains("Reconcile")))))
                return true;
            return stat == ActorStat.SelfControl;
        }

        static string Who(EffectReceiver receiver, EventDefinition def)
        {
            switch (receiver)
            {
                case EffectReceiver.AllInRoom: return def.targetType == TargetType.Global ? "Все в доме" : "Все в комнате";
                case EffectReceiver.Witnesses: return "Свидетели";
                case EffectReceiver.RandomActor: return "Случайный участник";
                default:
                    if (def.targetType == TargetType.Zone)
                        return "Все в комнате";
                    if (def.PlayTarget == TargetType.Global)
                        return "Все";
                    return "Цель";
            }
        }

        static string Line(EventDefinition def, CardEffect e, List<DiceEffect> dice, HashSet<DiceEffect> usedDice, ref bool spawned)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(e.amount));
            switch (e.type)
            {
                case CardEffectType.ChangeStat:
                case CardEffectType.ChangeRelationship:
                    return null;
                case CardEffectType.ChangeHighestNegative:
                {
                    var die = FindDie(dice, usedDice, DiceSubject.Check, null, null);
                    bool down = die != null ? die.lower : def.category == "Control";
                    return "Цель: самая сильная плохая эмоция " + (down ? "−" : "+") + (die != null ? Dice.Notation(die) : "");
                }
                case CardEffectType.AddState:
                {
                    string state = StageFx.StateTitle(e.key) ?? e.key;
                    return "Состояние: " + state + Duration(e);
                }
                case CardEffectType.RemoveState:
                    return "Снимает состояние: " + (StageFx.StateTitle(e.key) ?? e.key);
                case CardEffectType.WorldEvent:
                {
                    var tags = Split(e.key);
                    if (tags.Count == 0)
                        return null;
                    var names = new List<string>();
                    foreach (var t in tags)
                        names.Add(TagName(t));
                    return "Событие: " + string.Join(", ", names) + (tags.Contains("Public") ? " — видят все, свидетели реагируют" : " — люди реагируют по своим чертам");
                }
                case CardEffectType.MoveActor:
                    return "Уводит пару в укромный угол — наедине";
                case CardEffectType.ForceMovement:
                    return "Все срываются с мест и меняют комнату";
                case CardEffectType.SpawnObject:
                case CardEffectType.CreateAura:
                {
                    if (spawned)
                        return e.type == CardEffectType.CreateAura ? Aura(def) : null;
                    spawned = true;
                    string kind = !string.IsNullOrEmpty(def.environmentId) ? def.environmentId : def.id;
                    string name = StageProp.LabelOf(kind) ?? kind;
                    string life = def.environmentLifetime == EnvironmentLifetime.Seconds ? "на " + Mathf.RoundToInt(def.environmentSeconds) + " с" : def.environmentLifetime == EnvironmentLifetime.UntilEpisodeEnd ? "до конца выпуска" : "до конца съёмки";
                    string line = "Ставит «" + name + "» " + life + (CardStage.UsesSlot(def) ? " (занимает Production Slot)" : "");
                    if (e.type == CardEffectType.CreateAura)
                        line += ". " + Aura(def);
                    return line;
                }
                case CardEffectType.NpcInteraction:
                case CardEffectType.EventCandidate:
                case CardEffectType.BehaviourWeights:
                {
                    string kind = !string.IsNullOrEmpty(def.environmentId) ? def.environmentId : def.id;
                    string use = StageProp.UseOf(kind);
                    return use != null ? "Сами: " + use : null;
                }
                case CardEffectType.TriggerZone:
                    return "Ловушка: кто войдёт — бросок";
                case CardEffectType.AddContext:
                {
                    var names = new List<string>();
                    foreach (var k in Split(e.key))
                    {
                        string c = ContextName(k);
                        if (c != null)
                            names.Add(c);
                    }

                    return names.Count > 0 ? "Обстановка: " + string.Join(", ", names) : null;
                }
                case CardEffectType.LowerPublicContext:
                    return "Обстановка: забывают о камерах — говорят лишнее";
                case CardEffectType.LockExit:
                    return "Запирает комнату на " + Mathf.RoundToInt(def.environmentSeconds > 0f ? def.environmentSeconds : 25f) + " с — никто не выйдет";
                case CardEffectType.QuietRoom:
                    return "В комнате тише: садятся, стресс падает";
                case CardEffectType.SponsorVisibility:
                    return "Кадр с объектом засчитывается спонсору";
                case CardEffectType.Check:
                {
                    var die = FindDie(dice, usedDice, DiceSubject.Check, null, null);
                    string roll = die != null ? Dice.Notation(die) + ": " : "";
                    if (e.key == "SelfControl")
                        return "Бросок " + roll + "не выдержит — признается";
                    if (e.key == "Performance")
                        return "Бросок " + roll + "удачно — бренд прозвучит в кадре";
                    if (e.key == "OnEnter")
                        return null;
                    return "Бросок " + roll + CheckName(e.key);
                }
                case CardEffectType.RevealSecret:
                    if (e.key == "Candidate")
                        return "Кто возьмёт телефон — раскроет чужой секрет";
                    if (e.key == "Conditional")
                        return "Может раскрыть секрет цели (бросок)";
                    return "Раскрывает секрет цели при всех";
                case CardEffectType.InviteActor:
                    return e.key == "Third" ? "Приходит третий — встаёт между парой" : "Приходит бывший цели";
                case CardEffectType.InterruptChain:
                    return "Обрывает слабую ссору или разговор";
                case CardEffectType.RandomOutcome:
                {
                    var die = FindDie(dice, usedDice, DiceSubject.Check, null, null);
                    return "Случайный исход " + (die != null ? Dice.Notation(die) : "d10") + ": от конфетти до сирены";
                }
                case CardEffectType.NextCaptureBonus:
                    return "Кадр, начатый в ближайшие 10 с, — качественнее";
                case CardEffectType.ReturnHandCardToLibrary:
                    return "Рука → Библиотека: " + Cards(n) + " из руки";
                case CardEffectType.MoveHandCardToUsed:
                    return "Рука → Использовано: " + Cards(n) + " из руки";
                case CardEffectType.DrawRandom:
                    return "Библиотека → Рука: +" + Cards(n) + " наугад";
                case CardEffectType.PeekLibrary:
                    return "Смотрит " + Cards(n) + " из Библиотеки";
                case CardEffectType.SearchLibrary:
                case CardEffectType.ChooseOneToHand:
                case CardEffectType.TakeSelectedIntoHand:
                    return "Библиотека → Рука: самая сильная найденная карта";
                case CardEffectType.RecoverUsedCard:
                    return "Использовано → Библиотека (наверх): самая сильная сыгранная";
                case CardEffectType.ReduceCost:
                    return "Следующая карта дешевле на " + HellToken.Format(e.amount > 0f ? e.amount : 1f);
                case CardEffectType.DuplicateEffect:
                    return "Следующая карта сработает дважды, второй раз — бесплатно";
                case CardEffectType.RetainCard:
                case CardEffectType.ProtectCard:
                    return "Лучшая карта руки под защитой: её не сбросит, и она останется в руке на следующую съёмку";
                default:
                    return null;
            }
        }

        static string Duration(CardEffect e)
        {
            switch (e.duration)
            {
                case StateDuration.Timed: return " на " + Mathf.RoundToInt(e.seconds > 0f ? e.seconds : 25f) + " с";
                case StateDuration.UntilConsumed: return " — пока не сработает";
                case StateDuration.UntilEpisodeEnd: return " — до конца выпуска";
                default: return " — до конца съёмки";
            }
        }

        static string Aura(EventDefinition def)
        {
            var rates = StageProp.RatesFor(def);
            if (rates.Count == 0)
                return "Аура рядом с ним";
            var parts = new List<string>();
            foreach (var (stat, rate, onlyFor) in rates)
            {
                string part = StageFx.StatName(stat) + (rate > 0f ? " ↑" : " ↓");
                if (onlyFor == "shy")
                    part += " у застенчивых";
                else if (onlyFor == "vain")
                    part += " у тщеславных";
                if (!parts.Contains(part))
                    parts.Add(part);
            }

            return "Рядом: " + string.Join(", ", parts);
        }

        static void Legacy(EventDefinition def, List<string> lines)
        {
            string pitch = CardStage.LegacyPitch(def.id);
            if (!string.IsNullOrEmpty(pitch))
                lines.Add(pitch);
            if (def.rageSeconds > 0f)
                lines.Add("Цель в ярости " + Mathf.RoundToInt(def.rageSeconds) + " с — лезет в драку");
            if (def.tags != null && def.tags.Count > 0 && string.IsNullOrEmpty(pitch))
            {
                var names = new List<string>();
                foreach (var t in def.tags)
                    names.Add(TagName(t));
                lines.Add("Событие: " + string.Join(", ", names) + " — люди реагируют по своим чертам");
            }
        }

        // ---------- Как играть ----------

        public static string How(EventDefinition def)
        {
            if (def == null)
                return "";
            if (CardRuntime.OnlyDeck(def))
                return "нажми карту — сработает сразу, в руке и колоде";
            switch (def.targetType)
            {
                case TargetType.Actor: return "нажми карту → кликни по участнику";
                case TargetType.ActorPair: return "нажми карту → кликни по участнику; второй в паре — его соперник или ближайший";
                case TargetType.Zone: return "нажми карту → кликни по полу комнаты (зелёный круг — можно)";
                case TargetType.Object: return "нажми карту → кликни по предмету";
                default: return "нажми карту — сработает сразу на весь дом";
            }
        }

        public static string TargetShort(EventDefinition def)
        {
            if (def == null)
                return "";
            if (CardRuntime.OnlyDeck(def))
                return "колода";
            switch (def.targetType)
            {
                case TargetType.Actor: return "на участника";
                case TargetType.ActorPair: return "на пару";
                case TargetType.Zone: return "на комнату";
                case TargetType.Object: return "на предмет";
                default: return "на весь дом";
            }
        }

        public static string CategoryName(string category)
        {
            switch (category)
            {
                case "Provocation": return "ПРОВОКАЦИЯ";
                case "Social": return "ОБЩЕНИЕ";
                case "Control": return "КОНТРОЛЬ";
                case "Environment": return "ОКРУЖЕНИЕ";
                case "Reveal": return "РАЗОБЛАЧЕНИЕ";
                case "Comedy": return "ХАОС";
                case "Sponsor": return "СПОНСОР";
                case "DeckManagement": return "КОЛОДА";
                case "Confession": return "ИСПОВЕДЬ";
                default: return string.IsNullOrEmpty(category) ? "КАРТА" : category.ToUpperInvariant();
            }
        }

        // ---------- Условия ----------

        public static List<string> Conditions(EventDefinition def)
        {
            var list = new List<string>();
            if (def == null)
                return list;
            if (def.limitTrait)
                list.Add("только на: " + TraitName(def.targetTrait));
            if (def.targetFilters != null)
            {
                foreach (var f in def.targetFilters)
                {
                    if (f == null)
                        continue;
                    switch (f.type)
                    {
                        case TargetFilterType.ActorHasTrait: list.Add("цель с чертой «" + Condition(f.key) + "»"); break;
                        case TargetFilterType.ActorLacksTrait: list.Add("цель без черты «" + Condition(f.key) + "»"); break;
                        case TargetFilterType.ActorStatAtLeast: list.Add("цель: " + StageFx.StatName(f.stat) + " не меньше " + f.value); break;
                        case TargetFilterType.ActorStatAtMost: list.Add("цель: " + StageFx.StatName(f.stat) + " не больше " + f.value); break;
                        case TargetFilterType.ActorHasState: list.Add("цель: " + (StageFx.StateTitle(f.key) ?? f.key)); break;
                        case TargetFilterType.ActorLacksState: list.Add("цель не «" + (StageFx.StateTitle(f.key) ?? f.key) + "»"); break;
                        default: list.Add("рядом: " + f.key); break;
                    }
                }
            }

            if (def.diceEffects != null)
            {
                foreach (var d in def.diceEffects)
                {
                    if (d == null || d.stepUps == null)
                        continue;
                    foreach (var s in d.stepUps)
                    {
                        if (s == null)
                            continue;
                        string when;
                        switch (s.condition)
                        {
                            case StepUpCondition.TargetHasTrait: when = "цель " + Condition(s.key); break;
                            case StepUpCondition.TargetHasState: when = "цель " + (StageFx.StateTitle(s.key) ?? s.key); break;
                            case StepUpCondition.TargetStatAtLeast: when = StageFx.StatName(s.stat) + " цели от " + s.value; break;
                            default: when = "рядом " + (StageProp.LabelOf(s.key) ?? s.key); break;
                        }

                        list.Add("кубик крупнее (" + Dice.Notation(d) + " → " + Dice.Notation(d, Mathf.Max(1, s.steps)) + "), если " + when);
                    }
                }
            }

            // Кто сильнее откликнется на теги карты — по чертам (без скрытых).
            var tags = Tags(def);
            if (tags.Contains("Humiliation") || tags.Contains("Conflict"))
                list.Add("сильнее на агрессивных и ревнивых");
            if (tags.Contains("Flirt") || tags.Contains("Romance"))
                list.Add("ревнивые злятся, когда флиртуют другие");
            if (tags.Contains("Confession") || tags.Contains("Reveal"))
                list.Add("сентиментальные плачут, честные говорят правду");
            if (tags.Contains("Public") && (def.category == "Provocation" || def.category == "Reveal"))
                list.Add("при свидетелях сильнее");
            return list;
        }

        static string Condition(string onlyIf)
        {
            if (string.IsNullOrEmpty(onlyIf))
                return "";
            var parts = new List<string>();
            foreach (var raw in onlyIf.Split('/', ','))
            {
                string t = raw.Trim();
                if (t.Length == 0)
                    continue;
                if (System.Enum.TryParse(t, true, out TraitId id))
                    parts.Add(TraitName(id));
                else if (t.Equals("Anxious", System.StringComparison.OrdinalIgnoreCase))
                    parts.Add("тревожный");
                else
                    parts.Add(StageFx.StateTitle(t) ?? t.ToLowerInvariant());
            }

            return string.Join(" или ", parts);
        }

        public static string TraitName(TraitId id)
        {
            switch (id)
            {
                case TraitId.Aggressive: return "агрессивный";
                case TraitId.Sentimental: return "сентиментальный";
                case TraitId.Timid: return "робкий";
                case TraitId.Panicker: return "паникёр";
                case TraitId.Jealous: return "ревнивый";
                case TraitId.Cowardly: return "трус";
                case TraitId.Vain: return "тщеславный";
                case TraitId.Opportunist: return "оппортунист";
                case TraitId.Honest: return "честный";
                case TraitId.Shy: return "застенчивый";
                default: return "хаотичный";
            }
        }

        // ---------- Прогноз реакции ----------

        // Насколько цель откликнется, если карту сыграть на неё. Без точных чисел — уровень и 1–2 причины.
        public static Forecast Predict(EventDefinition def, NPCController npc)
        {
            var f = new Forecast { actor = npc, level = Level.None };
            if (def == null || npc == null || CardRuntime.OnlyDeck(def))
                return f;
            if (!Eligible(def, npc))
            {
                f.reasons.Add("не подходит: нужен " + TraitName(def.targetTrait));
                return f;
            }

            var delta = new float[6];
            var why = new List<(float weight, string text)>();
            bool touches = false;

            void Push(ActorStat stat, float amount)
            {
                touches = true;
                float gain = 1f;
                if (amount > 0f && npc.Trait != null)
                {
                    switch (stat)
                    {
                        case ActorStat.Anger: gain = npc.Trait.angerGain; break;
                        case ActorStat.Stress: gain = npc.Trait.stressGain; break;
                        case ActorStat.Sadness: gain = npc.Trait.sadnessGain; break;
                        case ActorStat.Attraction: gain = npc.Trait.attractionGain; break;
                    }

                    if (gain >= 1.15f)
                        why.Add((gain * 10f, TraitName(npc.Trait.traitId)));
                    else if (gain <= 0.8f)
                        why.Add((3f, "черта гасит " + StageFx.StatName(stat)));
                }

                if (amount > 0f && npc.HasState(NPCController.StateAmplified) && (stat == ActorStat.Anger || stat == ActorStat.Stress || stat == ActorStat.Sadness))
                {
                    gain *= 2f;
                    why.Add((14f, "«масло в огонь»: удар вдвое"));
                }

                delta[(int)stat] += amount * gain;
            }

            var dice = def.diceEffects ?? new List<DiceEffect>();
            var used = new HashSet<DiceEffect>();
            if (def.effects != null)
            {
                foreach (var e in def.effects)
                {
                    if (e == null)
                        continue;
                    if (e.type == CardEffectType.ChangeStat)
                    {
                        var die = FindDie(dice, used, DiceSubject.Stat, e.stat, null);
                        if (!CardRuntime.Holds(e.onlyIf, npc, def))
                        {
                            why.Add((2f, "условие не про него"));
                            continue;
                        }

                        float size = e.amount != 0f ? Mathf.Abs(e.amount) : (die != null ? Dice.Average(die) : 2f) * CardStage.DiePoints;
                        bool down = e.amount < 0f || (die != null ? die.lower : Calming(def, e.stat));
                        Push(e.stat, down ? -size : size);
                    }
                    else if (e.type == CardEffectType.ChangeHighestNegative)
                    {
                        var die = FindDie(dice, used, DiceSubject.Check, null, null);
                        var stat = npc.Anger >= npc.Stress && npc.Anger >= npc.Sadness ? ActorStat.Anger : npc.Stress >= npc.Sadness ? ActorStat.Stress : ActorStat.Sadness;
                        float size = (die != null ? Dice.Average(die) : 3f) * CardStage.DiePoints;
                        bool down = die != null ? die.lower : def.category == "Control";
                        Push(stat, down ? -size : size);
                    }
                    else if (e.type == CardEffectType.ChangeRelationship && e.axis == RelationshipAxis.Hostility)
                    {
                        var die = FindDie(dice, used, DiceSubject.Relationship, null, e.axis);
                        float size = (die != null ? Dice.Average(die) : 3f) * CardStage.DiePoints;
                        bool down = e.amount < 0f || (die != null && die.lower);
                        Push(ActorStat.Anger, (down ? -size : size) * 0.5f);
                        if (!down && npc.Hostility + size >= 45)
                            why.Add((12f, "вражда и так высокая"));
                    }
                    else if (e.type == CardEffectType.RevealSecret || e.type == CardEffectType.InviteActor)
                    {
                        touches = true;
                        delta[(int)ActorStat.Stress] += 12f;
                    }
                }
            }

            foreach (var d in dice)
            {
                if (d == null || d.subject != DiceSubject.Stat || used.Contains(d))
                    continue;
                float size = Dice.Average(d) * CardStage.DiePoints;
                Push(d.stat, d.lower ? -size : size);
            }

            // Реакции черты на теги карты (правила ReactionRuleSet) — те же, что сработают в комнате.
            var tags = Tags(def);
            bool snapRule = false;
            if (npc.Rules != null && npc.Rules.rules != null && tags.Count > 0)
            {
                foreach (var r in npc.Rules.rules)
                {
                    if (r == null || r.othersOnly || string.IsNullOrEmpty(r.eventTag) || !tags.Contains(r.eventTag))
                        continue;
                    touches = true;
                    delta[(int)ActorStat.Anger] += r.anger;
                    delta[(int)ActorStat.Stress] += r.stress;
                    delta[(int)ActorStat.Sadness] += r.sadness;
                    delta[(int)ActorStat.Attraction] += r.attraction;
                    if (r.action == NpcActionId.SeekFight || r.action == NpcActionId.Panic)
                        snapRule = true;
                    why.Add((8f + r.priority * 0.3f, "реагирует на «" + TagName(r.eventTag) + "»"));
                }
            }

            if (!touches)
                return f;

            // Что будет после: дойдёт ли до срыва (драка, паника, слёзы, флирт) — как в NPCController.Escalate.
            float anger = npc.Anger + delta[(int)ActorStat.Anger];
            float stress = npc.Stress + delta[(int)ActorStat.Stress];
            float sadness = npc.Sadness + delta[(int)ActorStat.Sadness];
            float attraction = npc.Attraction + delta[(int)ActorStat.Attraction];
            float control = npc.SelfControl + delta[(int)ActorStat.SelfControl];
            if (npc.HasState(NPCController.StateDrunk))
            {
                control -= 15f;
                why.Add((9f, "пьян"));
            }

            bool snap = snapRule || (anger >= 75f && control <= 50f) || stress >= 75f || sadness >= 70f || attraction >= 70f;
            float magnitude = 0f;
            for (int i = 0; i < 6; i++)
                magnitude += Mathf.Abs(delta[i]);

            // Успокоить можно только того, кто на взводе.
            bool calming = delta[(int)ActorStat.Anger] + delta[(int)ActorStat.Stress] + delta[(int)ActorStat.Sadness] < 0f;
            if (calming)
            {
                float heat = Mathf.Max(npc.Anger, Mathf.Max(npc.Stress, npc.Sadness));
                magnitude = Mathf.Min(magnitude, heat);
                why.Add(heat >= 45f ? (12f, "на взводе — есть что гасить") : (6f, "и так спокоен"));
                snap = false;
            }
            else
            {
                if (npc.Anger >= 45 && delta[(int)ActorStat.Anger] > 0f)
                    why.Add((11f, "уже злится"));
                if (npc.Stress >= 45 && delta[(int)ActorStat.Stress] > 0f)
                    why.Add((11f, "уже на нервах"));
                if (npc.Sadness >= 40 && delta[(int)ActorStat.Sadness] > 0f)
                    why.Add((10f, "уже грустит"));
                if (snap)
                    why.Add((20f, anger >= 75f ? "может сорваться в драку" : stress >= 75f ? "может впасть в панику" : sadness >= 70f ? "может расплакаться" : "может начать флирт"));
            }

            f.score = magnitude + (snap ? 40f : 0f);
            f.level = snap || magnitude >= 30f ? Level.High : magnitude >= 12f ? Level.Medium : Level.Low;
            if (f.level == Level.Low && why.Count == 0)
                why.Add((1f, "спокойный"));
            why.Sort((a, b) => b.weight.CompareTo(a.weight));
            foreach (var (_, text) in why)
            {
                if (!f.reasons.Contains(text))
                    f.reasons.Add(text);
                if (f.reasons.Count >= 2)
                    break;
            }

            return f;
        }

        // Карту можно сыграть на этого участника (черта-ограничение карты).
        public static bool Eligible(EventDefinition def, NPCController npc)
        {
            return def == null || npc == null || !def.limitTrait || (npc.Trait != null && npc.Trait.traitId == def.targetTrait);
        }

        public static string LevelName(Level level)
        {
            switch (level)
            {
                case Level.High: return "ВЫСОКИЙ";
                case Level.Medium: return "СРЕДНИЙ";
                case Level.Low: return "НИЗКИЙ";
                default: return "—";
            }
        }

        public static string LevelColor(Level level)
        {
            switch (level)
            {
                case Level.High: return Red;
                case Level.Medium: return Gold;
                default: return Muted;
            }
        }

        // ---------- Подсказка целиком ----------

        // Текст подсказки карты в руке (rich text). cast — участники на площадке (для прогноза), hell — остаток HellToken.
        public static string Tooltip(EventDefinition def, IList<NPCController> cast, float hell, float cost)
        {
            var sb = new StringBuilder();
            sb.Append("<color=").Append(Muted).Append(">").Append(CategoryName(def.category)).Append(" · ").Append(TargetShort(def)).Append("</color>");

            var what = What(def);
            if (what.Count > 0)
            {
                sb.Append("\n<color=").Append(Gold).Append(">Что делает</color>");
                foreach (var line in what)
                    sb.Append("\n• ").Append(line);
            }
            else if (!string.IsNullOrEmpty(def.description))
            {
                sb.Append("\n").Append(def.description);
            }

            sb.Append("\n<color=").Append(Gold).Append(">Как играть:</color> ").Append(How(def));

            var conditions = Conditions(def);
            if (conditions.Count > 0)
                sb.Append("\n<color=").Append(Gold).Append(">Условия:</color> ").Append(string.Join("; ", conditions));

            if (cast != null && def.targetType != TargetType.Zone && !CardRuntime.OnlyDeck(def))
            {
                var list = new List<Forecast>();
                int eligible = 0;
                foreach (var npc in cast)
                {
                    if (npc == null)
                        continue;
                    if (Eligible(def, npc))
                        eligible++;
                    var f = Predict(def, npc);
                    if (f.level != Level.None)
                        list.Add(f);
                }

                if (eligible == 0 && def.limitTrait)
                    sb.Append("\n<color=").Append(Red).Append(">Сейчас не на кого: в касте нет — ").Append(TraitName(def.targetTrait)).Append("</color>");
                if (list.Count > 0)
                {
                    list.Sort((a, b) => b.score.CompareTo(a.score));
                    sb.Append("\n<color=").Append(Gold).Append(">Прогноз реакции</color>");
                    for (int i = 0; i < list.Count && i < 3; i++)
                    {
                        var f = list[i];
                        if (f.level == Level.High)
                        {
                            sb.Append("\n<color=").Append(LevelColor(f.level)).Append(">").Append(f.actor.DisplayName).Append(" сорвётся.</color>");
                            continue;
                        }

                        sb.Append("\n").Append(f.actor.DisplayName).Append(" — <color=").Append(LevelColor(f.level)).Append(">").Append(LevelName(f.level)).Append("</color>");
                        if (f.reasons.Count > 0)
                            sb.Append(": ").Append(string.Join(", ", f.reasons));
                    }
                }
            }

            float left = Mathf.Max(0f, hell);
            sb.Append("\n<color=").Append(Gold).Append(">Цена:</color> ");
            if (cost <= 0f)
                sb.Append("бесплатно");
            else if (cost <= left + 0.001f)
                sb.Append(HellToken.Format(cost)).Append(" HellToken · останется ").Append(HellToken.Format(left - cost));
            else
                sb.Append("<color=").Append(Red).Append(">").Append(HellToken.Format(cost)).Append(" — не хватает ").Append(HellToken.Format(cost - left)).Append("</color>");
            return sb.ToString();
        }

        // ---------- Слова ----------

        static List<string> Tags(EventDefinition def)
        {
            var tags = new List<string>();
            if (def.tags != null)
                tags.AddRange(def.tags);
            if (def.effects != null)
            {
                foreach (var e in def.effects)
                {
                    if (e != null && e.type == CardEffectType.WorldEvent)
                        tags.AddRange(Split(e.key));
                }
            }

            return tags;
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

        static List<string> Split(string key)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(key))
                return list;
            foreach (var raw in key.Split(','))
            {
                string t = raw.Trim();
                if (t.Length > 0)
                    list.Add(t);
            }

            return list;
        }

        public static string TagName(string tag)
        {
            switch (tag)
            {
                case "Public": return "при всех";
                case "Provocation": return "провокация";
                case "Humiliation": return "унижение";
                case "PettyHumiliation": return "мелкая подстава";
                case "Petty": return "мелкая подстава";
                case "Conflict": return "конфликт";
                case "Popularity": return "рейтинг";
                case "Reveal": return "разоблачение";
                case "Betrayal": return "предательство";
                case "Rumour": return "слух";
                case "Confession": return "признание";
                case "AudienceQuestion": return "вопрос зрителя";
                case "Flirt": return "флирт";
                case "Romance": return "романтика";
                case "Surprise": return "сюрприз";
                case "Reunion": return "встреча с бывшим";
                case "SponsorMention": return "упоминание бренда";
                case "Sponsor": return "спонсор";
                case "Alarm": return "тревога";
                case "Chaos": return "хаос";
                case "Fire": return "огонь";
                case "Fight": return "драка";
                case "Slap": return "пощёчина";
                case "Crying": return "слёзы";
                case "Hug": return "объятия";
                case "Warmth": return "тепло";
                case "Misery": return "бытовая беда";
                case "Theft": return "кража";
                case "Argument": return "спор";
                case "Reconcile": return "примирение";
                case "Singing": return "песня";
                case "Calm": return "тишина";
                default: return tag;
            }
        }

        static string ContextName(string key)
        {
            switch (key)
            {
                case "Private": return "без камер — легче признаться";
                case "Pressure": return "давление — стресс растёт";
                case "Witness": return "есть свидетель";
                case "Gift": return null;
                default: return key;
            }
        }

        static string AxisName(RelationshipAxis axis)
        {
            switch (axis)
            {
                case RelationshipAxis.Trust: return "доверие";
                case RelationshipAxis.Attraction: return "влечение";
                default: return "вражда";
            }
        }

        static string CheckName(string check)
        {
            if (string.IsNullOrEmpty(check))
                return "сила события";
            string s = check.ToLowerInvariant();
            if (s.Contains("outcome"))
                return "сила исхода";
            if (s.Contains("interaction"))
                return "что сделают с объектом";
            if (s.Contains("performance"))
                return "как выступят";
            if (s.Contains("reveal"))
                return "раскроется ли секрет";
            if (s.Contains("selfcontrol"))
                return "выдержит ли";
            return check;
        }

        static string Cards(int n)
        {
            if (n == 1)
                return "1 карта";
            if (n >= 2 && n <= 4)
                return n + " карты";
            return n + " карт";
        }
    }
}
