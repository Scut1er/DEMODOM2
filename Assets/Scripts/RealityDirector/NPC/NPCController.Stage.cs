using System;
using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Events;
using RealityDirector.Util;
using UnityEngine;

namespace RealityDirector.NPC
{
    // Что участник показывает телом, пока его ведёт карта.
    public enum StageAnim
    {
        None,
        Drink,
        Sing,
        Slip,
        Shiver,
        Sit,
        Bang,
        Phone,
        Cheer,
        Laugh,
        Cower,
        Confess,
        Grab,
        Dance
    }

    // Шаг, который карта даёт участнику: дойти, сделать, закончить. Реакции мира (огонь, драка) важнее —
    // паника или погоня отменяют шаг.
    public class StageBeat
    {
        public Vector2 goal;
        public bool walk = true;
        public float speed = 1.7f;
        public string line;
        public bool thought;
        public float hold = 3.5f;
        public StageAnim anim;
        public float giveUp = 7f;
        // Срочно — даже из паники (сирена, запертая дверь). Драку ничто не прерывает.
        public bool urgent;
        public Action<NPCController> onArrive;
        public Action<NPCController> onDone;
    }

    // Карты и реквизит: ведомые действия, временные состояния, запертая комната, срывы по эмоциям.
    public partial class NPCController
    {
        public const string StateDrunk = "Drunk";
        public const string StateSuspicious = "Suspicious";
        public const string StateAmplified = "NextNegativeEventAmplified";
        public const string StateWet = "Wet";
        public const string StateTrapped = "Trapped";
        public const string StateSpotlit = "Spotlit";
        public const string StateSecretOut = "SecretOut";

        // Карта или реквизит сдвинули эмоцию — сцена показывает число над головой.
        // subtle — фон (аура реквизита): число мельче.
        public static event Action<NPCController, ActorStat, int, bool> StatShifted;
        // Состояние повесили / сняли — над головой меняется значок.
        public static event Action<NPCController, string, bool> StateChanged;
        // Срыв от эмоций: «злость» → драка, «стресс» → паника, «грусть» → слёзы, «влечение» → флирт.
        public static event Action<NPCController, string> Snapped;

        StageBeat _beat;
        bool _beatArrived;
        float _beatUntil;
        float _beatGiveUp;
        StageAnim _anim;
        float _animStart;
        readonly Dictionary<string, float> _states = new Dictionary<string, float>();
        readonly List<string> _expired = new List<string>();
        Rect _lockRoom;
        float _lockUntil;
        float _cryUntil;
        float _snapReady;
        float _nextSuspect;
        bool _amplifying;
        float _tilt;

        public bool StageBusy => _beat != null;
        public bool StageCrying => Time.time < _cryUntil;
        public bool IsLocked => Time.time < _lockUntil;
        public Rect LockedRoom => _lockRoom;
        public StageAnim CurrentAnim => _beat != null && _beatArrived ? _anim : StageAnim.None;
        public bool StageArrived => _beat != null && _beatArrived;

        public IEnumerable<string> States => _states.Keys;

        void ResetStage()
        {
            _beat = null;
            _beatArrived = false;
            _anim = StageAnim.None;
            _states.Clear();
            _lockUntil = 0f;
            _cryUntil = 0f;
            _snapReady = 0f;
            _tilt = 0f;
            if (_visual != null)
                _visual.localRotation = Quaternion.identity;
        }

        // ---------- Ведомые действия ----------

        public bool CanDirect(bool urgent)
        {
            if (IsFighting)
                return false;
            if (urgent)
                return true;
            return _action != NpcActionId.Panic && _action != NpcActionId.SeekFight && !_hasPending;
        }

        public bool Direct(StageBeat beat)
        {
            if (beat == null || !CanDirect(beat.urgent))
                return false;
            _hasPending = false;
            _noticed = false;
            _queued = null;
            _trapArmed = false;
            ClearBeatFlags();
            _seekingComfort = false;
            _beatComfort = false;
            _hugging = false;
            _stealing = false;
            _planting = false;
            _tripping = false;
            _pose = PoseKind.None;
            RestoreVisual();
            _beat = beat;
            _beatArrived = false;
            _beatGiveUp = Time.time + Mathf.Max(0.5f, beat.giveUp);
            _action = NpcActionId.Roam;
            if (!beat.walk)
                ArriveBeat();
            return true;
        }

