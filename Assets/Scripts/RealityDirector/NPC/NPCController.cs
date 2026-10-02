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
        public ReactionRuleSet Rules;
        public NPCController Rival;
        public Vector2 Home;
        public Vector2 FleePoint;
        public float PanicSpeed = 2.2f;
        public float ChaseSpeed = 3.15f;
        public float RelationToRival;

        public bool HasRage => Time.time < _rageUntil;
        public bool IsFighting => _action == NpcActionId.Fight;
        public bool IsCrying => _action == NpcActionId.Panic && Trait != null && Trait.traitId == TraitId.Sentimental;
        public NpcActionId Action => _action;
        public string Emote => _emote;

        public static event Action<NPCController, NPCController> FightStarted;

        NpcActionId _action = NpcActionId.Idle;
        float _rageUntil;
        float _emoteUntil;
        string _emote = "";
        bool _fightAnnounced;
        Transform _visual;
        SpriteRenderer _ring;

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
            _fightAnnounced = false;
            RelationToRival = 0f;
            transform.position = Home;
            if (_visual != null)
                _visual.localPosition = Vector3.zero;
            if (_ring != null)
                _ring.enabled = false;
        }

        public void ApplyRage(float seconds)
        {
            _rageUntil = Mathf.Max(_rageUntil, Time.time + seconds);
            Say("злость", 90f);
            Sfx.Play(Cue.Blip, 0.4f, 0.62f);
            FadeBit.Burst(transform.position + Vector3.up * 0.85f, 6, new Color(1f, 0.28f, 0.12f, 1f));
            if (Interactable.AnyOnFire && !IsFighting && _action != NpcActionId.SeekFight)
                Run(NpcActionId.SeekFight, "!!!");
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
            Say("ДРАКА", 99f);
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

            if ((_action == NpcActionId.Panic || _action == NpcActionId.SeekFight) && best.action == NpcActionId.Emote)
                return;

            Run(best.action, best.emote);
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
                    Say(string.IsNullOrEmpty(emote) ? "!" : emote, 99f);
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
                    Say(string.IsNullOrEmpty(emote) ? "!!!" : emote, 99f);
                    break;
                case NpcActionId.Emote:
                    _action = NpcActionId.Emote;
                    _emoteUntil = Time.time + 2.4f;
                    Say(emote, 2.4f);
                    break;
                default:
                    _action = NpcActionId.Idle;
                    break;
            }
        }

        void Say(string text, float seconds)
        {
            _emote = text ?? "";
            _emoteUntil = Time.time + seconds;
        }

        void Update()
        {
            if (_emote.Length > 0 && Time.time > _emoteUntil && !IsFighting && !HasRage)
                _emote = "";

            switch (_action)
            {
                case NpcActionId.Idle:
                    Bob();
                    if (HasRage && _emote.Length == 0)
                        _emote = "злость";
                    break;
                case NpcActionId.Panic:
                    MoveTowards(FleePoint, PanicSpeed);
                    break;
                case NpcActionId.SeekFight:
                    Chase();
                    break;
                case NpcActionId.Fight:
                    FightShake();
                    break;
                case NpcActionId.Emote:
                    Bob();
                    if (Time.time >= _emoteUntil)
                        _action = NpcActionId.Idle;
                    break;
            }

            if (_ring != null && _ring.enabled && !IsFighting)
            {
                var color = _ring.color;
                color.a = 0.45f + Mathf.Sin(Time.time * 6f) * 0.35f;
                _ring.color = color;
            }
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

        void MoveTowards(Vector2 target, float speed)
        {
            Vector2 current = transform.position;
            Vector2 next = Vector2.MoveTowards(current, target, speed * Time.deltaTime);
            Face(next.x - current.x);
            transform.position = next;
            if (_visual != null && _action != NpcActionId.Fight)
                _visual.localPosition = Vector3.zero;
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
}
