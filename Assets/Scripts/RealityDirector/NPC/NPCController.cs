using System;
using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Util;
using RealityDirector.Events;
using UnityEngine;

namespace RealityDirector.NPC
{
    public class NPCController : MonoBehaviour
    {
        public string Id;
        public string DisplayName;
        public string AccusativeName;
        public TraitDefinition Trait;
        public HiddenTrait Hidden;
        public ReactionRuleSet Rules;
        public NPCController Rival;
        public Vector2 Home;
        public Vector2 FleePoint;
        public bool BlockEast;
        public float EastLimit = 3.05f;
        public float PanicSpeed = 2.2f;
        public float ChaseSpeed = 3.15f;
        public float RelationToRival;

        public bool HasRage => Time.time < _rageUntil;
        public bool IsFighting => _action == NpcActionId.Fight;
        public bool IsCrying => _action == NpcActionId.Panic && Trait != null && Trait.traitId == TraitId.Sentimental;
        public bool IsApproaching => _hasPending && _noticed;
        public bool CanChat => !IsFighting && !_hasPending && _action != NpcActionId.Panic && _action != NpcActionId.SeekFight;
        public NpcActionId Action => _action;
        public string Emote => _emote;
        public bool Thought => _thought;

        public static event Action<NPCController, NPCController> FightStarted;

        enum PoseKind
        {
            None,
            Sit,
            Pushup
        }

        NpcActionId _action = NpcActionId.Idle;
        float _rageUntil;
        float _emoteUntil;
        string _emote = "";
        bool _thought;
        bool _fightAnnounced;
        Transform _visual;
        SpriteRenderer _ring;
        float _nextThink;
        Vector2 _goal;
        PoseKind _pose;
        bool _hasPending;
        bool _noticed;
        float _noticeAt;
        float _reactHoldUntil;
        Vector2 _locus;
        ReactionRule _queued;

        public void BindVisual(Transform visual, SpriteRenderer ring)
        {
            _visual = visual;
            _ring = ring;
            if (_ring != null)
                _ring.enabled = false;
        }

        void OnEnable()
        {
            EventBus.Published += OnWorld;
        }

        void OnDisable()
        {
            EventBus.Published -= OnWorld;
        }

        public void ResetState()
        {
            _action = NpcActionId.Idle;
            _rageUntil = 0f;
            _emoteUntil = 0f;
            _emote = "";
            _thought = false;
            _fightAnnounced = false;
            _hasPending = false;
            _noticed = false;
            _queued = null;
            _pose = PoseKind.None;
            _reactHoldUntil = 0f;
            _nextThink = Time.time + UnityEngine.Random.Range(0.4f, 1.4f);
            RelationToRival = 0f;
            transform.position = Home;
            RestoreVisual();
            if (_visual != null)
                _visual.localPosition = Vector3.zero;
            if (_ring != null)
                _ring.enabled = false;
        }

        public void ApplyRage(float seconds)
        {
            _rageUntil = Mathf.Max(_rageUntil, Time.time + seconds);
            Say("злость", 90f, true);
            Sfx.Play(Cue.Blip, 0.4f, 0.62f);
            FadeBit.Burst(transform.position + Vector3.up * 0.85f, 6, new Color(1f, 0.28f, 0.12f, 1f));
            if (Interactable.AnyOnFire && !IsFighting && _action != NpcActionId.SeekFight)
            {
                _hasPending = false;
                _noticed = false;
                _queued = null;
                Run(NpcActionId.SeekFight, "!!!");
            }
        }

        public void SetTargeted(bool on)
        {
            if (_ring == null || IsFighting)
                return;
            _ring.enabled = on;
            if (on)
                _ring.color = new Color(1f, 0.86f, 0.25f, 0.95f);
        }

        public void ForceFight(NPCController other)
        {
            _action = NpcActionId.Fight;
            Rival = other;
            Say("ДРАКА", 99f, false);
            if (_ring != null)
            {
                _ring.enabled = true;
                _ring.color = new Color(1f, 0.22f, 0.16f, 1f);
            }
        }

