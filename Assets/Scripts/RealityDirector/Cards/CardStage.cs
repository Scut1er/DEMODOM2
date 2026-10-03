using System;
using System.Collections;
using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Events;
using RealityDirector.NPC;
using RealityDirector.UI;
using RealityDirector.Util;
using UnityEngine;

namespace RealityDirector.Cards
{
    // Карта на площадке — не цифры в интерфейсе, а то, что происходит в комнате:
    // карта влетает в точку, кубики решают силу, эффекты карты (данные из таблицы) превращаются
    // в действия людей, реквизит, связи, запертые двери. Работает по данным карты — новые карты дизайнера
    // оживают без кода; старые карты без эффектов получили свою постановку.
    public partial class CardStage : MonoBehaviour
    {
        // Сила эмоции из кубика: грань × множитель (d6 → до 30 пунктов из 100).
        public const int DiePoints = 5;

        List<NPCController> _cast;
        Func<bool> _bedOpen;
        Func<bool> _bathOpen;
        Action<string> _toast;
        CameraShake _shake;
        Transform _fridge;
        StageFx _fx;
        readonly List<StageProp> _props = new List<StageProp>();
        public IReadOnlyList<StageProp> Props => _props;
        readonly List<GameObject> _junk = new List<GameObject>();
        readonly List<Guest> _guests = new List<Guest>();

        public IReadOnlyList<NPCController> Cast => _cast;
        public StageFx Fx => _fx;
        public bool BedOpen => _bedOpen != null && _bedOpen();
        public bool BathOpen => _bathOpen != null && _bathOpen();
        public Transform Fridge => _fridge;

        public void Init(List<NPCController> cast, Func<bool> bedOpen, Func<bool> bathOpen, Action<string> toast, CameraShake shake, Transform fridge)
        {
            _cast = cast;
            _bedOpen = bedOpen;
            _bathOpen = bathOpen;
            _toast = toast;
            _shake = shake;
            _fridge = fridge;
            _fx = StageFx.Ensure();
        }

        void OnDestroy()
        {
            ClearAll();
            if (_fx != null)
                Destroy(_fx.gameObject);
        }

        // Съёмка закончилась — реквизит, гости, замки убираются.
        public void ClearAll()
        {
            StopAllCoroutines();
            for (int i = 0; i < _props.Count; i++)
            {
                if (_props[i] != null)
                    Destroy(_props[i].gameObject);
            }

            _props.Clear();
            for (int i = 0; i < _guests.Count; i++)
            {
                if (_guests[i] != null)
                    Destroy(_guests[i].gameObject);
            }

            _guests.Clear();
            for (int i = 0; i < _junk.Count; i++)
            {
                if (_junk[i] != null)
                    Destroy(_junk[i]);
            }

            _junk.Clear();
            if (_cast != null)
            {
                for (int i = 0; i < _cast.Count; i++)
                {
                    if (_cast[i] != null)
                        _cast[i].Unlock();
                }
            }

            if (_fx != null)
                _fx.Clear();
        }

        public Room RoomAt(Vector2 p)
        {
            return HouseMap.At(p, BedOpen, BathOpen);
        }

        // Можно ли поставить сюда реквизит / направить зональную карту.
        public bool CanPlace(Vector2 p)
        {
            return RoomAt(p) != Room.None;
        }

        // Production Slots (GDD: объект окружения занимает слот до конца съёмки). Сколько — SeasonConfig.productionSlots.
        public int ProductionSlots = 3;

        public static bool UsesSlot(EventDefinition def)
        {
            return def != null && def.specialRules != null && def.specialRules.Contains(SpecialRule.UsesProductionSlot);
        }

        public int SlotsUsed
        {
            get
            {
                int used = 0;
                foreach (var prop in _props)
                {
                    if (prop != null && !prop.Dressing && UsesSlot(prop.Def))
                        used++;
                }

                return used;
            }
        }

        public bool SlotFree(EventDefinition def)
        {
            return !UsesSlot(def) || SlotsUsed < ProductionSlots;
        }

        // ---------- Прицел карты на комнату ----------

        SpriteRenderer _previewDisc;
        SpriteRenderer _previewRing;
        SpriteRenderer _previewIcon;
        string _previewKind;

        // Карта наведена на участника: над ним — прогноз реакции (уровень и причины, без формул).
        public void PreviewActor(EventDefinition def, NPCController npc)
        {
            if (def == null || npc == null)
            {
                _fx.Hint(null, null);
                return;
            }

            var f = CardBrief.Predict(def, npc);
            string text = f.level == CardBrief.Level.None
                ? npc.DisplayName
                : "РЕАКЦИЯ: <color=" + CardBrief.LevelColor(f.level) + ">" + CardBrief.LevelName(f.level) + "</color>";
            if (f.reasons.Count > 0)
                text += "\n<size=13>" + string.Join(" · ", f.reasons) + "</size>";
            _fx.Hint(npc.transform, text);
        }

        // Пока карта на комнату в руке — под курсором круг ауры и призрак реквизита. Красный — сюда нельзя
        // (не пол открытой комнаты или заняты все Production Slots).
        public void Preview(EventDefinition def, Vector2 point)
        {
            if (def == null)
            {
                if (_previewDisc != null)
                    _previewDisc.gameObject.SetActive(false);
                return;
            }

            if (_previewDisc == null)
            {
                var root = new GameObject("ZonePreview").transform;
                _previewDisc = SpriteUtil.Show(root, "disc", Vector3.zero, _fx.DiscSprite(), 2);
                _previewRing = SpriteUtil.Show(_previewDisc.transform, "ring", Vector3.zero, _fx.Ring01, 2);
                _previewIcon = SpriteUtil.Show(root, "ghost", Vector3.zero, null, 7);
                _previewDisc.transform.SetParent(root, false);
                _junk.Add(root.gameObject);
            }

            var rootT = _previewDisc.transform.parent;
            rootT.gameObject.SetActive(true);
            _previewDisc.gameObject.SetActive(true);
            bool ok = CanPlace(point) && SlotFree(def);
            float radius = def.aura != null && def.aura.enabled && def.aura.radius > 0.1f ? def.aura.radius : 1.4f;
            rootT.position = point;
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.03f;
            _previewDisc.transform.localScale = Vector3.one * radius * pulse;
            Color c = ok ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 0.3f, 0.25f);
            _previewDisc.color = new Color(c.r, c.g, c.b, 0.18f);
            _previewRing.color = new Color(c.r, c.g, c.b, 0.85f);
            string kind = !string.IsNullOrEmpty(def.environmentId) ? def.environmentId : null;
            if (kind != _previewKind)
            {
                _previewKind = kind;
                _previewIcon.sprite = kind != null ? GhostSprite(kind) : null;
                if (_previewIcon.sprite != null)
                {
                    var size = _previewIcon.sprite.bounds.size;
                    float k = 0.9f / Mathf.Max(0.01f, Mathf.Max(size.x, size.y));
                    _previewIcon.transform.localScale = new Vector3(k, k, 1f);
                }
            }

            _previewIcon.color = new Color(1f, 1f, 1f, ok ? 0.55f : 0.2f);
        }

        static Sprite GhostSprite(string kind)
        {
            switch (kind)
            {
                case "RomanceSofa": return GameArt.Sofa != null ? GameArt.Sofa : IllustratedArt.Sofa;
                case "HellColaFridge": return GameArt.Fridge != null ? GameArt.Fridge : IllustratedArt.Fridge;
                default: return PropArt.Get(kind);
            }
        }

        // ---------- Розыгрыш ----------

        class Ctx
        {
            public EventDefinition def;
            public NPCController target;
            public NPCController partner;
            public Interactable obj;
            public Vector2 point;
            public Room room;
            public StageProp prop;
            public readonly List<Roll> rolls = new List<Roll>();
        }

        class Roll
        {
            public DiceEffect dice;
            public int value;
            public bool used;
        }

        public void Play(EventDefinition def, Interactable obj, NPCController target, Vector2? point)
        {
            if (def == null)
                return;
            var c = new Ctx { def = def, target = target, obj = obj };
            c.partner = target != null ? Partner(target) : null;
            if (point.HasValue)
                c.point = point.Value;
            else if (target != null)
                c.point = target.transform.position;
            else if (obj != null)
                c.point = obj.transform.position;
            else
                c.point = BusiestRoomCenter();
            c.room = RoomAt(c.point);
            if (c.room == Room.None)
                c.room = Room.Kitchen;
            StartCoroutine(Run(c));
        }

