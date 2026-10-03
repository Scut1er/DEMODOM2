using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Events;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI
{
    // Лицо карты — одно на всю игру: рука на съёмке, колода и магазин хаба, полёт сыгранной карты.
    // Слои снизу вверх: подложка → арт (обрезка без растяжения) → плашки и текст → рамка художницы
    // → свечение наведения/выбора, затемнение недоступной. Весь текст — из данных карты, в PNG его нет.
    // Раскладка — в пикселях рамки 1188×2048 (якоря в долях), шрифт — от ширины карты.
    public class CardFace : MonoBehaviour
    {
        const float W = 1188f;
        const float H = 2048f;

        // Раскладка в пикселях рамки (x0, y0, x1, y1 от левого верхнего угла) — общая для игры и превью в редакторе.
        public static readonly Vector4 BackBox = new Vector4(76, 66, 1112, 1980);
        public static readonly Vector4 CategoryBox = new Vector4(300, 250, 888, 400);
        public static readonly Vector4 TitlePlateBox = new Vector4(110, 408, 1078, 602);
        public static readonly Vector4 TitleBox = new Vector4(132, 414, 1056, 596);
        public static readonly Vector4 ArtBox = new Vector4(128, 612, 1060, 1132);
        public static readonly Vector4 CostBox = new Vector4(108, 1078, 488, 1206);
        public static readonly Vector4 DiceBox = new Vector4(700, 1078, 1080, 1206);
        public static readonly Vector4 TextPlateBox = new Vector4(128, 1220, 1060, 1562);
        public static readonly Vector4 TextBox = new Vector4(160, 1234, 1028, 1548);
        // Между «крыльями» низа рамки — подсказка по центру, короткая.
        public static readonly Vector4 HintBox = new Vector4(236, 1570, 952, 1660);

        public static Vector2 SizeFor(float width) => new Vector2(width, Mathf.Round(width / CardVisuals.Aspect));

        // Область раскладки внутри прямоугольника карты (для IMGUI: y вниз).
        public static Rect Within(Rect card, Vector4 box)
        {
            return new Rect(card.x + card.width * box.x / W, card.y + card.height * box.y / H,
                card.width * (box.z - box.x) / W, card.height * (box.w - box.y) / H);
        }

        RectTransform _rect;
        Image _back;
        Image _frame;
        Text _category;
        Text _title;
        RectTransform _window;
        Image _art;
        AspectRatioFitter _artFit;
        Image _fallbackShade;
        Image _fallbackIcon;
        Image _cost;
        Text _costText;
        Image _dice;
        Image _dieIcon;
        Text _diceText;
        Text _body;
        Text _hint;
        Image _hotkey;
        Text _hotkeyText;
        Image _banner;
        Text _bannerText;
        Image _hoverGlow;
        Image _selectedGlow;
        Image _disabled;
        readonly Image[] _moods = new Image[2];
        Color _accent = Color.white;
        float _lastWidth = -1f;
        Vector2 _home;
        bool _homeSet;
        float _raise;
        float _raiseTarget;

        public RectTransform Rect => _rect;
        // Подложка ловит клики и наведение по всей карте.
        public Image Back => _back;

        public static CardFace Create(Transform parent, string name, float width)
        {
            var rt = UiKit.Rect(name, parent);
            rt.sizeDelta = SizeFor(width);
            return Build(rt);
        }

        public static CardFace Build(RectTransform root)
        {
            var face = root.gameObject.AddComponent<CardFace>();
            face.Construct(root);
            return face;
        }

        void Construct(RectTransform root)
        {
            _rect = root;
            _back = UiKit.Img("Back", root, null, new Color(0.075f, 0.04f, 0.06f, 1f), true);
            Region(_back.rectTransform, BackBox);

            // Арт: окно с обрезкой, внутри — арт по размеру «с запасом» (заполняет окно, лишнее срезается).
            var edge = UiKit.Img("ArtEdge", root, null, new Color(0.62f, 0.47f, 0.22f, 0.9f));
            Region(edge.rectTransform, ArtBox.x - 8, ArtBox.y - 8, ArtBox.z + 8, ArtBox.w + 8);
            _window = UiKit.Rect("ArtWindow", root);
            Region(_window, ArtBox);
            _window.gameObject.AddComponent<RectMask2D>();
            var windowFill = UiKit.Img("Fill", _window, null, new Color(0.05f, 0.03f, 0.05f, 1f));
            UiKit.Stretch(windowFill.rectTransform);
            _fallbackShade = UiKit.Img("CategoryShade", _window, UiKit.VerticalGradient(), Color.white);
            UiKit.Stretch(_fallbackShade.rectTransform);
            _fallbackIcon = UiKit.Img("CategoryArt", _window, null, Color.white);
            _fallbackIcon.preserveAspect = true;
            _fallbackIcon.rectTransform.anchorMin = new Vector2(0.3f, 0.14f);
            _fallbackIcon.rectTransform.anchorMax = new Vector2(0.7f, 0.86f);
            _fallbackIcon.rectTransform.offsetMin = _fallbackIcon.rectTransform.offsetMax = Vector2.zero;
            _art = UiKit.Img("Art", _window, null, Color.white);
            _art.rectTransform.anchorMin = _art.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _artFit = _art.gameObject.AddComponent<AspectRatioFitter>();
            _artFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

            // Тон шоу, который даёт карта, — значки в правом верхнем углу арта.
            for (int i = 0; i < _moods.Length; i++)
            {
                var stamp = UiKit.Img("Mood" + i, root, UiKit.Circle(), new Color(0.06f, 0.035f, 0.05f, 0.88f));
                Region(stamp.rectTransform, 930 - i * 132, ArtBox.y + 14, 1046 - i * 132, ArtBox.y + 130);
                _moods[i] = UiKit.Img("Icon", stamp.transform, null, Color.white);
                _moods[i].preserveAspect = true;
                UiKit.Stretch(_moods[i].rectTransform, 2f);
                stamp.gameObject.SetActive(false);
            }

            // Категория — подписью цветом категории между уголками рамки (иконка там нечитаемо мелкая).
            _category = Label("Category", root, TextAnchor.MiddleCenter, true);
            Region(_category.rectTransform, CategoryBox);

            var titlePlate = UiKit.Img("TitlePlate", root, CardVisuals.Element("TitleBackplate"), new Color(1f, 1f, 1f, 0.85f));
            Region(titlePlate.rectTransform, TitlePlateBox);
            _title = Label("Title", root, TextAnchor.MiddleCenter, true);
            Region(_title.rectTransform, TitleBox);

            _cost = UiKit.Img("Cost", root, CardVisuals.Element("CostBadge"), Color.white);
            Region(_cost.rectTransform, CostBox);
            _costText = Label("Value", _cost.transform, TextAnchor.MiddleCenter, true);
            UiKit.Stretch(_costText.rectTransform);
            _costText.color = UiKit.Gold;

            _dice = UiKit.Img("Dice", root, CardVisuals.Element("DiceBadge"), Color.white);
            Region(_dice.rectTransform, DiceBox);
            _dieIcon = UiKit.Img("Die", _dice.transform, null, Color.white);
            _dieIcon.preserveAspect = true;
            _dieIcon.rectTransform.anchorMin = new Vector2(0.06f, 0.08f);
            _dieIcon.rectTransform.anchorMax = new Vector2(0.38f, 0.92f);
            _dieIcon.rectTransform.offsetMin = _dieIcon.rectTransform.offsetMax = Vector2.zero;
            _diceText = Label("Value", _dice.transform, TextAnchor.MiddleCenter, true);
            _diceText.rectTransform.anchorMin = new Vector2(0.34f, 0f);
            _diceText.rectTransform.anchorMax = new Vector2(0.96f, 1f);
            _diceText.rectTransform.offsetMin = _diceText.rectTransform.offsetMax = Vector2.zero;
            _diceText.color = new Color(0.78f, 0.86f, 1f, 1f);

            var plate = UiKit.Img("TextPlate", root, CardVisuals.Element("DescriptionBackplate"), new Color(1f, 1f, 1f, 0.9f));
            Region(plate.rectTransform, TextPlateBox);
            _body = Label("Text", root, TextAnchor.MiddleCenter, false);
            Region(_body.rectTransform, TextBox);
            _body.color = UiKit.Paper;

            _hint = Label("Hint", root, TextAnchor.MiddleCenter, false);
            Region(_hint.rectTransform, HintBox);
            _hint.color = UiKit.Muted;

            _frame = UiKit.Img("Frame", root, null, Color.white);
            UiKit.Stretch(_frame.rectTransform);

            _banner = UiKit.Img("Banner", root, null, new Color(0.08f, 0.05f, 0.07f, 0.92f));
            Region(_banner.rectTransform, 110, CostBox.y - 96, 1078, CostBox.y);
            _bannerText = Label("Text", _banner.transform, TextAnchor.MiddleCenter, true);
            UiKit.Stretch(_bannerText.rectTransform);
            _banner.gameObject.SetActive(false);

            _hotkey = UiKit.Img("Hotkey", root, UiKit.Circle(), new Color(0.1f, 0.06f, 0.08f, 0.95f));
            Region(_hotkey.rectTransform, 26, 130, 176, 280);
            UiKit.Glow(_hotkey, new Color(0.95f, 0.76f, 0.36f, 0.9f), 1.5f);
            _hotkeyText = Label("Key", _hotkey.transform, TextAnchor.MiddleCenter, true);
            UiKit.Stretch(_hotkeyText.rectTransform);
            _hotkey.gameObject.SetActive(false);

            _hoverGlow = Overlay("Hover", "HoverGlow", new Color(1f, 0.85f, 0.55f, 0.9f));
            _selectedGlow = Overlay("Selected", "SelectedGlow", UiKit.Gold);
            _disabled = Overlay("Disabled", "DisabledOverlay", new Color(1f, 1f, 1f, 0.85f));
            Fit();
        }

        Image Overlay(string name, string element, Color color)
        {
            var img = UiKit.Img(name, _rect, CardVisuals.Element(element), color);
            UiKit.Stretch(img.rectTransform);
            img.gameObject.SetActive(false);
            return img;
        }

        static Text Label(string name, Transform parent, TextAnchor align, bool bold)
        {
            var t = UiKit.Txt(name, parent, "", 12, UiKit.Paper, align);
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.resizeTextForBestFit = true;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.lineSpacing = 0.92f;
            UiKit.Shadow(t, 1f, 0.8f);
            return t;
        }

        // Прямоугольник в пикселях рамки (от левого верхнего угла) → якоря в долях карты.
        static void Region(RectTransform rt, Vector4 box) => Region(rt, box.x, box.y, box.z, box.w);

        static void Region(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0 / W, 1f - y1 / H);
            rt.anchorMax = new Vector2(x1 / W, 1f - y0 / H);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void OnRectTransformDimensionsChange()
        {
            if (_rect != null && !Mathf.Approximately(_rect.rect.width, _lastWidth))
                Fit();
        }

        // Размер шрифта — доля ширины карты: одинаково читается в руке (176 px) и в превью.
        void Fit()
        {
            float w = _rect.rect.width > 1f ? _rect.rect.width : _rect.sizeDelta.x;
            if (w <= 1f)
                return;
            _lastWidth = w;
            Size(_title, w * 0.088f, w * 0.058f);
            Size(_category, w * 0.064f, w * 0.05f);
            Size(_costText, w * 0.084f, w * 0.058f);
            Size(_diceText, w * 0.08f, w * 0.056f);
            Size(_body, w * 0.076f, w * 0.056f);
            Size(_hint, w * 0.062f, w * 0.05f);
            Size(_hotkeyText, w * 0.075f, w * 0.05f);
            Size(_bannerText, w * 0.07f, w * 0.05f);
        }

        static void Size(Text text, float max, float min)
        {
            if (text == null)
                return;
            int hi = Mathf.Max(6, Mathf.RoundToInt(max));
            text.fontSize = hi;
            text.resizeTextMaxSize = hi;
            text.resizeTextMinSize = Mathf.Clamp(Mathf.RoundToInt(min), 6, hi);
        }

        // ---------- данные ----------

        public void Show(EventDefinition def)
        {
            if (def == null)
                return;
            var style = CardVisuals.StyleOf(def.category);
            _accent = style != null ? style.accent : Color.white;
            _frame.sprite = style != null ? style.frame : null;
            _frame.enabled = _frame.sprite != null;
            _category.text = Cards.CardBrief.CategoryName(def.category);
            _category.color = Color.Lerp(_accent, Color.white, 0.25f);
            _title.text = (def.displayName ?? "").ToUpperInvariant();

            SetArt(CardVisuals.Art(def), style != null ? style.icon : null);
            SetCost(HellToken.Format(def.cost), false);

            bool hasDie = def.diceEffects != null && def.diceEffects.Count > 0 && def.diceEffects[0] != null;
            _dice.gameObject.SetActive(hasDie);
            if (hasDie)
            {
                _dieIcon.sprite = CardVisuals.Die(def.diceEffects[0].die);
                _diceText.text = Dice.Notation(def.diceEffects[0]);
            }

            _body.text = CardVisuals.Text(def);
            // Подсказка «как играть» — отдельной строкой, если текст карты не она же.
            _hint.text = _body.text == def.hint ? CardBriefTarget(def) : def.hint ?? "";
        }

        // Арт по ширине окна без растяжения; нет арта — иконка категории на тёмном фоне её цвета.
        void SetArt(Sprite art, Sprite categoryIcon)
        {
            _art.sprite = art;
            _art.enabled = art != null;
            if (art != null)
                _artFit.aspectRatio = art.rect.width / Mathf.Max(1f, art.rect.height);
            _fallbackShade.enabled = art == null;
            _fallbackShade.color = new Color(_accent.r * 0.45f, _accent.g * 0.45f, _accent.b * 0.45f, 1f);
            _fallbackIcon.sprite = categoryIcon;
            _fallbackIcon.enabled = art == null && categoryIcon != null;
            _fallbackIcon.color = Color.Lerp(_accent, Color.white, 0.5f);
        }

        // Цена: в руке — HellToken, в магазине хаба — кр или нал. Не хватает — красная.
        public void SetCost(string text, bool poor)
        {
            _cost.gameObject.SetActive(!string.IsNullOrEmpty(text));
            _costText.text = text ?? "";
            _costText.color = poor ? new Color(1f, 0.45f, 0.4f, 1f) : UiKit.Gold;
            _cost.color = poor ? new Color(1f, 0.45f, 0.45f, 1f) : Color.white;
        }

        public void SetMoods(IList<ShowMood> moods)
        {
            for (int i = 0; i < _moods.Length; i++)
            {
                // Справа налево: первый тон — у самого края.
                bool on = moods != null && i < moods.Count;
                _moods[i].transform.parent.gameObject.SetActive(on);
                if (!on)
                    continue;
                _moods[i].sprite = UiKit.MoodIcon(moods[i]);
                _moods[i].color = MoodStyle.ColorOf(moods[i]);
            }
        }

        // Поднять карту на pixels вверх от её места (плавно); 0 — вернуть.
        public void Raise(float pixels)
        {
            if (!_homeSet)
            {
                _home = _rect.anchoredPosition;
                _homeSet = true;
            }

            _raiseTarget = pixels;
        }

        void Update()
        {
            if (!_homeSet || Mathf.Approximately(_raise, _raiseTarget))
                return;
            _raise = Mathf.MoveTowards(_raise, _raiseTarget, Time.unscaledDeltaTime * 520f);
            _rect.anchoredPosition = _home + new Vector2(0f, _raise);
        }

        public void SetHotkey(int number)
        {
            _hotkey.gameObject.SetActive(number > 0);
            _hotkeyText.text = number > 0 ? number.ToString() : "";
        }

        static string CardBriefTarget(EventDefinition def) => Cards.CardBrief.TargetShort(def);

        public void SetHint(string text)
        {
            _hint.text = text ?? "";
        }

        // Лента поперёк низа арта: «В РУКЕ», «сыграно», «В ЗАПАСЕ».
        public void SetBanner(string text, Color color)
        {
            bool on = !string.IsNullOrEmpty(text);
            _banner.gameObject.SetActive(on);
            if (!on)
                return;
            _bannerText.text = text;
            _bannerText.color = color;
        }

        public void SetHover(bool on)
        {
            _hoverGlow.gameObject.SetActive(on && !_selectedGlow.gameObject.activeSelf);
        }

        public void SetSelected(bool on)
        {
            _selectedGlow.gameObject.SetActive(on);
            if (on)
                _hoverGlow.gameObject.SetActive(false);
        }

        public void SetDisabled(bool on)
        {
            _disabled.gameObject.SetActive(on);
        }
    }
}
