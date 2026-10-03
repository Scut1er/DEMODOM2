using System;
using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.NPC;
using RealityDirector.Util;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RealityDirector.Capture
{
    public class CaptureSystem : MonoBehaviour
    {
        public const float HalfX = 1.5f;
        public const float HalfY = 1.25f;
        public const float MaxClip = 3f;
        // Состояние сцены, не картинка. 0,25 с ловит драку короче секунды и не читает пиксели каждый кадр.
        public const float SampleStep = 0.25f;

        public int Capacity = 2;
        public bool Mode => _sticky || _hold;

        readonly List<CapturedMoment> _moments = new List<CapturedMoment>();
        EpisodeContext _context;
        List<NPCController> _cast;
        Func<bool> _sceneOnFire;
        Transform _fridge;
        Transform _bathroom;
        Func<bool> _bathOpen;
        Transform _reticle;
        bool _sticky;
        bool _hold;
        bool _busy;
        bool _recording;
        bool _grabbing;
        float _recordStart;
        float _nextSample;
        readonly List<ClipHit> _take = new List<ClipHit>();

        public IReadOnlyList<CapturedMoment> Moments => _moments;
        // Рамка на площадке: центр и видна ли (для плашки «В кадре» у её края).
        public Vector2 FrameCenter => _reticle != null ? (Vector2)_reticle.position : Vector2.zero;
        public bool FrameVisible => _reticle != null && _reticle.gameObject.activeSelf;

        // «Нужный момент» (карта NextCaptureBonus): запись, начатая до этого времени, получает бонус качества.
        public static float BonusUntil;
        public static bool BonusActive => Time.unscaledTime <= BonusUntil;
        bool _bonus;

        // Что сейчас в рамке — по тем же правилам, по которым кадр попадёт в футаж.
        public class FrameScan
        {
            public readonly List<NPCController> actors = new List<NPCController>();
            public readonly List<string> tags = new List<string>();
            public bool fire;
            public Vector2 center;
        }

        public bool Scan(FrameScan scan)
        {
            scan.actors.Clear();
            scan.tags.Clear();
            scan.fire = false;
            if (_reticle == null || _cast == null || !Mode)
                return false;
            Vector2 origin = _reticle.position;
            scan.center = origin;
            for (int i = 0; i < _cast.Count; i++)
            {
                if (_cast[i] == null)
                    continue;
                Vector2 p = _cast[i].transform.position;
                if (Mathf.Abs(p.x - origin.x) <= HalfX && Mathf.Abs(p.y - origin.y) <= HalfY)
                    scan.actors.Add(_cast[i]);
            }

            bool fridgeIn = InFrame(origin, _fridge) && _sceneOnFire != null && _sceneOnFire();
            var cues = new List<string>();
            if (scan.actors.Count > 0)
                scan.tags.AddRange(FramedTags(origin, scan.actors, cues));
            if (!fridgeIn)
                scan.tags.Remove(MomentTags.Fire);
            AddLiveTags(scan.tags, scan.actors, fridgeIn, cues);
            scan.fire = fridgeIn;
            return true;
        }
        public bool IsFull => _moments.Count >= Capacity;
        public bool Recording => _recording;
        public float Recorded => _recording ? Mathf.Min(MaxClip, Time.unscaledTime - _recordStart) : 0f;
        public event Action<CapturedMoment> Captured;
        public event Action Missed;

        public void Init(EpisodeContext context, List<NPCController> cast, Func<bool> sceneOnFire, Transform fridge, Transform bathroom, Func<bool> bathOpen)
        {
            _context = context;
            _cast = cast;
            _sceneOnFire = sceneOnFire;
            _fridge = fridge;
            _bathroom = bathroom;
            _bathOpen = bathOpen;
            _reticle = BuildReticle();
            _reticle.gameObject.SetActive(false);
        }

        // Забирает ролики, не уничтожая текстуры. Библиотека выпуска забирает их себе.
        public List<CapturedMoment> Detach()
        {
            if (_recording)
                EndRecord();
            else
                CancelRecord();
            var taken = new List<CapturedMoment>(_moments);
            _moments.Clear();
            _busy = false;
            _sticky = false;
            _hold = false;
            if (_reticle != null)
                _reticle.gameObject.SetActive(false);
            return taken;
        }

        public void ResetCapture()
        {
            CancelRecord();
            for (int i = 0; i < _moments.Count; i++)
                _moments[i].Release();
            _moments.Clear();
            _busy = false;
            _sticky = false;
            _hold = false;
            if (_reticle != null)
                _reticle.gameObject.SetActive(false);
        }

        public bool BeginRecord()
        {
            if (!Mode || IsFull || _recording || _busy || _cast == null || _reticle == null)
                return false;
            _recording = true;
            _recordStart = Time.unscaledTime;
            _bonus = BonusActive;
            _nextSample = SampleStep;
            _take.Clear();
            TakeSample();
            return true;
        }

        public void EndRecord()
        {
            if (!_recording)
                return;
            _recording = false;
            PaintReticle(false);
            if (_take.Count == 0 || IsFull)
            {
                DropTake();
                Missed?.Invoke();
                return;
            }

            var moment = Fold(_take, Time.unscaledTime - _recordStart);
            _take.Clear();
            if (_bonus)
            {
                // Качество кадра выше: монтаж и эфир читают это из cues футажа.
                CapturedMoment.AddCue(moment.cues, "Quality", "+1");
                BonusUntil = 0f;
                _bonus = false;
            }
            _moments.Add(moment);
            Captured?.Invoke(moment);
        }

        public void CancelRecord()
        {
            if (!_recording && _take.Count == 0)
                return;
            _recording = false;
            PaintReticle(false);
            DropTake();
        }

        public void ToggleSticky()
        {
            _sticky = !_sticky;
        }

        public void SetSticky(bool on)
        {
            if (!on && _recording)
                CancelRecord();
            _sticky = on;
        }

        public void Peek(List<string> names)
        {
            if (names == null)
                return;
            names.Clear();
            if (_reticle == null || _cast == null || !Mode)
                return;
            Vector2 origin = _reticle.position;
            for (int i = 0; i < _cast.Count; i++)
            {
                if (_cast[i] == null)
                    continue;
                Vector2 p = _cast[i].transform.position;
                if (Mathf.Abs(p.x - origin.x) <= HalfX && Mathf.Abs(p.y - origin.y) <= HalfY)
                    names.Add(_cast[i].DisplayName);
            }

            if (_fridge != null && _sceneOnFire != null && _sceneOnFire() && InFrame(origin, _fridge))
                names.Add("Холодильник");
        }

        public void SetHold(bool hold)
        {
            _hold = hold;
        }

        void TakeSample()
        {
            if (_reticle == null)
                return;
            Vector2 origin = _reticle.position;
            _grabbing = true;
            _reticle.gameObject.SetActive(false);
            Vector2 screen;
            Texture2D photo = Grab(origin, out screen);
            _grabbing = false;
            if (_reticle != null)
                _reticle.gameObject.SetActive(true);
            if (photo == null)
                return;

            var inside = new List<NPCController>();
            for (int i = 0; i < _cast.Count; i++)
            {
                Vector2 p = _cast[i].transform.position;
                if (Mathf.Abs(p.x - origin.x) <= HalfX && Mathf.Abs(p.y - origin.y) <= HalfY)
                    inside.Add(_cast[i]);
            }

            bool fridgeIn = InFrame(origin, _fridge) && _sceneOnFire != null && _sceneOnFire();
            bool bathIn = InFrame(origin, _bathroom) && _bathOpen != null && _bathOpen();
            bool cast = inside.Count > 0;
            var cues = new List<string>();
            var tags = cast ? FramedTags(origin, inside, cues) : new List<string>();
            if (!fridgeIn)
                tags.Remove(MomentTags.Fire);
            AddLiveTags(tags, inside, fridgeIn, cues);
            ShowMood mood;
            CaptureGrade grade;
            if (cast)
            {
                grade = CaptureGrade.Cast;
                mood = ResolveMood(inside, tags, fridgeIn);
                if (HugInFrame(inside))
                    Add(tags, MomentTags.Hug);
                if (mood == ShowMood.Family)
                    Add(tags, MomentTags.Warmth);
            }
            else if (fridgeIn || bathIn)
            {
                grade = CaptureGrade.Prop;
                if (fridgeIn)
                {
                    mood = ShowMood.Trash;
                    Add(tags, MomentTags.Fire);
                    Add(tags, MomentTags.Chaos);
                }
                else
                {
                    mood = ShowMood.Family;
                    Add(tags, MomentTags.Warmth);
                }
            }
            else
            {
                grade = CaptureGrade.Blank;
                mood = ShowMood.Family;
            }

            var names = new List<string>();
            for (int i = 0; i < inside.Count; i++)
                names.Add(inside[i].DisplayName);
            if (grade == CaptureGrade.Cast)
            {
                for (int i = 0; i < inside.Count; i++)
                {
                    if (inside[i].IsTripping || inside[i].IsPlanting)
                        CapturedMoment.AddCue(cues, HiddenTrait.Prankster.ToString(), inside[i].DisplayName);
                    else if (inside[i].IsStealing)
                        CapturedMoment.AddCue(cues, HiddenTrait.Kleptomaniac.ToString(), inside[i].DisplayName);
                }
            }

            _take.Add(new ClipHit
            {
                image = photo,
                screen = screen,
                tags = tags,
                cues = cues,
                names = names,
                mood = mood,
                grade = grade,
                exposed = grade == CaptureGrade.Cast ? ExposedIn(inside) : HiddenTrait.None
            });
        }

        // Теги недавних событий, но только тех, что видны в кадре: участник события в кадре
        // или место события (горящий холодильник) в кадре. Событие «на всю квартиру» без места — общее.
        // Раньше в ролик попадали все теги за 5 с, и в эфире «плакал» тот, кого в кадре не было.
        List<string> FramedTags(Vector2 origin, List<NPCController> inside, List<string> cues)
        {
            var tags = new List<string>();
            if (_context == null)
                return tags;
            var stamps = _context.RecentStamps(5f);
            for (int s = 0; s < stamps.Count; s++)
            {
                var stamp = stamps[s];
                bool actors = !string.IsNullOrEmpty(stamp.sourceActorId) || !string.IsNullOrEmpty(stamp.targetActorId);
                if (actors)
                {
                    var source = Find(inside, stamp.sourceActorId);
                    var target = Find(inside, stamp.targetActorId);
                    if (source == null && target == null)
                        continue;
                    Add(tags, stamp.tag);
                    if (source != null)
                        CapturedMoment.AddCue(cues, stamp.tag, source.DisplayName);
                    if (target != null)
                        CapturedMoment.AddCue(cues, stamp.tag, target.DisplayName);
                    continue;
                }

                if (stamp.hasLocus && (Mathf.Abs(stamp.locus.x - origin.x) > HalfX + 0.5f || Mathf.Abs(stamp.locus.y - origin.y) > HalfY + 0.5f))
                    continue;
                Add(tags, stamp.tag);
            }

            return tags;
        }

        static NPCController Find(List<NPCController> inside, string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            for (int i = 0; i < inside.Count; i++)
            {
                if (inside[i] != null && inside[i].Id == id)
                    return inside[i];
            }

            return null;
        }

        static CapturedMoment Fold(List<ClipHit> hits, float duration)
        {
            CaptureGrade best = CaptureGrade.Blank;
            for (int i = 0; i < hits.Count; i++)
            {
                if (Rank(hits[i].grade) > Rank(best))
                    best = hits[i].grade;
            }

            var moodCount = new int[3];
            int moodPick = 0;
            var tags = new List<string>();
            var cues = new List<string>();
            var names = new List<string>();
            HiddenTrait exposed = HiddenTrait.None;
            var frames = new List<Texture2D>(hits.Count);
            for (int i = 0; i < hits.Count; i++)
            {
                var hit = hits[i];
                frames.Add(hit.image);
                for (int t = 0; t < hit.tags.Count; t++)
                    Add(tags, hit.tags[t]);
                if (hit.cues != null)
                {
                    for (int c = 0; c < hit.cues.Count; c++)
                        Add(cues, hit.cues[c]);
                }
                for (int n = 0; n < hit.names.Count; n++)
                {
                    if (!names.Contains(hit.names[n]))
                        names.Add(hit.names[n]);
                }

                if (hit.exposed == HiddenTrait.Prankster)
                    exposed = HiddenTrait.Prankster;
                else if (exposed != HiddenTrait.Prankster && hit.exposed == HiddenTrait.Kleptomaniac)
                    exposed = HiddenTrait.Kleptomaniac;
                if (hit.grade != best)
                    continue;
                int slot = (int)hit.mood;
                moodCount[slot]++;
                if (moodCount[slot] >= moodCount[moodPick])
                    moodPick = slot;
            }

            return new CapturedMoment
            {
                time = Time.time,
                duration = Mathf.Max(0.05f, duration),
                tags = tags,
                photo = frames[0],
                frames = frames,
                screenPoint = hits[0].screen,
                mood = (ShowMood)moodPick,
                grade = best,
                exposed = best == CaptureGrade.Cast ? exposed : HiddenTrait.None,
                actorNames = names,
                cues = cues
            };
        }

        static int Rank(CaptureGrade grade)
        {
            if (grade == CaptureGrade.Cast)
                return 2;
            if (grade == CaptureGrade.Prop)
                return 1;
            return 0;
        }

        void DropTake()
        {
            for (int i = 0; i < _take.Count; i++)
            {
                if (_take[i].image != null)
                    Destroy(_take[i].image);
            }

            _take.Clear();
        }

        struct ClipHit
        {
            public Texture2D image;
            public Vector2 screen;
            public List<string> tags;
            public List<string> cues;
            public List<string> names;
            public ShowMood mood;
            public CaptureGrade grade;
            public HiddenTrait exposed;
        }

        static Texture2D Grab(Vector2 origin, out Vector2 screenCenter)
        {
            screenCenter = Vector2.zero;
            var cam = Camera.main;
            if (cam == null)
                return null;

            Vector3 bl = cam.WorldToScreenPoint(origin + new Vector2(-HalfX, -HalfY));
            Vector3 tr = cam.WorldToScreenPoint(origin + new Vector2(HalfX, HalfY));
            int x = Mathf.RoundToInt(Mathf.Min(bl.x, tr.x));
            int y = Mathf.RoundToInt(Mathf.Min(bl.y, tr.y));
            int w = Mathf.RoundToInt(Mathf.Abs(tr.x - bl.x));
            int h = Mathf.RoundToInt(Mathf.Abs(tr.y - bl.y));
            if (w < 8 || h < 8)
                return null;

            screenCenter = new Vector2(x + w * 0.5f, y + h * 0.5f);

            var rect = cam.pixelRect;
            int rx = Mathf.RoundToInt(x - rect.x);
            int ry = Mathf.RoundToInt(y - rect.y);
            int rw = Mathf.Max(8, Mathf.RoundToInt(rect.width));
            int rh = Mathf.Max(8, Mathf.RoundToInt(rect.height));
            rx = Mathf.Clamp(rx, 0, rw - 2);
            ry = Mathf.Clamp(ry, 0, rh - 2);
            w = Mathf.Clamp(w, 8, rw - rx);
            h = Mathf.Clamp(h, 8, rh - ry);

            var rt = RenderTexture.GetTemporary(rw, rh, 24, RenderTextureFormat.ARGB32);
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            RenderPipeline.SubmitRenderRequest(cam, request);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(rx, ry, w, h), 0, 0);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        static bool InFrame(Vector2 origin, Transform prop)
        {
            if (prop == null || !prop.gameObject.activeInHierarchy)
                return false;
            var renderers = prop.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].enabled)
                    continue;
                Bounds b = renderers[i].bounds;
                if (b.max.x < origin.x - HalfX || b.min.x > origin.x + HalfX)
                    continue;
                if (b.max.y < origin.y - HalfY || b.min.y > origin.y + HalfY)
                    continue;
                return true;
            }

            return false;
        }

        void AddLiveTags(List<string> tags, List<NPCController> inside, bool fridgeIn, List<string> cues)
        {
            for (int i = 0; i < inside.Count; i++)
            {
                string name = inside[i].DisplayName;
                if (inside[i].IsFighting)
                {
                    Add(tags, MomentTags.Fight);
                    Add(tags, MomentTags.Slap);
                    Add(tags, MomentTags.Conflict);
                    CapturedMoment.AddCue(cues, MomentTags.Fight, name);
                }

                if (inside[i].IsCrying)
                {
                    Add(tags, MomentTags.Crying);
                    CapturedMoment.AddCue(cues, MomentTags.Crying, name);
                }

                if (inside[i].IsHugging || inside[i].IsSeekingComfort)
                {
                    Add(tags, MomentTags.Hug);
                    CapturedMoment.AddCue(cues, MomentTags.Hug, name);
                }

                if (inside[i].HasRage)
                {
                    Add(tags, MomentTags.Conflict);
                    CapturedMoment.AddCue(cues, MomentTags.Conflict, name);
                }
            }

            if (fridgeIn)
            {
                Add(tags, MomentTags.Fire);
                Add(tags, MomentTags.Chaos);
            }
        }

        static ShowMood ResolveMood(List<NPCController> inside, List<string> tags, bool sceneOnFire)
        {
            bool fight = false;
            bool cry = false;
            bool rage = false;
            bool emoting = false;
            for (int i = 0; i < inside.Count; i++)
            {
                if (inside[i].IsFighting)
                    fight = true;
                if (inside[i].IsCrying)
                    cry = true;
                if (inside[i].HasRage)
                    rage = true;
                if (inside[i].Action == NpcActionId.Emote)
                    emoting = true;
            }

            if (fight)
                return ShowMood.Trash;
            if (HugInFrame(inside))
                return ShowMood.Family;
            if (cry)
                return ShowMood.Drama;
            if (rage || sceneOnFire)
                return ShowMood.Trash;
            if (emoting && tags.Contains(MomentTags.Misery))
                return ShowMood.Drama;
            if (emoting && tags.Contains(MomentTags.Conflict))
                return ShowMood.Trash;
            return ShowMood.Family;
        }

        static bool HugInFrame(List<NPCController> inside)
        {
            bool walk = false;
            int n = 0;
            for (int i = 0; i < inside.Count; i++)
            {
                n++;
                if (inside[i].IsHugging)
                    return true;
                if (inside[i].IsSeekingComfort)
                    walk = true;
            }

            return walk && n >= 2;
        }

        static HiddenTrait ExposedIn(List<NPCController> inside)
        {
            HiddenTrait secret = HiddenTrait.None;
            for (int i = 0; i < inside.Count; i++)
            {
                if (inside[i].IsTripping || inside[i].IsPlanting)
                    return HiddenTrait.Prankster;
                if (inside[i].IsStealing)
                    secret = HiddenTrait.Kleptomaniac;
            }

            return secret;
        }

        static void Add(List<string> tags, string tag)
        {
            if (!tags.Contains(tag))
                tags.Add(tag);
        }

        void Update()
        {
            if (_reticle == null)
                return;

            if (_recording)
            {
                float elapsed = Time.unscaledTime - _recordStart;
                if (elapsed + 0.0001f >= _nextSample)
                {
                    _nextSample += SampleStep;
                    TakeSample();
                }

                if (elapsed >= MaxClip)
                    EndRecord();
            }

            bool show = (Mode || _recording) && !_grabbing;
            _reticle.gameObject.SetActive(show);
            PaintReticle(_recording);
            if ((!Mode && !_recording) || Mouse.current == null || Camera.main == null)
                return;

            Vector2 screen = Mouse.current.position.ReadValue();
            float dist = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, dist));
            world.z = 0f;
            _reticle.position = world;
        }

        void PaintReticle(bool recording)
        {
            if (_reticle == null)
                return;
            var frame = recording ? new Color(1f, 0.28f, 0.22f, 0.95f) : new Color(1f, 0.92f, 0.55f, 0.95f);
            var fill = recording ? new Color(1f, 0.2f, 0.16f, 0.14f) : new Color(1f, 0.95f, 0.7f, 0.08f);
            var renderers = _reticle.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].color = renderers[i].name == "fill" ? fill : frame;
        }

        static Transform BuildReticle()
        {
            var root = new GameObject("Reticle").transform;
            var frame = new Color(1f, 0.92f, 0.55f, 0.95f);
            var fill = new Color(1f, 0.95f, 0.7f, 0.08f);
            SpriteUtil.Box(root, "fill", Vector3.zero, new Vector2(HalfX * 2f, HalfY * 2f), fill, 25);
            SpriteUtil.Box(root, "top", new Vector3(0f, HalfY, 0f), new Vector2(HalfX * 2f, 0.05f), frame, 26);
            SpriteUtil.Box(root, "bot", new Vector3(0f, -HalfY, 0f), new Vector2(HalfX * 2f, 0.05f), frame, 26);
            SpriteUtil.Box(root, "left", new Vector3(-HalfX, 0f, 0f), new Vector2(0.05f, HalfY * 2f), frame, 26);
            SpriteUtil.Box(root, "right", new Vector3(HalfX, 0f, 0f), new Vector2(0.05f, HalfY * 2f), frame, 26);
            return root;
        }
    }
}
