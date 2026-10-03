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
            Pin(rect, 80f, 300f, 700f, 56f);
            fieldGo.GetComponent<Image>().color = new Color(0.14f, 0.11f, 0.16f, 1f);
            var text = Label(fieldGo.transform, "", 22, Color.white);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(12f, 0f);
            text.supportRichText = false;
            var field = fieldGo.GetComponent<InputField>();
            field.textComponent = text;
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

        public void PickCast(CastMember[] all, List<string> current, int min, int max, bool reveal, Action<List<string>> done)
        {
            Open();
            var picked = new List<string>();
            if (current != null)
                picked.AddRange(current);
            Title("КАСТ ВЫПУСКА");
            var status = Label(_page.transform, "", 18, new Color(0.9f, 0.86f, 0.8f, 1f));
            Pin(status.rectTransform, 80f, 120f, 900f, 32f);
            float y = 170f;
            var rows = new List<(string id, string line, Text label)>();
            for (int i = 0; i < all.Length; i++)
            {
                var member = all[i];
                string secret = reveal ? member.secretKnown : member.secretHidden;
                string line = member.name + "  ·  " + string.Join(", ", member.traits);
                if (!string.IsNullOrEmpty(secret))
                    line += "  ·  " + secret;
                var label = Button(_page.transform, line, new Vector2(80f, y), () =>
                {
                    if (picked.Contains(member.id))
                        picked.Remove(member.id);
                    else if (picked.Count < max)
                        picked.Add(member.id);
                    for (int r = 0; r < rows.Count; r++)
                    {
                        bool on = picked.Contains(rows[r].id);
                        rows[r].label.text = (on ? "●  " : "○  ") + rows[r].line;
                    }

                    status.text = picked.Count + " / " + max + "   минимум " + min;
                });
                rows.Add((member.id, line, label));
                bool already = picked.Contains(member.id);
                label.text = (already ? "●  " : "○  ") + line;
                y += 64f;
            }

            status.text = picked.Count + " / " + max + "   минимум " + min;
            var focusGo = new GameObject("focus", typeof(RectTransform));
            focusGo.transform.SetParent(_page.transform, false);
            Focus = focusGo.GetComponent<RectTransform>();
            Pin(Focus, 60f, 150f, 980f, Mathf.Max(80f, y - 130f));
            Button(_page.transform, "УТВЕРДИТЬ", new Vector2(80f, y + 16f), () =>
            {
                if (picked.Count < min)
                {
                    status.text = "Нужно хотя бы " + min;
                    return;
                }

                Hide();
                done?.Invoke(picked);
            });
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
            Pin(label.rectTransform, 80f, 48f, 1400f, 48f);
        }

        void Body(string text)
        {
            var label = Label(_page.transform, text, 20, new Color(0.9f, 0.86f, 0.8f, 1f));
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
