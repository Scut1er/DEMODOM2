using System;
using System.Collections;
using System.Collections.Generic;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Экран события поверх карты выпуска: иллюстрация, текст, 2–3 выбора, затем результат.
    // Собирается кодом на канвасе хаба — в сцене ничего настраивать не нужно.
    public class EventRoomView : MonoBehaviour
    {
        static readonly Color Dim = new Color(0f, 0f, 0f, 0.78f);
        static readonly Color PanelColor = new Color(0.12f, 0.11f, 0.15f, 0.98f);
        static readonly Color ChoiceColor = new Color(0.2f, 0.18f, 0.24f, 1f);
        static readonly Color ChoiceLocked = new Color(0.14f, 0.13f, 0.16f, 1f);
        static readonly Color Gold = new Color(0.93f, 0.76f, 0.36f, 1f);
        static readonly Color Light = new Color(0.96f, 0.93f, 0.88f, 1f);
        static readonly Color Muted = new Color(0.62f, 0.58f, 0.66f, 1f);
        static readonly Color Dark = new Color(0.06f, 0.05f, 0.07f, 1f);
        static readonly Color Good = new Color(0.42f, 0.82f, 0.48f, 1f);
        static readonly Color Bad = new Color(0.96f, 0.34f, 0.28f, 1f);

        Font _font;
        Font _titleFont;
        CanvasGroup _group;
        RectTransform _panel;
        Image _accent;
        Image _artFrame;
        Image _art;
        Text _kicker;
        Text _title;
        Text _body;
        RectTransform _choices;
        RectTransform _result;
        Text _stamp;
        Text _resultText;
        Text _summary;
        Button _continue;
        Action<int> _onPick;
        Action _onContinue;
        bool _busy;

        public bool IsOpen => gameObject.activeSelf;
        // Куда указывает обучение (BossCoach) — блок вариантов.
        public RectTransform Focus => _choices;
        public RectTransform BodyFocus => _body != null ? _body.rectTransform : _choices;

        public static EventRoomView Create(Transform canvas, Font titleFont)
        {
            var go = new GameObject("EventRoom", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var view = go.AddComponent<EventRoomView>();
            view.Build(titleFont);
            go.SetActive(false);
            return view;
        }

        void Build(Font titleFont)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _titleFont = titleFont != null ? titleFont : _font;

            var root = (RectTransform)transform;
            Stretch(root);
            var dim = gameObject.AddComponent<Image>();
            dim.color = Dim;
            _group = gameObject.AddComponent<CanvasGroup>();

            _panel = Box(root, "Panel", PanelColor);
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(1260f, 840f);

            _accent = Box(_panel, "Accent", Gold).GetComponent<Image>();
            Place((RectTransform)_accent.transform, 0f, 1f, 1f, 1f, new Vector2(0f, -8f), Vector2.zero);

            _artFrame = Box(_panel, "ArtFrame", Gold).GetComponent<Image>();
            Place((RectTransform)_artFrame.transform, 0f, 1f, 0f, 1f, new Vector2(40f, -436f), new Vector2(436f, -40f));
            var artBg = Box((RectTransform)_artFrame.transform, "ArtBg", new Color(0.08f, 0.07f, 0.1f, 1f));
            Place(artBg, 0f, 0f, 1f, 1f, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            _art = Box(artBg, "Art", Color.white).GetComponent<Image>();
            Place((RectTransform)_art.transform, 0f, 0f, 1f, 1f, new Vector2(12f, 12f), new Vector2(-12f, -12f));
            _art.preserveAspect = true;

            _kicker = Label(_panel, "Kicker", _font, 20, Gold, TextAnchor.UpperLeft);
            UiTypography.Apply(_kicker, TextRole.Label);
            Place((RectTransform)_kicker.transform, 0f, 1f, 1f, 1f, new Vector2(470f, -80f), new Vector2(-40f, -44f));
            _title = Label(_panel, "Title", _titleFont, 52, Gold, TextAnchor.UpperLeft);
            UiTypography.Apply(_title, TextRole.Title);
            Place((RectTransform)_title.transform, 0f, 1f, 1f, 1f, new Vector2(470f, -150f), new Vector2(-40f, -78f));
            // Длинные названия событий не обрезаются: шрифт ужимается под строку.
            _title.horizontalOverflow = HorizontalWrapMode.Wrap;
            _title.verticalOverflow = VerticalWrapMode.Truncate;
            _title.resizeTextForBestFit = true;
            _title.resizeTextMinSize = 26;
            _title.resizeTextMaxSize = _title.fontSize;
            _body = Label(_panel, "Body", _font, 25, Light, TextAnchor.UpperLeft);
            UiTypography.Apply(_body, TextRole.Body);
            Place((RectTransform)_body.transform, 0f, 1f, 1f, 1f, new Vector2(470f, -440f), new Vector2(-40f, -160f));

            _choices = new GameObject("Choices", typeof(RectTransform)).GetComponent<RectTransform>();
            _choices.SetParent(_panel, false);
            Place(_choices, 0f, 0f, 1f, 0f, new Vector2(40f, 36f), new Vector2(-40f, 372f));
            var layout = _choices.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            _result = new GameObject("Result", typeof(RectTransform)).GetComponent<RectTransform>();
            _result.SetParent(_panel, false);
            Place(_result, 0f, 0f, 1f, 0f, new Vector2(40f, 36f), new Vector2(-40f, 372f));
            _stamp = Label(_result, "Stamp", _titleFont, 40, Good, TextAnchor.UpperLeft);
            UiTypography.Apply(_stamp, TextRole.Heading);
            Place((RectTransform)_stamp.transform, 0f, 1f, 1f, 1f, new Vector2(0f, -54f), Vector2.zero);
            _resultText = Label(_result, "Text", _font, 26, Light, TextAnchor.UpperLeft);
            UiTypography.Apply(_resultText, TextRole.Body);
            Place((RectTransform)_resultText.transform, 0f, 0f, 1f, 1f, new Vector2(0f, 130f), new Vector2(0f, -60f));
            _summary = Label(_result, "Summary", _font, 22, Gold, TextAnchor.LowerLeft);
            UiTypography.Apply(_summary, TextRole.Label);
            Place((RectTransform)_summary.transform, 0f, 0f, 1f, 0f, new Vector2(0f, 84f), new Vector2(-300f, 124f));
            _continue = MakeButton(_result, "Continue", "ДАЛЬШЕ", Gold, Dark, 26);
            Place((RectTransform)_continue.transform, 1f, 0f, 1f, 0f, new Vector2(-280f, 0f), new Vector2(0f, 72f));
            _continue.onClick.AddListener(() =>
            {
                if (_busy)
                    return;
                var next = _onContinue;
                _onContinue = null;
                next?.Invoke();
            });

            // Окно события в рамке пака, иллюстрация в тонкой рамке, «Дальше» — главная кнопка.
            UiKit.DressSolid(_panel.GetComponent<Image>(), UiKit.Frame.Gold, 10f);
            _accent.gameObject.SetActive(false);
            UiKit.Dress(_artFrame, UiKit.Frame.Dialog);
            UiKit.Primary(_continue, 26);
            UiKit.Shadow(_title, 3f);
        }

        public void Show(string kicker, string title, string body, Sprite art, Color color, List<EventChoiceModel> choices, string leaveLabel, Action<int> onPick)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _onPick = onPick;
            _busy = false;
            _kicker.text = kicker;
            _title.text = title;
            _body.text = body;
            _accent.color = color;
            _artFrame.color = color;
            _art.sprite = art;
            _art.enabled = art != null;

            _result.gameObject.SetActive(false);
            _choices.gameObject.SetActive(true);
            for (int i = _choices.childCount - 1; i >= 0; i--)
                Destroy(_choices.GetChild(i).gameObject);

            bool any = false;
            foreach (var c in choices)
            {
                AddChoice(c);
                any |= c.available;
            }

            // Тупик: все варианты закрыты — дать уйти без последствий.
            if (!any)
                AddChoice(new EventChoiceModel { index = -1, label = leaveLabel, description = "Ни один вариант сейчас недоступен.", available = true });

            StopAllCoroutines();
            StartCoroutine(Appear());
        }

        void AddChoice(EventChoiceModel c)
        {
            var button = MakeButton(_choices, "Choice", "", c.available ? ChoiceColor : ChoiceLocked, Light, 26);
            var rect = (RectTransform)button.transform;
            rect.sizeDelta = new Vector2(0f, 104f);
            var plate = button.GetComponent<Image>();
            UiKit.Dress(plate, UiKit.Frame.Dialog, 1.6f);
            if (!c.available && plate != null)
                plate.color = new Color(0.55f, 0.5f, 0.52f, 0.8f);
            var label = button.GetComponentInChildren<Text>();
            label.text = c.label;
            label.alignment = TextAnchor.UpperLeft;
            label.color = c.available ? Light : Muted;
            Place((RectTransform)label.transform, 0f, 0f, 1f, 1f, new Vector2(24f, 0f), new Vector2(-260f, -10f));

            // Под названием — к чему это приведёт: при успехе и при провале (направление каждого эффекта).
            var sub = Label(rect, "Sub", _font, 17, Muted, TextAnchor.UpperLeft);
            UiTypography.Apply(sub, TextRole.Caption);
            sub.supportRichText = true;
            Place((RectTransform)sub.transform, 0f, 0f, 1f, 1f, new Vector2(24f, 6f), new Vector2(-260f, -44f));
            var lines = new List<string>();
            if (!c.available)
                lines.Add("Закрыто: " + c.reason);
            if (!string.IsNullOrEmpty(c.description))
                lines.Add(c.description);
            if (c.available || !string.IsNullOrEmpty(c.preview))
                lines.Add("<color=#7FE08A>Если получится:</color> " + (string.IsNullOrEmpty(c.preview) ? "без видимых последствий" : c.preview));
            if (c.chance < 100)
                lines.Add("<color=#FF7A5C>Если нет:</color> " + (string.IsNullOrEmpty(c.failPreview) ? "ничего не изменится" : c.failPreview));
            sub.text = string.Join("\n", lines);

            // Справа — цена и риск словами: надёжно / риск / большой риск.
            var side = Label(rect, "Side", _font, 20, c.available ? Gold : Muted, TextAnchor.MiddleRight);
            UiTypography.Apply(side, TextRole.Label);
            side.supportRichText = true;
            Place((RectTransform)side.transform, 1f, 0f, 1f, 1f, new Vector2(-250f, 0f), new Vector2(-24f, 0f));
            string risk = c.chance >= 100 ? "" : c.chance >= 80 ? "<color=#7FE08A>надёжно</color>" : c.chance >= 50 ? "<color=#F2C35C>риск</color>" : "<color=#FF7A5C>большой риск</color>";
            side.text = Join(c.costLabel, c.chanceLabel) + (risk.Length > 0 ? "\n" + risk : "");

            button.interactable = c.available;
            int index = c.index;
            button.onClick.AddListener(() =>
            {
                if (_busy)
                    return;
                _busy = true;
                _onPick?.Invoke(index);
            });
        }

        public void ShowResult(EventOutcome outcome, bool showStamp, Action onContinue)
        {
            _busy = false;
            _onContinue = onContinue;
            _choices.gameObject.SetActive(false);
            _result.gameObject.SetActive(true);
            _stamp.gameObject.SetActive(showStamp);
            _stamp.text = outcome.success ? "ПОЛУЧИЛОСЬ" : "НЕ ПОЛУЧИЛОСЬ";
            _stamp.color = outcome.success ? Good : Bad;
            _resultText.text = outcome.text;
            _summary.text = string.IsNullOrEmpty(outcome.summary) ? "" : "ЧТО ИЗМЕНИЛОСЬ:   " + outcome.summary;
            StopAllCoroutines();
            StartCoroutine(Punch(_result));
        }

        public void Hide()
        {
            _onPick = null;
            _onContinue = null;
            gameObject.SetActive(false);
        }

        IEnumerator Appear()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.22f;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                _group.alpha = k;
                _panel.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, k);
                yield return null;
            }

            _group.alpha = 1f;
            _panel.localScale = Vector3.one;
        }

        IEnumerator Punch(RectTransform target)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.25f;
                float s = 1f + Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.03f;
                target.localScale = Vector3.one * s;
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        static string Join(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
                return b ?? "";
            if (string.IsNullOrEmpty(b))
                return a;
            return a + "\n" + b;
        }

        // ---------- построение ----------

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, float minX, float minY, float maxX, float maxY, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        static RectTransform Box(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        Text Label(Transform parent, string name, Font font, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        Button MakeButton(Transform parent, string name, string caption, Color bg, Color fg, int size)
        {
            var rect = Box(parent, name, bg);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.9f);
            colors.colorMultiplier = 1.4f;
            button.colors = colors;
            var text = Label(rect, "Label", _font, size, fg, TextAnchor.MiddleCenter);
            UiTypography.Apply(text, TextRole.Button);
            Stretch((RectTransform)text.transform);
            text.text = caption;
            return button;
        }
    }
}
