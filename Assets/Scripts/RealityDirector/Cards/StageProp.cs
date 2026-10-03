using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Events;
using RealityDirector.NPC;
using RealityDirector.UI;
using RealityDirector.Util;
using UnityEngine;

namespace RealityDirector.Cards
{
    // Реквизит карты на полу: видно, где он и докуда достаёт (аура), и люди им пользуются сами —
    // пьют из ящика, поют в караоке, скользят на масле, жмут красную кнопку, читают чужой телефон.
    public class StageProp : MonoBehaviour
    {
        public string Kind { get; private set; }
        public EventDefinition Def { get; private set; }
        public Room Room { get; private set; }
        public bool Sponsor;

        CardStage _stage;
        SpriteRenderer _body;
        SpriteRenderer _disc;
        SpriteRenderer _ring;
        SpriteRenderer _extra;
        Vector3 _bodyScale;
        float _born;
        float _until = float.MaxValue;
        bool _aura;
        bool _use;
        bool _trap;
        bool _spent;
        float _radius = 2.5f;
        public float Radius => _radius;
        Color _tint = Color.white;
        float _nextUse;
        float _nextStamp;
        float _nextFx;
        float _nextEscalate;
        NPCController _holder;
        readonly Dictionary<NPCController, float[]> _acc = new Dictionary<NPCController, float[]>();
        readonly Dictionary<NPCController, float> _slipReady = new Dictionary<NPCController, float>();
        readonly HashSet<NPCController> _greeted = new HashSet<NPCController>();
        readonly List<(ActorStat stat, float rate, string onlyFor)> _rates = new List<(ActorStat, float, string)>();

        public static StageProp Create(CardStage stage, EventDefinition def, string kind, Vector2 at, Room room, bool invisible)
        {
            var go = new GameObject("Prop_" + kind);
            go.transform.position = at;
            var prop = go.AddComponent<StageProp>();
            prop._stage = stage;
            prop.Def = def;
            prop.Kind = kind;
            prop.Room = room;
            prop._born = Time.time;
            prop.Build(invisible);
            if (def != null && def.environmentLifetime == EnvironmentLifetime.Seconds && def.environmentSeconds > 0f)
                prop.Lifetime(def.environmentSeconds);
            return prop;
        }

        public void Lifetime(float seconds)
        {
            _until = Time.time + seconds;
        }

        // ---------- Вид ----------

        void Build(bool invisible)
        {
            var aura = Def != null ? Def.aura : null;
            if (aura != null && aura.radius > 0.1f)
                _radius = aura.radius;
            _tint = TintOf(aura != null ? aura.tags : null, Kind);

            _disc = SpriteUtil.Show(transform, "aura", Vector3.zero, _stage.Fx.DiscSprite(), 1);
            _disc.transform.localScale = Vector3.one * _radius;
            _disc.color = new Color(_tint.r, _tint.g, _tint.b, 0f);
            _ring = SpriteUtil.Show(transform, "auraRing", Vector3.zero, _stage.Fx.Ring01, 1);
            _ring.transform.localScale = Vector3.one * _radius;
            _ring.color = new Color(_tint.r, _tint.g, _tint.b, 0f);

            if (invisible)
                return;
            Sprite sprite;
            Vector2 box;
            Color color = Color.white;
            int order = 6;
            Vector3 offset = Vector3.zero;
            switch (Kind)
            {
                case "RomanceSofa":
                    sprite = GameArt.Sofa != null ? GameArt.Sofa : IllustratedArt.Sofa;
                    box = new Vector2(1.7f, 1.05f);
                    color = new Color(1f, 0.72f, 0.86f);
                    break;
                case "HellColaFridge":
                    sprite = GameArt.Fridge != null ? GameArt.Fridge : IllustratedArt.Fridge;
                    box = new Vector2(0.8f, 1.35f);
                    color = new Color(1f, 0.42f, 0.42f);
                    break;
                case "OilSpill":
                    sprite = PropArt.Get("OilSpill");
                    box = new Vector2(1.5f, 0.6f);
                    order = 1;
                    break;
                case "Poster_Leviathan":
                    sprite = PropArt.Get("Poster_Leviathan");
                    box = new Vector2(0.75f, 1.0f);
                    // Постер — на задней стене над точкой.
                    transform.position = new Vector3(transform.position.x, Room == Room.Bathroom ? 7.75f : 4.45f, 0f);
                    order = 5;
                    break;
                default:
                    sprite = PropArt.Get(Kind);
                    box = SizeOf(Kind);
                    break;
            }

            _body = SpriteUtil.Show(transform, "body", offset, sprite, order);
            if (sprite != null)
            {
                var size = sprite.bounds.size;
                float k = Mathf.Min(box.x / Mathf.Max(0.01f, size.x), box.y / Mathf.Max(0.01f, size.y));
                _body.transform.localScale = new Vector3(k, k, 1f);
                // Арт из пака стоит опорой по центру — поднимаем, чтобы стоял на точке, как рисованный.
                if (sprite.pivot.y > sprite.rect.height * 0.25f && Kind != "OilSpill" && Kind != "Poster_Leviathan")
                    _body.transform.localPosition = new Vector3(0f, size.y * k * 0.5f - 0.15f, 0f);
            }

            _body.color = color;
            _bodyScale = _body.transform.localScale;
            if (Kind == "Spotlight")
            {
                _extra = SpriteUtil.Show(transform, "cone", new Vector3(0.28f, 0.7f, 0f), PropArt.Get("Cone"), 2);
                _extra.transform.localScale = new Vector3(2.2f, 1.5f, 1f);
                _extra.transform.rotation = Quaternion.Euler(0f, 0f, 35f);
            }

            string label = LabelOf(Kind);
            if (label != null)
                _stage.Fx.Label(transform, label, _tint, new Vector2(0f, -28f));
            FadeBit.Burst(transform.position + Vector3.up * 0.2f, 14, new Color(0.85f, 0.75f, 0.6f));
            Sfx.Play(Cue.Card, 0.5f, 0.75f);
        }