        IEnumerator Run(Ctx c)
        {
            var def = c.def;
            // Колодные эффекты меняют руку, а не комнату — их видно в руке (PitchFlow.DeckPlay).
            if (CardRuntime.OnlyDeck(def))
                yield break;
            yield return _fx.CardFly(def, (Vector3)c.point + Vector3.up * 0.6f);
            if (_shake != null)
                _shake.Punch(0.07f, 0.12f);

            // Что обещает карта — коротко над точкой удара: игрок видит описание прямо там, где оно сбудется.
            string sub = Pitch(def);
            _fx.Banner(Anchor(c), def.displayName.ToUpperInvariant(), sub, UiKit.Gold, 3.4f);

            RollDice(c);
            if (c.rolls.Count > 0)
                yield return new WaitForSeconds(0.95f);

            bool any = def.effects != null && def.effects.Count > 0;
            if (any)
            {
                for (int i = 0; i < def.effects.Count; i++)
                {
                    var e = def.effects[i];
                    if (e == null)
                        continue;
                    Apply(e, c);
                    yield return new WaitForSeconds(0.18f);
                }
            }
            else
            {
                Legacy(c);
            }

            // Кубик, который ни один эффект не забрал, всё равно двигает эмоцию цели — бросок не пропадает.
            for (int i = 0; i < c.rolls.Count; i++)
            {
                var r = c.rolls[i];
                if (r.used || r.dice.subject != DiceSubject.Stat || c.target == null)
                    continue;
                r.used = true;
                c.target.ShiftStat(r.dice.stat, DieSign(r.dice) * r.value * DiePoints);
            }

            yield return new WaitForSeconds(0.25f);
            EscalateAll();
        }

        Func<Vector3> Anchor(Ctx c)
        {
            if (c.target != null)
            {
                var t = c.target.transform;
                return () => t.position;
            }

            Vector3 p = c.point;
            return () => p;
        }

        // Строка обещания карты: описание из таблицы, а у старых карт — своя фраза.
        static string Pitch(EventDefinition def)
        {
            string text = def.description;
            if (string.IsNullOrEmpty(text) || text == def.hint)
                text = LegacyPitch(def.id);
            if (string.IsNullOrEmpty(text))
                return null;
            text = text.Trim();
            if (text.Length > 70)
            {
                int cut = text.IndexOf('.', 30);
                text = cut > 0 && cut < 90 ? text.Substring(0, cut + 1) : text.Substring(0, 70) + "…";
            }

            return text;
        }

        public static string LegacyPitch(string id)
        {
            switch (id)
            {
                case "fridge_fire": return "Холодильник горит. Кто рядом — паникует или лезет в драку.";
                case "provoke": return "Злой срывается на всех вокруг.";
                case "no_hot_water": return "Горячей воды нет. Все мёрзнут и ворчат.";
                case "spoiled_food": return "Кухня воняет — все бегут оттуда, злые.";
                case "cut_wifi": return "Сети нет. Ищут сигнал и винят друг друга.";
                case "meditation_bell": return "Звон успокаивает дом: садятся, выдыхают.";
                case "confession_cam": return "Камера-исповедальня: цель признается и заплачет.";
                case "open_bathroom": return "Новая комната — новые встречи.";
                case "open_bedroom": return "Спальня открыта — тепло и близость.";
                case "sponsor_cola": return "Банка Hell Cola в кадре — деньги спонсора.";
                case "sponsor_energy": return "Энергетик в кадре — бодрость и реклама.";
                default: return null;
            }
        }

        // ---------- Кубики ----------

