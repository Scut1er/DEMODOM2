using System;
using System.Collections;
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

        public IReadOnlyList<CapturedMoment> Moments => _moments;
        public bool IsFull => _moments.Count >= Capacity;
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

        public void ResetCapture()
        {
            _moments.Clear();
            _busy = false;
            _sticky = false;
            _hold = false;
            if (_reticle != null)
                _reticle.gameObject.SetActive(false);
        }

        public void ToggleSticky()
        {
            _sticky = !_sticky;
        }

        public void SetSticky(bool on)
        {
            _sticky = on;
        }

        public void SetHold(bool hold)
        {
            _hold = hold;
        }

        public bool TryCapture()
        {
            if (!Mode || IsFull || _busy || _cast == null || _reticle == null)
                return false;

            var inside = new List<NPCController>();
            Vector2 origin = _reticle.position;
            for (int i = 0; i < _cast.Count; i++)
            {
                Vector2 p = _cast[i].transform.position;
                if (Mathf.Abs(p.x - origin.x) <= HalfX && Mathf.Abs(p.y - origin.y) <= HalfY)
                    inside.Add(_cast[i]);
            }

            _busy = true;
            _reticle.gameObject.SetActive(false);
            StartCoroutine(Shoot(origin, inside));
            return true;
        }

        IEnumerator Shoot(Vector2 origin, List<NPCController> inside)
        {
            yield return null;

            Vector2 screen;
            Texture2D photo = Grab(origin, out screen);
            _busy = false;
            if (_reticle != null)
                _reticle.gameObject.SetActive(Mode && !_busy);

            if (photo == null || IsFull)
            {
                if (photo != null)
                    Destroy(photo);
                Missed?.Invoke();
                yield break;
            }

            bool fridgeIn = InFrame(origin, _fridge) && _sceneOnFire != null && _sceneOnFire();
            bool bathIn = InFrame(origin, _bathroom) && _bathOpen != null && _bathOpen();
            bool cast = inside.Count > 0;
            var tags = cast && _context != null ? _context.RecentTags(5f) : new List<string>();
            if (!fridgeIn)
                tags.Remove(MomentTags.Fire);
            AddLiveTags(tags, inside, fridgeIn);
            ShowMood mood;
            CaptureGrade grade;
            if (cast)
            {
                grade = CaptureGrade.Cast;
                mood = ResolveMood(inside, tags, fridgeIn);
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

            var moment = new CapturedMoment
            {
                time = Time.time,
                tags = tags,
                photo = photo,
                screenPoint = screen,
                mood = mood,
                grade = grade
            };
            for (int i = 0; i < inside.Count; i++)
                moment.actorNames.Add(inside[i].DisplayName);

            _moments.Add(moment);
            Captured?.Invoke(moment);
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

        void AddLiveTags(List<string> tags, List<NPCController> inside, bool fridgeIn)
        {
            for (int i = 0; i < inside.Count; i++)
            {
                if (inside[i].IsFighting)
                {
                    Add(tags, MomentTags.Fight);
                    Add(tags, MomentTags.Slap);
                    Add(tags, MomentTags.Conflict);
                }

                if (inside[i].IsCrying)
                    Add(tags, MomentTags.Crying);

                if (inside[i].HasRage)
                    Add(tags, MomentTags.Conflict);
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

        static void Add(List<string> tags, string tag)
        {
            if (!tags.Contains(tag))
                tags.Add(tag);
        }

        void Update()
        {
            if (_reticle == null)
                return;

            _reticle.gameObject.SetActive(Mode && !_busy);
            if (!Mode || Mouse.current == null || Camera.main == null)
                return;

            Vector2 screen = Mouse.current.position.ReadValue();
            float dist = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, dist));
            world.z = 0f;
            _reticle.position = world;
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
