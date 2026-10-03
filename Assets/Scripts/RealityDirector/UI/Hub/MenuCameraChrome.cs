using UnityEngine;
using UnityEngine.UI;
using RealityDirector.Util;

namespace RealityDirector.UI.Hub
{
        // Фон — плотный снег старого ТВ. Меню на вертикальной карточке. Рамка — жирный простой видоискатель.
        public class MenuCameraChrome : MonoBehaviour
        {
            const int Width = 192;
            const int Height = 108;
            const float Step = 0.08f;
        const float PlateW = 640f;
        const float PlateH = 900f;

        static readonly Color Ink = new Color(0.07f, 0.02f, 0.01f, 1f);
        static readonly Color Paper = new Color(0.93f, 0.9f, 0.82f, 0.96f);
        static readonly Color Rec = new Color(0.92f, 0.16f, 0.16f, 1f);

        Texture2D _tex;
        Color32[] _px;
            Image _plate;
            Image _rec;
            Text _clock;
            float _next;
            float _run;
            uint _seed = 1;

        public static void Mount(MainMenuView menu)
        {
            if (menu == null || menu.GetComponent<MenuCameraChrome>() != null)
                return;
            menu.gameObject.AddComponent<MenuCameraChrome>();
        }

        void Awake()
        {
            _plate = GetComponent<Image>();
            _tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Point;
            _tex.wrapMode = TextureWrapMode.Clamp;
            _tex.hideFlags = HideFlags.HideAndDontSave;
            _px = new Color32[Width * Height];
            Paint();
            var sprite = Sprite.Create(_tex, new Rect(0f, 0f, Width, Height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            if (_plate != null)
            {
                _plate.sprite = sprite;
                _plate.type = Image.Type.Simple;
                _plate.color = Color.white;
            }

            BuildPlate();
            var hud = new GameObject("Viewfinder", typeof(RectTransform));
            hud.transform.SetParent(transform, false);
            Stretch(hud.GetComponent<RectTransform>());
            BuildScreen(hud.transform);
            DressButtons();
            var version = transform.Find("Version") as RectTransform;
            if (version != null)
                version.anchoredPosition = new Vector2(150f, 40f);
            hud.transform.SetAsLastSibling();
        }

        void Update()
        {
            _run += Time.unscaledDeltaTime;
            if (_clock != null)
            {
                int s = (int)_run;
                _clock.text = (s / 3600).ToString("00") + ":" + ((s / 60) % 60).ToString("00") + ":" + (s % 60).ToString("00");
            }

            if (_rec != null)
            {
                var c = Rec;
                c.a = Mathf.Repeat(Time.unscaledTime, 1.6f) < 1.05f ? 1f : 0.25f;
                _rec.color = c;
            }

            if (Time.unscaledTime < _next)
                return;
            _next = Time.unscaledTime + Step;
            _seed = _seed * 1664525u + 1013904223u;
            Paint();
        }

        void OnDestroy()
        {
            if (_tex != null)
                Destroy(_tex);
        }

        void Paint()
        {
            int tear = (int)(_seed % (uint)Height);
            for (int y = 0; y < Height; y++)
            {
                bool scan = (y & 1) == 0;
                int band = y - tear;
                if (band < 0)
                    band += Height;
                bool tearRow = band < 4;
                for (int x = 0; x < Width; x++)
                {
                    uint h = Mix(_seed ^ (uint)(x * 374761393 + y * 668265263));
                    int spark = (int)((h >> 1) & 63);
                    int v = (h & 1) == 0 ? spark / 4 : 188 + spark;
                    if (scan)
                        v = v * 3 / 5;
                    if (tearRow)
                        v = Mathf.Min(255, v + 110);
                    _px[y * Width + x] = new Color32((byte)v, (byte)v, (byte)v, 255);
                }
            }

            _tex.SetPixels32(_px);
            _tex.Apply(false);
        }

        static uint Mix(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            x ^= x >> 16;
            return x;
        }

        void BuildPlate()
        {
            var shadow = Block("plateShadow", new Color(0f, 0f, 0f, 0.5f));
            shadow.transform.SetParent(transform, false);
            PlaceCenter(shadow.rectTransform, new Vector2(14f, -6f), new Vector2(PlateW, PlateH));
            shadow.raycastTarget = false;

            var plate = Block("InfoPlate", new Color(0.08f, 0.05f, 0.07f, 0.96f));
            plate.transform.SetParent(transform, false);
            PlaceCenter(plate.rectTransform, new Vector2(0f, 10f), new Vector2(PlateW, PlateH));
            plate.raycastTarget = false;
            Rim(plate.transform, 10f, Ink);

            Fit("Title", plate.transform, new Vector2(0f, 300f), new Vector2(540f, 200f));
            Fit("Subtitle", plate.transform, new Vector2(0f, 140f), new Vector2(540f, 72f));
            Fit("Buttons", plate.transform, new Vector2(0f, -150f), new Vector2(500f, 360f));
            shadow.transform.SetAsFirstSibling();
        }

        void Fit(string childName, Transform plate, Vector2 pos, Vector2 size)
        {
            var child = transform.Find(childName) as RectTransform;
            if (child == null)
                return;
            child.SetParent(plate, false);
            child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
            child.pivot = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = pos;
            child.sizeDelta = size;
        }

        void BuildScreen(Transform hud)
        {
            float arm = 112f;
            float thick = 12f;
            float inset = 26f;
            Corner(hud, new Vector2(0f, 1f), arm, thick, inset, Paper);
            Corner(hud, new Vector2(1f, 1f), arm, thick, inset, Paper);
            Corner(hud, new Vector2(0f, 0f), arm, thick, inset, Paper);
            Corner(hud, new Vector2(1f, 0f), arm, thick, inset, Paper);
            Tick(hud, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -inset), new Vector2(36f, thick), Paper);
            Tick(hud, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, inset), new Vector2(36f, thick), Paper);
            Tick(hud, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(inset, 0f), new Vector2(thick, 36f), Paper);
            Tick(hud, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-inset, 0f), new Vector2(thick, 36f), Paper);