        void RollDice(Ctx c)
        {
            var list = c.def.diceEffects;
            if (list == null)
                return;
            int slot = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                if (d == null)
                    continue;
                if (!Condition(d.onlyIf, c.target, c))
                    continue;
                // Кубик реквизита бросается, когда им пользуются, — не при постановке.
                if (d.subject == DiceSubject.Check && HasEffect(c.def, CardEffectType.SpawnObject))
                    continue;
                var roll = Dice.Roll(d, StepsFor(d, c.target, c.point));
                c.rolls.Add(new Roll { dice = d, value = roll.value });
                string label = DiceLabel(d) + (roll.steps > 0 ? " ↑" : "");
                Color color = d.subject == DiceSubject.Stat ? StageFx.StatColor(d.stat) : d.subject == DiceSubject.Relationship ? new Color(1f, 0.5f, 0.7f) : UiKit.Gold;
                StartCoroutine(_fx.Dice(Anchor(c), roll.die, roll.value, label, color, slot, slot * 0.08f));
                slot++;
            }
        }

        // Ступени кубика: условия из таблицы (черта, состояние, эмоция цели, тег окружения рядом)
        // и аура реквизита, в которой стоит цель.
        public int StepsFor(DiceEffect d, NPCController target, Vector2 at)
        {
            int steps = 0;
            if (d.stepUps != null)
            {
                foreach (var s in d.stepUps)
                {
                    if (s != null && StepHolds(s, target, at))
                        steps += Mathf.Max(1, s.steps);
                }
            }

            if (d.subject == DiceSubject.Stat && target != null)
            {
                foreach (var prop in _props)
                {
                    if (prop == null || prop.Def == null || prop.Def.aura == null || prop.Def.aura.diceModifiers == null)
                        continue;
                    if (Vector2.Distance(prop.transform.position, target.transform.position) > prop.Radius)
                        continue;
                    foreach (var m in prop.Def.aura.diceModifiers)
                    {
                        if (m != null && m.stat == d.stat)
                            steps += m.steps;
                    }
                }
            }

            return Mathf.Max(0, steps);
        }

        bool StepHolds(DiceStepUp s, NPCController npc, Vector2 at)
        {
            switch (s.condition)
            {
                case StepUpCondition.TargetHasTrait:
                    return npc != null && HasTrait(npc, s.key);
                case StepUpCondition.TargetHasState:
                    return npc != null && npc.HasState(s.key);
                case StepUpCondition.TargetStatAtLeast:
                    return npc != null && npc.Read(s.stat) >= s.value;
                default:
                    Vector2 where = npc != null ? (Vector2)npc.transform.position : at;
                    foreach (var prop in _props)
                    {
                        if (prop == null || prop.Def == null || Vector2.Distance(prop.transform.position, where) > prop.Radius)
                            continue;
                        if (prop.Kind == s.key || (prop.Def.aura != null && prop.Def.aura.tags != null && prop.Def.aura.tags.Contains(s.key)))
                            return true;
                    }

                    return false;
            }
        }

        public static bool HasTrait(NPCController npc, string key)
        {
            if (npc == null || npc.Trait == null || string.IsNullOrEmpty(key))
                return false;
            string k = key.Trim();
            return string.Equals(npc.Trait.traitId.ToString(), k, StringComparison.OrdinalIgnoreCase)
                   || (!string.IsNullOrEmpty(npc.Trait.displayName) && npc.Trait.displayName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        static bool HasEffect(EventDefinition def, CardEffectType type)
        {
            if (def.effects == null)
                return false;
            for (int i = 0; i < def.effects.Count; i++)
            {
                if (def.effects[i] != null && def.effects[i].type == type)
                    return true;
            }

            return false;
        }

        static string DiceLabel(DiceEffect d)
        {
            switch (d.subject)
            {
                case DiceSubject.Stat: return StageFx.StatName(d.stat);
                case DiceSubject.Relationship: return "отношения";
                default:
                    string check = string.IsNullOrEmpty(d.check) ? "проверка" : d.check;
                    if (check.Length > 16)
                        check = check.Substring(0, 15) + "…";
                    return check.ToLowerInvariant();
            }
        }

        // Забирает бросок под эффект: сначала точное совпадение по эмоции/оси, потом первый свободный нужного вида.
        int Take(Ctx c, DiceSubject subject, ActorStat? stat, int fallback)
        {
            var roll = TakeRoll(c, subject, stat, null);
            return roll != null ? roll.value : fallback;
        }

        Roll TakeRoll(Ctx c, DiceSubject subject, ActorStat? stat, RelationshipAxis? axis)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < c.rolls.Count; i++)
                {
                    var r = c.rolls[i];
                    if (r.used || r.dice.subject != subject)
                        continue;
                    if (pass == 0 && stat.HasValue && r.dice.stat != stat.Value)
                        continue;
                    if (pass == 0 && axis.HasValue && r.dice.axis != axis.Value)
                        continue;
                    r.used = true;
                    return r;
                }
            }

            return null;
        }

        // Знак броска — из таблицы: галочка «понижает» у кубика.
        static int DieSign(DiceEffect d)
        {
            return d != null && d.lower ? -1 : 1;
        }

        // ---------- Эффекты по данным карты ----------

        void Apply(CardEffect e, Ctx c)
        {
            var def = c.def;
            switch (e.type)
            {
                case CardEffectType.ChangeStat:
                {
                    // Сила — из кубика (грань × DiePoints) или из «Силы» эффекта; знак — «понижает» у кубика или минус в силе.
                    var roll = TakeRoll(c, DiceSubject.Stat, e.stat, null);
                    int amount = e.amount != 0f ? Mathf.RoundToInt(e.amount) : (roll != null ? roll.value : 2) * DiePoints;
                    int sign = e.amount < 0f ? -1 : roll != null ? DieSign(roll.dice) : Sign(def, e.stat);
                    int signed = sign * Mathf.Abs(amount);
                    foreach (var npc in Receivers(e, c))
                    {
                        if (!Condition(e.onlyIf, npc, c))
                            continue;
                        Flinch(npc, signed > 0 && (e.stat == ActorStat.Anger || e.stat == ActorStat.Stress));
                        npc.ShiftStat(e.stat, signed);
                    }

                    break;
                }
                case CardEffectType.ChangeHighestNegative:
                {
                    int faces = Take(c, DiceSubject.Check, null, 3);
                    bool calm = Calming(def);
                    foreach (var npc in Receivers(e, c))
                    {
                        var stat = HighestNegative(npc);
                        npc.ShiftStat(stat, (calm ? -1 : 1) * faces * DiePoints);
                        if (calm)
                            npc.CalmDown(stat == ActorStat.Anger ? "ладно, проехали" : stat == ActorStat.Stress ? "фух..." : "спасибо...");
                        else
                            npc.Line(stat == ActorStat.Anger ? "да вы издеваетесь!" : stat == ActorStat.Stress ? "я больше не могу" : "зачем вы так...", 3.2f);
                    }

                    break;
                }
                case CardEffectType.ChangeRelationship:
                {
                    var roll = TakeRoll(c, DiceSubject.Relationship, null, e.axis);
                    int amount = e.amount != 0f ? Mathf.Abs(Mathf.RoundToInt(e.amount)) : (roll != null ? roll.value : 3) * DiePoints;
                    int sign = e.amount < 0f ? -1 : roll != null ? DieSign(roll.dice) : (e.axis == RelationshipAxis.Hostility && Calming(def) ? -1 : 1);
                    Bond(e.axis, c, sign * amount);
                    break;
                }
                case CardEffectType.AddState:
                {
                    float seconds = e.duration == StateDuration.Timed ? (e.seconds > 0f ? e.seconds : 25f) : 0f;
                    foreach (var npc in Receivers(e, c))
                    {
                        npc.AddState(e.key, seconds);
                        _fx.Icon(npc.transform.position + Vector3.up * 1.4f, StateIconKey(e.key), new Vector3(0f, 0.6f, 0f), 1.4f, 0.4f);
                    }

                    if (e.key == NPCController.StateSuspicious && c.target != null && c.partner != null)
                    {
                        c.target.Line("а с кем ты переписывался?..", 3.4f, true);
                        _fx.Link(c.target, c.partner, "Eye", new Color(0.8f, 0.55f, 1f), 3f);
                    }

                    break;
                }
                case CardEffectType.WorldEvent:
                {
                    // Теги карты уже разослал EventExecutor — здесь только новые (Alarm, Rumour…), чтобы не реагировали дважды.
                    var extra = Split(e.key);
                    if (def.tags != null)
                        extra.RemoveAll(t => def.tags.Contains(t));
                    if (extra.Count > 0)
                        Publish(c, extra, "card_" + def.id + "_" + e.key);
                    break;
                }
                case CardEffectType.MoveActor:
                    Private(c);
                    break;
                case CardEffectType.ForceMovement:
                    Scatter(c);
                    break;
                case CardEffectType.SpawnObject:
                    c.prop = Spawn(def, c.point, c.room);
                    break;
                case CardEffectType.CreateAura:
                    if (c.prop == null)
                        c.prop = Spawn(def, c.point, c.room, true);
                    if (c.prop != null)
                        c.prop.EnableAura();
                    break;
                case CardEffectType.NpcInteraction:
                case CardEffectType.EventCandidate:
                case CardEffectType.BehaviourWeights:
                    if (c.prop != null)
                        c.prop.EnableUse();
                    break;
                case CardEffectType.TriggerZone:
                    if (c.prop != null)
                        c.prop.EnableTrap();
                    break;
                case CardEffectType.SponsorVisibility:
                    if (c.prop != null)
                        c.prop.Sponsor = true;
                    break;
                case CardEffectType.LockExit:
                    StartCoroutine(Lock(c, def.environmentSeconds > 0f ? def.environmentSeconds : 25f));
                    break;
                case CardEffectType.QuietRoom:
                    Quiet(c, Take(c, DiceSubject.Stat, ActorStat.Stress, 2) * DiePoints);
                    break;
                case CardEffectType.AddContext:
                case CardEffectType.LowerPublicContext:
                    Context(c, e.type == CardEffectType.LowerPublicContext ? "Private" : e.key);
                    break;
                case CardEffectType.Check:
                    Check(e, c);
                    break;
                case CardEffectType.RevealSecret:
                    if (e.key == "Candidate")
                        break; // секрет достанет тот, кто возьмёт телефон
                    if (e.key == "Conditional" && Take(c, DiceSubject.Check, null, 4) < 5)
                    {
                        if (c.target != null)
                            c.target.Line("без комментариев", 3f);
                        break;
                    }

                    Reveal(c.target, c.partner, "При всех");
                    break;
                case CardEffectType.InviteActor:
                    Invite(c, e.key);
                    break;
                case CardEffectType.InterruptChain:
                    foreach (var npc in Receivers(e, c))
                    {
                        npc.CalmDown("ладно, о другом...");
                        _fx.Ring(npc.transform.position + Vector3.up * 0.6f, 2.2f, new Color(0.5f, 0.85f, 1f, 0.9f), 0.7f);
                    }

                    break;
                case CardEffectType.RandomOutcome:
                    // У «Красной кнопки» исход наступает, когда кто-то её нажмёт (StageProp).
                    if (c.prop == null)
                        Outcome(c.point, c.room, Take(c, DiceSubject.Check, null, UnityEngine.Random.Range(1, 11)));
                    break;
                case CardEffectType.NextCaptureBonus:
                    Capture.CaptureSystem.BonusUntil = Time.unscaledTime + (e.seconds > 0f ? e.seconds : 10f);
                    _fx.Banner(() => (Vector3)HouseMap.Center(c.room) + Vector3.up * 1.5f, "НУЖНЫЙ МОМЕНТ", "камера: качество кадра выше " + Mathf.RoundToInt(e.seconds > 0f ? e.seconds : 10f) + " с", new Color(0.55f, 0.9f, 1f), 3f);
                    break;
                default:
                    // Колодные эффекты (добор, сброс, цена) живут в руке, а не в комнате.
                    break;
            }
        }

        // Знак изменения: «контроль» и «тише» успокаивают, «унизить» бьёт по уверенности, остальное — разгоняет.
        static int Sign(EventDefinition def, ActorStat stat)
        {
            bool negativeStat = stat == ActorStat.Anger || stat == ActorStat.Stress || stat == ActorStat.Sadness;
            if (negativeStat && Calming(def))
                return -1;
            if (stat == ActorStat.Confidence && def.tags != null && (def.tags.Contains("Humiliation") || def.tags.Contains("Petty")))
                return -1;
            if (stat == ActorStat.SelfControl)
                return -1;
            return 1;
        }

        static bool Calming(EventDefinition def)
        {
            if (def.category == "Control")
                return true;
            return def.tags != null && (def.tags.Contains("Calm") || def.tags.Contains("Safe") || def.tags.Contains("Reconcile"));
        }

        // Условие эффекта/кубика из таблицы — по смыслу: черта, рейтинг, романтический контекст.
        bool Condition(string onlyIf, NPCController npc, Ctx c)
        {
            return CardRuntime.Holds(onlyIf, npc, c.def);
        }

        static ActorStat HighestNegative(NPCController npc)
        {
            if (npc.Anger >= npc.Stress && npc.Anger >= npc.Sadness)
                return ActorStat.Anger;
            return npc.Stress >= npc.Sadness ? ActorStat.Stress : ActorStat.Sadness;
        }

        IEnumerable<NPCController> Receivers(CardEffect e, Ctx c)
        {
            var list = new List<NPCController>();
            switch (e.receiver)
            {
                case EffectReceiver.AllInRoom:
                {
                    // Карта на весь дом — все; иначе те, кто в комнате цели или точки удара.
                    Room room = c.target != null ? RoomAt(c.target.transform.position) : c.room;
                    foreach (var npc in Alive())
                    {
                        if (c.def.targetType == TargetType.Global || RoomAt(npc.transform.position) == room)
                            list.Add(npc);
                    }

                    break;
                }
                case EffectReceiver.Witnesses:
                    foreach (var npc in Alive())
                    {
                        if (npc != c.target && RoomAt(npc.transform.position) == RoomAt(c.target != null ? (Vector2)c.target.transform.position : c.point))
                            list.Add(npc);
                    }

                    break;
                case EffectReceiver.RandomActor:
                {
                    var all = Alive();
                    if (all.Count > 0)
                        list.Add(all[UnityEngine.Random.Range(0, all.Count)]);
                    break;
                }
                default:
                    if (c.target != null)
                    {
                        list.Add(c.target);
                        if (c.def.targetType == TargetType.ActorPair && c.partner != null && e.type != CardEffectType.ChangeStat)
                            list.Add(c.partner);
                    }
                    else
                    {
                        // Карта на комнату / дом без цели: все, кого она задевает.
                        foreach (var npc in Alive())
                        {
                            if (c.def.targetType == TargetType.Global || RoomAt(npc.transform.position) == c.room)
                                list.Add(npc);
                        }
                    }

                    break;
            }

            return list;
        }

        public List<NPCController> Alive()
        {
            var list = new List<NPCController>();
            if (_cast == null)
                return list;
            for (int i = 0; i < _cast.Count; i++)
            {
                if (_cast[i] != null)
                    list.Add(_cast[i]);
            }

            return list;
        }

        public NPCController Partner(NPCController npc)
        {
            if (npc == null)
                return null;
            if (npc.Rival != null && npc.Rival != npc)
                return npc.Rival;
            NPCController best = null;
            float bestD = float.MaxValue;
            foreach (var other in Alive())
            {
                if (other == npc)
                    continue;
                float d = Vector2.Distance(other.transform.position, npc.transform.position);
                if (d < bestD)
                {
                    bestD = d;
                    best = other;
                }
            }

            return best;
        }

        Vector2 BusiestRoomCenter()
        {
            var counts = new Dictionary<Room, int>();
            foreach (var npc in Alive())
            {
                var r = RoomAt(npc.transform.position);
                counts.TryGetValue(r, out int n);
                counts[r] = n + 1;
            }

            Room best = Room.Kitchen;
            int bestN = -1;
            foreach (var pair in counts)
            {
                if (pair.Key != Room.None && pair.Value > bestN)
                {
                    best = pair.Key;
                    bestN = pair.Value;
                }
            }

            return HouseMap.Center(best);
        }

        void Flinch(NPCController npc, bool hard)
        {
            if (npc == null)
                return;
            FadeBit.Burst(npc.transform.position + Vector3.up * 0.9f, hard ? 6 : 3, hard ? new Color(1f, 0.35f, 0.2f) : new Color(1f, 0.85f, 0.5f));
        }

        void EscalateAll()
        {
            foreach (var npc in Alive())
                npc.Escalate();
        }

        static string StateIconKey(string key)
        {
            switch (key)
            {
                case NPCController.StateSuspicious: return "Eye";
                case NPCController.StateAmplified: return "Bolt";
                default: return "Bang";
            }
        }

        static List<string> Split(string key)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(key))
                return list;
            foreach (var part in key.Split(','))
            {
                var t = part.Trim();
                if (t.Length > 0)
                    list.Add(t);
            }

            return list;
        }

        // Событие мира от карты — на него отвечают правила черт, его видит камера.
        void Publish(Ctx c, List<string> extra, string id)
        {
            var tags = new List<string>();
            if (extra != null)
                tags.AddRange(extra);
            EventBus.Publish(new WorldEvent
            {
                eventId = id,
                tags = tags,
                targetActorId = c.target != null ? c.target.Id : null,
                hasLocus = c.target == null,
                locus = c.point,
                time = Time.time
            });
        }

        public void Stamp(Vector2 at, NPCController who, params string[] tags)
        {
            EventBus.Publish(new WorldEvent
            {
                eventId = "stage",
                tags = new List<string>(tags),
                sourceActorId = who != null ? who.Id : null,
                hasLocus = who == null,
                locus = at,
                time = Time.time,
                depth = 1
            });
        }

        // ---------- Отношения ----------

        void Bond(RelationshipAxis axis, Ctx c, int amount)
        {
            var a = c.target;
            var b = c.partner;
            if (a == null || b == null)
                return;
            Vector2 mid = ((Vector2)a.transform.position + (Vector2)b.transform.position) * 0.5f;
            Vector2 side = ((Vector2)a.transform.position - mid).normalized;
            if (side.sqrMagnitude < 0.01f)
                side = Vector2.left;
            if (amount < 0)
            {
                Cool(axis, a, b, mid, side, -amount);
                return;
            }

            switch (axis)
            {
                case RelationshipAxis.Hostility:
                    a.ShiftBond(amount, 0);
                    b.ShiftBond(amount, 0);
                    a.ShiftStat(ActorStat.Anger, amount / 2);
                    b.ShiftStat(ActorStat.Anger, amount / 2);
                    _fx.Link(a, b, "Bolt", new Color(1f, 0.3f, 0.2f), 4f);
                    Face(a, mid + side * 0.55f, "это ТЫ начал!", StageAnim.Bang);
                    Face(b, mid - side * 0.55f, "я?! да ты сам!", StageAnim.Bang);
                    _fx.FloatText(() => (Vector3)(((Vector2)a.transform.position + (Vector2)b.transform.position) * 0.5f) + Vector3.up * 1.6f, "вражда +" + amount, new Color(1f, 0.35f, 0.3f), 26);
                    Stamp(mid, a, MomentTags.Conflict, "Argument");
                    if (a.Hostility >= 45 && a.Anger >= 45)
                        StartCoroutine(Later(2.6f, () =>
                        {
                            a.ShiftStat(ActorStat.Anger, 25);
                            a.Escalate();
                        }));
                    break;
                case RelationshipAxis.Trust:
                    a.ShiftBond(0, amount);
                    b.ShiftBond(0, amount);
                    _fx.Link(a, b, "Handshake", new Color(1f, 0.85f, 0.4f), 3.5f);
                    Face(a, mid + side * 0.5f, "ну... прости", StageAnim.None);
                    Face(b, mid - side * 0.5f, "ладно, проехали", StageAnim.None, () =>
                    {
                        if (b.AcceptHug(3f) && a.AcceptHug(3f))
                        {
                            a.Announce("reconcile", b, MomentTags.Hug, MomentTags.Warmth);
                            _fx.Icon(mid + Vector2.up * 1.3f, "Heart", new Vector3(0f, 0.8f, 0f), 1.4f, 0.45f);
                        }
                    });
                    a.ShiftStat(ActorStat.Anger, -amount / 2);
                    b.ShiftStat(ActorStat.Anger, -amount / 2);
                    _fx.FloatText(() => (Vector3)mid + Vector3.up * 1.6f, "доверие +" + amount, new Color(1f, 0.85f, 0.4f), 26);
                    break;
                default:
                    a.ShiftStat(ActorStat.Attraction, amount);
                    b.ShiftStat(ActorStat.Attraction, amount);
                    _fx.Link(a, b, "Heart", new Color(1f, 0.45f, 0.7f), 4f);
                    Face(a, mid + side * 0.5f, "ты сегодня...", StageAnim.None);
                    Face(b, mid - side * 0.5f, "ой, перестань", StageAnim.Laugh);
                    Stamp(mid, a, "Flirt", "Romance", MomentTags.Warmth);
                    break;
            }
        }

        // Отношения вниз: вражда тает (мирятся), доверие/влечение остывают (отворачиваются).
        void Cool(RelationshipAxis axis, NPCController a, NPCController b, Vector2 mid, Vector2 side, int amount)
        {
            if (axis == RelationshipAxis.Hostility)
            {
                a.ShiftBond(-amount, 0);
                b.ShiftBond(-amount, 0);
                a.ShiftStat(ActorStat.Anger, -amount / 2);
                b.ShiftStat(ActorStat.Anger, -amount / 2);
                _fx.Link(a, b, "Handshake", new Color(0.55f, 0.95f, 0.6f), 3.5f);
                Face(a, mid + side * 0.5f, "давай без войны", StageAnim.None);
                Face(b, mid - side * 0.5f, "давай...", StageAnim.None);
                _fx.FloatText(() => (Vector3)mid + Vector3.up * 1.6f, "вражда −" + amount, new Color(0.55f, 0.95f, 0.6f), 26);
                Stamp(mid, a, "Reconcile", MomentTags.Warmth);
                return;
            }

            if (axis == RelationshipAxis.Trust)
                a.ShiftBond(0, -amount);
            else
            {
                a.ShiftStat(ActorStat.Attraction, -amount);
                b.ShiftStat(ActorStat.Attraction, -amount);
            }

            _fx.Link(a, b, "Bolt", new Color(0.6f, 0.65f, 0.8f), 3f);
            Face(a, mid + side * 1.2f, axis == RelationshipAxis.Trust ? "я тебе больше не верю" : "знаешь, нет", StageAnim.None);
            _fx.FloatText(() => (Vector3)mid + Vector3.up * 1.6f, (axis == RelationshipAxis.Trust ? "доверие −" : "влечение −") + amount, new Color(0.6f, 0.7f, 0.9f), 26);
            Stamp(mid, a, "Cold");
        }

        // Подойти к точке и сыграть реплику.
        void Face(NPCController npc, Vector2 spot, string line, StageAnim anim, Action arrived = null)
        {
            npc.Direct(new StageBeat
            {
                goal = spot,
                line = line,
                hold = 3.2f,
                anim = anim,
                speed = 2.2f,
                giveUp = 3.5f,
                urgent = true,
                onArrive = arrived == null ? null : (Action<NPCController>)(me => arrived())
            });
        }

        static IEnumerator Later(float seconds, Action act)
        {
            yield return new WaitForSeconds(seconds);
            act?.Invoke();
        }

        // ---------- Наедине / разбежались ----------

        void Private(Ctx c)
        {
            var a = c.target;
            var b = c.partner;
            if (a == null)
                return;
            Room room = BedOpen ? Room.Bedroom : BathOpen ? Room.Bathroom : Room.Living;
            Vector2 spot = room == Room.Living ? new Vector2(-6.6f, 3.5f) : HouseMap.Center(room);
            var shade = _fx.Shade(Rect.MinMaxRect(spot.x - 1.6f, spot.y - 1.2f, spot.x + 1.6f, spot.y + 1.2f), new Color(1f, 0.5f, 0.75f, 0.14f), 2, 10f);
            _junk.Add(shade.gameObject);
            _fx.Banner(() => (Vector3)spot + Vector3.up * 1f, "НАЕДИНЕ", HouseMap.Name(room) + ": без лишних глаз", new Color(1f, 0.6f, 0.8f), 3f);
            bool romance = c.def.tags != null && c.def.tags.Contains("Romance");
            a.Direct(new StageBeat
            {
                goal = spot + new Vector2(-0.5f, 0f),
                line = "можно тебя на минутку?",
                hold = 5f,
                speed = 2f,
                giveUp = 6f,
                urgent = true,
                onArrive = me =>
                {
                    me.ShiftStat(ActorStat.Stress, -10);
                    if (b != null && romance)
                    {
                        int faces = Take(c, DiceSubject.Stat, ActorStat.Attraction, 3);
                        me.ShiftStat(ActorStat.Attraction, faces * DiePoints);
                        b.ShiftStat(ActorStat.Attraction, faces * DiePoints);
                        _fx.Link(me, b, "Heart", new Color(1f, 0.45f, 0.7f), 4f);
                    }

                    Stamp(spot, me, "Private", romance ? "Romance" : "Confession", MomentTags.Warmth);
                }
            });
            if (b != null)
            {
                b.Direct(new StageBeat
                {
                    goal = spot + new Vector2(0.5f, 0f),
                    line = "ну давай...",
                    hold = 5f,
                    speed = 2f,
                    giveUp = 6f,
                    urgent = true,
                    onArrive = me => me.ShiftStat(ActorStat.Stress, -10)
                });
            }
        }

        // Сирена: все разбегаются по другим комнатам.
        void Scatter(Ctx c)
        {
            var rooms = new List<Room> { Room.Living, Room.Kitchen };
            if (BedOpen)
                rooms.Add(Room.Bedroom);
            if (BathOpen)
                rooms.Add(Room.Bathroom);
            foreach (var npc in Alive())
            {
                var here = RoomAt(npc.transform.position);
                var others = rooms.FindAll(r => r != here);
                var to = others.Count > 0 ? others[UnityEngine.Random.Range(0, others.Count)] : here;
                npc.Direct(new StageBeat
                {
                    goal = HouseMap.RandomPoint(to),
                    speed = 3.4f,
                    line = UnityEngine.Random.value < 0.5f ? "ГОРИМ?!" : "ЧТО ЭТО?!",
                    hold = 2.2f,
                    anim = StageAnim.Cower,
                    urgent = true,
                    giveUp = 4f
                });
            }

            StartCoroutine(Siren(3.5f));
        }

        IEnumerator Siren(float seconds)
        {
            float t = 0f;
            var overlay = _fx.Shade(Rect.MinMaxRect(-8f, 0.2f, 8f, 8.3f), new Color(1f, 0.1f, 0.05f, 0f), 30, 0f);
            _junk.Add(overlay.gameObject);
            while (t < seconds)
            {
                t += Time.deltaTime;
                float a = Mathf.Repeat(t, 0.5f) < 0.25f ? 0.22f : 0.04f;
                overlay.color = new Color(1f, 0.1f, 0.05f, a);
                if (Mathf.Repeat(t, 0.5f) < Time.deltaTime)
                    Sfx.Play(Cue.Blip, 0.55f, 1.6f);
                yield return null;
            }

            Destroy(overlay.gameObject);
        }

        // ---------- Комнаты: замок, тишина, контекст ----------

        IEnumerator Lock(Ctx c, float seconds)
        {
            Room room = c.room;
            Rect whole = HouseMap.Whole(room);
            var locks = new List<GameObject>();
            foreach (var door in HouseMap.Doorways(room, BedOpen, BathOpen))
            {
                var chain = SpriteUtil.Show(null, "chain", door, PropArt.Get("Chain"), 12);
                SpriteUtil.Fit(chain, new Vector2(1.1f, 0.28f));
                chain.transform.rotation = Quaternion.Euler(0f, 0f, door.y > 4.9f ? 0f : 90f);
                var pad = SpriteUtil.Show(null, "padlock", door + new Vector2(0f, -0.1f), PropArt.Get("Padlock"), 13);
                SpriteUtil.Fit(pad, new Vector2(0.42f, 0.53f));
                locks.Add(chain.gameObject);
                locks.Add(pad.gameObject);
                _junk.Add(chain.gameObject);
                _junk.Add(pad.gameObject);
                FadeBit.Burst(door, 10, new Color(0.8f, 0.8f, 0.85f));
            }

            Sfx.Play(Cue.Slap, 0.6f, 0.5f);
            var shade = _fx.Shade(whole, new Color(1f, 0.15f, 0.1f, 0.12f), 2, seconds);
            _junk.Add(shade.gameObject);
            _fx.Banner(() => (Vector3)HouseMap.Center(room) + Vector3.up * 1.2f, "ЗАПЕРТО", HouseMap.Name(room) + " · " + Mathf.RoundToInt(seconds) + " с без выхода", new Color(1f, 0.4f, 0.35f), 3f);
            var trapped = new List<NPCController>();
            foreach (var npc in Alive())
            {
                if (RoomAt(npc.transform.position) == room)
                {
                    npc.LockIn(whole, seconds);
                    trapped.Add(npc);
                }
            }

            int stress = Take(c, DiceSubject.Stat, ActorStat.Stress, 2) * DiePoints;
            foreach (var npc in trapped)
                npc.ShiftStat(ActorStat.Stress, stress);
            if (trapped.Count == 0)
                _toast?.Invoke("В комнате никого — заперли пустоту.");
            else
                Stamp(HouseMap.Center(room), trapped[0], "Pressure", "Private", MomentTags.Conflict);

            float t = 0f;
            float nextBang = 1.2f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                if (t >= nextBang && trapped.Count > 0)
                {
                    nextBang = t + UnityEngine.Random.Range(5f, 7f);
                    var who = trapped[UnityEngine.Random.Range(0, trapped.Count)];
                    if (who != null && who.CanDirect(false))
                    {
                        who.Direct(new StageBeat
                        {
                            goal = HouseMap.Door(room),
                            line = UnityEngine.Random.value < 0.5f ? "ОТКРОЙТЕ!" : "ЭЙ, ВЫПУСТИТЕ!",
                            hold = 2.4f,
                            anim = StageAnim.Bang,
                            speed = 2.4f,
                            onArrive = me =>
                            {
                                me.ShiftStat(ActorStat.Stress, 5);
                                if (_shake != null)
                                    _shake.Punch(0.03f, 0.08f);
                            }
                        });
                    }

                    // Двое в запертой комнате — давление растёт и на отношения.
                    if (trapped.Count >= 2 && trapped[0] != null && trapped[1] != null)
                    {
                        trapped[0].ShiftBond(4, 0);
                        trapped[1].ShiftBond(4, 0);
                    }
                }

                yield return null;
            }

            foreach (var go in locks)
            {
                if (go != null)
                {
                    FadeBit.Burst(go.transform.position, 8, new Color(0.8f, 0.8f, 0.85f));
                    Destroy(go);
                }
            }

            foreach (var npc in trapped)
            {
                if (npc == null)
                    continue;
                npc.Unlock();
                npc.Line("наконец-то!", 2.5f);
            }

            Sfx.Play(Cue.Card, 0.5f, 1.3f);
        }

        void Quiet(Ctx c, int calm)
        {
            Room room = c.room;
            var shade = _fx.Shade(HouseMap.Whole(room), new Color(0.35f, 0.6f, 1f, 0.12f), 2, 14f);
            _junk.Add(shade.gameObject);
            _fx.Banner(() => (Vector3)HouseMap.Center(room) + Vector3.up * 1.2f, "ТИХО, МОТОР", HouseMap.Name(room) + ": без помех", new Color(0.55f, 0.8f, 1f), 3f);
            foreach (var npc in Alive())
            {
                if (RoomAt(npc.transform.position) != room)
                    continue;
                npc.CalmDown("тсс...");
                npc.ShiftStat(ActorStat.Stress, -Mathf.Max(5, calm));
                npc.Direct(new StageBeat { walk = false, hold = 3.5f, anim = StageAnim.Sit });
            }
        }

        // Контекст комнаты — табличка над ней и мягкий сдвиг эмоций у тех, кто внутри.
        void Context(Ctx c, string key)
        {
            var keys = Split(key);
            if (keys.Count == 0)
                return;
            Room room = c.target != null ? RoomAt(c.target.transform.position) : c.room;
            // Давление сильнее приватности: в запертой комнате не расслабляются.
            if (keys.Contains("Pressure"))
                keys.RemoveAll(k => k == "Private" || k == "Safe");
            // Замок и «наедине» уже показали свою плашку — вторая не нужна.
            string title = HasEffect(c.def, CardEffectType.LockExit) || HasEffect(c.def, CardEffectType.MoveActor) ? null : ContextTitle(keys[0]);
            if (title != null)
                _fx.Banner(() => (Vector3)HouseMap.Center(room) + Vector3.up * 1.7f, title, HouseMap.Name(room), ContextColor(keys[0]), 2.6f);
            foreach (var npc in Alive())
            {
                if (RoomAt(npc.transform.position) != room)
                    continue;
                foreach (var k in keys)
                {
                    if (k == "Private" || k == "Safe")
                    {
                        bool shy = npc.Trait != null && (npc.Trait.traitId == TraitId.Shy || npc.Trait.traitId == TraitId.Timid || npc.Trait.traitId == TraitId.Panicker);
                        npc.ShiftStat(ActorStat.Stress, shy ? -15 : -5);
                    }
                    else if (k == "Pressure")
                        npc.ShiftStat(ActorStat.Stress, 8);
                    else if (k == "Witness")
                        npc.ShiftStat(ActorStat.Stress, 5);
                }
            }

            if (keys.Contains("Private"))
            {
                var shade = _fx.Shade(HouseMap.Whole(room), new Color(0.6f, 0.4f, 1f, 0.08f), 2, 12f);
                _junk.Add(shade.gameObject);
            }
        }

        static string ContextTitle(string key)
        {
            switch (key)
            {
                case "Private": return "БЕЗ КАМЕР";
                case "Pressure": return "ДАВЛЕНИЕ";
                case "Gift": return null;
                case "Witness": return "СВИДЕТЕЛЬ";
                default: return null;
            }
        }

        static Color ContextColor(string key)
        {
            switch (key)
            {
                case "Private": return new Color(0.75f, 0.6f, 1f);
                case "Pressure": return new Color(1f, 0.45f, 0.35f);
                default: return UiKit.Gold;
            }
        }

        // ---------- Проверки, секреты, гости ----------

        void Check(CardEffect e, Ctx c)
        {
            var npc = c.target;
            switch (e.key)
            {
                case "SelfControl":
                {
                    if (npc == null)
                        break;
                    int roll = Take(c, DiceSubject.Check, null, 4);
                    bool breaks = roll * 10 > npc.SelfControl;
                    _fx.FloatText(() => npc.transform.position + Vector3.up * 1.9f, (breaks ? "сломался" : "держится") + "  " + roll * 10 + " vs " + npc.SelfControl, breaks ? new Color(0.5f, 0.8f, 1f) : UiKit.Muted, 22);
                    if (breaks)
                        Confess(npc, c.point);
                    else
                    {
                        npc.Line("мне нечего сказать", 3f);
                        npc.ShiftStat(ActorStat.Anger, 10);
                    }

                    break;
                }
                case "Performance":
                {
                    if (npc == null)
                        break;
                    int roll = Take(c, DiceSubject.Check, null, 4);
                    if (roll >= 5)
                    {
                        npc.Direct(new StageBeat { walk = false, line = "Hell Cola — вкус ада!", hold = 3f, anim = StageAnim.Cheer, urgent = true });
                        npc.ShiftStat(ActorStat.Confidence, 10);
                        Stamp(npc.transform.position, npc, MomentTags.Sponsor, "SponsorMention");
                        _fx.Icon(npc.transform.position + Vector3.up * 1.2f, "ColaCan", new Vector3(0f, 0.7f, 0f), 1.6f, 0.3f);
                    }
                    else
                    {
                        npc.Direct(new StageBeat { walk = false, line = "эээ... Хелл... как её... Кока?", hold = 3.4f, anim = StageAnim.Cower, urgent = true });
                        npc.ShiftStat(ActorStat.Confidence, -10);
                        Laugh(npc, 0.8f);
                        Stamp(npc.transform.position, npc, "Comedy", "Humiliation");
                    }

                    break;
                }
            }
        }

        // Признание: садится, говорит главное, плачет.
        public void Confess(NPCController npc, Vector2 at)
        {
            if (npc == null)
                return;
            string line = ConfessLine(npc);
            npc.Direct(new StageBeat
            {
                walk = false,
                line = line,
                hold = 4.5f,
                anim = StageAnim.Confess,
                urgent = true,
                onDone = me =>
                {
                    me.ShiftStat(ActorStat.Sadness, 25);
                    me.Cry(5f);
                }
            });
            _fx.Banner(() => npc.transform.position, "ПРИЗНАНИЕ", npc.DisplayName + " раскрывается", new Color(0.55f, 0.8f, 1f), 3f);
            npc.Announce("confession", null, "Confession", "Secret", MomentTags.Crying);
        }

        static string ConfessLine(NPCController npc)
        {
            switch (npc.Hidden)
            {
                case HiddenTrait.Kleptomaniac: return "я... иногда беру чужое";
                case HiddenTrait.Prankster: return "это я подкладывал всем гадости";
                case HiddenTrait.Singer: return "я мечтал петь, а не вот это всё";
                default: return "мне так одиноко здесь...";
            }
        }

        public static string SecretName(HiddenTrait hidden)
        {
            switch (hidden)
            {
                case HiddenTrait.Kleptomaniac: return "клептоман";
                case HiddenTrait.Prankster: return "пакостник";
                case HiddenTrait.Singer: return "тайный певец";
                default: return "тайная переписка";
            }
        }

        // Секрет раскрыт: плашка над головой, жертва в шоке, остальные злятся.
        public void Reveal(NPCController npc, NPCController witness, string how)
        {
            if (npc == null)
                return;
            npc.AddState(NPCController.StateSecretOut, 0f);
            _fx.Banner(() => npc.transform.position, "СЕКРЕТ РАСКРЫТ", npc.DisplayName + ": " + SecretName(npc.Hidden), new Color(0.85f, 0.55f, 1f), 3.6f);
            if (_shake != null)
                _shake.Punch(0.1f, 0.2f);
            Sfx.Play(Cue.Bell, 0.5f, 0.7f);
            npc.ShiftStat(ActorStat.Stress, 20);
            npc.ShiftStat(ActorStat.Anger, 10);
            npc.Direct(new StageBeat { walk = false, line = "откуда вы знаете?!", hold = 3f, anim = StageAnim.Cower, urgent = true });
            if (witness != null)
            {
                witness.ShiftBond(10, 0);
                witness.Line(npc.Hidden == HiddenTrait.Kleptomaniac ? "так это ты тырил?!" : "серьёзно?!", 3f);
                _fx.Link(witness, npc, "Bang", new Color(0.85f, 0.55f, 1f), 3f);
            }

            npc.Announce("reveal", witness, "Secret", "Reveal", "Betrayal", "Public");
        }

        void Invite(Ctx c, string kind)
        {
            var target = c.target;
            if (target == null)
                return;
            var guest = Guest.Spawn(this, kind == "Third" ? "третий" : "бывший", target, kind == "Third");
            if (guest != null)
            {
                // Чем встретит бывшего — решают кубики самой карты: влечение против злости.
                guest.Love = Take(c, DiceSubject.Stat, ActorStat.Attraction, UnityEngine.Random.Range(1, 7));
                guest.Rage = Take(c, DiceSubject.Stat, ActorStat.Anger, UnityEngine.Random.Range(1, 7));
                _guests.Add(guest);
            }
        }

        // Реакция гостя, когда дошёл: бывший — сердце или ярость по броску, третий — неловкость у пары.
        public void GuestArrived(Guest guest, NPCController target, bool third)
        {
            if (target == null)
                return;
            var partner = Partner(target);
            if (third)
            {
                guest.Say("о, я не помешаю?", 3f);
                target.ShiftStat(ActorStat.Stress, 10);
                if (partner != null)
                {
                    partner.ShiftStat(ActorStat.Stress, 10);
                    partner.Line("а это ещё кто?", 3f);
                }

                Stamp(target.transform.position, target, "Witness", "Triangle", MomentTags.Conflict);
                return;
            }

            guest.Say("привет... не ждал(а)?", 3.2f);
            int love = guest.Love;
            int rage = guest.Rage;
            _fx.FloatText(() => target.transform.position + Vector3.up * 1.9f, love >= rage ? "старые чувства" : "старые обиды", love >= rage ? new Color(1f, 0.5f, 0.75f) : new Color(1f, 0.4f, 0.3f), 24);
            if (love >= rage)
            {
                target.ShiftStat(ActorStat.Attraction, love * DiePoints);
                target.Line("ты?.. здесь?..", 3f);
                _fx.Icon(target.transform.position + Vector3.up * 1.3f, "Heart", new Vector3(0f, 0.8f, 0f), 1.4f, 0.45f);
                if (partner != null)
                {
                    partner.ShiftStat(ActorStat.Anger, 15);
                    partner.Line("это что ещё за...", 3f);
                    partner.Announce("jealous", target, "Jealousy", MomentTags.Conflict);
                }
            }
            else
            {
                target.ShiftStat(ActorStat.Anger, rage * DiePoints);
                target.Line("ВОН ОТСЮДА!", 3f);
            }

            target.Announce("reunion", null, "Surprise", "Reunion", MomentTags.Crying, MomentTags.Conflict);
            target.Escalate();
        }

        public void ForgetGuest(Guest guest)
        {
            _guests.Remove(guest);
        }

        // Остальные в комнате смеются над тем, кто опозорился.
        public void Laugh(NPCController butt, float delay)
        {
            StartCoroutine(Later(delay, () =>
            {
                foreach (var npc in Alive())
                {
                    if (npc == butt || npc == null || !npc.CanDirect(false))
                        continue;
                    if (Vector2.Distance(npc.transform.position, butt.transform.position) > 6f)
                        continue;
                    npc.Direct(new StageBeat { walk = false, line = UnityEngine.Random.value < 0.5f ? "АХАХА" : "ну ты даёшь", hold = 2.4f, anim = StageAnim.Laugh });
                }
            }));
        }

        // Аплодисменты публики.
        public void Cheer(NPCController star, float delay)
        {
            StartCoroutine(Later(delay, () =>
            {
                foreach (var npc in Alive())
                {
                    if (npc == star || npc == null || !npc.CanDirect(false))
                        continue;
                    npc.Direct(new StageBeat { walk = false, line = "браво!", hold = 2.4f, anim = StageAnim.Cheer });
                }
            }));
        }

        // ---------- Красная кнопка и прочие случайности ----------

        public void Outcome(Vector2 at, Room room, int roll)
        {
            roll = Mathf.Clamp(roll, 1, 10);
            Rect area = HouseMap.Whole(room);
            if (roll <= 3)
            {
                _fx.Banner(() => (Vector3)at + Vector3.up * 1.4f, "СПРИНКЛЕРЫ", "всех окатило водой", new Color(0.5f, 0.8f, 1f), 3f);
                _fx.Rain(area, 3.5f);
                foreach (var npc in Alive())
                {
                    if (RoomAt(npc.transform.position) != room)
                        continue;
                    npc.AddState(NPCController.StateWet, 25f);
                    npc.ShiftStat(ActorStat.Stress, 10);
                    npc.ShiftStat(ActorStat.Anger, 10);
                    npc.Direct(new StageBeat { walk = false, line = "ААА, ХОЛОДНАЯ!", hold = 2.6f, anim = StageAnim.Shiver, urgent = true });
                }

                Stamp(at, null, MomentTags.Misery, MomentTags.Chaos, "Comedy");
            }
            else if (roll <= 6)
            {
                _fx.Banner(() => (Vector3)at + Vector3.up * 1.4f, "КОНФЕТТИ!", "внезапная вечеринка", new Color(1f, 0.85f, 0.35f), 3f);
                _fx.Confetti((Vector3)at + Vector3.up * 0.6f, 60);
                Sfx.Play(Cue.Bell, 0.6f, 1.4f);
                foreach (var npc in Alive())
                {
                    npc.ShiftStat(ActorStat.Stress, -10);
                    npc.ShiftStat(ActorStat.Sadness, -10);
                    if (npc.CanDirect(false))
                        npc.Direct(new StageBeat { walk = false, line = "ЕЕЕЕ!", hold = 2.6f, anim = StageAnim.Dance });
                }

                Stamp(at, null, "Party", MomentTags.Warmth);
            }
            else if (roll <= 8)
            {
                _fx.Banner(() => (Vector3)at + Vector3.up * 1.4f, "СВЕТ ВЫРУБИЛСЯ", "в темноте все на нервах", new Color(0.7f, 0.7f, 0.8f), 3f);
                var dark = _fx.Shade(Rect.MinMaxRect(-8f, 0.2f, 8f, 8.3f), new Color(0.02f, 0.02f, 0.05f, 0.78f), 30, 4f);
                _junk.Add(dark.gameObject);
                foreach (var npc in Alive())
                {
                    npc.ShiftStat(ActorStat.Stress, 15);
                    npc.Line("кто выключил свет?!", 3f);
                }

                Stamp(at, null, MomentTags.Chaos, "Panic");
            }
            else
            {
                _fx.Banner(() => (Vector3)at + Vector3.up * 1.4f, "СИРЕНА", "вой на весь дом", new Color(1f, 0.4f, 0.3f), 3f);
                StartCoroutine(Siren(2.5f));
                foreach (var npc in Alive())
                {
                    npc.ShiftStat(ActorStat.Stress, 20);
                    if (npc.CanDirect(true))
                        npc.Direct(new StageBeat { walk = false, line = "ЧТО ЭТО?!", hold = 2f, anim = StageAnim.Cower, urgent = true });
                }

                Stamp(at, null, MomentTags.Chaos, "Panic", MomentTags.Misery);
            }

            if (_shake != null)
                _shake.Punch(0.12f, 0.22f);
            EscalateAll();
        }

        // ---------- Реквизит ----------

        StageProp Spawn(EventDefinition def, Vector2 at, Room room, bool invisible = false)
        {
            string kind = !string.IsNullOrEmpty(def.environmentId) ? def.environmentId : def.id;
            at = HouseMap.Clamp(room, at);
            var prop = StageProp.Create(this, def, kind, at, room, invisible);
            if (prop != null)
                _props.Add(prop);
            return prop;
        }

        public StageProp SpawnKind(string kind, EventDefinition def, Vector2 at, float seconds)
        {
            var room = RoomAt(at);
            if (room == Room.None)
                room = Room.Kitchen;
            var prop = StageProp.Create(this, def, kind, HouseMap.Clamp(room, at), room, false);
            if (prop != null)
            {
                prop.Lifetime(seconds);
                _props.Add(prop);
            }

            return prop;
        }

        public void Forget(StageProp prop)
        {
            _props.Remove(prop);
        }

        public void Junk(GameObject go)
        {
            if (go != null)
                _junk.Add(go);
        }

        // ---------- Старые карты без эффектов — своя постановка ----------

        void Legacy(Ctx c)
        {
            var def = c.def;
            switch (def.id)
            {
                case "provoke":
                    if (c.target != null)
                    {
                        _fx.Icon(c.target.transform.position + new Vector3(-0.8f, 1.2f, 0f), "Megaphone", new Vector3(0.9f, 0.2f, 0f), 1.1f, 0.5f);
                        c.target.ShiftStat(ActorStat.Stress, 5);
                    }

                    break;
                case "no_hot_water":
                    NoWater();
                    break;
                case "spoiled_food":
                    StartCoroutine(Stench(18f));
                    break;
                case "cut_wifi":
                    StartCoroutine(NoWifi());
                    break;
                case "meditation_bell":
                    Bell();
                    break;
                case "confession_cam":
                    ConfessionCam(c);
                    break;
                case "sponsor_cola":
                case "sponsor_energy":
                    SponsorSip(def, def.id == "sponsor_cola");
                    break;
                case "fridge_fire":
                    foreach (var npc in Alive())
                    {
                        if (Vector2.Distance(npc.transform.position, c.point) < 4f)
                            npc.ShiftStat(ActorStat.Stress, 8);
                    }

                    break;
            }
        }

        void NoWater()
        {
            if (BathOpen)
            {
                for (int i = 0; i < 6; i++)
                    _fx.Icon(new Vector3(-1.25f, 7.3f, 0f), "Drop", new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), -1.2f, 0f), 1f, 0.22f);
            }

            StartCoroutine(Clank());
            foreach (var npc in Alive())
            {
                npc.ShiftStat(ActorStat.Stress, 10);
                npc.ShiftStat(ActorStat.Sadness, 8);
                npc.AddState(NPCController.StateWet, 15f);
                npc.Direct(new StageBeat { walk = false, line = "брр, ледяная!", hold = 3.5f, anim = StageAnim.Shiver, urgent = true });
                _fx.Icon(npc.transform.position + Vector3.up * 1.3f, "Drop", new Vector3(0f, 0.5f, 0f), 1.4f, 0.3f);
            }

            var any = Alive();
            if (any.Count > 0)
                Stamp(any[0].transform.position, any[0], MomentTags.Misery, "Cold");
        }

        IEnumerator Clank()
        {
            for (int i = 0; i < 3; i++)
            {
                Sfx.Play(Cue.Tick, 0.5f, 0.4f + i * 0.1f);
                if (_shake != null)
                    _shake.Punch(0.025f, 0.06f);
                yield return new WaitForSeconds(0.22f);
            }
        }

        IEnumerator Stench(float seconds)
        {
            Vector3 source = _fridge != null ? _fridge.position : new Vector3(1.15f, 3.45f, 0f);
            var fled = new HashSet<NPCController>();
            float t = 0f;
            float puff = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                puff -= Time.deltaTime;
                if (puff <= 0f)
                {
                    puff = 0.22f;
                    // Клубы — в пределах кухни, не за стеной.
                    _fx.Stink(new Vector3(UnityEngine.Random.Range(-2f, 1.9f), UnityEngine.Random.Range(1.4f, 3.6f), 0f), 0.3f);
                }

                foreach (var npc in Alive())
                {
                    if (fled.Contains(npc) || RoomAt(npc.transform.position) != Room.Kitchen || !npc.CanDirect(true))
                        continue;
                    fled.Add(npc);
                    npc.ShiftStat(ActorStat.Anger, 10);
                    npc.ShiftStat(ActorStat.Sadness, 6);
                    npc.Direct(new StageBeat
                    {
                        goal = HouseMap.RandomPoint(Room.Living),
                        line = "ФУ! что это?!",
                        hold = 1.6f,
                        speed = 3f,
                        urgent = true,
                        anim = StageAnim.Cower,
                        onDone = me => fled.Remove(me)
                    });
                    Stamp(npc.transform.position, npc, MomentTags.Misery, "Disgust");
                }

                yield return null;
            }
        }

        IEnumerator NoWifi()
        {
            var all = Alive();
            foreach (var npc in all)
            {
                _fx.Icon(npc.transform.position + Vector3.up * 1.5f, "WifiOff", new Vector3(0f, 0.3f, 0f), 2f, 0.42f);
                npc.ShiftStat(ActorStat.Anger, 12);
                var room = RoomAt(npc.transform.position);
                npc.Direct(new StageBeat
                {
                    goal = HouseMap.RandomPoint(room == Room.None ? Room.Kitchen : room),
                    line = "ловлю сеть...",
                    hold = 3.2f,
                    speed = 1.2f,
                    anim = StageAnim.Phone,
                    urgent = true
                });
            }

            yield return new WaitForSeconds(4.2f);
            if (all.Count >= 2 && all[0] != null && all[1] != null)
            {
                all[0].Line("это ты всё качаешь!", 3f);
                all[1].Line("я?! ты сам сидишь в телефоне!", 3f);
                all[0].ShiftBond(10, 0);
                all[1].ShiftBond(10, 0);
                _fx.Link(all[0], all[1], "Bolt", new Color(1f, 0.35f, 0.25f), 3f);
                all[0].Announce("blame", all[1], MomentTags.Conflict, "Argument");
            }

            EscalateAll();
        }

        void Bell()
        {
            Vector3 center = new Vector3(0f, 2.6f, 0f);
            for (int i = 0; i < 3; i++)
                StartCoroutine(Later(i * 0.35f, () => _fx.Ring(center, 9f, new Color(1f, 0.85f, 0.4f, 0.8f), 1.6f)));
            foreach (var npc in Alive())
            {
                npc.CalmDown("оммм...");
                npc.ShiftStat(ActorStat.Stress, -15);
                npc.ShiftStat(ActorStat.Anger, -15);
                npc.Direct(new StageBeat { walk = false, hold = 4f, anim = StageAnim.Sit, line = "оммм...", thought = true });
            }

            var any = Alive();
            if (any.Count > 0)
                Stamp(any[0].transform.position, any[0], MomentTags.Warmth, "Calm");
        }

        void ConfessionCam(Ctx c)
        {
            var npc = c.target;
            if (npc == null)
                return;
            Vector2 spot = (Vector2)npc.transform.position + new Vector2(npc.transform.position.x < 0f ? 1.1f : -1.1f, 0.15f);
            var room = RoomAt(spot);
            if (room == Room.None)
                spot = npc.transform.position;
            var cam = SpawnKind("Tripod", c.def, spot, 12f);
            npc.Direct(new StageBeat
            {
                goal = (Vector2)npc.transform.position,
                walk = false,
                line = "можно я скажу на камеру?",
                hold = 1.8f,
                urgent = true,
                onDone = me => Confess(me, spot)
            });
        }

        void SponsorSip(EventDefinition def, bool cola)
        {
            var all = Alive();
            if (all.Count == 0)
                return;
            var npc = all[UnityEngine.Random.Range(0, all.Count)];
            Vector2 spot = (Vector2)npc.transform.position + new Vector2(0.8f, 0.1f);
            var can = SpawnKind("ColaCan", def, HouseMap.Clamp(RoomAt(npc.transform.position) == Room.None ? Room.Kitchen : RoomAt(npc.transform.position), spot), 10f);
            if (can != null)
                can.Sponsor = true;
            npc.Direct(new StageBeat
            {
                goal = spot - new Vector2(0.4f, 0f),
                line = cola ? "Hell Cola — освежает!" : "энергетик! погнали!",
                hold = 3f,
                anim = cola ? StageAnim.Drink : StageAnim.Cheer,
                onArrive = me =>
                {
                    if (cola)
                        me.ShiftStat(ActorStat.Stress, -10);
                    else
                        me.ShiftStat(ActorStat.Confidence, 15);
                    Stamp(me.transform.position, me, MomentTags.Sponsor);
                    if (can != null)
                        can.Lifetime(1.5f);
                }
            });
        }
    }
}