        public void CancelBeat()
        {
            if (_beat == null)
                return;
            _beat = null;
            _anim = StageAnim.None;
            RestoreVisual();
            if (_action == NpcActionId.Roam || _action == NpcActionId.Emote)
                _action = NpcActionId.Idle;
        }

        // Драка не вечная: через несколько секунд разнимаются, злость спадает, вражда остаётся —
        // и следующая карта снова может их завести.
        public const float FightSeconds = 9f;
        float _fightSince = -1f;

        void TickFight()
        {
            if (!IsFighting)
            {
                _fightSince = -1f;
                return;
            }

            if (_fightSince < 0f)
                _fightSince = Time.time;
            if (Time.time - _fightSince >= FightSeconds)
                BreakFight();
        }

        public void BreakFight()
        {
            if (!IsFighting)
                return;
            var other = Rival;
            float side = other != null && other.transform.position.x > transform.position.x ? -1f : 1f;
            EndFight(side, "ещё встретимся!");
            if (other != null && other.IsFighting && other.Rival == this)
                other.EndFight(-side, "отвали от меня!");
        }

        void EndFight(float side, string line)
        {
            _action = NpcActionId.Idle;
            _rageUntil = 0f;
            _fightAnnounced = false;
            _fightSince = -1f;
            Anger = Mathf.Clamp(Anger - 25, 0, 100);
            Stress = Mathf.Clamp(Stress + 10, 0, 100);
            SelfControl = Mathf.Clamp(SelfControl + 10, 0, 100);
            _snapReady = Time.time + 6f;
            if (_ring != null)
                _ring.enabled = false;
            RestoreVisual();
            Vector2 goal = (Vector2)transform.position + new Vector2(side * 1.6f, 0f);
            goal.x = Mathf.Clamp(goal.x, -7.1f, 7.3f);
            Direct(new StageBeat { goal = goal, line = line, hold = 2.4f, speed = 2.6f, urgent = true, giveUp = 2f });
        }

        bool TickStage()
        {
            TickFight();
            TickStates();
            if (_beat == null)
                return false;
            // Огонь, драка, вопль соседа — сильнее карты.
            if (IsFighting || _hasPending || _action == NpcActionId.Panic || _action == NpcActionId.SeekFight)
            {
                _beat = null;
                _anim = StageAnim.None;
                return false;
            }

            if (!_beatArrived)
            {
                float speed = _beat.speed * (HasState(StateDrunk) ? 0.7f : 1f);
                MoveTowards(_beat.goal, speed);
                Stride(speed);
                if (Vector2.Distance(transform.position, _beat.goal) < 0.3f || Time.time >= _beatGiveUp)
                    ArriveBeat();
                return true;
            }

            Animate();
            if (Time.time < _beatUntil)
                return true;

            var done = _beat.onDone;
            _beat = null;
            _anim = StageAnim.None;
            RestoreVisual();
            _action = NpcActionId.Idle;
            _nextThink = Time.time + UnityEngine.Random.Range(0.5f, 1.2f);
            done?.Invoke(this);
            return true;
        }

        void ArriveBeat()
        {
            _beatArrived = true;
            _anim = _beat.anim;
            _animStart = Time.time;
            _beatUntil = Time.time + Mathf.Max(0.3f, _beat.hold);
            _action = NpcActionId.Emote;
            _emoteUntil = _beatUntil;
            if (!string.IsNullOrEmpty(_beat.line))
                Say(_beat.line, _beat.hold, _beat.thought);
            if (_beat.anim == StageAnim.Slip)
                Sfx.Play(Cue.Slap, 0.5f, 0.7f);
            var arrive = _beat.onArrive;
            arrive?.Invoke(this);
        }

