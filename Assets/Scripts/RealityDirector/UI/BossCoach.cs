using System;
using System.Collections.Generic;
using RealityDirector.Meta;
using RealityDirector.Util;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI
{
    public enum BossMood
    {
        Stern,
        Annoyed,
        Yell,
        Smug,
        Think,
        Shock,
        Grin,
        Mad,
        Aside,
        Sigh
    }

    public class CoachStep
    {
        public readonly string line;
        public readonly BossMood mood;
        public readonly RectTransform[] targets;

        public CoachStep(string line, params RectTransform[] targets)
            : this(BossMood.Stern, line, targets)
        {
        }

        public CoachStep(BossMood mood, string line, params RectTransform[] targets)
        {
            this.mood = mood;
            this.line = line;
            this.targets = targets;
        }
    }

    // Затемняет экран, оставляет дырку на нужной кнопке, текст говорит босс.
    public class BossCoach : MonoBehaviour
    {
        const int DoneBeat = 6;

        static BossCoach _instance;
        static readonly Dictionary<string, Sprite> Cutouts = new Dictionary<string, Sprite>();

        Font _font;
        RectTransform _root;
        GameObject _page;
        Image _top;
        Image _bottom;
        Image _left;
        Image _right;
        Image _full;
        RectTransform _dock;
        Text _line;
        CoachStep[] _steps;
        int _index;
        Action _done;
        bool _wait;

        public bool IsOpen => _page != null;
        public bool Ordering => _page != null && _wait;

        public static BossCoach Ensure()
        {
            if (_instance != null)
                return _instance;
            if (!Application.isPlaying)
                return null;
            var go = new GameObject("BossCoach");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<BossCoach>();
            return _instance;
        }

        public static void Dismiss()
        {
            if (_instance == null)
                return;
            _instance.Hide();
        }

        public static void Line(List<CoachStep> steps, string line, params RectTransform[] targets)
        {
            Line(steps, BossMood.Stern, line, targets);
        }

        public static void Line(List<CoachStep> steps, BossMood mood, string line, params RectTransform[] targets)
        {
            if (steps == null || targets == null)
                return;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                    continue;
                steps.Add(new CoachStep(mood, line, targets));
                return;
            }
        }

        public static void Guide(int beat, CoachStep[] steps, Action then)
        {
            var state = GameSession.State;
            if (state == null || !state.wantsTutorial || state.tutorialBeat != beat || steps == null || steps.Length == 0)
            {
                then?.Invoke();
                return;
            }

            var coach = Ensure();
            if (coach == null)
            {
                then?.Invoke();
                return;
            }

            coach.Chain(steps, then);
        }

        public static void Play(int beat, int next, params CoachStep[] steps)
        {
            var state = GameSession.State;
            if (state == null || !state.wantsTutorial || state.tutorialBeat != beat || steps == null || steps.Length == 0)
                return;
            var coach = Ensure();
            if (coach == null)
                return;
            coach.Chain(steps, () =>
            {
                state.tutorialBeat = next;
                if (next >= DoneBeat)
                    state.wantsTutorial = false;
                GameSession.Save();
            });
        }

        public static Sprite Portrait()
        {
            return Cutout("Art/Boss/boss_devil");
        }

        public static Sprite Head()
        {
            return Cutout("Art/Boss/boss_head") ?? Portrait();
        }

        public static Sprite MoodFace(BossMood mood)
        {
            return Cutout("Art/Boss/boss_" + mood.ToString().ToLowerInvariant()) ?? Head();
        }

        static Sprite Cutout(string path)
        {
            if (Cutouts.TryGetValue(path, out var cached) && cached != null)
                return cached;
            var source = Resources.Load<Texture2D>(path);
            if (source == null)
                return null;
            Texture2D tex = source;
            try
            {
                var pixels = source.GetPixels32();
                bool transparent = false;
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a < 250)
                    {
                        transparent = true;
                        break;
                    }
                }

                if (!transparent)
                {
                    var cut = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        var p = pixels[i];
                        if (p.r > 240 && p.g > 240 && p.b > 240)
                            p.a = 0;
                        pixels[i] = p;
                    }

                    cut.SetPixels32(pixels);
                    cut.Apply();
                    cut.hideFlags = HideFlags.HideAndDontSave;
                    tex = cut;
                }
            }
            catch (UnityException)
            {
                tex = source;
            }

            var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cutouts[path] = sprite;
            return sprite;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 520;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            _root = gameObject.GetComponent<RectTransform>();
        }

        public void Order(string line, params RectTransform[] holes)
        {
            Say(BossMood.Stern, line, true, null, holes);
        }

        public void Order(BossMood mood, string line, params RectTransform[] holes)
        {
            Say(mood, line, true, null, holes);
        }

        public void Freeze(string line, Action next, params RectTransform[] holes)
        {
            Say(BossMood.Stern, line, false, next, holes);
        }

        public void Freeze(BossMood mood, string line, Action next, params RectTransform[] holes)
        {
            Say(mood, line, false, next, holes);
        }

        public void Hide()
        {
            _steps = null;
            _done = null;
            _wait = false;
            if (_page != null)
                Destroy(_page);
            _page = null;
        }

        void Say(BossMood mood, string line, bool wait, Action next, RectTransform[] holes)
        {
            Hide();
            _wait = wait;
            _steps = new[] { new CoachStep(mood, line, holes) };
            _done = next;
            _index = 0;
            Canvas.ForceUpdateCanvases();
            ShowCurrent();
        }

        void LateUpdate()
        {
            if (_page == null || _top == null || _steps == null)
                return;
            if (!TryHole(out var hole))
                return;
            _full.gameObject.SetActive(false);
            _top.gameObject.SetActive(true);
            _bottom.gameObject.SetActive(true);
            _left.gameObject.SetActive(true);
            _right.gameObject.SetActive(true);
            Cut(hole);
        }

        void Chain(CoachStep[] steps, Action done)
        {
            Hide();
            _steps = steps;
            _done = done;
            _index = 0;
            Canvas.ForceUpdateCanvases();
            ShowCurrent();
        }

        void ShowCurrent()
        {
            Canvas.ForceUpdateCanvases();
            if (_page != null)
                Destroy(_page);
            _page = new GameObject("coach", typeof(RectTransform));
            _page.transform.SetParent(transform, false);
            Stretch(_page.GetComponent<RectTransform>());

            _full = Shade("full");
            _top = Shade("top");
            _bottom = Shade("bottom");
            _left = Shade("left");
            _right = Shade("right");

            bool hole = TryHole(out Rect holeRect);
            _full.gameObject.SetActive(!hole);
            _top.gameObject.SetActive(hole);
            _bottom.gameObject.SetActive(hole);
            _left.gameObject.SetActive(hole);
            _right.gameObject.SetActive(hole);
            if (hole)
                Cut(holeRect);

            bool low = hole && holeRect.center.y < 0f;
            BuildDock(!low);
            _line.text = _steps[_index].line;
        }

        bool TryHole(out Rect hole)
        {
            hole = new Rect();
            var targets = _steps[_index].targets;
            if (targets == null || targets.Length == 0)
                return false;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool any = false;
            for (int i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (target == null)
                    continue;
                var corners = new Vector3[4];
                target.GetWorldCorners(corners);
                for (int c = 0; c < 4; c++)
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, corners[c]);
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local))
                        continue;
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                    any = true;
                }
            }

            if (!any)
                return false;
            const float pad = 14f;
            min -= new Vector2(pad, pad);
            max += new Vector2(pad, pad);
            hole = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return hole.width > 8f && hole.height > 8f;
        }

        void Cut(Rect hole)
        {
            var canvas = _root.rect;
            float left = hole.xMin - canvas.xMin;
            float right = canvas.xMax - hole.xMax;
            float bottom = hole.yMin - canvas.yMin;
            float top = canvas.yMax - hole.yMax;
            Place(_top.rectTransform, canvas.xMin, hole.yMax, canvas.width, Mathf.Max(0f, top));
            Place(_bottom.rectTransform, canvas.xMin, canvas.yMin, canvas.width, Mathf.Max(0f, bottom));
            Place(_left.rectTransform, canvas.xMin, hole.yMin, Mathf.Max(0f, left), hole.height);
            Place(_right.rectTransform, hole.xMax, hole.yMin, Mathf.Max(0f, right), hole.height);
        }

        void BuildDock(bool atBottom)
        {
            var go = new GameObject("dock", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_page.transform, false);
            _dock = go.GetComponent<RectTransform>();
            _dock.anchorMin = _dock.anchorMax = atBottom ? new Vector2(0f, 0f) : new Vector2(0f, 1f);
            _dock.pivot = new Vector2(0f, atBottom ? 0f : 1f);
            _dock.anchoredPosition = Vector2.zero;
            _dock.sizeDelta = new Vector2(1920f, 250f);
            go.GetComponent<Image>().color = new Color(0.1f, 0.06f, 0.09f, 0.96f);

            var portrait = new GameObject("boss", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(_dock, false);
            var face = portrait.GetComponent<RectTransform>();
            face.anchorMin = face.anchorMax = new Vector2(0f, 0.5f);
            face.pivot = new Vector2(0f, 0.5f);
            face.anchoredPosition = new Vector2(28f, 0f);
            face.sizeDelta = new Vector2(200f, 230f);
            var img = portrait.GetComponent<Image>();
            img.sprite = MoodFace(_steps[_index].mood);
            img.preserveAspect = true;
            img.raycastTarget = false;

            var who = Text(go.transform, "БОСС", 16, new Color(0.96f, 0.78f, 0.22f, 1f));
            Pin(who.rectTransform, 230f, 18f, 400f, 28f);
            _line = Text(go.transform, "", 22, new Color(0.94f, 0.9f, 0.86f, 1f));
            Pin(_line.rectTransform, 230f, 52f, 1280f, 140f);

            if (!_wait)
                DockButton("ДАЛЬШЕ", new Vector2(1540f, 150f), new Vector2(320f, 56f), new Color(0.72f, 0.22f, 0.18f, 1f), Advance);
            var stop = DockButton("ХВАТИТ", new Vector2(1540f, 86f), new Vector2(320f, 48f), new Color(0.22f, 0.16f, 0.2f, 1f), Quit);
            stop.raycastTarget = true;
        }

        void Advance()
        {
            _index++;
            if (_steps == null || _index >= _steps.Length)
            {
                var done = _done;
                Hide();
                done?.Invoke();
                return;
            }

            ShowCurrent();
        }

        void Quit()
        {
            var state = GameSession.State;
            if (state != null)
            {
                state.wantsTutorial = false;
                state.tutorialBeat = DoneBeat;
                GameSession.Save();
            }

            Time.timeScale = 1f;
            Hide();
        }

        Image Shade(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_page.transform, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.02f, 0.01f, 0.03f, 0.78f);
            if (name == "full")
                Stretch(image.rectTransform);
            return image;
        }

        Image DockButton(string label, Vector2 pos, Vector2 size, Color color, Action click)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_dock, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(pos.x, -pos.y);
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = color;
            var text = Text(go.transform, label, 20, Color.white);
            Stretch(text.rectTransform);
            text.alignment = TextAnchor.MiddleCenter;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                Sfx.Play(Cue.Click, 0.3f);
                click();
            });
            return image;
        }

        Text Text(Transform parent, string value, int size, Color color)
        {
            var go = new GameObject("t", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = _font;
            label.text = value;
            label.fontSize = size;
            UiTypography.Apply(label, UiTypography.ForSize(size));
            label.color = color;
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        static void Pin(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