        static Vector2 SizeOf(string kind)
        {
            switch (kind)
            {
                case "AlcoholCrate": return new Vector2(0.95f, 0.9f);
                case "OpenMic": return new Vector2(0.5f, 1.0f);
                case "GiftBox": return new Vector2(0.6f, 0.6f);
                case "KaraokeMachine": return new Vector2(0.8f, 1.05f);
                case "RomanticSpeaker": return new Vector2(0.5f, 0.66f);
                case "AnxietyLight": return new Vector2(0.6f, 1.05f);
                case "HiddenCameraProp": return new Vector2(0.5f, 0.55f);
                case "Spotlight": return new Vector2(0.65f, 1.05f);
                case "UnattendedPhone": return new Vector2(0.24f, 0.38f);
                case "RedButton": return new Vector2(0.62f, 0.68f);
                case "Tripod": return new Vector2(0.65f, 1.0f);
                case "ColaCan": return new Vector2(0.2f, 0.32f);
                default: return new Vector2(0.7f, 0.7f);
            }
        }

        public static string LabelOf(string kind)
        {
            switch (kind)
            {
                case "AlcoholCrate": return "алкоголь";
                case "RomanceSofa": return "диван для двоих";
                case "OpenMic": return "открытый микрофон";
                case "GiftBox": return "подарок";
                case "KaraokeMachine": return "караоке";
                case "OilSpill": return "масло";
                case "RomanticSpeaker": return "романтика";
                case "AnxietyLight": return "тревожный свет";
                case "HiddenCameraProp": return "скрытая камера";
                case "Spotlight": return "софит";
                case "UnattendedPhone": return "чужой телефон";
                case "RedButton": return "НЕ НАЖИМАТЬ";
                case "Poster_Leviathan": return "Leviathan";
                case "HellColaFridge": return "Hell Cola";
                case "Tripod": return "исповедь";
                default: return null;
            }
        }

        static Color TintOf(List<string> tags, string kind)
        {
            bool Has(string t) => tags != null && tags.Contains(t);
            if (kind == "AnxietyLight" || Has("Pressure"))
                return new Color(1f, 0.3f, 0.25f);
            if (Has("Romance"))
                return new Color(1f, 0.45f, 0.7f);
            if (Has("Alcohol") || Has("Party"))
                return new Color(0.6f, 1f, 0.45f);
            if (Has("Private") || Has("Secret"))
                return new Color(0.7f, 0.55f, 1f);
            if (Has("Public") || Has("Attention"))
                return new Color(1f, 0.85f, 0.35f);
            if (Has("Chaos") || Has("Comedy"))
                return new Color(1f, 0.6f, 0.2f);
            return new Color(0.9f, 0.85f, 0.7f);
        }

        // ---------- Включение возможностей ----------

        public void EnableAura()
        {
            _aura = true;
            _rates.Clear();
            _rates.AddRange(RatesFor(Def));
            _disc.color = new Color(_tint.r, _tint.g, _tint.b, 0.09f);
            _ring.color = new Color(_tint.r, _tint.g, _tint.b, 0.35f);
        }