        // Тело во время действия. Наклон — отдельный канал: его не трогает остальной код.
        void Animate()
        {
            if (_visual == null)
                return;
            float t = Time.time - _animStart;
            float left = _beatUntil - Time.time;
            switch (_anim)
            {
                case StageAnim.Drink:
                    _tilt = Mathf.Lerp(0f, -22f, Mathf.Clamp01(t * 3f)) * (left < 0.4f ? left / 0.4f : 1f);
                    _visual.localPosition = new Vector3(0f, Mathf.Sin(t * 5f) * 0.02f, 0f);
                    break;
                case StageAnim.Sing:
                case StageAnim.Dance:
                    _visual.localPosition = new Vector3(Mathf.Sin(t * 4f) * 0.05f, Mathf.Abs(Mathf.Sin(t * 7f)) * 0.1f, 0f);
                    _tilt = Mathf.Sin(t * 3.5f) * 8f;
                    break;
                case StageAnim.Slip:
                    float fall = Mathf.Clamp01(t * 6f);
                    float rise = left < 0.45f ? Mathf.Clamp01(left / 0.45f) : 1f;
                    _tilt = -84f * fall * rise;
                    _visual.localPosition = new Vector3(0.18f * fall * rise, -0.35f * fall * rise, 0f);
                    break;
                case StageAnim.Shiver:
                    _visual.localPosition = new Vector3(Mathf.Sin(t * 46f) * 0.035f, 0f, 0f);
                    break;
                case StageAnim.Sit:
                case StageAnim.Confess:
                    _visual.localPosition = new Vector3(0f, -0.2f, 0f);
                    _tilt = _anim == StageAnim.Confess ? Mathf.Sin(t * 1.6f) * 5f : 0f;
                    break;
                case StageAnim.Bang:
                    _visual.localPosition = new Vector3(Mathf.Max(0f, Mathf.Sin(t * 9f)) * 0.12f * Facing(), 0f, 0f);
                    if (Mathf.Repeat(t, 0.7f) < Time.deltaTime)
                        Sfx.Play(Cue.Tick, 0.4f, 0.55f);
                    break;
                case StageAnim.Phone:
                    _visual.localPosition = new Vector3(0f, Mathf.Sin(t * 2f) * 0.015f, 0f);
                    _tilt = 6f;
                    break;
                case StageAnim.Cheer:
                    _visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(t * 9f)) * 0.16f, 0f);
                    break;
                case StageAnim.Laugh:
                    _visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(t * 16f)) * 0.05f, 0f);
                    _tilt = Mathf.Sin(t * 8f) * 4f;
                    break;
                case StageAnim.Cower:
                    var scale = _visual.localScale;
                    scale.y = 0.86f;
                    _visual.localScale = scale;
                    _visual.localPosition = new Vector3(Mathf.Sin(t * 30f) * 0.02f, -0.06f, 0f);
                    break;
                case StageAnim.Grab:
                    _visual.localPosition = new Vector3(0f, -Mathf.Abs(Mathf.Sin(t * 5f)) * 0.08f, 0f);
                    break;
                default:
                    Bob();
                    break;
            }
        }

        float Facing()
        {
            return _visual != null && _visual.localScale.x < 0f ? -1f : 1f;
        }

        // Походка: пьяный шатается, остальные чуть подпрыгивают.
        void Stride(float speed)
        {
            if (_visual == null)
                return;
            _visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * speed * 6f)) * 0.04f, 0f);
        }

        void LateUpdate()
        {
            if (_visual == null)
                return;
            float sway = HasState(StateDrunk) && _anim != StageAnim.Slip ? Mathf.Sin(Time.time * 2.6f + Home.x) * 9f : 0f;
            float target = _anim != StageAnim.None ? _tilt + sway : sway;
            _visual.localRotation = Quaternion.Euler(0f, 0f, target);
            if (_anim == StageAnim.None)
                _tilt = 0f;
        }

        // ---------- Состояния ----------

        public void AddState(string key, float seconds)
        {
            if (string.IsNullOrEmpty(key))
                return;
            bool fresh = !_states.ContainsKey(key);
            _states[key] = seconds <= 0f ? float.MaxValue : Time.time + seconds;
            if (fresh)
                StateChanged?.Invoke(this, key, true);
        }

        public bool HasState(string key)
        {
            return key != null && _states.TryGetValue(key, out float until) && until > Time.time;
        }

        public void RemoveState(string key)
        {
            if (key != null && _states.Remove(key))
                StateChanged?.Invoke(this, key, false);
        }

        void TickStates()
        {
            if (_states.Count > 0)
            {
                _expired.Clear();
                foreach (var pair in _states)
                {
                    if (pair.Value <= Time.time)
                        _expired.Add(pair.Key);
                }

                for (int i = 0; i < _expired.Count; i++)
                    RemoveState(_expired[i]);
            }

            if (_lockUntil > 0f && Time.time >= _lockUntil)
            {
                _lockUntil = 0f;
                RemoveState(StateTrapped);
            }

            // Подозрительный ходит за соперником и косится.
            if (HasState(StateSuspicious) && _beat == null && _action == NpcActionId.Idle && Rival != null && Time.time >= _nextSuspect)
            {
                _nextSuspect = Time.time + UnityEngine.Random.Range(6f, 9f);
                Vector2 gap = (Vector2)transform.position - (Vector2)Rival.transform.position;
                if (gap.sqrMagnitude < 0.04f)
                    gap = Vector2.right;
                Direct(new StageBeat
                {
                    goal = (Vector2)Rival.transform.position + gap.normalized * 1.3f,
                    line = UnityEngine.Random.value < 0.5f ? "я слежу за тобой" : "что-то ты темнишь",
                    thought = true,
                    hold = 2.6f,
                    speed = 1.3f
                });
            }
        }

        // ---------- Запертая комната ----------

        public void LockIn(Rect room, float seconds)
        {
            _lockRoom = room;
            _lockUntil = Time.time + seconds;
            AddState(StateTrapped, seconds);
        }

        public void Unlock()
        {
            _lockUntil = 0f;
            RemoveState(StateTrapped);
        }

        Vector2 ClampLock(Vector2 current, Vector2 next)
        {
            if (!IsLocked)
                return next;
            const float pad = 0.35f;
            next.x = Mathf.Clamp(next.x, _lockRoom.xMin + pad, _lockRoom.xMax - pad);
            next.y = Mathf.Clamp(next.y, _lockRoom.yMin + pad, _lockRoom.yMax - pad);
            return next;
        }

        // ---------- Эмоции от карт ----------

        // Сдвиг эмоции: рост — с чувствительностью черты, спад — как есть. Возвращает, на сколько реально сдвинулось.
        public int ShiftStat(ActorStat stat, int delta, bool subtle = false)
        {
            if (delta == 0)
                return 0;
            float gain = 1f;
            if (delta > 0 && Trait != null)
            {
                switch (stat)
                {
                    case ActorStat.Anger: gain = Trait.angerGain; break;
                    case ActorStat.Stress: gain = Trait.stressGain; break;
                    case ActorStat.Sadness: gain = Trait.sadnessGain; break;
                    case ActorStat.Attraction: gain = Trait.attractionGain; break;
                }
            }

            int amount = delta > 0 ? Mathf.Max(1, Mathf.RoundToInt(delta * Mathf.Max(0f, gain))) : delta;
            int before = Read(stat);
            int after = Mathf.Clamp(before + amount, 0, 100);
            Write(stat, after);
            int moved = after - before;
            if (moved != 0)
                StatShifted?.Invoke(this, stat, moved, subtle);
            return moved;
        }

        public int Read(ActorStat stat)
        {
            switch (stat)
            {
                case ActorStat.Anger: return Anger;
                case ActorStat.Stress: return Stress;
                case ActorStat.Sadness: return Sadness;
                case ActorStat.Attraction: return Attraction;
                case ActorStat.Confidence: return Confidence;
                default: return SelfControl;
            }
        }

        void Write(ActorStat stat, int value)
        {
            switch (stat)
            {
                case ActorStat.Anger: Anger = value; break;
                case ActorStat.Stress: Stress = value; break;
                case ActorStat.Sadness: Sadness = value; break;
                case ActorStat.Attraction: Attraction = value; break;
                case ActorStat.Confidence: Confidence = value; break;
                default: SelfControl = value; break;
            }
        }

        // Отношения к сопернику: вражда растёт — доверие падает, и наоборот.
        public void ShiftBond(int hostility, int trust)
        {
            Hostility = Mathf.Clamp(Hostility + hostility - trust, 0, 100);
            RelationToRival += trust - hostility;
        }

        // Эмоция перевалила порог — участник срывается так, что это видно.
        public string Escalate()
        {
            if (IsFighting || _action == NpcActionId.SeekFight || Time.time < _snapReady)
                return null;
            string kind = null;
            if (Anger >= 75 && SelfControl <= 50 && Rival != null && !Rival.IsFighting)
            {
                CancelBeat();
                _rageUntil = Mathf.Max(_rageUntil, Time.time + 6f);
                Run(NpcActionId.SeekFight, "ВСЁ, ДЕРЖИТЕ МЕНЯ");
                kind = "anger";
            }
            else if (Stress >= 75)
            {
                CancelBeat();
                Run(NpcActionId.Panic, "НЕ МОГУ!");
                kind = "stress";
            }
            else if (Sadness >= 70)
            {
                Cry(6f);
                kind = "sadness";
            }
            else if (Attraction >= 70 && Rival != null && Rival.CanChat && _beat == null)
            {
                Vector2 gap = (Vector2)transform.position - (Vector2)Rival.transform.position;
                if (gap.sqrMagnitude < 0.04f)
                    gap = Vector2.left;
                var other = Rival;
                Direct(new StageBeat
                {
                    goal = (Vector2)Rival.transform.position + gap.normalized * 0.9f,
                    line = "ты сегодня такая...",
                    hold = 3.2f,
                    anim = StageAnim.None,
                    onArrive = me => Broadcast("flirt", other, "Flirt", "Romance", MomentTags.Warmth)
                });
                kind = "attraction";
            }

            if (kind != null)
            {
                _snapReady = Time.time + 9f;
                Snapped?.Invoke(this, kind);
            }

            return kind;
        }

        // Слёзы от карты: садится, плачет, камера видит «в слезах», мир слышит.
        public void Cry(float seconds)
        {
            if (IsFighting)
                return;
            _cryUntil = Time.time + seconds;
            Direct(new StageBeat
            {
                walk = false,
                line = "слёзы",
                thought = true,
                hold = seconds,
                anim = StageAnim.Sit,
                urgent = true
            });
            Sfx.Play(Cue.Cry, 0.6f);
            FadeBit.Burst(transform.position + Vector3.up * 0.9f, 7, new Color(0.45f, 0.75f, 1f, 1f));
            Broadcast("crying", null, MomentTags.Crying);
        }

        // Реплика без смены действия — для карт, которые заставляют что-то сказать.
        public void Line(string text, float seconds, bool thought = false)
        {
            Say(text, seconds, thought);
        }

        // Публичное событие от лица участника (раскрыл секрет, спел, поскользнулся).
        public void Announce(string eventId, NPCController target, params string[] tags)
        {
            Broadcast(eventId, target, tags);
        }

        // Сбить цепочку: перестаёт паниковать и рваться в драку.
        public void CalmDown(string line)
        {
            if (IsFighting)
                return;
            _hasPending = false;
            _noticed = false;
            _queued = null;
            _rageUntil = 0f;
            CancelBeat();
            _action = NpcActionId.Idle;
            Chain = 0;
            Say(line, ReadFor(line), false);
        }

        // «Подлить масла»: следующая негативная реакция бьёт вдвое.
        void AmplifyBump(ReactionRule rule)
        {
            if (_amplifying || !HasState(StateAmplified) || rule == null)
                return;
            bool negative = rule.anger > 0 || rule.stress > 0 || rule.sadness > 0
                            || rule.action == NpcActionId.Panic || rule.action == NpcActionId.SeekFight;
            if (!negative)
                return;
            _amplifying = true;
            Bump(rule);
            _amplifying = false;
            RemoveState(StateAmplified);
            Say("ДА СКОЛЬКО МОЖНО!", 3f, false);
            FadeBit.Burst(transform.position + Vector3.up * 1f, 12, new Color(1f, 0.55f, 0.1f, 1f));
            Snapped?.Invoke(this, "amplified");
        }
    }
}