            _rec = Tick(hud, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(156f, -40f), new Vector2(18f, 18f), Rec);
            var rec = Label(hud, "REC", 22, Rec);
            Pin(rec.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(182f, -32f), new Vector2(90f, 32f));
            var cam = Label(hud, "CAM 1", 22, Paper);
            Pin(cam.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(280f, -32f), new Vector2(140f, 32f));
            _clock = Label(hud, "00:00:00", 24, Paper);
            _clock.alignment = TextAnchor.MiddleRight;
            var clock = _clock.rectTransform;
            clock.anchorMin = clock.anchorMax = new Vector2(1f, 1f);
            clock.pivot = new Vector2(1f, 1f);
            clock.anchoredPosition = new Vector2(-150f, -30f);
            clock.sizeDelta = new Vector2(220f, 36f);
            var standby = Label(hud, "STBY", 20, Paper);
            standby.alignment = TextAnchor.MiddleRight;
            var st = standby.rectTransform;
            st.anchorMin = st.anchorMax = new Vector2(1f, 0f);
            st.pivot = new Vector2(1f, 0f);
            st.anchoredPosition = new Vector2(-150f, 34f);
            st.sizeDelta = new Vector2(120f, 30f);
        }

        void DressButtons()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                var image = button.GetComponent<Image>();
                if (image == null)
                    continue;
                image.sprite = SpriteUtil.White;
                image.type = Image.Type.Simple;
                image.color = Color.white;
                var colors = button.colors;
                colors.normalColor = new Color(0.9f, 0.3f, 0.16f, 1f);
                colors.highlightedColor = new Color(1f, 0.52f, 0.2f, 1f);
                colors.pressedColor = new Color(0.68f, 0.18f, 0.1f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.32f, 0.26f, 0.26f, 0.6f);
                colors.fadeDuration = 0.08f;
                button.colors = colors;
                var rim = new GameObject("rim", typeof(RectTransform));
                rim.transform.SetParent(button.transform, false);
                rim.transform.SetAsFirstSibling();
                var rimRect = rim.GetComponent<RectTransform>();
                Stretch(rimRect);
                rimRect.offsetMin = new Vector2(-6f, -6f);
                rimRect.offsetMax = new Vector2(6f, 6f);
                Rim(rim.transform, 6f, Ink);
            }
        }

        static void PlaceCenter(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        static void Rim(Transform parent, float thick, Color color)
        {
            Bar(parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, thick), color);
            Bar(parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, thick), color);
            Bar(parent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(thick, 0f), color);
            Bar(parent, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(thick, 0f), color);
        }

        static void Corner(Transform parent, Vector2 anchor, float arm, float thick, float inset, Color color)
        {
            float sx = anchor.x < 0.5f ? 1f : -1f;
            float sy = anchor.y < 0.5f ? 1f : -1f;
            var pos = new Vector2(sx * inset, sy * inset);
            var h = Tick(parent, anchor, anchor, pos, new Vector2(arm, thick), color);
            var v = Tick(parent, anchor, anchor, pos, new Vector2(thick, arm), color);
            h.rectTransform.pivot = anchor;
            v.rectTransform.pivot = anchor;
        }

        static Image Tick(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var image = Block("tick", color);
            image.transform.SetParent(parent, false);
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            image.raycastTarget = false;
            return image;
        }

        static Image Bar(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var image = Block("bar", color);
            image.transform.SetParent(parent, false);
            image.transform.SetAsFirstSibling();
            var rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            image.raycastTarget = false;
            return image;
        }

        static Image Block(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var image = go.GetComponent<Image>();
            image.sprite = SpriteUtil.White;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text Label(Transform parent, string value, int size, Color color)
        {
            var go = new GameObject("t", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            return text;
        }

        static void Pin(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
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
