using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI
{
    // Общий визуальный язык меты: адское ТВ — чернила, кровь, золото. Спрайты из пака интро (Art/Intro/vn)
    // и Core Gameplay (Art/UI/CoreGameplay). Экраны сцены одеваются кодом при запуске — сцену не трогаем.
    public static class UiKit
    {
        public static readonly Color Ink = new Color(0.055f, 0.027f, 0.047f, 1f);
        public static readonly Color Panel = new Color(0.10f, 0.055f, 0.08f, 0.94f);
        public static readonly Color PanelHi = new Color(0.17f, 0.09f, 0.12f, 0.96f);
        public static readonly Color Gold = new Color(0.95f, 0.76f, 0.36f, 1f);
        public static readonly Color GoldDim = new Color(0.62f, 0.47f, 0.22f, 1f);
        public static readonly Color Blood = new Color(0.78f, 0.12f, 0.16f, 1f);
        public static readonly Color Ember = new Color(1f, 0.45f, 0.17f, 1f);
        public static readonly Color Paper = new Color(0.97f, 0.92f, 0.86f, 1f);
        public static readonly Color Muted = new Color(0.74f, 0.64f, 0.66f, 1f);
        public static readonly Color Faint = new Color(0.5f, 0.41f, 0.44f, 1f);
        public static readonly Color Good = new Color(0.45f, 0.86f, 0.52f, 1f);

        public enum Frame
        {
            Gold,    // тёмная панель с золотой рамкой (input_panel)
            Dialog,  // тёмная плашка с тонкой золотой кромкой (dialog_box)
            Dark,    // простая тёмная (panel_dark)
            Actor,   // тёмная сине-фиолетовая (panel_actor)
            GoldTile,
            RedTile,
            TealTile
        }

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static Font _display;
        static Sprite _vignette;
        static Sprite _halo;
        static Sprite _circle;
        static readonly Dictionary<int, Sprite> Gradients = new Dictionary<int, Sprite>();

        public static Sprite Load(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (Cache.TryGetValue(path, out var s) && s != null)
                return s;
            s = Resources.Load<Sprite>(path);
            Cache[path] = s;
            return s;
        }

        public static Sprite Vn(string name) => Load("Art/Intro/vn/" + name);
        public static Sprite Icon(string name) => Load("Art/UI/CoreGameplay/UI/Icons/" + name);

        // Иконка тона шоу из пака.
        public static Sprite MoodIcon(ShowMood mood)
        {
            switch (mood)
            {
                case ShowMood.Drama: return Icon("icon_drama");
                case ShowMood.Trash: return Icon("icon_trash");
                default: return Icon("icon_family");
            }
        }

        // Шрифт логотипа (Metal Mania) — берём у любого текста сцены, где он уже стоит.
        public static Font Display
        {
            get
            {
                if (_display != null)
                    return _display;
                foreach (var t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
                {
                    if (t.font != null && t.font.name.StartsWith("MetalMania"))
                    {
                        _display = t.font;
                        break;
                    }
                }

                return _display;
            }
            set => _display = value;
        }

        public static Font Body
        {
            get
            {
                var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        // Рамка панели. scale — во сколько раз тоньше родной рамки спрайта.
        public static void Dress(Image image, Frame frame, float scale = 1f)
        {
            if (image == null)
                return;
            Sprite sprite;
            float mult;
            switch (frame)
            {
                case Frame.Gold: sprite = Vn("input_panel"); mult = 3.2f; break;
                case Frame.Dialog: sprite = Vn("dialog_box"); mult = 2.6f; break;
                case Frame.Actor: sprite = Load("Art/UI/CoreGameplay/UI/Panels/panel_actor_9slice"); mult = 1f; break;
                case Frame.GoldTile: sprite = Load("Art/UI/CoreGameplay/UI/Panels/panel_gold_9slice"); mult = 1f; break;
                case Frame.RedTile: sprite = Load("Art/UI/CoreGameplay/UI/Panels/panel_red_9slice"); mult = 1f; break;
                case Frame.TealTile: sprite = Load("Art/UI/CoreGameplay/UI/Panels/panel_token_9slice"); mult = 1f; break;
                default: sprite = Load("Art/UI/CoreGameplay/UI/Panels/panel_dark_9slice"); mult = 1f; break;
            }

            if (sprite == null)
                return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = mult * scale;
            image.color = Color.white;
        }

        // Рамка + плотная тёмная заливка внутри: арт за панелью не просвечивает сквозь текст.
        public static void DressSolid(Image image, Frame frame, float inset = 7f, float scale = 1f)
        {
            if (image == null)
                return;
            Dress(image, frame, scale);
            if (image.transform.Find("Solid") != null)
                return;
            var fill = Img("Solid", image.transform, null, new Color(0.07f, 0.035f, 0.055f, 0.93f));
            fill.transform.SetAsFirstSibling();
            Stretch(fill.rectTransform, inset);
        }

        // Главная кнопка: красная с золотом из пака интро, со сменой спрайта на наведение/нажатие.
        public static void Primary(Button button, int fontSize = 0)
        {
            if (button == null)
                return;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            var normal = Vn("button_normal");
            if (image == null || normal == null)
                return;
            image.sprite = normal;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2.2f;
            image.color = Color.white;
            // Прежний переход цветом оставляет тинт на рендерере — при смене спрайтов он бы так и висел.
            image.canvasRenderer.SetColor(Color.white);
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = Vn("button_hover"),
                selectedSprite = normal,
                pressedSprite = Vn("button_pressed"),
                disabledSprite = Vn("button_disabled")
            };
            Label(button, Paper, fontSize, true);
        }

        // Второстепенная: тёмная с тонкой кромкой, при наведении светлеет.
        public static void Secondary(Button button, int fontSize = 0)
        {
            if (button == null)
                return;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null)
                return;
            Dress(image, Frame.Dialog, 1.6f);
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(0.86f, 0.82f, 0.84f, 1f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = colors.normalColor;
            colors.pressedColor = new Color(0.7f, 0.6f, 0.6f, 1f);
            colors.disabledColor = new Color(0.5f, 0.45f, 0.48f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            Label(button, Gold, fontSize, true);
        }

        static void Label(Button button, Color color, int fontSize, bool shadow)
        {
            var text = button.GetComponentInChildren<Text>(true);
            if (text == null)
                return;
            text.color = color;
            text.fontStyle = FontStyle.Bold;
            if (fontSize > 0)
                text.fontSize = fontSize;
            if (shadow)
                Shadow(text);
        }

        public static void Shadow(Graphic graphic, float distance = 2f, float alpha = 0.75f)
        {
            if (graphic == null || graphic.GetComponent<Shadow>() != null)
                return;
            var s = graphic.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, alpha);
            s.effectDistance = new Vector2(distance, -distance);
        }

        public static void Glow(Graphic graphic, Color color, float distance = 2f)
        {
            if (graphic == null || graphic.GetComponent<Outline>() != null)
                return;
            var o = graphic.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(distance, -distance);
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static Image Img(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Txt(string name, Transform parent, string value, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft, Font font = null)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font != null ? font : Body;
            t.text = value;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        // Полноэкранный арт за экраном: обрезка uv (без вшитых плашек), заливка с сохранением пропорций,
        // медленный наезд камеры и затемнение градиентами, чтобы текст читался.
        public static RawImage Backdrop(RectTransform screen, string texturePath, Rect uv, float dim = 0.55f)
        {
            if (screen == null)
                return null;
            var existing = screen.Find("KitBackdrop");
            if (existing != null)
                return existing.GetComponentInChildren<RawImage>();
            var tex = Resources.Load<Texture2D>(texturePath);
            if (tex == null)
                return null;

            var holder = Rect("KitBackdrop", screen);
            holder.SetSiblingIndex(0);
            Stretch(holder);
            var mask = holder.gameObject.AddComponent<RectMask2D>();
            mask.enabled = true;

            var art = Rect("Art", holder);
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
            var raw = art.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.uvRect = uv;
            raw.raycastTarget = false;
            var fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = tex.width * uv.width / Mathf.Max(1f, tex.height * uv.height);
            art.gameObject.AddComponent<KenBurns>();

            var shade = Img("Shade", holder, null, new Color(0.04f, 0.01f, 0.03f, dim));
            Stretch(shade.rectTransform);
            var bottom = Img("ShadeBottom", holder, VerticalGradient(), new Color(0.04f, 0.01f, 0.03f, 0.95f));
            Stretch(bottom.rectTransform);
            var vignette = Img("Vignette", holder, Vignette(), new Color(0f, 0f, 0f, 0.85f));
            Stretch(vignette.rectTransform);
            return raw;
        }

        // Прозрачно сверху → цвет снизу. Для подложек под текст и низа экрана.
        public static Sprite VerticalGradient()
        {
            const int key = 1;
            if (Gradients.TryGetValue(key, out var s) && s != null)
                return s;
            var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 64; y++)
            {
                float t = y / 63f;
                float a = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(t * 1.9f));
                tex.SetPixel(0, y, new Color(1f, 1f, 1f, a));
            }

            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
            s.hideFlags = HideFlags.HideAndDontSave;
            Gradients[key] = s;
            return s;
        }

        // Затемнение по краям.
        public static Sprite Vignette()
        {
            if (_vignette != null)
                return _vignette;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - n / 2f) / (n / 2f);
                    float dy = (y - n / 2f) / (n / 2f);
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.35f, d));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply();
            _vignette = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
            _vignette.hideFlags = HideFlags.HideAndDontSave;
            return _vignette;
        }

        // Мягкое пятно света: ярко в центре, к краям в ноль.
        public static Sprite Halo()
        {
            if (_halo != null)
                return _halo;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f - n / 2f) / (n / 2f);
                    float dy = (y + 0.5f - n / 2f) / (n / 2f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }

            tex.Apply();
            _halo = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
            _halo.hideFlags = HideFlags.HideAndDontSave;
            return _halo;
        }

        // Белый круг со сглаженным краем — аватарки, точки, бейджи.
        public static Sprite Circle()
        {
            if (_circle != null)
                return _circle;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - n / 2f, dy = y + 0.5f - n / 2f;
                    float a = Mathf.Clamp01(n / 2f - Mathf.Sqrt(dx * dx + dy * dy));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
            _circle.hideFlags = HideFlags.HideAndDontSave;
            return _circle;
        }

        // Ряд ромбиков уровня: закрашено filled из total.
        public static RectTransform Pips(Transform parent, string name, int filled, int total, Color on, float size = 12f, float gap = 6f)
        {
            var row = parent.Find(name) as RectTransform;
            if (row == null)
            {
                row = Rect(name, parent);
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = gap;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.childAlignment = TextAnchor.MiddleLeft;
            }

            while (row.childCount < total)
            {
                var pip = Img("Pip", row, null, Color.white);
                pip.rectTransform.sizeDelta = new Vector2(size, size);
                pip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            for (int i = 0; i < row.childCount; i++)
            {
                var pip = row.GetChild(i).GetComponent<Image>();
                pip.gameObject.SetActive(i < total);
                pip.color = i < filled ? on : new Color(1f, 1f, 1f, 0.22f);
            }

            return row;
        }

        // Плавно пульсирует — для главного призыва к действию.
        public static Breathe Pulse(Component target, float amount = 0.035f, float speed = 2.2f)
        {
            if (target == null)
                return null;
            var b = target.GetComponent<Breathe>();
            if (b == null)
                b = target.gameObject.AddComponent<Breathe>();
            b.amount = amount;
            b.speed = speed;
            return b;
        }
    }

    // Медленный наезд и дрейф фона — экран живой, даже когда никто ничего не жмёт.
    public class KenBurns : MonoBehaviour
    {
        public float zoom = 0.06f;
        public float period = 28f;
        public Vector2 drift = new Vector2(18f, 8f);
        RectTransform _rt;
        float _t;

        void Awake()
        {
            _rt = transform as RectTransform;
            _t = Random.value * period;
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            float k = 0.5f - 0.5f * Mathf.Cos(_t / period * Mathf.PI * 2f);
            _rt.localScale = Vector3.one * (1.04f + zoom * k);
            _rt.anchoredPosition = new Vector2(drift.x * (k - 0.5f), drift.y * Mathf.Sin(_t / period * Mathf.PI * 2f));
        }
    }

    // Дыхание кнопки: пульсирующее свечение позади неё. Масштаб не трогает — его ведёт наведение.
    public class Breathe : MonoBehaviour
    {
        public float amount = 0.035f;
        public float speed = 2.2f;
        public Color color = new Color(1f, 0.42f, 0.12f, 1f);
        Image _glow;
        Selectable _sel;

        void OnEnable()
        {
            _sel = GetComponent<Selectable>();
            if (_glow == null && transform.parent != null)
            {
                var rt = UiKit.Rect(name + "Glow", transform.parent);
                var le = rt.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = true;
                _glow = rt.gameObject.AddComponent<Image>();
                _glow.sprite = UiKit.Halo();
                _glow.raycastTarget = false;
                _glow.color = Color.clear;
            }

            if (_glow != null)
                _glow.gameObject.SetActive(true);
        }

        void OnDisable()
        {
            if (_glow != null)
                _glow.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (_glow != null)
                Destroy(_glow.gameObject);
        }

        void LateUpdate()
        {
            if (_glow == null)
                return;
            var me = (RectTransform)transform;
            var rt = _glow.rectTransform;
            // Свечение рисуется до кнопки — значит, позади неё.
            if (rt.GetSiblingIndex() > transform.GetSiblingIndex())
                rt.SetSiblingIndex(transform.GetSiblingIndex());
            // Тот же прямоугольник, что у кнопки, расширенный поровну во все стороны (при любых якорях и опоре).
            Vector2 size = me.rect.size;
            Vector2 extra = new Vector2(size.x * 0.25f + 30f, size.y * 0.9f + 20f) * me.localScale.x;
            rt.anchorMin = me.anchorMin;
            rt.anchorMax = me.anchorMax;
            rt.pivot = me.pivot;
            rt.sizeDelta = me.sizeDelta + extra;
            rt.anchoredPosition = me.anchoredPosition + Vector2.Scale(me.pivot - new Vector2(0.5f, 0.5f), extra);
            bool live = (_sel == null || _sel.interactable) && gameObject.activeInHierarchy;
            float s = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed);
            var c = color;
            c.a = live ? 0.16f + 0.24f * s : 0f;
            _glow.color = c;
        }
    }
}