        // Что аура делает с людьми в радиусе (в секунду): из таблицы, а если там пусто — по смыслу тегов ауры.
        // onlyFor: null — всем, "shy" — стеснительным, "vain" — тщеславным.
        public static List<(ActorStat stat, float rate, string onlyFor)> RatesFor(EventDefinition def)
        {
            var rates = new List<(ActorStat, float, string)>();
            var aura = def != null ? def.aura : null;
            if (aura != null && aura.actorModifiers != null)
            {
                foreach (var m in aura.actorModifiers)
                {
                    if (m != null && Mathf.Abs(m.perSecond) > 0.001f)
                        rates.Add((m.stat, m.perSecond, null));
                }
            }

            var tags = aura != null ? aura.tags : null;
            if (tags == null)
                return rates;
            if (tags.Contains("Alcohol"))
            {
                rates.Add((ActorStat.SelfControl, -1.6f, null));
                rates.Add((ActorStat.Anger, 0.6f, null));
            }

            if (tags.Contains("Party"))
            {
                rates.Add((ActorStat.Stress, -1f, null));
                rates.Add((ActorStat.Sadness, -0.8f, null));
            }

            if (tags.Contains("Romance"))
                rates.Add((ActorStat.Attraction, 2f, null));
            if (tags.Contains("Music"))
                rates.Add((ActorStat.Stress, -0.6f, null));
            if (tags.Contains("Pressure"))
                rates.Add((ActorStat.Stress, 2.2f, null));
            if (tags.Contains("Private") || tags.Contains("Confession"))
                rates.Add((ActorStat.Stress, -1.4f, null));
            if (tags.Contains("Public") || tags.Contains("Visual"))
            {
                rates.Add((ActorStat.Stress, 2f, "shy"));
                rates.Add((ActorStat.Confidence, 2f, "vain"));
            }

            return rates;
        }

        // Что люди делают с реквизитом сами — одной строкой для подсказки карты.
        public static string UseOf(string kind)
        {
            switch (kind)
            {
                case "AlcoholCrate": return "люди пьют и пьянеют — хуже держат себя в руках";
                case "RomanceSofa": return "садятся вдвоём — флирт и признания";
                case "OpenMic": return "берут микрофон — признание или спор при всех";
                case "GiftBox": return "дарят, крадут или разбивают (бросок d6)";
                case "KaraokeMachine": return "выходят петь: успех — овации, провал — смех (d8)";
                case "OilSpill": return "кто наступит — бросок d8, может поскользнуться";
                case "RomanticSpeaker": return "музыка сближает тех, кто рядом";
                case "AnxietyLight": return "мигает — нервы сдают";
                case "HiddenCameraProp": return "люди забывают о камерах и говорят лишнее";
                case "Spotlight": return "кадр ярче, стеснительным тяжело";
                case "UnattendedPhone": return "любопытный или ревнивый прочтёт чужой телефон";
                case "RedButton": return "кто-то нажмёт — исход по d10";
                case "Poster_Leviathan": return "кадры с постером засчитываются спонсору";
                case "HellColaFridge": return "берут колу в кадре — спонсору";
                default: return null;
            }
        }

        public void EnableUse()
        {
            _use = true;
            // Первый раз — почти сразу: игрок должен увидеть, что карта работает.
            _nextUse = Time.time + 2.2f;
        }

        public void EnableTrap()
        {
            _trap = true;
            _nextUse = Time.time + 2f;
        }

        // ---------- Жизнь ----------

        void Update()
        {
            float age = Time.time - _born;
            if (_body != null)
            {
                // Падение на пол с отскоком.
                float drop = age < 0.35f ? (1f - age / 0.35f) : 0f;
                float squash = age < 0.6f ? Mathf.Sin(age * 20f) * (0.6f - age) * 0.25f : 0f;
                _body.transform.localScale = new Vector3(_bodyScale.x * (1f + squash), _bodyScale.y * (1f - squash), 1f);
                var lp = _body.transform.localPosition;
                _body.transform.localPosition = new Vector3(lp.x, BaseY() + drop * drop * 2.2f, 0f);
            }

            if (_aura)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.2f);
                _ring.transform.localScale = Vector3.one * _radius * (0.97f + pulse * 0.05f);
                var rc = _ring.color;
                // Первые секунды кольцо яркое — видно, докуда достаёт; потом остаётся едва заметным.
                float fresh = Mathf.Clamp01(1f - (Time.time - _born - 2f) / 1.5f);
                rc.a = Mathf.Lerp(0.07f + pulse * 0.06f, 0.35f + pulse * 0.3f, fresh);
                var dc = _disc.color;
                if (Kind != "AnxietyLight")
                {
                    dc.a = Mathf.Lerp(0.04f, 0.1f, fresh);
                    _disc.color = dc;
                }
                _ring.color = rc;
                if (Kind == "AnxietyLight")
                {
                    float flick = Random.value < 0.15f ? 0.05f : 0.28f;
                    _disc.color = new Color(1f, 0.15f, 0.1f, flick);
                }

