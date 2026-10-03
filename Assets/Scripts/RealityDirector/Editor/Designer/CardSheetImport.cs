using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using RealityDirector.Core;
using RealityDirector.Events;
using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Импорт карт из таблицы дизайна (.xlsx, лист «Карты»): строка = карта, id = имя ассета.
    // Колонки таблицы перезаписывают свои поля при каждом импорте; цены, тон, цвет, арт и злость задаются
    // только новой карте — дальше их правит дизайнер в Unity, и повторный импорт их не трогает.
    public static class CardSheetImport
    {
        const string PathPref = "RealityDirector.CardSheetPath";
        const string SheetName = "Карты";

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string LastPath => EditorPrefs.GetString(PathPref, "");

        [MenuItem("RealityDirector/Карты: импорт из таблицы (.xlsx)…", priority = 2)]
        public static void ImportWithDialog()
        {
            string last = LastPath;
            string path = EditorUtility.OpenFilePanel("Таблица карт", string.IsNullOrEmpty(last) ? "" : Path.GetDirectoryName(last), "xlsx");
            if (!string.IsNullOrEmpty(path))
                ImportAndReport(path);
        }

        public static void ImportAndReport(string path)
        {
            string report;
            try
            {
                report = Import(path);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Импорт карт", "Не получилось прочитать таблицу:\n" + e.Message, "ОК");
                return;
            }

            Debug.Log("Импорт карт из " + path + "\n" + report);
            EditorUtility.DisplayDialog("Импорт карт", report.Length > 1500 ? report.Substring(0, 1500) + "\n…полный отчёт в консоли." : report, "ОК");
        }

        public static string Import(string path)
        {
            var rows = Xlsx.ReadSheet(path, SheetName);
            int header = rows.FindIndex(r => r.Contains("ID") && r.Contains("Название"));
            if (header < 0)
                throw new System.Exception("На листе «" + SheetName + "» нет строки заголовков с колонками «ID» и «Название».");
            var columns = new Dictionary<string, int>();
            for (int c = 0; c < rows[header].Count; c++)
            {
                string name = rows[header][c].Trim();
                if (name.Length > 0 && !columns.ContainsKey(name))
                    columns[name] = c;
            }

            EditorPrefs.SetString(PathPref, path);
            Directory.CreateDirectory(DesignerData.CardsRoot);
            var existing = new Dictionary<string, EventDefinition>();
            foreach (var card in DesignerData.LoadAll<EventDefinition>())
            {
                if (!string.IsNullOrEmpty(card.id) && !existing.ContainsKey(card.id))
                    existing[card.id] = card;
            }

            int created = 0, updated = 0;
            var notes = new List<string>();
            var sponsors = new List<EventDefinition>();
            for (int i = header + 1; i < rows.Count; i++)
            {
                var row = new Row(rows[i], columns);
                string id = row["ID"];
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(row["Название"]))
                    continue;

                bool isNew = !existing.TryGetValue(id, out var def);
                if (isNew)
                {
                    def = ScriptableObject.CreateInstance<EventDefinition>();
                    def.name = id;
                    def.id = id;
                }

                var cardNotes = new List<string>();
                Apply(def, row, isNew, cardNotes);
                if (isNew)
                {
                    AssetDatabase.CreateAsset(def, DesignerData.CardsRoot + "/" + id + ".asset");
                    existing[id] = def;
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(def);
                    updated++;
                }

                if (def.sponsor)
                    sponsors.Add(def);
                foreach (var n in cardNotes)
                    notes.Add(id + ": " + n);
            }

            int offers = AddSponsorOffers(sponsors);
            AssetDatabase.SaveAssets();
            DesignerData.Invalidate();
            ContentLibrary.Clear();

            var sb = new StringBuilder();
            sb.AppendLine("Новых карт: " + created + ", обновлено: " + updated + ".");
            if (offers > 0)
                sb.AppendLine("Спонсорских контрактов добавлено в комнаты маркетинга: " + offers + ".");
            if (notes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Не разобрано в структуру (текст сохранён в карте, блок «Из таблицы дизайна»):");
                foreach (var n in notes)
                    sb.AppendLine("• " + n);
            }

            return sb.ToString();
        }

        // ---------- Строка таблицы → карта ----------

        static void Apply(EventDefinition def, Row row, bool isNew, List<string> notes)
        {
            def.displayName = row["Название"];
            def.status = Status(row["Статус"]);
            def.category = Category(row["Категория"], notes);
            def.tier = Parse(row["Tier"], CardTier.I);
            def.rarity = Parse(row["Редкость"], CardRarity.Common);
            def.cost = Money(row["HellToken $"], notes);
            def.targetType = Target(row["Цель"], notes);
            def.description = row["Описание для игрока"];
            def.environmentId = row["Environment / Prefab"];
            def.sponsor = def.category == "Sponsor";
            string sponsorId = row["Sponsor"];
            def.sponsorId = sponsorId.StartsWith("По ") ? "" : sponsorId;
            def.designIntent = row["Design intent"];

            def.sheet = new CardSheetSpec
            {
                targetFilters = row["Фильтры цели"],
                effects = row["Эффекты / правила"],
                dice = row["Dice effects"],
                aura = row["Aura / влияние локации"],
                footage = row["Влияние на Footage"],
                lifecycle = row["Special rules / Lifecycle"],
                upgradeTier2 = row["Upgrade Tier II"],
                upgradeTier3 = row["Upgrade Tier III"]
            };

            var tags = Split(row["Теги"], ',');
            def.diceEffects = DiceList(row["Dice effects"], out var stepUps, notes);
            def.effects = Effects(row["Эффекты / правила"], def.diceEffects, notes);
            def.aura = Aura(row["Aura / влияние локации"], tags, stepUps);
            def.footage = Footage(row["Влияние на Footage"]);
            Lifecycle(def, row["Special rules / Lifecycle"]);
            def.tags = WithMomentTags(tags, def);

            if (isNew)
                Defaults(def);
        }

        // Значения для квартиры-прототипа и магазинов — только новой карте. Дальше их ведёт дизайнер.
        static void Defaults(EventDefinition def)
        {
            def.cardColor = CategoryColor(def.category);
            def.moods = Moods(def);
            def.hint = Hint(def.targetType);
            var anger = def.diceEffects.FirstOrDefault(d => d.subject == DiceSubject.Stat && d.stat == ActorStat.Anger && !d.lower);
            if (def.PlayTarget == TargetType.Actor && anger != null)
                def.rageSeconds = Dice.Faces(anger.die) * 10f;
            if (def.sponsor)
            {
                def.sponsorPay = 100;
                def.sponsorScoreHit = 2;
                return;
            }

            switch (def.rarity)
            {
                case CardRarity.Common: def.price = 100; def.runPrice = 30; break;
                case CardRarity.Uncommon: def.price = 140; def.runPrice = 40; break;
                case CardRarity.Rare: def.price = 200; def.runPrice = 60; break;
                default: def.price = 260; def.runPrice = 80; break;
            }
        }

        static CardStatus Status(string s)
        {
            switch (s.ToLowerInvariant())
            {
                case "новая":
                case "тест":
                case "на тесте":
                case "testing": return CardStatus.Testing;
                case "черновик":
                case "draft": return CardStatus.Draft;
                case "выключена":
                case "disabled": return CardStatus.Disabled;
                default: return CardStatus.Ready;
            }
        }

        static string Category(string s, List<string> notes)
        {
            switch (s.ToLowerInvariant().Replace(" ", ""))
            {
                case "deckmanagement": return "DeckManagement";
                case "provocation": return "Provocation";
                case "social": return "Social";
                case "reveal/secret":
                case "reveal": return "Reveal";
                case "control": return "Control";
                case "chaos/comedy":
                case "chaos":
                case "comedy": return "Comedy";
                case "environment": return "Environment";
                case "sponsor": return "Sponsor";
                case "confession": return "Confession";
                case "": return "";
                default:
                    notes.Add("категория «" + s + "» игре неизвестна — откроется только на Сценаристах ур. 4");
                    return s.Replace(" ", "");
            }
        }

        static float Money(string s, List<string> notes)
        {
            string clean = s.Replace("$", "").Replace(",", ".").Trim();
            if (float.TryParse(clean, NumberStyles.Float, Inv, out float v))
                return Mathf.Max(0f, v);
            if (clean.Length > 0)
                notes.Add("цена «" + s + "» — не число");
            return 0f;
        }

        static TargetType Target(string s, List<string> notes)
        {
            switch (s.ToLowerInvariant())
            {
                case "actor": return TargetType.Actor;
                case "actorpair": return TargetType.ActorPair;
                case "zone": return TargetType.Zone;
                case "prop":
                case "object": return TargetType.Object;
                case "cardinhand": return TargetType.CardInHand;
                case "usedcard": return TargetType.UsedCard;
                case "none":
                case "global":
                case "": return TargetType.Global;
                default:
                    notes.Add("цель «" + s + "» неизвестна — поставлено «сразу»");
                    return TargetType.Global;
            }
        }

        // ---------- Эффекты ----------

        static readonly Regex Token = new Regex(@"^(\w+)\s*(?:\((.*)\))?$");

        static List<CardEffect> Effects(string text, List<DiceEffect> dice, List<string> notes)
        {
            var list = new List<CardEffect>();
            bool highest = false;
            foreach (var part in Split(text, ';'))
            {
                var m = Token.Match(part);
                if (!m.Success)
                {
                    notes.Add("эффект «" + part + "»");
                    continue;
                }

                string name = m.Groups[1].Value;
                string args = m.Groups[2].Value.Trim();
                var argList = Split(args, ',');
                switch (name)
                {
                    case "ReturnHandCardToLibrary": list.Add(Fx(CardEffectType.ReturnHandCardToLibrary, 1)); break;
                    case "SearchLibrary": list.Add(Fx(CardEffectType.SearchLibrary, 1)); break;
                    case "TakeSelectedCardIntoHand": list.Add(Fx(CardEffectType.TakeSelectedIntoHand)); break;
                    case "RecoverUsedCardToLibrary": list.Add(Fx(CardEffectType.RecoverUsedCard, 1)); break;
                    case "PeekRandomLibrary": list.Add(Fx(CardEffectType.PeekLibrary, Number(args, 1))); break;
                    case "ChooseOneToHand": list.Add(Fx(CardEffectType.ChooseOneToHand)); break;
                    case "MoveHandCardToUsed": list.Add(Fx(CardEffectType.MoveHandCardToUsed, 1)); break;
                    case "DrawRandom": list.Add(Fx(CardEffectType.DrawRandom, Number(args, 1))); break;
                    case "ProtectFromNextHandLoss": list.Add(Fx(CardEffectType.ProtectCard, 1)); break;
                    case "MarkCard": list.Add(Fx(CardEffectType.DuplicateEffect, 1)); break;
                    case "AddSpecialRule":
                        if (args == "Retain")
                            list.Add(Fx(CardEffectType.RetainCard, 1));
                        else
                            list.Add(Key(CardEffectType.AddState, args));
                        break;
                    case "AddNextCardCostModifier":
                    {
                        float v = Number(args, -1f);
                        list.Add(Fx(v <= 0f ? CardEffectType.ReduceCost : CardEffectType.IncreaseCost, Mathf.Abs(v)));
                        break;
                    }
                    case "AddNextCaptureModifier":
                    {
                        var fx = Key(CardEffectType.NextCaptureBonus, Regex.Match(args, @"[A-Za-z]+").Value);
                        var bonus = Regex.Match(args, @"[+-]\s*(\d+)");
                        fx.amount = bonus.Success ? float.Parse(bonus.Groups[1].Value, Inv) : 1f;
                        var secs = Regex.Match(args, @"(\d+)\s*s\b");
                        if (secs.Success)
                        {
                            fx.duration = StateDuration.Timed;
                            fx.seconds = float.Parse(secs.Groups[1].Value, Inv);
                        }
                        else
                        {
                            fx.duration = StateDuration.UntilConsumed;
                        }

                        list.Add(fx);
                        break;
                    }
                    case "FindHighestNegativeEmotion":
                    case "ModifyHighestNegativeEmotion":
                        list.Add(Fx(CardEffectType.ChangeHighestNegative));
                        highest = name == "FindHighestNegativeEmotion";
                        break;
                    case "ModifyEmotion":
                    case "ConditionalModifyEmotion":
                    case "ModifyEmotionAll":
                        if (argList.Count == 0)
                        {
                            if (!highest)
                                notes.Add("эффект «" + part + "» без эмоции");
                            break;
                        }

                        foreach (var a in argList.SelectMany(x => x.Split('/')))
                        {
                            if (!TryStat(a, out var stat))
                            {
                                notes.Add("эмоция «" + a + "» в «" + part + "»");
                                continue;
                            }

                            var fx = Fx(CardEffectType.ChangeStat);
                            fx.stat = stat;
                            if (name == "ModifyEmotionAll")
                                fx.receiver = EffectReceiver.AllInRoom;
                            if (name == "ConditionalModifyEmotion")
                                fx.onlyIf = dice.FirstOrDefault(d => d.subject == DiceSubject.Stat && d.stat == stat && !string.IsNullOrEmpty(d.onlyIf))?.onlyIf;
                            list.Add(fx);
                        }

                        break;
                    case "ModifyEmotionByContext":
                    case "ConditionalEmotionByPopularityAndTrait":
                        // Эмоции и условия — из колонки кубиков.
                        foreach (var d in dice.Where(d => d.subject == DiceSubject.Stat))
                        {
                            var fx = Fx(CardEffectType.ChangeStat);
                            fx.stat = d.stat;
                            fx.onlyIf = string.IsNullOrEmpty(d.onlyIf) ? "по контексту" : d.onlyIf;
                            list.Add(fx);
                        }

                        break;
                    case "ModifyRelationship":
                        foreach (var a in argList)
                        {
                            if (!TryAxis(a, out var axis))
                            {
                                notes.Add("ось отношений «" + a + "»");
                                continue;
                            }

                            var fx = Fx(CardEffectType.ChangeRelationship);
                            fx.axis = axis;
                            list.Add(fx);
                        }

                        break;
                    case "AddTemporaryState":
                    {
                        var fx = Key(CardEffectType.AddState, args);
                        fx.duration = args.StartsWith("Next") ? StateDuration.UntilConsumed : StateDuration.UntilEndOfSituation;
                        list.Add(fx);
                        break;
                    }
                    case "CreateWorldEvent": list.Add(Key(CardEffectType.WorldEvent, string.Join(",", argList))); break;
                    case "CreateGlobalWorldEvent":
                    {
                        var fx = Key(CardEffectType.WorldEvent, string.Join(",", argList));
                        fx.receiver = EffectReceiver.AllInRoom;
                        list.Add(fx);
                        break;
                    }
                    case "CreateWorldEventCandidate": list.Add(Key(CardEffectType.EventCandidate, args)); break;
                    case "MoveActorsToPrivateInteraction": list.Add(Key(CardEffectType.MoveActor, "Private")); break;
                    case "InviteActorByRelationship": list.Add(Key(CardEffectType.InviteActor, "Relationship")); break;
                    case "InviteThirdActorToInteraction": list.Add(Key(CardEffectType.InviteActor, "Third")); break;
                    case "AddContext":
                    case "CreateContext": list.Add(Key(CardEffectType.AddContext, string.Join(",", argList))); break;
                    case "ApplyZoneContext":
                    {
                        var fx = Key(CardEffectType.AddContext, string.Join(",", argList));
                        fx.receiver = EffectReceiver.AllInRoom;
                        list.Add(fx);
                        break;
                    }
                    case "ModifyPublicContext": list.Add(Fx(CardEffectType.LowerPublicContext)); break;
                    case "LockZoneExit": list.Add(Fx(CardEffectType.LockExit)); break;
                    case "ReduceAmbientInterruption": list.Add(Fx(CardEffectType.QuietRoom)); break;
                    case "SpawnEnvironmentObject": list.Add(Fx(CardEffectType.SpawnObject)); break;
                    case "CreateAura": list.Add(Fx(CardEffectType.CreateAura)); break;
                    case "CreateTriggerZone": list.Add(Fx(CardEffectType.TriggerZone)); break;
                    case "EnableNPCInteraction": list.Add(Fx(CardEffectType.NpcInteraction)); break;
                    case "BehaviourWeightModifier": list.Add(Fx(CardEffectType.BehaviourWeights)); break;
                    case "AddSponsorVisibilityRule": list.Add(Fx(CardEffectType.SponsorVisibility)); break;
                    case "TestOnEnter": list.Add(Key(CardEffectType.Check, "OnEnter")); break;
                    case "TestAgainstSelfControl": list.Add(Key(CardEffectType.Check, "SelfControl")); break;
                    case "TestPerformance": list.Add(Key(CardEffectType.Check, "Performance")); break;
                    case "RevealSecret": list.Add(Fx(CardEffectType.RevealSecret)); break;
                    case "ConditionalReveal": list.Add(Key(CardEffectType.RevealSecret, "Conditional")); break;
                    case "RevealSecretCandidate": list.Add(Key(CardEffectType.RevealSecret, "Candidate")); break;
                    case "InterruptLowIntensityChain": list.Add(Fx(CardEffectType.InterruptChain)); break;
                    case "RandomOutcomeTable": list.Add(Fx(CardEffectType.RandomOutcome)); break;
                    case "ForceMovementCandidate": list.Add(Fx(CardEffectType.ForceMovement)); break;
                    default:
                        notes.Add("эффект «" + part + "» — такого типа в картах пока нет");
                        break;
                }

                if (name != "FindHighestNegativeEmotion")
                    highest = false;
            }

            return list;
        }

        static CardEffect Fx(CardEffectType type, float amount = 0f)
        {
            return new CardEffect { type = type, amount = amount };
        }

        static CardEffect Key(CardEffectType type, string key)
        {
            return new CardEffect { type = type, key = key };
        }

        // ---------- Кубики ----------

        static readonly Regex Signed = new Regex(@"^(?:(?<cond>[^:+−-]+?):\s*)?(?<subj>[A-Za-z]+(?:\s*(?:/|или)\s*[A-Za-z]+)*)\s*(?<sign>[+-])\s*(?<n>\d*)d(?<f>\d+)(?<rest>.*)$");
        static readonly Regex Plain = new Regex(@"^(?<what>.*?)\s*:?\s*(?<sign>[+-])?\s*(?<n>\d*)d(?<f>\d+)(?<rest>.*)$");
        static readonly Regex StepUp = new Regex(@"^(?<what>[A-Za-z/]+)\s+(?:rolls|dice)\s+Step\s*Up", RegexOptions.IgnoreCase);

        static List<DiceEffect> DiceList(string text, out List<ActorStat> stepUps, List<string> notes)
        {
            var list = new List<DiceEffect>();
            stepUps = new List<ActorStat>();
            foreach (var part in Split(text.Replace('−', '-'), ';'))
            {
                string raw = Regex.Replace(part, @"^Target\s+", "");
                var step = StepUp.Match(raw);
                if (step.Success && !Regex.IsMatch(raw, @"\d*d\d+"))
                {
                    bool any = false;
                    foreach (var s in step.Groups["what"].Value.Split('/'))
                    {
                        if (TryStat(s, out var stat))
                        {
                            stepUps.Add(stat);
                            any = true;
                        }
                    }

                    if (!any)
                        notes.Add("кубики «" + raw + "»");
                    continue;
                }

                var m = Signed.Match(raw);
                if (m.Success && Subjects(m.Groups["subj"].Value, out var subjects))
                {
                    string onlyIf = m.Groups["cond"].Value.Trim();
                    string rest = m.Groups["rest"].Value.Trim();
                    if (onlyIf.Length == 0 && rest.StartsWith("при "))
                        onlyIf = rest;
                    foreach (var s in subjects)
                        list.Add(Die(s, m.Groups["sign"].Value == "-", m.Groups["n"].Value, m.Groups["f"].Value, onlyIf, notes, raw));
                    continue;
                }

                var p = Plain.Match(raw);
                if (p.Success)
                {
                    string what = p.Groups["what"].Value.Trim().TrimEnd(':').Trim();
                    string rest = p.Groups["rest"].Value.Trim();
                    // «Attraction/Anger по relationship context 1d6» — эмоции с условием.
                    var head = Regex.Match(what, @"^([A-Za-z]+(?:/[A-Za-z]+)*)\s+(.+)$");
                    if (head.Success && Subjects(head.Groups[1].Value, out var stats) && stats.All(x => x.subject == DiceSubject.Stat))
                    {
                        foreach (var s in stats)
                            list.Add(Die(s, p.Groups["sign"].Value == "-", p.Groups["n"].Value, p.Groups["f"].Value, head.Groups[2].Value.Trim(), notes, raw));
                        continue;
                    }

                    bool when = rest.StartsWith("при ");
                    string label = what.Length > 0 ? (rest.Length > 0 && !when ? what + " " + rest : what) : rest.Replace("против", "").Trim();
                    var d = Die(new DiceEffect { subject = DiceSubject.Check }, p.Groups["sign"].Value == "-", p.Groups["n"].Value, p.Groups["f"].Value,
                        when ? rest : "", notes, raw);
                    d.check = label;
                    list.Add(d);
                    continue;
                }

                notes.Add("кубики «" + raw + "»");
            }

            return list;
        }

        // «Anger/Stress», «Anger или Stress», «Hostility» → что бросаем.
        static bool Subjects(string text, out List<DiceEffect> result)
        {
            result = new List<DiceEffect>();
            foreach (var raw in Regex.Split(text, @"\s*(?:/|или)\s*"))
            {
                string s = raw.Trim();
                if (TryStat(s, out var stat))
                    result.Add(new DiceEffect { subject = DiceSubject.Stat, stat = stat });
                else if (TryAxis(s, out var axis))
                    result.Add(new DiceEffect { subject = DiceSubject.Relationship, axis = axis });
                else
                    return false;
            }

            return result.Count > 0;
        }

        static DiceEffect Die(DiceEffect d, bool lower, string count, string faces, string onlyIf, List<string> notes, string raw)
        {
            d.lower = lower;
            d.count = string.IsNullOrEmpty(count) ? 1 : int.Parse(count, Inv);
            d.onlyIf = onlyIf;
            switch (faces)
            {
                case "4": d.die = DieSize.D4; break;
                case "6": d.die = DieSize.D6; break;
                case "8": d.die = DieSize.D8; break;
                case "10": d.die = DieSize.D10; break;
                case "12": d.die = DieSize.D12; break;
                default:
                    notes.Add("кубик d" + faces + " в «" + raw + "» — бывают d4–d12");
                    break;
            }

            return d;
        }

        static bool TryStat(string s, out ActorStat stat)
        {
            return System.Enum.TryParse(s.Trim(), true, out stat) && System.Enum.IsDefined(typeof(ActorStat), stat);
        }

        static bool TryAxis(string s, out RelationshipAxis axis)
        {
            return System.Enum.TryParse(s.Trim(), true, out axis) && System.Enum.IsDefined(typeof(RelationshipAxis), axis);
        }

        // ---------- Аура, footage, жизненный цикл ----------

        static readonly string[] AuraContexts = { "Public", "Private", "Romance", "Party", "Alcohol", "Pressure", "Attention", "Secret", "Confession", "Curiosity", "Comedy", "Chaos", "Music", "Visual" };

        static AuraDefinition Aura(string text, List<string> cardTags, List<ActorStat> stepUps)
        {
            var aura = new AuraDefinition();
            foreach (var stat in stepUps)
                aura.diceModifiers.Add(new AuraDiceModifier { stat = stat, steps = 1 });
            if (text.Length == 0 || text.StartsWith("Без "))
            {
                aura.enabled = aura.diceModifiers.Count > 0;
                return aura;
            }

            aura.enabled = true;
            string t = text.Replace(" и ", "/");
            var radius = Regex.Match(t, @"[Rr]adius\s*(\d+(?:[.,]\d+)?)\s*m");
            if (radius.Success)
                aura.radius = float.Parse(radius.Groups[1].Value.Replace(',', '.'), Inv);
            else if (t.IndexOf("room", System.StringComparison.OrdinalIgnoreCase) >= 0)
                aura.radius = 0f;

            foreach (Match m in Regex.Matches(t, @"([A-Za-z/]+)\s+(?:behaviour\s+)?weights?\s*([↑↓])"))
            {
                foreach (var b in m.Groups[1].Value.Split('/'))
                {
                    if (b.Length > 0 && aura.behaviourWeights.All(w => w.behaviour != b))
                        aura.behaviourWeights.Add(new BehaviourWeight { behaviour = b, multiplier = m.Groups[2].Value == "↑" ? 1.5f : 0.5f });
                }
            }

            foreach (Match m in Regex.Matches(t, @"([A-Za-z/]+)\s+dice\s+Step\s*Up", RegexOptions.IgnoreCase))
            {
                foreach (var s in m.Groups[1].Value.Split('/'))
                {
                    if (TryStat(s, out var stat) && aura.diceModifiers.All(d => d.stat != stat))
                        aura.diceModifiers.Add(new AuraDiceModifier { stat = stat, steps = 1 });
                }
            }

            foreach (Match m in Regex.Matches(t, @"\b([A-Za-z]+)(?:\s+sensitivity)?\s*([↑↓])"))
            {
                if (TryStat(m.Groups[1].Value, out var stat) && aura.actorModifiers.All(a => a.stat != stat))
                    aura.actorModifiers.Add(new AuraStatModifier { stat = stat, perSecond = m.Groups[2].Value == "↑" ? 0.5f : -0.5f });
            }

            foreach (var ctx in AuraContexts)
            {
                bool mentioned = Regex.IsMatch(t, @"\b" + ctx + @"\b") && !Regex.IsMatch(t, @"\b" + ctx + @"\s+off\b");
                if ((mentioned || cardTags.Contains(ctx)) && !aura.tags.Contains(ctx))
                    aura.tags.Add(ctx);
            }

            return aura;
        }

        static readonly HashSet<string> NotFootageTags = new HashSet<string> { "Value", "CommercialValue", "TechnicalQuality", "Quality", "Next", "CapturedMoment", "VisibleInFrame", "SponsorTag", "Public" };

        static FootageInfluence Footage(string text)
        {
            var f = new FootageInfluence();
            if (text.Length == 0)
                return f;
            var value = Regex.Match(text, @"(?<![A-Za-z])Value\s*\+\s*(\d+)");
            if (value.Success)
                f.valueBonus = int.Parse(value.Groups[1].Value, Inv);
            var commercial = Regex.Match(text, @"CommercialValue\s*\+\s*(\d+)");
            if (commercial.Success)
                f.commercialValue = int.Parse(commercial.Groups[1].Value, Inv);
            var quality = Regex.Match(text, @"(?:TechnicalQuality|Quality)\s*\+\s*(\d+)");
            if (quality.Success)
                f.technicalQuality = int.Parse(quality.Groups[1].Value, Inv);

            var found = new List<string>();
            foreach (Match m in Regex.Matches(text, @"([A-Z][A-Za-z]+(?:/[A-Z][A-Za-z]+)*)\s+(?:footage|tags?|moments|контекстные)"))
                found.AddRange(m.Groups[1].Value.Split('/'));
            foreach (Match m in Regex.Matches(text, @"(?:→\s*|\b(?:получают|получает|дать|с)\s+)([A-Z][A-Za-z]+(?:\s*(?:\+|/)\s*[A-Z][A-Za-z]+)*)"))
                found.AddRange(Regex.Split(m.Groups[1].Value, @"\s*[+/]\s*"));
            if (Regex.IsMatch(text, @"SponsorTag"))
                found.Add("Sponsor");
            foreach (var tag in found)
            {
                string t = tag.Trim();
                if (t.Length > 0 && !NotFootageTags.Contains(t) && !f.tags.Contains(t))
                    f.tags.Add(t);
            }

            return f;
        }

        static void Lifecycle(EventDefinition def, string text)
        {
            def.lifetime = text.IndexOf("Temporary Episode", System.StringComparison.OrdinalIgnoreCase) >= 0 ? CardLifetime.UntilBroadcast : CardLifetime.Permanent;
            if (def.specialRules == null)
                def.specialRules = new List<SpecialRule>();
            def.specialRules.Remove(SpecialRule.UsesProductionSlot);
            if (text.Contains("Production Slot") && text.IndexOf("не занимает", System.StringComparison.OrdinalIgnoreCase) < 0)
                def.specialRules.Add(SpecialRule.UsesProductionSlot);
        }

        // Теги таблицы + теги, на которые сейчас реагируют участники квартиры (Conflict, Warmth, Crying, Misery, Chaos, Sponsor).
        static List<string> WithMomentTags(List<string> tags, EventDefinition def)
        {
            var result = new List<string>(tags);
            void Add(string tag)
            {
                if (!result.Contains(tag))
                    result.Add(tag);
            }

            bool Has(params string[] any) => any.Any(tags.Contains);
            bool Raises(ActorStat stat) => def.diceEffects.Any(d => d.subject == DiceSubject.Stat && d.stat == stat && !d.lower);
            if (Has("Conflict", "Provocation", "Humiliation", "Rivalry", "Argument", "Pressure") || Raises(ActorStat.Anger)
                || def.diceEffects.Any(d => d.subject == DiceSubject.Relationship && d.axis == RelationshipAxis.Hostility && !d.lower))
                Add(MomentTags.Conflict);
            if (Has("Romance", "Reconcile", "Calm", "Comfort", "Safe", "Gift", "Music", "Party"))
                Add(MomentTags.Warmth);
            if (Has("Confession", "Emotional", "Betrayal", "Reunion"))
                Add(MomentTags.Crying);
            if (Has("Stress", "Panic") || Raises(ActorStat.Stress))
                Add(MomentTags.Misery);
            if (Has("Chaos", "Risk", "Slippery"))
                Add(MomentTags.Chaos);
            if (def.sponsor)
                Add(MomentTags.Sponsor);
            return result;
        }

        static List<ShowMood> Moods(EventDefinition def)
        {
            var moods = new List<ShowMood>();
            switch (def.category)
            {
                case "Provocation": moods.Add(ShowMood.Trash); moods.Add(ShowMood.Drama); break;
                case "Comedy": moods.Add(ShowMood.Trash); break;
                case "Reveal": moods.Add(ShowMood.Drama); break;
                case "Social":
                case "Control": moods.Add(ShowMood.Family); break;
                case "Environment":
                    if (def.tags.Contains("Romance") || def.tags.Contains("Comfort"))
                        moods.Add(ShowMood.Family);
                    else if (def.tags.Contains("Stress") || def.tags.Contains("Pressure") || def.tags.Contains("Secret"))
                        moods.Add(ShowMood.Drama);
                    else
                        moods.Add(ShowMood.Trash);
                    break;
            }

            return moods;
        }

        static string Hint(TargetType target)
        {
            switch (target)
            {
                case TargetType.Actor: return "клик по участнику";
                case TargetType.ActorPair: return "клик по участнику пары";
                case TargetType.Object: return "клик по предмету";
                case TargetType.Zone: return "сразу на комнату";
                case TargetType.CardInHand: return "сразу · карта из руки";
                case TargetType.UsedCard: return "сразу · из использованных";
                default: return "сразу на весь дом";
            }
        }

        static Color CategoryColor(string category)
        {
            switch (category)
            {
                case "Provocation": return new Color(0.62f, 0.16f, 0.16f, 1f);
                case "Social": return new Color(0.55f, 0.25f, 0.42f, 1f);
                case "Reveal": return new Color(0.3f, 0.2f, 0.45f, 1f);
                case "Control": return new Color(0.2f, 0.42f, 0.4f, 1f);
                case "Comedy": return new Color(0.78f, 0.52f, 0.12f, 1f);
                case "Environment": return new Color(0.3f, 0.4f, 0.22f, 1f);
                case "Sponsor": return new Color(0.75f, 0.12f, 0.14f, 1f);
                case "DeckManagement": return new Color(0.22f, 0.3f, 0.48f, 1f);
                default: return new Color(0.4f, 0.36f, 0.32f, 1f);
            }
        }

        // Спонсорская карта нужна в комнате маркетинга как контракт. Пустой список комнаты сначала получает стандартные предложения.
        static int AddSponsorOffers(List<EventDefinition> sponsors)
        {
            if (sponsors.Count == 0)
                return 0;
            int added = 0;
            foreach (var room in DesignerData.LoadAll<MarketingRoomDefinition>())
            {
                if (room.offers == null)
                    room.offers = new List<MarketingOffer>();
                if (room.offers.Count == 0)
                    room.offers.AddRange(JamContent.Offers(int.MaxValue));
                foreach (var card in sponsors)
                {
                    if (room.offers.Any(o => o != null && o.cardId == card.id))
                        continue;
                    room.offers.Add(new MarketingOffer
                    {
                        cardId = card.id,
                        title = card.displayName,
                        blurb = string.IsNullOrEmpty(card.description) ? "Сними кадр и вставь его в эфир. Иначе выплаты нет." : card.description,
                        kind = OfferKind.Contract,
                        payout = card.sponsorPay,
                        scoreHit = card.sponsorScoreHit
                    });
                    added++;
                }

                EditorUtility.SetDirty(room);
            }

            return added;
        }

        // ---------- Мелочи ----------

        static List<string> Split(string text, char sep)
        {
            return text.Split(sep).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        }

        static float Number(string s, float fallback)
        {
            return float.TryParse(s.Replace("$", "").Trim(), NumberStyles.Float, Inv, out float v) ? v : fallback;
        }

        static T Parse<T>(string s, T fallback) where T : struct
        {
            return System.Enum.TryParse(s.Trim(), true, out T v) ? v : fallback;
        }

        // Ячейки строки по имени колонки; «—» и пусто — пустая строка.
        class Row
        {
            readonly List<string> _cells;
            readonly Dictionary<string, int> _columns;

            public Row(List<string> cells, Dictionary<string, int> columns)
            {
                _cells = cells;
                _columns = columns;
            }

            public string this[string column]
            {
                get
                {
                    if (!_columns.TryGetValue(column, out int i) || i >= _cells.Count)
                        return "";
                    string v = (_cells[i] ?? "").Trim();
                    return v == "—" || v == "-" || v == "–" ? "" : v;
                }
            }
        }
    }

    // Минимальное чтение .xlsx: значения ячеек одного листа построчно (общие строки, inline-строки, числа).
    static class Xlsx
    {
        static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        static readonly XNamespace PackageRel = "http://schemas.openxmlformats.org/package/2006/relationships";

        public static List<List<string>> ReadSheet(string path, string sheetName)
        {
            // Файл может быть открыт в Excel — читаем копию.
            byte[] bytes;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                bytes = new byte[fs.Length];
                int read = 0;
                while (read < bytes.Length)
                    read += fs.Read(bytes, read, bytes.Length - read);
            }

            using (var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
            {
                var strings = new List<string>();
                var shared = zip.GetEntry("xl/sharedStrings.xml");
                if (shared != null)
                {
                    foreach (var si in Load(shared).Root.Elements(Main + "si"))
                        strings.Add(string.Concat(si.Descendants(Main + "t").Select(t => t.Value)));
                }

                var workbook = Load(zip.GetEntry("xl/workbook.xml"));
                var rels = Load(zip.GetEntry("xl/_rels/workbook.xml.rels")).Root.Elements(PackageRel + "Relationship")
                    .ToDictionary(r => (string)r.Attribute("Id"), r => (string)r.Attribute("Target"));
                var sheets = workbook.Root.Element(Main + "sheets").Elements(Main + "sheet").ToList();
                var sheet = sheets.FirstOrDefault(s => (string)s.Attribute("name") == sheetName) ?? sheets.First();
                string target = rels[(string)sheet.Attribute(Rel + "id")].TrimStart('/');
                if (!target.StartsWith("xl/"))
                    target = "xl/" + target;

                var rows = new List<List<string>>();
                foreach (var row in Load(zip.GetEntry(target)).Descendants(Main + "row"))
                {
                    var cells = new List<string>();
                    foreach (var c in row.Elements(Main + "c"))
                    {
                        int col = Column((string)c.Attribute("r"));
                        while (cells.Count < col)
                            cells.Add("");
                        string type = (string)c.Attribute("t");
                        string v = (string)c.Element(Main + "v") ?? "";
                        string value = type == "s" && v.Length > 0 ? strings[int.Parse(v, CultureInfo.InvariantCulture)]
                            : type == "inlineStr" ? string.Concat(c.Descendants(Main + "t").Select(t => t.Value))
                            : v;
                        if (col < cells.Count)
                            cells[col] = value;
                        else
                            cells.Add(value);
                    }

                    rows.Add(cells);
                }

                return rows;
            }
        }

        static XDocument Load(ZipArchiveEntry entry)
        {
            using (var s = entry.Open())
                return XDocument.Load(s);
        }

        // «AB12» → 27.
        static int Column(string reference)
        {
            int col = 0;
            foreach (char ch in reference ?? "")
            {
                if (ch < 'A' || ch > 'Z')
                    break;
                col = col * 26 + (ch - 'A' + 1);
            }

            return Mathf.Max(0, col - 1);
        }
    }
}
