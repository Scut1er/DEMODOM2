using System;
using System.Collections;
using System.Collections.Generic;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Экран события поверх карты выпуска (пак EventScreen): слева — что случилось и что на кону,
    // справа — иллюстрация и 1–3 карточки выбора (цвет — по риску), после выбора — результат на месте карточек.
    // Собирается кодом на канвасе хаба — в сцене ничего настраивать не нужно.
    public class EventRoomView : MonoBehaviour
    {
        const string Kit = "Art/UI/EventScreen/";
        static readonly Color Dim = new Color(0.02f, 0.01f, 0.02f, 0.86f);
        static readonly Color Neon = new Color(1f, 0.19f, 0.28f, 1f);
        static readonly Color Gold = new Color(0.96f, 0.76f, 0.31f, 1f);
        static readonly Color Green = new Color(0.21f, 0.93f, 0.54f, 1f);
        static readonly Color Light = new Color(0.95f, 0.93f, 0.91f, 1f);
        static readonly Color Muted = new Color(0.72f, 0.66f, 0.67f, 1f);
        static readonly Color Good = new Color(0.5f, 0.88f, 0.54f, 1f);
        static readonly Color Bad = new Color(1f, 0.42f, 0.36f, 1f);

        Font _font;
        Font _titleFont;
        CanvasGroup _group;
        RectTransform _panel;
        Image _art;
        AspectRatioFitter _artFit;
        Image _artFallback;
        Text _kicker;
        Text _title;
        Text _body;
        RectTransform _stakes;
        RectTransform _choices;
        CanvasGroup _choiceGroup;
        RectTransform _result;
        Image _stampPlate;
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
        public RectTransform ResultFocus => _result;

        // Лекция босса: кнопки вариантов не жмутся, пока он не договорил и не отдал ход.
        public void SetChoicesLocked(bool locked)
        {
            if (_choiceGroup != null)
                _choiceGroup.interactable = !locked;
        }

        public static EventRoomView Create(Transform canvas, Font titleFont)
        {
            var go = new GameObject("EventRoom", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var view = go.AddComponent<EventRoomView>();
            view.Build(titleFont);
            go.SetActive(false);
            return view;
        }

        static Sprite Sprite(string path) => UiKit.Load(Kit + path);

        void Build(Font titleFont)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _titleFont = titleFont != null ? titleFont : _font;

            var root = (RectTransform)transform;
            UiKit.Stretch(root);
            var dim = gameObject.AddComponent<Image>();
            dim.color = Dim;
            _group = gameObject.AddComponent<CanvasGroup>();

            _panel = UiKit.Rect("Panel", root);
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(1760f, 820f);
            _panel.anchoredPosition = new Vector2(0f, -40f);

            // ---------- слева: событие и ставки ----------
            var info = Sliced(_panel, "Info", "Panels/panel_event_main_9slice");
            Place(info, 0f, 0f, 0f, 1f, new Vector2(0f, 0f), new Vector2(600f, 0f));
            _kicker = Label(info, "Kicker", _font, 20, Neon, TextAnchor.UpperLeft, TextRole.Label);
            Place(_kicker.rectTransform, 0f, 1f, 1f, 1f, new Vector2(44f, -76f), new Vector2(-40f, -38f));
            _kicker.fontStyle = FontStyle.Bold;
            _title = Label(info, "Title", _titleFont, 50, Gold, TextAnchor.LowerLeft, TextRole.Title);
            Place(_title.rectTransform, 0f, 1f, 1f, 1f, new Vector2(42f, -204f), new Vector2(-36f, -78f));
            _title.resizeTextForBestFit = true;
            _title.resizeTextMinSize = 26;
            _title.resizeTextMaxSize = Mathf.Max(30, _title.fontSize);
            UiKit.Shadow(_title, 3f);
            var divider = UiKit.Img("Divider", info, Sprite("Decor/divider_event_red"), Color.white);
            Place(divider.rectTransform, 0f, 1f, 1f, 1f, new Vector2(36f, -222f), new Vector2(-36f, -206f));
            _body = Label(info, "Body", _font, 23, Light, TextAnchor.UpperLeft, TextRole.Body);
            Place(_body.rectTransform, 0f, 1f, 1f, 1f, new Vector2(44f, -500f), new Vector2(-40f, -236f));
            _body.resizeTextForBestFit = true;
            _body.resizeTextMinSize = 16;
            _body.resizeTextMaxSize = Mathf.Max(18, _body.fontSize);

            var stakesPlate = Sliced(info, "Stakes", "Panels/panel_effects_9slice");
            Place(stakesPlate, 0f, 0f, 1f, 0f, new Vector2(28f, 30f), new Vector2(-28f, 296f));
            var stakesHead = Label(stakesPlate, "Head", _font, 20, Gold, TextAnchor.UpperLeft, TextRole.Label);
            stakesHead.fontStyle = FontStyle.Bold;
            stakesHead.text = "ВОЗМОЖНЫЙ ЭФФЕКТ:";
            Place(stakesHead.rectTransform, 0f, 1f, 1f, 1f, new Vector2(30f, -58f), new Vector2(-24f, -22f));
            _stakes = UiKit.Rect("Rows", stakesPlate);
            Place(_stakes, 0f, 0f, 1f, 1f, new Vector2(30f, 20f), new Vector2(-24f, -62f));
            var rows = _stakes.gameObject.AddComponent<VerticalLayoutGroup>();
            rows.spacing = 6f;
            rows.childAlignment = TextAnchor.UpperLeft;
            rows.childControlHeight = false;
            rows.childControlWidth = true;
            rows.childForceExpandHeight = false;

            // ---------- справа: иллюстрация ----------
            var frame = Sliced(_panel, "ArtFrame", "Panels/panel_generic_9slice");
            Place(frame, 0f, 1f, 1f, 1f, new Vector2(630f, -500f), new Vector2(0f, 0f));
            var window = UiKit.Rect("Window", frame);
            // Арт — под рамкой (между заливкой и рамкой), уголки рамки лежат поверх картинки; у рамки — только кромка.
            window.SetSiblingIndex(1);
            var artEdge = frame.Find("Frame")?.GetComponent<Image>();
            if (artEdge != null)
                artEdge.fillCenter = false;
            Place(window, 0f, 0f, 1f, 1f, new Vector2(10f, 10f), new Vector2(-10f, -10f));
            window.gameObject.AddComponent<RectMask2D>();
            var fill = UiKit.Img("Fill", window, null, new Color(0.06f, 0.03f, 0.05f, 1f));
            UiKit.Stretch(fill.rectTransform);
            _artFallback = UiKit.Img("Fallback", window, UiKit.Halo(), new Color(0.75f, 0.12f, 0.2f, 0.55f));
            UiKit.Stretch(_artFallback.rectTransform);
            _art = UiKit.Img("Art", window, null, Color.white);
            _art.rectTransform.anchorMin = _art.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _artFit = _art.gameObject.AddComponent<AspectRatioFitter>();
            _artFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            var shade = UiKit.Img("Shade", window, UiKit.VerticalGradient(), new Color(0.04f, 0.01f, 0.03f, 0.75f));
            Place(shade.rectTransform, 0f, 0f, 1f, 0.35f, Vector2.zero, Vector2.zero);
            var onAir = UiKit.Img("OnAir", frame, Sprite("Badges/badge_on_air_blank"), Color.white);
            Place(onAir.rectTransform, 1f, 1f, 1f, 1f, new Vector2(-178f, -66f), new Vector2(-28f, -24f));
            var onAirText = Label(onAir.rectTransform, "Text", _font, 18, Light, TextAnchor.MiddleCenter, TextRole.Custom);
            onAirText.fontStyle = FontStyle.Bold;
            onAirText.text = "В ЭФИРЕ";
            UiKit.Stretch(onAirText.rectTransform);

            // ---------- карточки выбора / результат ----------
            _choices = UiKit.Rect("Choices", _panel);
            _choiceGroup = _choices.gameObject.AddComponent<CanvasGroup>();
            Place(_choices, 0f, 0f, 1f, 0f, new Vector2(630f, 0f), new Vector2(0f, 302f));
            var layout = _choices.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            _result = Sliced(_panel, "Result", "Panels/panel_effects_9slice");
            Place(_result, 0f, 0f, 1f, 0f, new Vector2(630f, 0f), new Vector2(0f, 302f));
            _stampPlate = UiKit.Img("Stamp", _result, Sprite("Badges/badge_success_blank"), Color.white);
            Place(_stampPlate.rectTransform, 0f, 1f, 0f, 1f, new Vector2(36f, -104f), new Vector2(420f, -34f));
            _stamp = Label(_stampPlate.rectTransform, "Text", _titleFont, 34, Good, TextAnchor.MiddleCenter, TextRole.Heading);
            UiKit.Stretch(_stamp.rectTransform);
            _resultText = Label(_result, "Text", _font, 24, Light, TextAnchor.UpperLeft, TextRole.Body);
            Place(_resultText.rectTransform, 0f, 0f, 1f, 1f, new Vector2(40f, 112f), new Vector2(-40f, -112f));
            _resultText.resizeTextForBestFit = true;
            _resultText.resizeTextMinSize = 16;
            _resultText.resizeTextMaxSize = Mathf.Max(18, _resultText.fontSize);
            _summary = Label(_result, "Summary", _font, 21, Gold, TextAnchor.LowerLeft, TextRole.Label);
            Place(_summary.rectTransform, 0f, 0f, 1f, 0f, new Vector2(40f, 34f), new Vector2(-330f, 112f));
            _summary.resizeTextForBestFit = true;
            _summary.resizeTextMinSize = 14;
            _summary.resizeTextMaxSize = Mathf.Max(16, _summary.fontSize);
            _continue = KitButton(_result, "Continue", "ДАЛЬШЕ", "Buttons/button_option_primary_9slice");
            Place((RectTransform)_continue.transform, 1f, 0f, 1f, 0f, new Vector2(-300f, 32f), new Vector2(-36f, 104f));
            _continue.onClick.AddListener(() =>
            {
                if (_busy)
                    return;
                var next = _onContinue;
                _onContinue = null;
                next?.Invoke();
            });
        }

        public void Show(string kicker, string title, string body, Sprite art, Color color, List<EventChoiceModel> choices,
            List<EventStake> stakes, string leaveLabel, Action<int> onPick)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _onPick = onPick;
            _busy = false;
            _kicker.text = kicker;
            _title.text = title;
            _body.text = body;
            _art.sprite = art;
            _art.enabled = art != null;
            if (art != null)
                _artFit.aspectRatio = art.rect.width / Mathf.Max(1f, art.rect.height);
            _artFallback.enabled = art == null;
            _artFallback.color = new Color(color.r, color.g, color.b, 0.55f);

            for (int i = _stakes.childCount - 1; i >= 0; i--)
                Destroy(_stakes.GetChild(i).gameObject);
            int shown = 0;
            if (stakes != null)
            {
                foreach (var s in stakes)
                {
                    if (shown++ >= 5)
                        break;
                    AddStake(s);
                }
            }

            if (shown == 0)
                AddStake(new EventStake { icon = "icon_diary", text = "Только история — без цифр" });

            _result.gameObject.SetActive(false);
            _choices.gameObject.SetActive(true);
            for (int i = _choices.childCount - 1; i >= 0; i--)
                Destroy(_choices.GetChild(i).gameObject);

            bool any = false;
            int number = 0;
            foreach (var c in choices)
            {
                AddChoice(c, ++number);
                any |= c.available;
            }

            // Тупик: все варианты закрыты — дать уйти без последствий.
            if (!any)
                AddChoice(new EventChoiceModel { index = -1, label = leaveLabel, description = "Ни один вариант сейчас недоступен.", available = true, accent = ChoiceAccent.Neutral }, ++number);

            StopAllCoroutines();
            StartCoroutine(Appear());
        }

        void AddStake(EventStake stake)
        {
            var row = UiKit.Rect("Stake", _stakes);
            row.sizeDelta = new Vector2(0f, 34f);
            var icon = UiKit.Img("Icon", row, Sprite("Icons/" + stake.icon) ?? Sprite("Icons/icon_diary"), Color.white);
            icon.preserveAspect = true;
            Place(icon.rectTransform, 0f, 0.5f, 0f, 0.5f, new Vector2(0f, -15f), new Vector2(30f, 15f));
            var text = Label(row, "Text", _font, 19, Light, TextAnchor.MiddleLeft, TextRole.Caption);
            Place(text.rectTransform, 0f, 0f, 1f, 1f, new Vector2(42f, 0f), Vector2.zero);
            text.text = stake.text;
        }

        // Карточка варианта: номер, название, пояснение, что будет при успехе и при провале, шанс и цена.
        void AddChoice(EventChoiceModel c, int number)
        {
            string tone = c.accent == ChoiceAccent.Risky ? "red" : c.accent == ChoiceAccent.Neutral ? "gold" : "green";
            Color accent = c.accent == ChoiceAccent.Risky ? Neon : c.accent == ChoiceAccent.Neutral ? Gold : Green;
            var rect = Sliced(_choices, "Choice", "Panels/panel_option_" + tone + "_9slice");
            var button = Clickable(rect);
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.55f, 0.52f, 0.55f, 0.85f);
            colors.colorMultiplier = 1.3f;
            button.colors = colors;

            // Номер на круге цвета карточки (у бейджей пака цвет кольца привязан к номеру, а не к риску).
            var ring = UiKit.Img("Ring", rect, UiKit.Circle(), accent);
            Place(ring.rectTransform, 0f, 1f, 0f, 1f, new Vector2(22f, -86f), new Vector2(86f, -22f));
            var badge = UiKit.Img("Badge", ring.rectTransform, UiKit.Circle(), new Color(0.07f, 0.035f, 0.055f, 1f));
            UiKit.Stretch(badge.rectTransform, 4f);
            var num = Label(badge.rectTransform, "N", _titleFont, 26, accent, TextAnchor.MiddleCenter, TextRole.Custom);
            num.text = number.ToString("00");
            num.resizeTextForBestFit = true;
            num.resizeTextMinSize = 14;
            num.resizeTextMaxSize = 26;
            UiKit.Stretch(num.rectTransform, 12f);

            var label = Label(rect, "Label", _font, 22, c.available ? accent : Muted, TextAnchor.MiddleLeft, TextRole.Heading);
            label.fontStyle = FontStyle.Bold;
            label.text = (c.label ?? "").ToUpperInvariant();
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = Mathf.Max(16, label.fontSize);
            Place(label.rectTransform, 0f, 1f, 1f, 1f, new Vector2(98f, -94f), new Vector2(-22f, -18f));

            // Ниже — к чему это приведёт: при успехе и при провале (направление каждого эффекта).
            var lines = new List<string>();
            if (!c.available)
                lines.Add("<color=#FF7A5C>Закрыто: " + c.reason + "</color>");
            if (!string.IsNullOrEmpty(c.description))
                lines.Add(c.description);
            if (c.available || !string.IsNullOrEmpty(c.preview))
                lines.Add("<color=#7FE08A>Если получится:</color> " + (string.IsNullOrEmpty(c.preview) ? "без видимых последствий" : c.preview));
            if (c.chance < 100)
                lines.Add("<color=#FF7A5C>Если нет:</color> " + (string.IsNullOrEmpty(c.failPreview) ? "ничего не изменится" : c.failPreview));
            var sub = Label(rect, "Sub", _font, 18, c.available ? Light : Muted, TextAnchor.UpperLeft, TextRole.Caption);
            sub.text = string.Join("\n", lines);
            sub.resizeTextForBestFit = true;
            sub.resizeTextMinSize = 12;
            sub.resizeTextMaxSize = Mathf.Max(14, sub.fontSize);
            Place(sub.rectTransform, 0f, 0f, 1f, 1f, new Vector2(26f, 62f), new Vector2(-22f, -100f));

            // Внизу — цена, шанс и риск словами: надёжно / риск / большой риск.
            string risk = c.chance >= 100 ? "" : c.chance >= 80 ? "<color=#7FE08A>надёжно</color>" : c.chance >= 50 ? "<color=#F2C35C>риск</color>" : "<color=#FF7A5C>большой риск</color>";
            var foot = Label(rect, "Foot", _font, 19, c.available ? Gold : Muted, TextAnchor.MiddleLeft, TextRole.Label);
            foot.fontStyle = FontStyle.Bold;
            foot.text = string.Join("   ·   ", NonEmpty(c.costLabel, c.chanceLabel, risk));
            Place(foot.rectTransform, 0f, 0f, 1f, 0f, new Vector2(26f, 16f), new Vector2(-22f, 56f));

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

        static List<string> NonEmpty(params string[] parts)
        {
            var list = new List<string>();
            foreach (var p in parts)
            {
                if (!string.IsNullOrEmpty(p))
                    list.Add(p);
            }

            return list;
        }

        public void ShowResult(EventOutcome outcome, bool showStamp, Action onContinue)
        {
            _busy = false;
            _onContinue = onContinue;
            _choices.gameObject.SetActive(false);
            _result.gameObject.SetActive(true);
            _stampPlate.gameObject.SetActive(showStamp);
            _stampPlate.sprite = Sprite(outcome.success ? "Badges/badge_success_blank" : "Badges/badge_warning_blank");
            _stamp.text = outcome.success ? "ПОЛУЧИЛОСЬ" : "НЕ ПОЛУЧИЛОСЬ";
            _stamp.color = outcome.success ? Good : Bad;
            _resultText.text = outcome.text;
            var rt = _resultText.rectTransform;
            rt.offsetMax = new Vector2(rt.offsetMax.x, showStamp ? -112f : -36f);
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

        // ---------- построение ----------

        static void Place(RectTransform rect, float minX, float minY, float maxX, float maxY, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // Окно пака: сплошная тёмная заливка (карта под окном не просвечивает), поверх — рамка 9-slice,
        // ещё выше — содержимое (дети контейнера). Нет спрайта — только заливка.
        static RectTransform Sliced(Transform parent, string name, string sprite)
        {
            var holder = UiKit.Rect(name, parent);
            var fill = UiKit.Img("Fill", holder, null, new Color(0.07f, 0.035f, 0.055f, 1f), true);
            var art = Sprite(sprite);
            UiKit.Stretch(fill.rectTransform, art != null ? 8f : 0f);
            if (art != null)
            {
                var frame = UiKit.Img("Frame", holder, art, Color.white);
                frame.type = Image.Type.Sliced;
                UiKit.Stretch(frame.rectTransform);
            }

            return holder;
        }

        // Кнопка на окне пака: подсветка — по рамке (её цвет и умножается).
        static Button Clickable(RectTransform holder)
        {
            var button = holder.gameObject.AddComponent<Button>();
            var frame = holder.Find("Frame");
            button.targetGraphic = frame != null ? frame.GetComponent<Image>() : holder.Find("Fill").GetComponent<Image>();
            return button;
        }

        Text Label(Transform parent, string name, Font font, int size, Color color, TextAnchor anchor, TextRole role)
        {
            var text = UiKit.Txt(name, parent, "", size, color, anchor, font);
            text.verticalOverflow = VerticalWrapMode.Truncate;
            if (role != TextRole.Custom)
                UiTypography.Apply(text, role);
            return text;
        }

        Button KitButton(Transform parent, string name, string caption, string sprite)
        {
            var plate = Sliced(parent, name, sprite);
            var button = Clickable(plate);
            var colors = button.colors;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.colorMultiplier = 1.4f;
            button.colors = colors;
            var text = Label(plate, "Label", _font, 26, Light, TextAnchor.MiddleCenter, TextRole.Button);
            text.fontStyle = FontStyle.Bold;
            UiKit.Stretch(text.rectTransform);
            text.text = caption;
            UiKit.Pulse(button);
            return button;
        }
    }
}