        public void MarkFightAnnounced()
        {
            _fightAnnounced = true;
        }

        void OnWorld(WorldEvent worldEvent)
        {
            if (Rules == null || Trait == null || IsFighting)
                return;

            ReactionRule best = null;
            var rules = Rules.rules;
            for (int i = 0; i < rules.Count; i++)
            {
                if (!Matches(rules[i], worldEvent))
                    continue;
                if (best == null || rules[i].priority > best.priority)
                    best = rules[i];
            }

            if (best == null)
                return;

            if (worldEvent.hasLocus && !best.requireTargetSelf)
            {
                QueueApproach(best, worldEvent.locus);
                return;
            }

            if ((_action == NpcActionId.Panic || _action == NpcActionId.SeekFight) && best.action == NpcActionId.Emote)
                return;

            Run(best.action, best.emote);
        }

        void QueueApproach(ReactionRule rule, Vector2 locus)
        {
            if (_hasPending && _queued != null && _queued.priority > rule.priority)
                return;
            float dist = Vector2.Distance(transform.position, locus);
            _queued = rule;
            _locus = locus;
            _hasPending = true;
            if (_noticed)
                return;
            _noticeAt = Time.time + Mathf.Min(2.6f, 0.16f + dist * 0.32f);
        }

        void BeginApproach()
        {
            _noticed = true;
            _pose = PoseKind.None;
            ClearBeatFlags();
            RestoreVisual();
            float side = Home.x < -5f ? -0.72f : 0.72f;
            _goal = _locus + new Vector2(side, -0.2f);
            if (BlockEast && _goal.x > EastLimit - 0.2f)
                _goal.x = EastLimit - 0.2f;
            _action = NpcActionId.Roam;
            Say("!", 2.6f, false);
        }

        void CommitPending()
        {
            ReactionRule rule = _queued;
            _hasPending = false;
            _noticed = false;
            _queued = null;
            if (rule == null)
            {
                _action = NpcActionId.Idle;
                return;
            }

            Run(rule.action, rule.emote);
            if (rule.action == NpcActionId.Panic)
                _reactHoldUntil = Time.time + 0.95f;
            else if (rule.action == NpcActionId.SeekFight)
                _reactHoldUntil = Time.time + 0.4f;
            else
                _reactHoldUntil = 0f;
        }

        bool Matches(ReactionRule rule, WorldEvent worldEvent)
        {
            if (rule.requiredTrait != Trait.traitId)
                return false;
            if (worldEvent.tags == null || !worldEvent.tags.Contains(rule.eventTag))
                return false;
            if (rule.requireTargetSelf && worldEvent.targetActorId != Id)
                return false;
            if (rule.requireRage && !HasRage)
                return false;
            return true;
        }

        void Run(NpcActionId action, string emote)
        {
            switch (action)
            {
                case NpcActionId.Panic:
                    bool fresh = _action != NpcActionId.Panic;
                    _action = NpcActionId.Panic;
                    Say(string.IsNullOrEmpty(emote) ? "!" : emote, 99f, ThoughtLine(emote));
                    if (fresh && IsCrying)
                    {
                        Sfx.Play(Cue.Cry, 0.65f);
                        FadeBit.Burst(transform.position + Vector3.up * 0.9f, 7, new Color(0.45f, 0.75f, 1f, 1f));
                    }

                    break;
                case NpcActionId.SeekFight:
                    if (Rival == null)
                        return;
                    _action = NpcActionId.SeekFight;
                    Say(string.IsNullOrEmpty(emote) ? "!!!" : emote, 99f, false);
                    break;
                case NpcActionId.Emote:
                    _action = NpcActionId.Emote;
                    float hold = ReadFor(emote);
                    Say(emote, hold, ThoughtLine(emote));
                    break;
                default:
                    _action = NpcActionId.Idle;
                    break;
            }
        }

