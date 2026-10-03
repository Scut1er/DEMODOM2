using System;
using System.Collections.Generic;
using RealityDirector.Meta;
using RealityDirector.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Контракт, выбор каста, событие, маркетинг, заставка серии. Поверх хаба.
    public class HubOverlays : MonoBehaviour
    {
        Font _font;
        GameObject _page;
        public RectTransform Focus { get; private set; }

        public static HubOverlays Create()
        {
            var go = new GameObject("HubOverlays");
            return go.AddComponent<HubOverlays>();
        }

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 280;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        public void SetPageLocked(bool locked)
        {
            if (_page == null)
                return;
            var buttons = _page.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].interactable = !locked;
        }

        public void Hide()
        {
            if (_page != null)
                Destroy(_page);
            _page = null;
            Focus = null;
        }

        public void AskName(string current, Action<string, bool> done)
        {
            Open();
            Title("ТРУДОВОЙ ДОГОВОР");
            Body("Должность — продюсер. Проект — ONLY WHAT MATTERS. Срок — один сезон. Рейтинг на тебе.\nБосс: подпись, потом сезон. Галочка — я проведу по площадке. Один раз.");
            var faceGo = new GameObject("boss", typeof(RectTransform), typeof(Image));
            faceGo.transform.SetParent(_page.transform, false);
            var face = faceGo.GetComponent<RectTransform>();
            Pin(face, 1100f, 80f, 280f, 360f);
            var faceImg = faceGo.GetComponent<Image>();
            faceImg.sprite = BossCoach.Portrait();
            faceImg.preserveAspect = true;
            faceImg.raycastTarget = false;
            var fieldGo = new GameObject("name", typeof(RectTransform), typeof(Image), typeof(InputField));
            fieldGo.transform.SetParent(_page.transform, false);
            var rect = fieldGo.GetComponent<RectTransform>();
            Pin(rect, 80f, 300f, 900f, 56f);
            fieldGo.GetComponent<Image>().color = new Color(0.14f, 0.11f, 0.16f, 1f);
            var text = Label(fieldGo.transform, "", 22, Color.white);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(16f, 0f);
            text.rectTransform.offsetMax = new Vector2(-16f, 0f);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            UiTypography.Apply(text, TextRole.Input);
            var hint = Label(fieldGo.transform, "Имя продюсера", 22, new Color(1f, 1f, 1f, 0.35f));
            Stretch(hint.rectTransform);
            hint.rectTransform.offsetMin = new Vector2(16f, 0f);
            hint.rectTransform.offsetMax = new Vector2(-16f, 0f);
            hint.alignment = TextAnchor.MiddleLeft;
            UiTypography.Apply(hint, TextRole.Input);
            hint.fontStyle = FontStyle.Italic;
            var field = fieldGo.GetComponent<InputField>();
            field.textComponent = text;
            field.placeholder = hint;
            field.text = string.IsNullOrEmpty(current) ? "" : current;
            bool teach = true;
            Text check = null;
            check = Button(_page.transform, "☑  Пройти обучение", new Vector2(80f, 380f), () =>
            {
                teach = !teach;
                check.text = (teach ? "☑  " : "☐  ") + "Пройти обучение";
            });
            Button(_page.transform, "ПОДПИСАТЬ", new Vector2(80f, 460f), () =>
            {
                string name = string.IsNullOrWhiteSpace(field.text) ? "Продюсер" : field.text.Trim();
                Hide();
                done?.Invoke(name, teach);
            });
        }

        public void Slate(int number, string producer, Action done)
        {
            Open();
            Title("СЕРИЯ " + number);
            Body(string.IsNullOrEmpty(producer) ? "ONLY WHAT MATTERS" : producer + "  ·  ONLY WHAT MATTERS");
            Button(_page.transform, "НА ПЛОЩАДКУ", new Vector2(80f, 320f), () =>
            {
                Hide();
                done?.Invoke();
            });
        }

        // Выбор каста: карточки участников (портрет, имя, черты, скрытая черта), счётчик мест и кнопка «Утвердить».
        public void PickCast(CastMember[] all, List<string> current, int min, int max, bool reveal, Action<List<string>> done, Action back = null)
        {
            Open();
            var picked = new List<string>();
            if (current != null)
            {
                foreach (var id in current)
                {
                    if (picked.Count < max && Array.Exists(all, m => m.id == id))
                        picked.Add(id);
                }
            }

            Title("КАСТ ВЫПУСКА");
            var intro = Label(_page.transform, "Кого пустишь в кадр в этом выпуске. Мест: " + max + (min > 0 ? ", минимум " + min : "") + ". Нажми на карточку, чтобы взять или убрать.",
                20, CastMuted);
            UiTypography.Apply(intro, TextRole.Body);
            Pin(intro.rectTransform, 80f, 112f, 1100f, 60f);

            // Счётчик мест: точки + «2 / 3».
            var meter = Label(_page.transform, "", 28, CastGold);
            UiTypography.Apply(meter, TextRole.Heading);
            meter.alignment = TextAnchor.UpperRight;
            Pin(meter.rectTransform, 1440f, 44f, 400f, 40f);
            var pips = new List<Image>();
            for (int i = 0; i < max; i++)
            {
                var pip = Box(_page.transform, "pip", CastChip);
                Pin(pip.rectTransform, 1840f - (max - i) * 30f, 96f, 22f, 22f);
                pips.Add(pip);
            }

            // Низ экрана создаётся после карточек, но Refresh видит его всегда.
            Image confirm = null;
            Button confirmButton = null;
            Text confirmText = null;
            Text status = null;

            // Карточки.
            int n = all.Length;
            float width = Mathf.Min(270f, (1760f - (n - 1) * 20f) / Mathf.Max(1, n));
            const float Height = 430f;
            const float Top = 190f;
            var cards = new List<(CastMember member, Image border, Image badge, Text hint, CanvasGroup group)>();
            for (int i = 0; i < n; i++)
            {
                var member = all[i];
                float x = 80f + i * (width + 20f);

                var border = Box(_page.transform, "cast_" + member.id, CastChip);
                Pin(border.rectTransform, x, Top, width, Height);
                var group = border.gameObject.AddComponent<CanvasGroup>();
                var button = border.gameObject.AddComponent<Button>();
                var colors = button.colors;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                colors.colorMultiplier = 1.3f;
                button.colors = colors;

                var body = Box(border.transform, "body", CastCard);
                Inset(body.rectTransform, 4f);

                var photoBg = Box(body.transform, "photo", CastPhoto);
                Pin(photoBg.rectTransform, 12f, 12f, width - 32f, 210f);
                if (member.portrait != null)
                {
                    var photo = Box(photoBg.transform, "img", Color.white);
                    Inset(photo.rectTransform, 6f);
                    photo.sprite = member.portrait;
                    photo.preserveAspect = true;
                }

                var badge = Box(body.transform, "badge", CastGold);
                Pin(badge.rectTransform, width - 140f, 20f, 112f, 30f);
                var badgeText = Label(badge.transform, "✓ В КАСТЕ", 16, CastInk);
                UiTypography.Apply(badgeText, TextRole.Caption);
                badgeText.fontStyle = FontStyle.Bold;
                badgeText.alignment = TextAnchor.MiddleCenter;
                Stretch(badgeText.rectTransform);

                var name = Label(body.transform, member.name, 28, CastLight);
                UiTypography.Apply(name, TextRole.Heading);
                Pin(name.rectTransform, 14f, 236f, width - 36f, 38f);

                // Видимые черты — «чипы».
                float chipX = 14f;
                float chipY = 282f;
                foreach (var trait in member.traits ?? new string[0])
                {
                    if (string.IsNullOrEmpty(trait))
                        continue;
                    var chipText = Label(body.transform, trait, 16, CastLight);
                    UiTypography.Apply(chipText, TextRole.Caption);
                    float w = chipText.preferredWidth + 20f;
                    if (chipX + w > width - 22f)
                    {
                        chipX = 14f;
                        chipY += 34f;
                    }

                    var chip = Box(body.transform, "chip", CastChip);
                    Pin(chip.rectTransform, chipX, chipY, w, 28f);
                    chipText.transform.SetParent(chip.transform, false);
                    chipText.alignment = TextAnchor.MiddleCenter;
                    Stretch(chipText.rectTransform);
                    chipX += w + 8f;
                }

                string secret = reveal ? member.secretKnown : member.secretHidden;
                if (!string.IsNullOrEmpty(secret))
                {
                    var secretText = Label(body.transform, reveal ? "Скрытая черта: " + secret : secret, 16, CastMuted);
                    UiTypography.Apply(secretText, TextRole.Caption);
                    secretText.fontStyle = FontStyle.Italic;
                    Pin(secretText.rectTransform, 14f, chipY + 40f, width - 36f, 24f);
                }

                var hint = Label(body.transform, "", 16, CastMuted);
                UiTypography.Apply(hint, TextRole.Caption);
                hint.alignment = TextAnchor.LowerCenter;
                Pin(hint.rectTransform, 14f, Height - 50f, width - 36f, 28f);

                cards.Add((member, border, badge, hint, group));
                button.onClick.AddListener(() =>
                {
                    if (picked.Contains(member.id))
                        picked.Remove(member.id);
                    else if (picked.Count < max)
                        picked.Add(member.id);
                    else
                        return;
                    Refresh();
                });
            }

            // Низ: кнопка «Утвердить» и подсказка.
            confirm = Box(_page.transform, "confirm", CastGold);
            Pin(confirm.rectTransform, 80f, Top + Height + 40f, 420f, 64f);
            confirmButton = confirm.gameObject.AddComponent<Button>();
            confirmText = Label(confirm.transform, "УТВЕРДИТЬ КАСТ", 20, CastInk);
            UiTypography.Apply(confirmText, TextRole.Button);
            confirmText.alignment = TextAnchor.MiddleCenter;
            Stretch(confirmText.rectTransform);
            status = Label(_page.transform, "", 19, CastMuted);
            UiTypography.Apply(status, TextRole.Label);
            status.alignment = TextAnchor.MiddleLeft;
            Pin(status.rectTransform, 524f, Top + Height + 40f, 900f, 64f);
            confirmButton.onClick.AddListener(() =>
            {
                if (picked.Count < min)
                    return;
                Hide();
                done?.Invoke(picked);
            });

            // «← В хаб» — передумал запускать выпуск.
            if (back != null)
            {
                var backBox = Box(_page.transform, "back", CastChip);
                Pin(backBox.rectTransform, 1440f, Top + Height + 40f, 400f, 64f);
                var backButton = backBox.gameObject.AddComponent<Button>();
                var backText = Label(backBox.transform, "←  В ХАБ", 20, CastLight);
                UiTypography.Apply(backText, TextRole.Button);
                backText.alignment = TextAnchor.MiddleCenter;
                Stretch(backText.rectTransform);
                backButton.onClick.AddListener(() =>
                {
                    Hide();
                    back();
                });
            }

            var focusGo = new GameObject("focus", typeof(RectTransform));
            focusGo.transform.SetParent(_page.transform, false);
            Focus = focusGo.GetComponent<RectTransform>();
            Pin(Focus, 64f, Top - 16f, Mathf.Min(1792f, n * (width + 20f) + 12f), Height + 32f);

            Refresh();

            void Refresh()
            {
                bool full = picked.Count >= max;
                foreach (var c in cards)
                {
                    bool on = picked.Contains(c.member.id);
                    c.border.color = on ? CastGold : CastChip;
                    c.badge.gameObject.SetActive(on);
                    c.group.alpha = !on && full ? 0.45f : 1f;
                    c.hint.text = on ? "нажми, чтобы убрать" : full ? "мест нет" : "нажми, чтобы взять";
                    c.hint.color = on ? CastGold : CastMuted;
                }

                for (int i = 0; i < pips.Count; i++)
                    pips[i].color = i < picked.Count ? CastGold : CastChip;
                meter.text = picked.Count + " / " + max;
                bool ready = picked.Count >= min;
                confirm.color = ready ? CastGold : CastChip;
                confirmText.color = ready ? CastInk : CastMuted;
                confirmButton.interactable = ready;
                int need = min - picked.Count;
                status.text = ready
                    ? (full ? "Все места заняты — можно утверждать." : "Можно утверждать или добрать ещё " + (max - picked.Count) + ".")
                    : "Нужно ещё " + need + " " + Plural(need);
            }
        }

        static readonly Color CastGold = new Color(0.96f, 0.78f, 0.22f, 1f);
        static readonly Color CastInk = new Color(0.07f, 0.055f, 0.08f, 1f);
        static readonly Color CastLight = new Color(0.96f, 0.93f, 0.88f, 1f);
        static readonly Color CastMuted = new Color(0.66f, 0.61f, 0.68f, 1f);
        static readonly Color CastCard = new Color(0.15f, 0.13f, 0.18f, 1f);
        static readonly Color CastPhoto = new Color(0.1f, 0.09f, 0.12f, 1f);
        static readonly Color CastChip = new Color(0.25f, 0.22f, 0.29f, 1f);

        static string Plural(int n)
        {
            n = Mathf.Abs(n) % 100;
            if (n >= 11 && n <= 14)
                return "участников";
            switch (n % 10)
            {
                case 1: return "участник";
                case 2:
                case 3:
                case 4: return "участника";
                default: return "участников";
            }
        }

        Image Box(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        static void Inset(RectTransform rect, float pad)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(pad, pad);
            rect.offsetMax = new Vector2(-pad, -pad);
        }

        public void ShowEvent(EventRoomDefinition def, Action<EventChoice> done)
        {
            Open();
            Title(def != null ? def.title : "СОБЫТИЕ");
            Body(def != null && !string.IsNullOrEmpty(def.body) ? def.body : "Нечего решать.");
            float y = 340f;
            var choices = def != null ? def.choices : null;
            if (choices == null || choices.Count == 0)
            {
                Button(_page.transform, "ДАЛЬШЕ", new Vector2(80f, y), () =>
                {
                    Hide();
                    done?.Invoke(null);
                });
                return;
            }

            for (int i = 0; i < choices.Count && i < 3; i++)
            {
                var choice = choices[i];
                Button(_page.transform, choice.label, new Vector2(80f, y), () =>
                {
                    Hide();
                    done?.Invoke(choice);
                });
                y += 64f;
            }

            var focusGo = new GameObject("focus", typeof(RectTransform));
            focusGo.transform.SetParent(_page.transform, false);
            Focus = focusGo.GetComponent<RectTransform>();
            Pin(Focus, 60f, 320f, 980f, Mathf.Max(80f, y - 320f));
        }

        public void ShowResult(string text, Action done)
        {
            Open();
            Title("ПОСЛЕДСТВИЕ");
            Body(text);
            Button(_page.transform, "НА КАРТУ ЭПИЗОДА", new Vector2(80f, 360f), () =>
            {
                Hide();
                done?.Invoke();
            });
        }

        public void ShowDeals(string header, List<MarketingOffer> offers, Action<MarketingOffer> pick, Action leave)
        {
            Open();
            Title("МАРКЕТИНГ");
            Body(header);
            float y = 250f;
            if (offers != null)
            {
                for (int i = 0; i < offers.Count; i++)
                {
                    var offer = offers[i];
                    string price = offer.kind == OfferKind.Contract ? "контракт" : offer.price + " нал";
                    Button(_page.transform, offer.title + "  ·  " + price + "\n" + offer.blurb, new Vector2(80f, y), () => pick?.Invoke(offer));
                    y += 72f;
                }
            }

            Button(_page.transform, "ЗАКРЫТЬ КОМНАТУ", new Vector2(80f, y + 12f), () =>
            {
                Hide();
                leave?.Invoke();
            });
        }

        void Open()
        {
            Hide();
            var go = new GameObject("page", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            Stretch(go.GetComponent<RectTransform>());
            go.GetComponent<Image>().color = new Color(0.07f, 0.055f, 0.08f, 1f);
            _page = go;
        }

        void Title(string text)
        {
            var label = Label(_page.transform, text, 32, new Color(0.96f, 0.78f, 0.22f, 1f));
            UiTypography.Apply(label, TextRole.Title);
            Pin(label.rectTransform, 80f, 48f, 1400f, 56f);
        }

        void Body(string text)
        {
            var label = Label(_page.transform, text, 20, new Color(0.9f, 0.86f, 0.8f, 1f));
            UiTypography.Apply(label, TextRole.Body);
            Pin(label.rectTransform, 80f, 120f, 1400f, 140f);
        }

        Text Button(Transform parent, string text, Vector2 pos, Action click)
        {
            var go = new GameObject("btn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(pos.x, -pos.y);
            rect.sizeDelta = new Vector2(900f, 56f);
            go.GetComponent<Image>().color = new Color(0.2f, 0.16f, 0.22f, 1f);
            var label = Label(go.transform, text, 18, Color.white);
            UiTypography.Apply(label, TextRole.Button);
            Stretch(label.rectTransform);
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.offsetMin = new Vector2(16f, 0f);
            go.GetComponent<Button>().onClick.AddListener(() => click());
            return label;
        }

        Text Label(Transform parent, string text, int size, Color color)
        {
            var go = new GameObject("t", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = _font;
            label.text = text;
            label.fontSize = size;
            UiTypography.Apply(label, UiTypography.ForSize(size));
            label.color = color;
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
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
