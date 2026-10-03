using System;
using System.Collections;
using System.Collections.Generic;
using RealityDirector.Events;
using RealityDirector.NPC;
using RealityDirector.UI;
using RealityDirector.Util;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.Cards
{
    // Всё, чем карта показывает себя на площадке: полёт карты, кубики над головой, числа эмоций,
    // плашки «секрет раскрыт», связи между участниками, значки состояний, затемнение комнаты.
    // Текст — на своём экранном канвасе под HUD съёмки, реквизит и связи — спрайтами в мире.
    public class StageFx : MonoBehaviour
    {
        public static StageFx Instance { get; private set; }

        RectTransform _canvas;
        Font _font;
        Sprite _ring;
        Sprite _disc;
        readonly List<Follow> _follows = new List<Follow>();
        readonly Dictionary<NPCController, Badges> _badges = new Dictionary<NPCController, Badges>();

        class Follow
        {
            public RectTransform rect;
            public Func<Vector3> world;
            public Vector2 offset;
            public float until;
            public float born;
            public float rise;
            public CanvasGroup group;
            public bool fade = true;
        }

        public static StageFx Ensure()
        {
            if (Instance != null)
                return Instance;
            var go = new GameObject("StageFx", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Instance = go.AddComponent<StageFx>();
            Instance._canvas = go.GetComponent<RectTransform>();
            Instance._font = UiKit.Body;
            return Instance;
        }

        void OnEnable()
        {
            NPCController.StatShifted += OnStat;
            NPCController.StateChanged += OnState;
        }

        void OnDisable()
        {
            NPCController.StatShifted -= OnStat;
            NPCController.StateChanged -= OnState;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            for (int i = _follows.Count - 1; i >= 0; i--)
            {
                var f = _follows[i];
                if (f.rect == null)
                {
                    _follows.RemoveAt(i);
                    continue;
                }

                Vector3 w;
                try
                {
                    w = f.world();
                }
                catch (Exception)
                {
                    Destroy(f.rect.gameObject);
                    _follows.RemoveAt(i);
                    continue;
                }

                float age = Time.time - f.born;
                Vector3 screen = cam.WorldToScreenPoint(w);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, screen, null, out var local))
                    f.rect.anchoredPosition = local + f.offset + new Vector2(0f, f.rise * age);
                if (f.group != null && f.fade)
                {
                    float left = f.until - Time.time;
                    f.group.alpha = Mathf.Clamp01(left / 0.4f) * Mathf.Clamp01(age / 0.12f);
                }

                if (Time.time >= f.until)
                {
                    Destroy(f.rect.gameObject);
                    _follows.RemoveAt(i);
                }
            }

            foreach (var pair in _badges)
                pair.Value.Tick();
        }

        // ---------- Текст над миром ----------

        RectTransform Track(GameObject go, Func<Vector3> world, Vector2 offset, float life, float rise, bool fade = true)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_canvas, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            var group = go.GetComponent<CanvasGroup>();
            if (group == null)
                group = go.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            _follows.Add(new Follow { rect = rect, world = world, offset = offset, until = Time.time + life, born = Time.time, rise = rise, group = group, fade = fade });
            return rect;
        }

        Text MakeText(Transform parent, string value, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("t", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.text = value;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.02f, 0.04f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        // Всплывающее число: «+25 злость» поднимается над головой и гаснет.
        public Text FloatText(Func<Vector3> world, string text, Color color, int size = 28, float life = 1.9f, Vector2 offset = default)
        {
            var go = new GameObject("float", typeof(RectTransform));
            var t = MakeText(go.transform, text, size, color);
            ((RectTransform)t.transform).sizeDelta = new Vector2(400f, 40f);
            Track(go, world, offset, life, 30f);
            StartCoroutine(Punch(go.transform, 1.35f, 0.18f));
            return t;
        }

        public void FloatText(Vector3 world, string text, Color color, int size = 28, float life = 1.9f)
        {
            FloatText(() => world, text, color, size, life);
        }

        // Плашка поверх участника: «СЕКРЕТ РАСКРЫТ», «ПОСКОЛЬЗНУЛСЯ», «ЗАПЕРТЫ».
        readonly List<(RectTransform rect, Func<Vector3> world)> _banners = new List<(RectTransform, Func<Vector3>)>();

        public void Banner(Func<Vector3> world, string title, string sub, Color color, float life = 3.2f)
        {
            // Новая плашка в том же месте сменяет старую — две друг на друге не читаются.
            Vector3 here = world();
            for (int i = _banners.Count - 1; i >= 0; i--)
            {
                var b = _banners[i];
                if (b.rect == null)
                {
                    _banners.RemoveAt(i);
                    continue;
                }

                Vector3 there;
                try
                {
                    there = b.world();
                }
                catch (Exception)
                {
                    there = new Vector3(9999f, 0f, 0f);
                }

                if (Vector2.Distance(here, there) < 1.6f)
                {
                    Destroy(b.rect.gameObject);
                    _banners.RemoveAt(i);
                }
            }

            var go = new GameObject("banner", typeof(RectTransform), typeof(Image));
            var plate = go.GetComponent<Image>();
            UiKit.Dress(plate, UiKit.Frame.Dialog, 1.4f);
            plate.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            var head = MakeText(go.transform, title, 22, color);
            var body = string.IsNullOrEmpty(sub) ? null : MakeText(go.transform, sub, 15, UiKit.Paper);
            float w = Mathf.Max(head.preferredWidth, body != null ? body.preferredWidth : 0f) + 50f;
            rect.sizeDelta = new Vector2(Mathf.Max(200f, w), body != null ? 64f : 44f);
            ((RectTransform)head.transform).anchoredPosition = new Vector2(0f, body != null ? 10f : 0f);
            if (body != null)
                ((RectTransform)body.transform).anchoredPosition = new Vector2(0f, -14f);
            // Над головой и пузырём реплики — не закрывает самих людей.
            _banners.Add((Track(go, world, new Vector2(0f, 250f), life, 4f), world));
            StartCoroutine(Punch(go.transform, 1.25f, 0.25f));
        }

        // Плашка постановки съёмки: по центру сверху, несколько строк, сама гаснет. Не привязана к людям.
        public void Slate(string title, IList<string> lines, Color color, float life = 6f)
        {
            var go = new GameObject("slate", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(_canvas, false);
            var plate = go.GetComponent<Image>();
            UiKit.Dress(plate, UiKit.Frame.Dialog, 1.4f);
            plate.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            var head = MakeText(go.transform, title, 26, color);
            float w = head.preferredWidth;
            var texts = new List<Text>();
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;
                    var t = MakeText(go.transform, line, 17, texts.Count == 0 ? UiKit.Paper : UiKit.Muted);
                    texts.Add(t);
                    w = Mathf.Max(w, t.preferredWidth);
                }
            }

            float width = Mathf.Min(980f, w + 70f);
            float height = 54f + texts.Count * 24f;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, -150f);
            ((RectTransform)head.transform).anchoredPosition = new Vector2(0f, height * 0.5f - 28f);
            for (int i = 0; i < texts.Count; i++)
                ((RectTransform)texts[i].transform).anchoredPosition = new Vector2(0f, height * 0.5f - 58f - i * 24f);
            StartCoroutine(Punch(go.transform, 1.15f, 0.25f));
            StartCoroutine(FadeOut(go.GetComponent<CanvasGroup>(), life));
        }

        static IEnumerator FadeOut(CanvasGroup group, float life)
        {
            float t = 0f;
            while (t < life && group != null)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Clamp01((life - t) / 0.8f);
                yield return null;
            }

            if (group != null)
                Destroy(group.gameObject);
        }

        // Метка реквизита под ним — живёт, пока жив реквизит.
        public void Label(Transform target, string text, Color color, Vector2 offset)
        {
            var go = new GameObject("label", typeof(RectTransform), typeof(Image));
            var plate = go.GetComponent<Image>();
            UiKit.Dress(plate, UiKit.Frame.Dark);
            plate.color = new Color(1f, 1f, 1f, 0.85f);
            plate.raycastTarget = false;
            var t = MakeText(go.transform, text, 15, color);
            ((RectTransform)go.transform).sizeDelta = new Vector2(t.preferredWidth + 22f, 26f);
            Track(go, () => target.position, offset, float.MaxValue, 0f, false);
        }

        // Подсказка над тем, на кого сейчас наведена карта: прогноз реакции. Одна на сцену, живёт, пока наводят.
        GameObject _hint;
        Text _hintText;
        Transform _hintTarget;

        public void Hint(Transform target, string text)
        {
            if (target == null || string.IsNullOrEmpty(text))
            {
                _hintTarget = null;
                if (_hint != null)
                    _hint.SetActive(false);
                return;
            }

            if (_hint == null)
            {
                _hint = new GameObject("aimHint", typeof(RectTransform), typeof(Image));
                var plate = _hint.GetComponent<Image>();
                UiKit.Dress(plate, UiKit.Frame.Dark);
                plate.color = new Color(1f, 1f, 1f, 0.94f);
                plate.raycastTarget = false;
                _hintText = MakeText(_hint.transform, "", 16, UiKit.Paper);
                _hintText.lineSpacing = 1.05f;
                Track(_hint, () => _hintTarget != null ? _hintTarget.position + Vector3.up * 1.9f : new Vector3(9999f, 0f, 0f), new Vector2(0f, 60f), float.MaxValue, 0f, false);
            }

            _hintTarget = target;
            if (_hintText.text != text)
            {
                _hintText.text = text;
                ((RectTransform)_hint.transform).sizeDelta = new Vector2(Mathf.Max(220f, _hintText.preferredWidth) + 30f, Mathf.Max(30f, _hintText.preferredHeight) + 16f);
            }

            if (!_hint.activeSelf)
                _hint.SetActive(true);
            _hint.transform.SetAsLastSibling();
        }

        // Полёт карты из руки в точку на площадке: видно, куда «ударил» продюсер.
        public IEnumerator CardFly(EventDefinition def, Vector3 world, float duration = 0.42f)
        {
            var cam = Camera.main;
            if (cam == null)
                yield break;
            var go = new GameObject("cardFly", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_canvas, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            var frame = CoreGameplayArt.Sprite(CoreGameplayArt.Frame(def.category));
            img.sprite = frame;
            img.color = frame != null ? Color.white : def.cardColor;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(150f, 202f);
            var art = new GameObject("art", typeof(RectTransform), typeof(Image));
            art.transform.SetParent(go.transform, false);
            var artImg = art.GetComponent<Image>();
            artImg.sprite = def.cardArt != null ? def.cardArt : CoreGameplayArt.Sprite(CoreGameplayArt.CardArt(def.category));
            artImg.preserveAspect = true;
            artImg.raycastTarget = false;
            ((RectTransform)art.transform).sizeDelta = new Vector2(110f, 80f);
            Vector2 from = new Vector2(0f, -470f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, cam.WorldToScreenPoint(world), null, out var to))
                {
                    Vector2 arc = Vector2.Lerp(from, to, k) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 120f);
                    rect.anchoredPosition = arc;
                }

                rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, k);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-14f, 360f, k));
                yield return null;
            }

            Destroy(go);
            FadeBit.Burst(world + Vector3.up * 0.3f, 14, def.cardColor.a > 0.1f ? def.cardColor : UiKit.Gold);
            Ring(world, 1.6f, UiKit.Gold, 0.5f);
        }

        // ---------- Кубики ----------

        // Бросок кубика над головой: крутится, перебирает грани, падает на результат. Подпись — что он решает.
        public IEnumerator Dice(Func<Vector3> world, DieSize die, int result, string label, Color color, int slot, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            var go = new GameObject("die", typeof(RectTransform), typeof(Image));
            var img = go.GetComponent<Image>();
            img.sprite = CoreGameplayArt.Sprite(CoreGameplayArt.Die(die));
            img.color = color;
            img.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(78f, 78f);
            var face = MakeText(go.transform, "?", 34, Color.white);
            var tag = MakeText(go.transform, label, 16, color);
            ((RectTransform)tag.transform).anchoredPosition = new Vector2(0f, -52f);
            // Сбоку от участника, на уровне головы: сверху — реплики, снизу — табличка.
            float x = 105f + slot * 90f;
            Track(go, world, new Vector2(x, 85f), 2.8f, 0f);
            int faces = RealityDirector.Events.Dice.Faces(die);
            float t = 0f;
            float tick = 0f;
            Sfx.Play(Cue.Tick, 0.35f, 1.4f);
            while (t < 0.75f)
            {
                t += Time.deltaTime;
                tick -= Time.deltaTime;
                if (tick <= 0f)
                {
                    tick = 0.06f + t * 0.06f;
                    face.text = UnityEngine.Random.Range(1, faces + 1).ToString();
                }

                rect.localRotation = Quaternion.Euler(0f, 0f, t * 900f * (1f - t / 0.75f));
                rect.localScale = Vector3.one * (0.8f + Mathf.Abs(Mathf.Sin(t * 14f)) * 0.25f);
                yield return null;
            }

            rect.localRotation = Quaternion.identity;
            face.text = result.ToString();
            face.color = UiKit.Gold;
            Sfx.Play(Cue.Coin, 0.4f, 0.8f + result / (float)faces * 0.6f);
            StartCoroutine(Punch(go.transform, 1.5f, 0.22f));
        }

        // ---------- Значки состояний ----------

        void OnState(NPCController npc, string key, bool on)
        {
            if (npc == null)
                return;
            if (!_badges.TryGetValue(npc, out var badges))
            {
                badges = new Badges(npc);
                _badges[npc] = badges;
            }

            badges.Set(key, on);
            if (on)
            {
                string title = StateTitle(key);
                if (title != null)
                {
                    var t = npc.transform;
                    Stacked(t, () => t.position + Vector3.up * 1.25f, title, StateColor(key), 22, 2.2f);
                }
            }
        }

        public static string StateTitle(string key)
        {
            switch (key)
            {
                case NPCController.StateDrunk: return "пьян";
                case NPCController.StateSuspicious: return "подозревает";
                case NPCController.StateAmplified: return "следующее ударит вдвое";
                case NPCController.StateWet: return "промок";
                case NPCController.StateTrapped: return "заперт";
                case NPCController.StateSpotlit: return "в свете софита";
                case NPCController.StateSecretOut: return "секрет раскрыт";
                default: return null;
            }
        }

        static Color StateColor(string key)
        {
            switch (key)
            {
                case NPCController.StateDrunk: return new Color(0.6f, 1f, 0.55f);
                case NPCController.StateSuspicious: return new Color(0.85f, 0.6f, 1f);
                case NPCController.StateAmplified: return new Color(1f, 0.55f, 0.15f);
                case NPCController.StateWet: return new Color(0.5f, 0.8f, 1f);
                case NPCController.StateTrapped: return new Color(1f, 0.35f, 0.3f);
                default: return UiKit.Gold;
            }
        }

        static string StateIcon(string key)
        {
            switch (key)
            {
                case NPCController.StateDrunk: return "Bottle";
                case NPCController.StateSuspicious: return "Eye";
                case NPCController.StateAmplified: return "Bolt";
                case NPCController.StateWet: return "Drop";
                case NPCController.StateTrapped: return "Padlock";
                case NPCController.StateSpotlit: return "Star";
                case NPCController.StateSecretOut: return "Bang";
                default: return null;
            }
        }

        // Ряд значков над головой участника — что с ним сейчас.
        class Badges
        {
            readonly NPCController _npc;
            readonly Dictionary<string, SpriteRenderer> _icons = new Dictionary<string, SpriteRenderer>();

            public Badges(NPCController npc)
            {
                _npc = npc;
            }

            public void Set(string key, bool on)
            {
                if (on)
                {
                    if (_icons.ContainsKey(key))
                        return;
                    string icon = StateIcon(key);
                    if (icon == null)
                        return;
                    var r = SpriteUtil.Show(_npc.transform, "badge_" + key, Vector3.zero, PropArt.Get(icon), 20);
                    SpriteUtil.Fit(r, new Vector2(0.28f, 0.28f * r.sprite.bounds.size.y / Mathf.Max(0.01f, r.sprite.bounds.size.x)));
                    _icons[key] = r;
                }
                else if (_icons.TryGetValue(key, out var r))
                {
                    if (r != null)
                        Destroy(r.gameObject);
                    _icons.Remove(key);
                }
            }

            public void Tick()
            {
                if (_npc == null)
                    return;
                int i = 0;
                int n = _icons.Count;
                foreach (var pair in _icons)
                {
                    if (pair.Value == null)
                        continue;
                    float x = (i - (n - 1) * 0.5f) * 0.34f;
                    pair.Value.transform.localPosition = new Vector3(x, 1.95f + Mathf.Sin(Time.time * 3f + i) * 0.04f, 0f);
                    i++;
                }
            }
        }

        // ---------- Числа эмоций ----------

        void OnStat(NPCController npc, ActorStat stat, int delta, bool subtle)
        {
            if (npc == null || delta == 0)
                return;
            var t = npc.transform;
            // Несколько сдвигов одной эмоции подряд (кубик + реакция) — одно число, а не столбик.
            var key = (t, stat);
            if (_merge.TryGetValue(key, out var open) && open.text != null && Time.time - open.time < 0.7f && Math.Sign(open.total) == Math.Sign(delta))
            {
                int total = open.total + delta;
                open.text.text = Signed(total) + " " + StatName(stat);
                _merge[key] = (open.text, total, Time.time);
                StartCoroutine(Punch(open.text.transform.parent, 1.25f, 0.15f));
                return;
            }

            var text = Stacked(t, () => t.position + Vector3.up * 1.25f, Signed(delta) + " " + StatName(stat), StatColor(stat, delta), subtle ? 19 : 26, subtle ? 1.4f : 1.9f);
            _merge[key] = (text, delta, Time.time);
        }

        readonly Dictionary<(Transform, ActorStat), (Text text, int total, float time)> _merge = new Dictionary<(Transform, ActorStat), (Text, int, float)>();

        static string Signed(int v)
        {
            return (v > 0 ? "+" : "−") + Mathf.Abs(v);
        }

        readonly Dictionary<Transform, (float time, int index)> _stack = new Dictionary<Transform, (float, int)>();

        // Несколько чисел над одним человеком подряд — столбиком, а не друг на друге.
        public Text Stacked(Transform key, Func<Vector3> world, string text, Color color, int size, float life)
        {
            int index = 0;
            if (key != null && _stack.TryGetValue(key, out var last) && Time.time - last.time < 0.9f)
                index = last.index + 1;
            if (key != null)
                _stack[key] = (Time.time, index);
            // Слева от человека: сверху пузыри реплик, справа кубики.
            return FloatText(world, text, color, size, life, new Vector2(-118f, -40f + index * 28f));
        }

        public static string StatName(ActorStat stat)
        {
            switch (stat)
            {
                case ActorStat.Anger: return "злость";
                case ActorStat.Stress: return "стресс";
                case ActorStat.Sadness: return "грусть";
                case ActorStat.Attraction: return "влечение";
                case ActorStat.Confidence: return "уверенность";
                default: return "самоконтроль";
            }
        }

        public static Color StatColor(ActorStat stat, int delta = 1)
        {
            switch (stat)
            {
                case ActorStat.Anger: return new Color(1f, 0.38f, 0.3f);
                case ActorStat.Stress: return new Color(1f, 0.65f, 0.25f);
                case ActorStat.Sadness: return new Color(0.48f, 0.72f, 1f);
                case ActorStat.Attraction: return new Color(1f, 0.5f, 0.78f);
                case ActorStat.Confidence: return UiKit.Gold;
                default: return delta >= 0 ? new Color(0.45f, 0.9f, 0.85f) : new Color(0.75f, 0.5f, 1f);
            }
        }

        // ---------- Мир: кольца, значки, связи, комнаты ----------

        Sprite RingSprite()
        {
            if (_ring != null)
                return _ring;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.92f) * 14f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

            tex.Apply();
            _ring = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / 2f);
            _ring.hideFlags = HideFlags.HideAndDontSave;
            return _ring;
        }

        // Мягкий диск: центр полупрозрачный, к краю гуще — зона ауры на полу.
        public Sprite DiscSprite()
        {
            if (_disc != null)
                return _disc;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = d > 1f ? 0f : Mathf.Lerp(0.25f, 0.7f, d * d) * Mathf.Clamp01((1f - d) * 20f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

            tex.Apply();
            _disc = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / 2f);
            _disc.hideFlags = HideFlags.HideAndDontSave;
            return _disc;
        }

        public Sprite Ring01 => RingSprite();

        // Волна от точки (звонок колокола, сирена, удар карты).
        public void Ring(Vector3 at, float radius, Color color, float life)
        {
            StartCoroutine(RingRoutine(at, radius, color, life));
        }

        IEnumerator RingRoutine(Vector3 at, float radius, Color color, float life)
        {
            var r = SpriteUtil.Show(null, "wave", at, RingSprite(), 17);
            float t = 0f;
            while (t < life)
            {
                t += Time.deltaTime;
                float k = t / life;
                r.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, radius, Mathf.Sqrt(k));
                var c = color;
                c.a = (1f - k) * color.a;
                r.color = c;
                yield return null;
            }

            Destroy(r.gameObject);
        }

        // Значок вылетает и тает: ноты над певцом, сердечки, звёзды после падения, капли.
        public void Icon(Vector3 at, string key, Vector3 velocity, float life = 1.2f, float size = 0.32f, Color? tint = null)
        {
            StartCoroutine(IconRoutine(at, key, velocity, life, size, tint ?? Color.white));
        }

        IEnumerator IconRoutine(Vector3 at, string key, Vector3 velocity, float life, float size, Color tint)
        {
            var r = SpriteUtil.Show(null, "icon", at, PropArt.Get(key), 19);
            float aspect = r.sprite.bounds.size.y / Mathf.Max(0.01f, r.sprite.bounds.size.x);
            SpriteUtil.Fit(r, new Vector2(size, size * aspect));
            Vector3 baseScale = r.transform.localScale;
            float t = 0f;
            while (t < life)
            {
                t += Time.deltaTime;
                r.transform.position += velocity * Time.deltaTime;
                r.transform.localScale = baseScale * (1f + Mathf.Sin(t * 10f) * 0.06f);
                var c = tint;
                c.a = Mathf.Clamp01((life - t) / (life * 0.4f));
                r.color = c;
                yield return null;
            }

            Destroy(r.gameObject);
        }

        // Связь между двумя участниками: вражда — молнии, симпатия — сердечки, мир — рукопожатие.
        public void Link(NPCController a, NPCController b, string iconKey, Color color, float life)
        {
            if (a == null || b == null)
                return;
            StartCoroutine(LinkRoutine(a, b, iconKey, color, life));
        }

        IEnumerator LinkRoutine(NPCController a, NPCController b, string iconKey, Color color, float life)
        {
            var line = SpriteUtil.Box(null, "link", Vector3.zero, Vector2.one, color, 17);
            const int count = 5;
            var icons = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                icons[i] = SpriteUtil.Show(null, "linkIcon", Vector3.zero, PropArt.Get(iconKey), 18);
                float aspect = icons[i].sprite.bounds.size.y / Mathf.Max(0.01f, icons[i].sprite.bounds.size.x);
                SpriteUtil.Fit(icons[i], new Vector2(0.26f, 0.26f * aspect));
            }

            float t = 0f;
            while (t < life && a != null && b != null)
            {
                t += Time.deltaTime;
                Vector3 pa = a.transform.position + Vector3.up * 0.75f;
                Vector3 pb = b.transform.position + Vector3.up * 0.75f;
                Vector3 d = pb - pa;
                float fade = Mathf.Clamp01((life - t) / 0.5f) * Mathf.Clamp01(t / 0.2f);
                line.transform.position = (pa + pb) * 0.5f;
                line.transform.localScale = new Vector3(d.magnitude, 0.045f, 1f);
                line.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                var lc = color;
                lc.a = 0.55f * fade;
                line.color = lc;
                for (int i = 0; i < count; i++)
                {
                    float k = Mathf.Repeat(i / (float)count + t * 0.45f, 1f);
                    float wobble = iconKey == "Bolt" ? Mathf.Sin(t * 30f + i) * 0.08f : Mathf.Sin(t * 4f + i) * 0.06f;
                    icons[i].transform.position = Vector3.Lerp(pa, pb, k) + new Vector3(0f, wobble, 0f);
                    var ic = Color.white;
                    ic.a = fade * Mathf.Sin(k * Mathf.PI);
                    icons[i].color = ic;
                }

                yield return null;
            }

            Destroy(line.gameObject);
            for (int i = 0; i < count; i++)
                Destroy(icons[i].gameObject);
        }

        // Заливка комнаты цветом (тихая — голубая, запертая — красная, свет выключили — тьма).
        public SpriteRenderer Shade(Rect area, Color color, int order, float life)
        {
            var r = SpriteUtil.Box(null, "shade", area.center, area.size, color, order);
            if (life > 0f)
                StartCoroutine(FadeOut(r, life));
            return r;
        }

        IEnumerator FadeOut(SpriteRenderer r, float life)
        {
            Color c = r.color;
            float t = 0f;
            while (t < life && r != null)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((life - t) / 0.5f);
                r.color = new Color(c.r, c.g, c.b, c.a * a);
                yield return null;
            }

            if (r != null)
                Destroy(r.gameObject);
        }

        // Дождь из спринклеров над прямоугольником.
        public void Rain(Rect area, float seconds)
        {
            StartCoroutine(RainRoutine(area, seconds));
        }

        IEnumerator RainRoutine(Rect area, float seconds)
        {
            float t = 0f;
            Sfx.Play(Cue.Splash, 0.6f, 0.8f);
            while (t < seconds)
            {
                t += Time.deltaTime;
                for (int i = 0; i < 3; i++)
                {
                    var at = new Vector3(UnityEngine.Random.Range(area.xMin, area.xMax), area.yMax + 0.4f, 0f);
                    FadeBit.Spawn(at, new Vector3(0f, -9f, 0f), new Color(0.55f, 0.8f, 1f, 0.85f), 0.45f, 0.06f);
                }

                yield return null;
            }
        }

        public void Confetti(Vector3 at, int count = 40)
        {
            Color[] palette = { new Color(1f, 0.3f, 0.5f), new Color(1f, 0.85f, 0.2f), new Color(0.35f, 0.85f, 1f), new Color(0.5f, 1f, 0.45f), Color.white };
            for (int i = 0; i < count; i++)
            {
                var v = new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(2.5f, 6f), 0f);
                FadeBit.Spawn(at, v, palette[i % palette.Length], UnityEngine.Random.Range(0.8f, 1.4f), UnityEngine.Random.Range(0.06f, 0.12f));
            }
        }

        // Вонь: зелёные клубы, поднимаются и тают.
        public void Stink(Vector3 at, float spread)
        {
            var r = SpriteUtil.Show(null, "stink", at + new Vector3(UnityEngine.Random.Range(-spread, spread), UnityEngine.Random.Range(-0.2f, 0.4f), 0f), PropArt.Get("Stink"), 16);
            SpriteUtil.Fit(r, new Vector2(0.8f, 0.55f));
            StartCoroutine(StinkRoutine(r));
        }

        IEnumerator StinkRoutine(SpriteRenderer r)
        {
            float t = 0f;
            float life = UnityEngine.Random.Range(1.6f, 2.4f);
            Vector3 drift = new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0.35f, 0f);
            Vector3 baseScale = r.transform.localScale;
            while (t < life && r != null)
            {
                t += Time.deltaTime;
                r.transform.position += drift * Time.deltaTime;
                r.transform.localScale = baseScale * (1f + t * 0.4f);
                r.color = new Color(0.55f, 0.85f, 0.2f, 0.55f * Mathf.Sin(Mathf.Clamp01(t / life) * Mathf.PI));
                yield return null;
            }

            if (r != null)
                Destroy(r.gameObject);
        }

        static IEnumerator Punch(Transform t, float scale, float time)
        {
            float k = 0f;
            Vector3 rest = Vector3.one;
            while (k < time && t != null)
            {
                k += Time.deltaTime;
                float s = Mathf.Lerp(scale, 1f, Mathf.SmoothStep(0f, 1f, k / time));
                t.localScale = rest * s;
                yield return null;
            }

            if (t != null)
                t.localScale = rest;
        }

        public void Clear()
        {
            for (int i = 0; i < _follows.Count; i++)
            {
                if (_follows[i].rect != null)
                    Destroy(_follows[i].rect.gameObject);
            }

            _follows.Clear();
        }
    }
}