        void Say(string text, float seconds, bool thought)
        {
            _emote = text ?? "";
            _thought = thought && _emote.Length > 0;
            _emoteUntil = Time.time + seconds;
        }

        static bool ThoughtLine(string emote)
        {
            return emote == "слёзы" || emote == "уют" || emote == "злость" || emote == "сидит" || emote == "пранк" || emote == "тырит";
        }

        static float ReadFor(string text)
        {
            int n = string.IsNullOrEmpty(text) ? 1 : text.Length;
            return Mathf.Clamp(2.8f + n * 0.32f, 4.2f, 7.2f);
        }

        void Update()
        {
            if (_emote.Length > 0 && Time.time > _emoteUntil && !IsFighting && !HasRage && _action != NpcActionId.Emote)
            {
                _emote = "";
                _thought = false;
            }

            if (IsFighting)
            {
                FightShake();
                PulseRing();
                return;
            }

            if (_hasPending)
                TickPending();
            else if (_action == NpcActionId.Panic || _action == NpcActionId.SeekFight)
            {
                if (Time.time < _reactHoldUntil)
                    Pose();
                else
                    StepAction();
            }
            else
                TickPassive();

            PulseRing();
        }

        void TickPending()
        {
            if (!_noticed)
            {
                if (Time.time < _noticeAt)
                {
                    TickPassive();
                    return;
                }

                BeginApproach();
            }

            if (_action == NpcActionId.Roam)
            {
                MoveTowards(_goal, 1.75f);
                if (Vector2.Distance(transform.position, _goal) < 0.82f)
                    CommitPending();
            }
        }

        void TickPassive()
        {
            if (_action == NpcActionId.Roam)
            {
                MoveTowards(_goal, 1.12f);
                if (Vector2.Distance(transform.position, _goal) < 0.28f)
                    StartBeat();
                return;
            }

            if (_action == NpcActionId.Emote)
            {
                Pose();
                if (Time.time >= _emoteUntil)
                {
                    _action = NpcActionId.Idle;
                    _pose = PoseKind.None;
                    RestoreVisual();
                    _nextThink = Time.time + UnityEngine.Random.Range(0.45f, 1.1f);
                }

                return;
            }

            if (_action != NpcActionId.Idle)
                return;

            Bob();
            if (HasRage && _emote.Length == 0)
                Say("злость", Mathf.Max(0.2f, _rageUntil - Time.time), true);
            if (Time.time >= _nextThink)
                PickBeat();
        }