                TickAura();
            }

            if (_holder != null)
            {
                transform.position = _holder.transform.position + new Vector3(0.38f, 0.2f, 0f);
                if (_body != null)
                    _body.sortingOrder = 12;
            }

            TickFlavour();
            if (_use && !_spent && Time.time >= _nextUse)
                TryUse();
            if (_trap)
                TickTrap();
            if (Sponsor && Time.time >= _nextStamp)
            {
                _nextStamp = Time.time + 1.5f;
                _stage.Stamp(transform.position, null, MomentTags.Sponsor);
            }

            if (Time.time >= _until)
                Vanish();
        }

        float _baseY = float.NaN;

        float BaseY()
        {
            if (float.IsNaN(_baseY))
                _baseY = _body != null ? _body.transform.localPosition.y : 0f;
            return _baseY;
        }

        void Vanish()
        {
            FadeBit.Burst(transform.position + Vector3.up * 0.3f, 10, new Color(0.85f, 0.8f, 0.7f));
            _stage.Forget(this);
            Destroy(gameObject);
        }

        // Эмоции тех, кто стоит в ауре, ползут по ставкам; раз в пару секунд проверка на срыв.
        void TickAura()
        {
            if (_rates.Count == 0)
                return;
            bool check = Time.time >= _nextEscalate;
            if (check)
                _nextEscalate = Time.time + 2f;
            foreach (var npc in _stage.Alive())
            {
                if (Vector2.Distance(npc.transform.position, transform.position) > _radius)
                    continue;
                if (!_acc.TryGetValue(npc, out var acc))
                {
                    acc = new float[6];
                    _acc[npc] = acc;
                }

                foreach (var (stat, rate, onlyFor) in _rates)
                {
                    if (onlyFor == "shy" && !Shy(npc))
                        continue;
                    if (onlyFor == "vain" && !Vain(npc))
                        continue;
                    int i = (int)stat;
                    acc[i] += rate * Time.deltaTime;
                    if (Mathf.Abs(acc[i]) >= 8f)
                    {
                        int step = Mathf.RoundToInt(acc[i]);
                        acc[i] -= step;
                        npc.ShiftStat(stat, step, true);
                    }
                }

                if (Kind == "Spotlight" && !_greeted.Contains(npc))
                {
                    _greeted.Add(npc);
                    npc.AddState(NPCController.StateSpotlit, 0f);
                    if (Vain(npc))
                        npc.Direct(new StageBeat { walk = false, line = "наконец-то мой свет!", hold = 2.6f, anim = StageAnim.Cheer });
                    else if (Shy(npc))
                        npc.Direct(new StageBeat { walk = false, line = "не смотрите на меня", hold = 2.6f, anim = StageAnim.Cower });
                }

                if (check)
                    npc.Escalate();
            }
        }

        static bool Shy(NPCController npc)
        {
            var t = npc.Trait != null ? npc.Trait.traitId : TraitId.Sentimental;
            return t == TraitId.Shy || t == TraitId.Timid || t == TraitId.Panicker || t == TraitId.Cowardly;
        }

        static bool Vain(NPCController npc)
        {
            return (npc.Trait != null && npc.Trait.traitId == TraitId.Vain) || npc.Hidden == HiddenTrait.Singer;
        }

        // Живые мелочи реквизита: ноты из колонки, мигание кнопки, вибрация телефона, огонёк камеры.
        void TickFlavour()
        {
            if (Time.time < _nextFx || _spent)
                return;
            switch (Kind)
            {
                case "RomanticSpeaker":
                    _nextFx = Time.time + 0.7f;
                    _stage.Fx.Icon(transform.position + new Vector3(Random.Range(-0.2f, 0.2f), 0.7f, 0f), Random.value < 0.5f ? "Note" : "Heart", new Vector3(Random.Range(-0.4f, 0.4f), 0.9f, 0f), 1.4f, 0.2f, new Color(1f, 0.6f, 0.8f));
                    break;
                case "UnattendedPhone":
                    _nextFx = Time.time + 2.6f;
                    Sfx.Play(Cue.Blip, 0.25f, 1.8f);
                    _stage.Fx.Icon(transform.position + Vector3.up * 0.45f, "Bang", new Vector3(0f, 0.4f, 0f), 0.8f, 0.16f, new Color(1f, 0.3f, 0.3f));
                    break;
                case "RedButton":
                    _nextFx = Time.time + 1.2f;
                    if (_body != null)
                        _body.color = _body.color.g > 0.9f ? new Color(1f, 0.75f, 0.75f) : Color.white;
                    break;
                case "HiddenCameraProp":
                    _nextFx = Time.time + 1.6f;
                    FadeBit.Spawn(transform.position + new Vector3(0.05f, 0.42f, 0f), Vector3.zero, new Color(1f, 0.15f, 0.1f, 1f), 0.5f, 0.05f);
                    break;
                case "AlcoholCrate":
                    _nextFx = Time.time + 1.4f;
                    FadeBit.Spawn(transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0.8f, 0f), new Vector3(0f, 0.6f, 0f), new Color(0.6f, 1f, 0.45f, 0.8f), 0.9f, 0.05f);
                    break;
            }
        }

        // ---------- Кто и как пользуется ----------

        void TryUse()
        {
            _nextUse = Time.time + Random.Range(8f, 12f);
            var npc = Pick();
            if (npc == null)
            {
                _nextUse = Time.time + 2f;
                return;
            }

            var beat = BeatFor(npc);
            if (beat != null)
                npc.Direct(beat);
        }

        // Кто подойдёт: сначала тот, кому реквизит «по характеру», потом любой свободный.
        NPCController Pick()
        {
            NPCController best = null;
            int bestScore = int.MinValue;
            foreach (var npc in _stage.Alive())
            {
                if (!npc.CanDirect(false) || npc.StageBusy || npc.IsLocked && _stage.RoomAt(npc.transform.position) != Room)
                    continue;
                int score = Random.Range(0, 3);
                var t = npc.Trait != null ? npc.Trait.traitId : TraitId.Sentimental;
                switch (Kind)
                {
                    case "AlcoholCrate":
                        if (t == TraitId.Aggressive || t == TraitId.Chaotic || npc.Sadness >= 30)
                            score += 5;
                        if (npc.HasState(NPCController.StateDrunk))
                            score -= 2;
                        break;
                    case "KaraokeMachine":
                        if (Vain(npc))
                            score += 6;
                        if (t == TraitId.Shy)
                            score -= 4;
                        break;
                    case "GiftBox":
                        if (npc.Hidden == HiddenTrait.Kleptomaniac)
                            score += 6;
                        if (t == TraitId.Jealous)
                            score += 3;
                        break;
                    case "RedButton":
                        if (t == TraitId.Chaotic || npc.Hidden == HiddenTrait.Prankster)
                            score += 6;
                        break;
                    case "UnattendedPhone":
                        if (t == TraitId.Jealous || t == TraitId.Chaotic)
                            score += 6;
                        break;
                    case "OpenMic":
                        if (Vain(npc) || npc.Sadness >= 30 || npc.Anger >= 30)
                            score += 4;
                        break;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = npc;
                }
            }

            return best;
        }

        Vector2 Beside(NPCController npc)
        {
            float side = npc.transform.position.x < transform.position.x ? -0.65f : 0.65f;
            return HouseMap.Clamp(Room, (Vector2)transform.position + new Vector2(side, -0.05f));
        }

        StageBeat BeatFor(NPCController npc)
        {
            switch (Kind)
            {
                case "AlcoholCrate":
                    return new StageBeat
                    {
                        goal = Beside(npc),
                        line = "буль-буль",
                        hold = 2.6f,
                        anim = StageAnim.Drink,
                        onArrive = me => _stage.Fx.Icon(me.transform.position + new Vector3(0.25f, 0.9f, 0f), "Bottle", new Vector3(0f, 0.3f, 0f), 2.4f, 0.28f),
                        onDone = me =>
                        {
                            me.AddState(NPCController.StateDrunk, 30f);
                            me.ShiftStat(ActorStat.SelfControl, -15);
                            me.ShiftStat(ActorStat.Anger, 8);
                            me.Line("ик!", 2f);
                            _stage.Stamp(me.transform.position, me, "Alcohol", "Party", "Disinhibition");
                            // Пьяный с бросков d6: на 5–6 его несёт — к сопернику, с флиртом или с кулаками.
                            if (Random.Range(1, 7) >= 5)
                            {
                                if (me.Anger >= me.Attraction)
                                    me.ShiftStat(ActorStat.Anger, 20);
                                else
                                    me.ShiftStat(ActorStat.Attraction, 25);
                                me.Escalate();
                            }
                        }
                    };
                case "KaraokeMachine":
                    return new StageBeat
                    {
                        goal = Beside(npc),
                        line = "♪ ла-ла-ла ♪",
                        hold = 5.5f,
                        anim = StageAnim.Sing,
                        onArrive = me =>
                        {
                            _stage.Stamp(me.transform.position, me, "Public", "Attention", "Comedy", "Singing");
                            _stage.StartCoroutine(Notes(me, 5.5f));
                        },
                        onDone = me =>
                        {
                            int roll = Random.Range(1, 9);
                            _stage.StartCoroutine(_stage.Fx.Dice(() => me.transform.position, DieSize.D8, roll, "выступление", UiKit.Gold, 0, 0f));
                            if (roll >= 5)
                            {
                                me.ShiftStat(ActorStat.Confidence, 15);
                                me.ShiftStat(ActorStat.Stress, -10);
                                _stage.Cheer(me, 0.9f);
                                _stage.Stamp(me.transform.position, me, MomentTags.Warmth, "Applause");
                            }
                            else
                            {
                                me.ShiftStat(ActorStat.Confidence, -12);
                                me.ShiftStat(ActorStat.Stress, 15);
                                me.Line("не смейтесь!", 2.6f);
                                _stage.Laugh(me, 0.9f);
                                _stage.Stamp(me.transform.position, me, "Humiliation", "Comedy");
                            }
                        }
                    };
                case "GiftBox":
                    return new StageBeat
                    {
                        goal = Beside(npc),
                        line = "о, подарок!",
                        hold = 1.6f,
                        anim = StageAnim.Grab,
                        onDone = me => OpenGift(me)
                    };
                case "RedButton":
                    return new StageBeat
                    {
                        goal = Beside(npc),
                        line = "а что будет, если...",
                        hold = 1.4f,
                        anim = StageAnim.Grab,
                        onDone = me =>
                        {
                            _spent = true;
                            int roll = Random.Range(1, 11);
                            _stage.StartCoroutine(_stage.Fx.Dice(() => transform.position, DieSize.D10, roll, "что будет", new Color(1f, 0.35f, 0.3f), 0, 0f));
                            Sfx.Play(Cue.Click, 0.7f, 0.6f);
                            if (_body != null)
                                _body.color = new Color(0.6f, 0.6f, 0.6f);
                            var at = (Vector2)transform.position;
                            _stage.StartCoroutine(Delay(0.8f, () => _stage.Outcome(at, Room, roll)));
                            Lifetime(6f);
                        }
                    };
                case "UnattendedPhone":
                    return new StageBeat
                    {
                        goal = Beside(npc),
                        line = "чей это телефон?..",
                        hold = 3.2f,
                        anim = StageAnim.Phone,
                        thought = true,
                        onDone = me =>
                        {
                            _spent = true;
                            int roll = Random.Range(1, 9);
                            _stage.StartCoroutine(_stage.Fx.Dice(() => me.transform.position, DieSize.D8, roll, "что нашёл", new Color(0.85f, 0.55f, 1f), 0, 0f));
                            var owner = _stage.Partner(me);
                            if (roll >= 4 && owner != null)
                            {
                                me.ShiftStat(ActorStat.Anger, 15);
                                me.ShiftBond(15, 0);
                                _stage.StartCoroutine(Delay(0.9f, () => _stage.Reveal(owner, me, "телефон")));
                            }
                            else
                                me.Line("ничего интересного", 2.4f);
                            Lifetime(2f);
                        }
                    };
                case "HellColaFridge":
                    return new StageBeat
                    {
                        goal = Beside(npc),
                        line = "Hell Cola — освежает!",
                        hold = 2.6f,
                        anim = StageAnim.Drink,
                        onArrive = me => _stage.Fx.Icon(me.transform.position + new Vector3(0.25f, 0.9f, 0f), "ColaCan", new Vector3(0f, 0.3f, 0f), 2.2f, 0.24f),
                        onDone = me =>
                        {
                            me.ShiftStat(ActorStat.Stress, -8);
                            if (Random.Range(1, 5) >= 3)
                                me.ShiftStat(ActorStat.Confidence, 10);
                            _stage.Stamp(me.transform.position, me, MomentTags.Sponsor, "ProductPlacement");
                        }
                    };
                case "OpenMic":
                    return new StageBeat
                    {
                        goal = Beside(npc),
                        line = "можно в микрофон?",
                        hold = 2f,
                        onDone = me => MicMoment(me)
                    };
                case "RomanceSofa":
                    return SofaBeat(npc, true);
                case "RomanticSpeaker":
                    return new StageBeat
                    {
                        goal = Beside(npc),
                        line = "какая песня...",
                        hold = 4f,
                        anim = StageAnim.Dance,
                        onArrive = me =>
                        {
                            var other = _stage.Partner(me);
                            if (other != null && other.CanDirect(false) && !other.StageBusy)
                                other.Direct(new StageBeat { goal = (Vector2)me.transform.position + new Vector2(0.8f, 0f), line = "потанцуем?", hold = 3.6f, anim = StageAnim.Dance, onArrive = o => _stage.Fx.Link(me, o, "Heart", new Color(1f, 0.45f, 0.7f), 3.4f) });
                            _stage.Stamp(me.transform.position, me, "Romance", "Music", MomentTags.Warmth);
                        }
                    };
                case "HiddenCameraProp":
                    return new StageBeat
                    {
                        goal = Beside(npc),
                        line = "тут никто не слышит, да?",
                        hold = 3.5f,
                        thought = true,
                        anim = StageAnim.Sit,
                        onDone = me =>
                        {
                            if (me.SelfControl < 55 || Random.value < 0.4f)
                                _stage.Confess(me, transform.position);
                            _stage.Stamp(me.transform.position, me, "Private", "Authentic", "Confession");
                        }
                    };
                case "OilSpill":
                    // Не замечает лужу и идёт напрямик.
                    return new StageBeat
                    {
                        goal = (Vector2)transform.position,
                        hold = 0.2f,
                        speed = 1.9f
                    };
                default:
                    return null;
            }
        }

        StageBeat SofaBeat(NPCController npc, bool invite)
        {
            return new StageBeat
            {
                goal = (Vector2)transform.position + new Vector2(-0.35f, 0.1f),
                line = "присяду...",
                hold = 5f,
                anim = StageAnim.Sit,
                onArrive = me =>
                {
                    var other = _stage.Partner(me);
                    if (!invite || other == null || !other.CanDirect(false) || other.StageBusy)
                    {
                        me.ShiftStat(ActorStat.Stress, -8);
                        return;
                    }

                    other.Direct(new StageBeat
                    {
                        goal = (Vector2)transform.position + new Vector2(0.4f, 0.1f),
                        line = "можно рядом?",
                        hold = 4.2f,
                        anim = StageAnim.Sit,
                        onArrive = o =>
                        {
                            _stage.Fx.Link(me, o, "Heart", new Color(1f, 0.45f, 0.7f), 4f);
                            me.ShiftStat(ActorStat.Attraction, 12);
                            o.ShiftStat(ActorStat.Attraction, 12);
                            _stage.Stamp(transform.position, me, "Romance", "Flirt", MomentTags.Warmth);
                        }
                    });
                }
            };
        }

        void OpenGift(NPCController me)
        {
            _spent = true;
            int roll = Random.Range(1, 7);
            _stage.StartCoroutine(_stage.Fx.Dice(() => me.transform.position, DieSize.D6, roll, "что сделает", UiKit.Gold, 0, 0f));
            var other = _stage.Partner(me);
            _stage.StartCoroutine(Delay(0.85f, () =>
            {
                if (roll <= 2)
                {
                    // Разбил.
                    _stage.Fx.Confetti(transform.position + Vector3.up * 0.3f, 25);
                    Sfx.Play(Cue.Slap, 0.6f, 1.3f);
                    me.Line("упс... разбил", 2.6f);
                    me.ShiftStat(ActorStat.Sadness, 15);
                    if (other != null)
                        other.ShiftStat(ActorStat.Anger, 10);
                    _stage.Stamp(transform.position, me, MomentTags.Misery, "Comedy");
                    Lifetime(0.1f);
                }
                else if (roll <= 4)
                {
                    // Утащил себе.
                    _holder = me;
                    _aura = false;
                    if (_disc != null)
                        _disc.enabled = false;
                    if (_ring != null)
                        _ring.enabled = false;
                    me.Line("моё!", 2.4f);
                    me.Announce("theft", other, "Theft", "Jealousy", MomentTags.Conflict);
                    if (other != null)
                    {
                        other.ShiftStat(ActorStat.Anger, 15);
                        other.ShiftBond(10, 0);
                        other.Line("эй, это было общее!", 2.8f);
                        _stage.Fx.Link(other, me, "Bolt", new Color(1f, 0.35f, 0.25f), 3f);
                    }

                    Lifetime(20f);
                }
                else if (other != null)
                {
                    // Подарил сопернику — тепло.
                    _holder = me;
                    me.Direct(new StageBeat
                    {
                        goal = (Vector2)other.transform.position + new Vector2(-0.85f, 0f),
                        line = "это тебе",
                        hold = 2.6f,
                        urgent = true,
                        onArrive = giver =>
                        {
                            _holder = other;
                            other.Line("мне?! спасибо!", 2.6f);
                            other.ShiftStat(ActorStat.Attraction, 15);
                            other.ShiftBond(0, 15);
                            _stage.Fx.Link(giver, other, "Heart", new Color(1f, 0.45f, 0.7f), 3f);
                            giver.Announce("gift", other, "Gift", MomentTags.Warmth, "Romance");
                        }
                    });
                    Lifetime(20f);
                }
            }));
        }

        void MicMoment(NPCController me)
        {
            var other = _stage.Partner(me);
            bool confession = me.Sadness >= me.Anger ? Random.value < 0.7f : Random.value < 0.3f;
            if (confession || other == null)
            {
                _stage.Confess(me, transform.position);
                _stage.Stamp(transform.position, me, "Public", "Confession");
                return;
            }

            me.Direct(new StageBeat
            {
                walk = false,
                line = "а вот ты, " + other.DisplayName + ", всех достал!",
                hold = 3.4f,
                anim = StageAnim.Bang,
                urgent = true
            });
            me.ShiftBond(12, 0);
            other.ShiftBond(12, 0);
            other.ShiftStat(ActorStat.Anger, 15);
            other.Line("ЧТО?!", 2.4f);
            _stage.Fx.Link(me, other, "Bolt", new Color(1f, 0.35f, 0.25f), 3.4f);
            _stage.Stamp(transform.position, me, "Public", "Argument", MomentTags.Conflict);
        }

        System.Collections.IEnumerator Notes(NPCController singer, float seconds)
        {
            float t = 0f;
            while (t < seconds && singer != null)
            {
                t += 0.35f;
                _stage.Fx.Icon(singer.transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 1.3f, 0f), "Note", new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0f), 1.3f, 0.24f, new Color(0.7f, 1f, 1f));
                yield return new WaitForSeconds(0.35f);
            }
        }

        static System.Collections.IEnumerator Delay(float seconds, System.Action act)
        {
            yield return new WaitForSeconds(seconds);
            act?.Invoke();
        }

        // Масло: кто наступил — бросок, и на 4+ летит на пол.
        void TickTrap()
        {
            foreach (var npc in _stage.Alive())
            {
                if (npc.IsFighting || npc.CurrentAnim == StageAnim.Slip)
                    continue;
                if (Vector2.Distance(npc.transform.position, transform.position) > 0.6f)
                    continue;
                _slipReady.TryGetValue(npc, out float ready);
                if (Time.time < ready)
                    continue;
                _slipReady[npc] = Time.time + 6f;
                int roll = Random.Range(1, 9);
                _stage.StartCoroutine(_stage.Fx.Dice(() => npc.transform.position, DieSize.D8, roll, "поскользнуться", new Color(1f, 0.6f, 0.2f), 0, 0f));
                var who = npc;
                _stage.StartCoroutine(Delay(0.8f, () =>
                {
                    if (who == null)
                        return;
                    if (roll >= 4)
                    {
                        who.Direct(new StageBeat { walk = false, line = "АЙ!", hold = 2.2f, anim = StageAnim.Slip, urgent = true });
                        for (int i = 0; i < 4; i++)
                            _stage.Fx.Icon(who.transform.position + Vector3.up * 0.4f, "Star", new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0.6f, 1.2f), 0f), 1f, 0.2f);
                        who.ShiftStat(ActorStat.Stress, 10);
                        who.ShiftStat(ActorStat.Confidence, -10);
                        _stage.Fx.Banner(() => who.transform.position, "ПОСКОЛЬЗНУЛСЯ", null, new Color(1f, 0.65f, 0.25f), 2f);
                        who.Announce("slip", null, "Comedy", "Humiliation", "Slippery");
                        _stage.Laugh(who, 0.6f);
                    }
                    else
                        who.Line("уф, чуть не...", 2f);
                }));
            }

            // Никто не идёт по луже сам — кого-нибудь «несёт напрямик» через неё.
            if (Time.time >= _nextUse)
                TryUse();
        }
    }
}