        void PickBeat()
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.3f)
                Wander();
            else if (roll < 0.52f)
                TryTalk();
            else if (roll < 0.72f)
                Sit();
            else
                Signature();
        }

        void Wander()
        {
            ClearBeatFlags();
            _goal = Hangout.Pick(SpotKind.Floor, Home);
            _pose = PoseKind.None;
            _action = NpcActionId.Roam;
        }

        void TryTalk()
        {
            if (Rival == null || !Rival.CanChat)
            {
                Wander();
                return;
            }

            ClearBeatFlags();
            Vector2 gap = (Vector2)transform.position - (Vector2)Rival.transform.position;
            if (gap.sqrMagnitude < 0.04f)
                gap = Vector2.right;
            _goal = (Vector2)Rival.transform.position + gap.normalized * 0.95f;
            _pose = PoseKind.None;
            _beatTalk = true;
            _action = NpcActionId.Roam;
        }

        bool _beatTalk;

        void Sit()
        {
            ClearBeatFlags();
            _goal = Hangout.Pick(SpotKind.Seat, Home + new Vector2(0f, -0.4f));
            _pose = PoseKind.Sit;
            _action = NpcActionId.Roam;
        }

        void Signature()
        {
            ClearBeatFlags();
            if (Hidden == HiddenTrait.Prankster)
            {
                if (UnityEngine.Random.value < 0.45f)
                {
                    _goal = Hangout.Pick(SpotKind.Floor, Home);
                    _pose = PoseKind.Pushup;
                }
                else
                {
                    _goal = Hangout.Pick(SpotKind.Prop, Home);
                    _prank = true;
                }

                _action = NpcActionId.Roam;
                return;
            }

            if (Hidden == HiddenTrait.Kleptomaniac && UnityEngine.Random.value < 0.6f)
            {
                _goal = Hangout.Pick(SpotKind.Prop, Home);
                _rummage = true;
                _action = NpcActionId.Roam;
                return;
            }

            _sing = true;
            _pose = PoseKind.None;
            _goal = transform.position;
            _action = NpcActionId.Roam;
        }

        void ClearBeatFlags()
        {
            _beatTalk = false;
            _prank = false;
            _rummage = false;
            _sing = false;
        }

        bool _rummage;
        bool _prank;
        bool _sing;

        void StartBeat()
        {
            _action = NpcActionId.Emote;
            if (_beatTalk)
            {
                _beatTalk = false;
                string mine = UnityEngine.Random.value < 0.5f ? "ну чё" : "да ладно";
                string theirs = UnityEngine.Random.value < 0.5f ? "серьёзно?" : "ахах";
                float hold = Mathf.Max(ReadFor(mine), ReadFor(theirs));
                Say(mine, hold, false);
                if (Rival != null && Rival.CanChat)
                    Rival.Say(theirs, hold, false);
                return;
            }

            if (_pose == PoseKind.Sit)
            {
                Say("сидит", ReadFor("сидит"), true);
                return;
            }

            if (_pose == PoseKind.Pushup)
            {
                Say("раз-два", ReadFor("раз-два"), false);
                return;
            }

            if (_prank)
            {
                _prank = false;
                Say("пранк", ReadFor("пранк"), true);
                return;
            }

            if (_rummage)
            {
                _rummage = false;
                Say("тырит", ReadFor("тырит"), true);
                return;
            }

            if (_sing)
            {
                _sing = false;
                Say("ла-ла", ReadFor("ла-ла"), false);
                return;
            }

            _action = NpcActionId.Idle;
            _nextThink = Time.time + UnityEngine.Random.Range(0.55f, 1.3f);
        }

        void Pose()
        {
            if (_visual == null)
                return;
            if (_pose == PoseKind.Pushup)
            {
                var scale = _visual.localScale;
                scale.y = 0.7f + Mathf.PingPong(Time.time * 4.2f, 0.3f);
                _visual.localScale = scale;
                return;
            }

            if (_pose == PoseKind.Sit)
            {
                _visual.localPosition = new Vector3(0f, -0.2f, 0f);
                return;
            }

            Bob();
        }

        void RestoreVisual()
        {
            if (_visual == null)
                return;
            var scale = _visual.localScale;
            scale.y = 1f;
            _visual.localScale = scale;
            _visual.localPosition = Vector3.zero;
        }

        void StepAction()
        {
            switch (_action)
            {
                case NpcActionId.Panic:
                    MoveTowards(FleePoint, PanicSpeed);
                    break;
                case NpcActionId.SeekFight:
                    Chase();
                    break;
            }
        }

        void PulseRing()
        {
            if (_ring == null || !_ring.enabled || IsFighting)
                return;
            var color = _ring.color;
            color.a = 0.45f + Mathf.Sin(Time.time * 6f) * 0.35f;
            _ring.color = color;
        }

        void Chase()
        {
            if (Rival == null)
            {
                _action = NpcActionId.Idle;
                return;
            }

            if (Rival.IsFighting)
            {
                BeginFight(Rival);
                return;
            }

            MoveTowards(Rival.transform.position, ChaseSpeed);
            if (Vector2.Distance(transform.position, Rival.transform.position) < 0.92f)
                BeginFight(Rival);
        }

        void BeginFight(NPCController other)
        {
            if (_fightAnnounced || other == null)
                return;

            _fightAnnounced = true;
            Vector3 mid = (transform.position + other.transform.position) * 0.5f;
            transform.position = mid + Vector3.left * 0.48f;
            other.transform.position = mid + Vector3.right * 0.48f;
            RelationToRival -= 20f;
            other.RelationToRival -= 20f;
            ForceFight(other);
            other.ForceFight(this);
            other.MarkFightAnnounced();

            EventBus.Publish(new WorldEvent
            {
                eventId = "fight",
                tags = new List<string> { MomentTags.Fight, MomentTags.Slap, MomentTags.Conflict },
                sourceActorId = Id,
                targetActorId = other.Id,
                time = Time.time
            });
            FightStarted?.Invoke(this, other);
        }

        const float BodyGap = 0.76f;

        void MoveTowards(Vector2 target, float speed)
        {
            Vector2 current = transform.position;
            Vector2 next = Vector2.MoveTowards(current, target, speed * Time.deltaTime);
            next = ClampEast(current, next);
            next = Separate(current, next);
            next = ClampEast(current, next);
            Face(next.x - current.x);
            transform.position = next;
            if (_visual != null && _action != NpcActionId.Fight)
                _visual.localPosition = Vector3.zero;
        }

        Vector2 ClampEast(Vector2 current, Vector2 next)
        {
            if (BlockEast && next.x > EastLimit)
                next.x = Mathf.Min(current.x, EastLimit);
            return next;
        }

        Vector2 Separate(Vector2 current, Vector2 next)
        {
            if (Rival == null || IsFighting || Rival.IsFighting)
                return next;
            Vector2 other = Rival.transform.position;
            Vector2 gap = next - other;
            float dist = gap.magnitude;
            if (dist >= BodyGap)
                return next;
            Vector2 away = dist < 0.0001f ? Vector2.right : gap / dist;
            Vector2 step = next - current;
            float into = Vector2.Dot(step, -away);
            Vector2 resolved = into > 0f ? current + (step + away * into) : next;
            Vector2 left = resolved - other;
            if (left.magnitude < BodyGap)
                resolved = other + (left.sqrMagnitude < 0.0001f ? Vector2.right : left.normalized) * BodyGap;
            return resolved;
        }

        void Face(float dx)
        {
            if (_visual == null || Mathf.Abs(dx) < 0.001f)
                return;
            var scale = _visual.localScale;
            float mag = Mathf.Abs(scale.x) < 0.001f ? 1f : Mathf.Abs(scale.x);
            scale.x = mag * (dx >= 0f ? 1f : -1f);
            _visual.localScale = scale;
        }

        void Bob()
        {
            if (_visual == null)
                return;
            float y = Mathf.Sin(Time.time * 2.4f + Home.x) * 0.035f;
            _visual.localPosition = new Vector3(0f, y, 0f);
        }

        void FightShake()
        {
            if (_visual == null)
                return;
            float x = Mathf.Sin(Time.time * 32f + Home.x * 3f) * 0.07f;
            _visual.localPosition = new Vector3(x, 0f, 0f);
        }
    }

    public enum SpotKind
    {
        Floor,
        Seat,
        Prop
    }

    public static class Hangout
    {
        static readonly List<Vector2> Floors = new List<Vector2>();
        static readonly List<Vector2> Seats = new List<Vector2>();
        static readonly List<Vector2> Props = new List<Vector2>();

        public static void Clear()
        {
            Floors.Clear();
            Seats.Clear();
            Props.Clear();
        }

        public static void Add(Vector2 point, SpotKind kind)
        {
            List(kind).Add(point);
        }

        public static Vector2 Pick(SpotKind kind, Vector2 fallback)
        {
            var list = List(kind);
            if (list.Count == 0)
                return fallback;
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        static List<Vector2> List(SpotKind kind)
        {
            if (kind == SpotKind.Seat)
                return Seats;
            if (kind == SpotKind.Prop)
                return Props;
            return Floors;
        }
    }
}
