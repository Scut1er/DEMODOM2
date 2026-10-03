using System;
using System.Collections.Generic;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Комната маркетинга (пак EventScreen): сверху — УЕ выпуска, репутация у спонсоров и слоты контрактов,
    // слева ПОКУПКИ (УЕ → карта или бонус), справа СПОНСОРСКИЕ КОНТРАКТЫ (карта бренда, задача, успех и провал).
    // Каждое предложение — карточка: картинка, что получишь, сколько действует, цена, кнопка или состояние.
    public class MarketingView : MonoBehaviour
    {
        const string Kit = "Art/UI/EventScreen/";
        const float ColumnWidth = 880f;
        const float BuyHeight = 136f;
        const float DealHeight = 204f;
        static readonly Color Gold = new Color(0.96f, 0.76f, 0.31f, 1f);
        static readonly Color Green = new Color(0.21f, 0.93f, 0.54f, 1f);
        static readonly Color Red = new Color(1f, 0.3f, 0.36f, 1f);
        static readonly Color Light = new Color(0.95f, 0.93f, 0.91f, 1f);
        static readonly Color Muted = new Color(0.72f, 0.66f, 0.67f, 1f);
        static readonly Color Fill = new Color(0.07f, 0.035f, 0.055f, 1f);

        RectTransform _root;
        Text _title;
        Text _cash;
        Text _reputation;
        RectTransform _reputationFill;
        Text _contracts;
        Text _notice;
        Image _noticePlate;
        RectTransform _buys;
        RectTransform _deals;
        Action<MarketingOffer> _pick;
        Action _leave;
        float _noticeUntil;

        public RectTransform Focus => _root;
        // Обучение указывает на колонку целиком (окно списка).
        public RectTransform BuysFocus => _buys != null ? (RectTransform)_buys.parent : null;
        public RectTransform DealsFocus => _deals != null ? (RectTransform)_deals.parent : null;

        public static MarketingView Create(Transform canvas)
        {
            var go = new GameObject("MarketingRoom", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var view = go.AddComponent<MarketingView>();
            view.Build();
            go.SetActive(false);
            return view;
        }

        static Sprite Sprite(string path) => UiKit.Load(Kit + path);

        void Build()
        {
            _root = (RectTransform)transform;
            UiKit.Stretch(_root);
            UiKit.Backdrop(_root, "Art/Intro/bg/scene_3", new Rect(0f, 0.3f, 1f, 0.7f), 0.7f);
            var shade = UiKit.Img("shade", _root, null, new Color(0.03f, 0.015f, 0.03f, 0.78f), true);
            UiKit.Stretch(shade.rectTransform);

            // ---------- шапка ----------
            _title = UiKit.Txt("title", _root, "МАРКЕТИНГ", 46, Gold, TextAnchor.MiddleLeft, UiKit.Display != null ? UiKit.Display : UiKit.Body);
            UiKit.Shadow(_title, 3f);
            _title.resizeTextForBestFit = true;
            _title.resizeTextMinSize = 26;
            _title.resizeTextMaxSize = 46;
            Pin(_title.rectTransform, 64f, 34f, 820f, 70f);
            var tag = UiKit.Txt("tag", _root, "Деньги выпуска — на бонусы, бренды — на контракты. Всё действует до эфира этого выпуска.", 18, Muted, TextAnchor.UpperLeft, UiKit.Body);
            Pin(tag.rectTransform, 66f, 104f, 820f, 26f);

            var cashChip = Chip(_root, "icon_budget_tint", "УЕ ВЫПУСКА", 908f, out _cash);
            var repChip = Chip(_root, "icon_rating_tint", "РЕПУТАЦИЯ У СПОНСОРОВ", 1228f, out _reputation);
            var track = UiKit.Img("track", repChip, UiKit.Load("Art/UI/HellTube/Progress/progress_thin_bg_9slice"), Color.white);
            track.type = Image.Type.Sliced;
            Pin(track.rectTransform, 62f, 70f, 236f, 9f);
            var fill = UiKit.Img("fill", track.rectTransform, UiKit.Load("Art/UI/HellTube/Progress/progress_thin_fill_white_9slice"), Green);
            fill.type = Image.Type.Sliced;
            _reputationFill = fill.rectTransform;
            _reputationFill.anchorMin = Vector2.zero;
            _reputationFill.anchorMax = new Vector2(0.25f, 1f);
            _reputationFill.offsetMin = _reputationFill.offsetMax = Vector2.zero;
            Chip(_root, "icon_contract_tint", "КОНТРАКТЫ ВЫПУСКА", 1548f, out _contracts);
            cashChip.name = "CashChip";

            // ---------- подтверждение после покупки ----------
            _noticePlate = UiKit.Img("notice", _root, Sprite("Badges/badge_success_blank"), Color.white);
            _noticePlate.type = Image.Type.Sliced;
            Pin(_noticePlate.rectTransform, 310f, 136f, 1300f, 64f);
            _notice = UiKit.Txt("text", _noticePlate.rectTransform, "", 19, Light, TextAnchor.MiddleCenter, UiKit.Body);
            _notice.fontStyle = FontStyle.Bold;
            _notice.supportRichText = true;
            _notice.resizeTextForBestFit = true;
            _notice.resizeTextMinSize = 12;
            _notice.resizeTextMaxSize = 18;
            _notice.verticalOverflow = VerticalWrapMode.Truncate;
            _notice.rectTransform.anchorMin = Vector2.zero;
            _notice.rectTransform.anchorMax = Vector2.one;
            _notice.rectTransform.offsetMin = new Vector2(30f, 8f);
            _notice.rectTransform.offsetMax = new Vector2(-30f, -8f);
            _noticePlate.gameObject.SetActive(false);

            // ---------- колонки ----------
            _buys = Column("ПОКУПКИ", "тратишь УЕ выпуска — получаешь карту или бонус", "icon_budget_tint", Gold, 60f);
            _deals = Column("СПОНСОРСКИЕ КОНТРАКТЫ", "бесплатно — платят, если кадр с брендом попадёт в эфир", "icon_contract_tint", Green, 60f + ColumnWidth + 40f);

            // ---------- выход ----------
            var leave = Panel(_root, "Buttons/button_option_primary_9slice", 1f);
            Pin(leave, 60f, 976f, 420f, 70f);
            var leaveButton = leave.gameObject.AddComponent<Button>();
            leaveButton.targetGraphic = leave.Find("Frame").GetComponent<Image>();
            Hover(leaveButton);
            var leaveText = UiKit.Txt("t", leave, "ЗАКРЫТЬ КОМНАТУ  →", 22, Light, TextAnchor.MiddleCenter, UiKit.Body);
            leaveText.fontStyle = FontStyle.Bold;
            UiKit.Stretch(leaveText.rectTransform);
            leaveButton.onClick.AddListener(() =>
            {
                Hide();
                _leave?.Invoke();
            });
            var hint = UiKit.Txt("hint", _root, "Купленное и контракты действуют до эфира этого выпуска. В комнату не вернуться.", 18, Muted, TextAnchor.MiddleLeft, UiKit.Body);
            Pin(hint.rectTransform, 510f, 990f, 1300f, 40f);
        }

        // Плашка-показатель в шапке: значок, подпись, значение.
        RectTransform Chip(RectTransform parent, string icon, string label, float x, out Text value)
        {
            var chip = Panel(parent, "Panels/panel_side_9slice", 2f);
            Pin(chip, x, 34f, 300f, 92f);
            var glyph = UiKit.Img("icon", chip, Sprite("Icons/" + icon) ?? Sprite("Icons/icon_people"), Color.white);
            glyph.preserveAspect = true;
            Pin(glyph.rectTransform, 18f, 20f, 34f, 34f);
            var caption = UiKit.Txt("label", chip, label, 14, Muted, TextAnchor.UpperLeft, UiKit.Body);
            caption.fontStyle = FontStyle.Bold;
            Pin(caption.rectTransform, 62f, 14f, 228f, 20f);
            value = UiKit.Txt("value", chip, "", 26, Light, TextAnchor.UpperLeft, UiKit.Body);
            value.fontStyle = FontStyle.Bold;
            value.supportRichText = true;
            value.resizeTextForBestFit = true;
            value.resizeTextMinSize = 14;
            value.resizeTextMaxSize = 26;
            Pin(value.rectTransform, 62f, 34f, 228f, 34f);
            return chip;
        }

        RectTransform Column(string head, string sub, string icon, Color accent, float x)
        {
            var glyph = UiKit.Img("icon", _root, Sprite("Icons/" + icon), accent);
            glyph.preserveAspect = true;
            Pin(glyph.rectTransform, x, 212f, 34f, 34f);
            var title = UiKit.Txt("head", _root, head, 26, accent, TextAnchor.MiddleLeft, UiKit.Body);
            title.fontStyle = FontStyle.Bold;
            Pin(title.rectTransform, x + 46f, 208f, ColumnWidth - 46f, 40f);
            var note = UiKit.Txt("sub", _root, sub, 17, Muted, TextAnchor.UpperLeft, UiKit.Body);
            Pin(note.rectTransform, x + 46f, 246f, ColumnWidth - 46f, 26f);
            var divider = UiKit.Img("divider", _root, Sprite("Decor/divider_event_red"), new Color(accent.r, accent.g, accent.b, 0.8f));
            Pin(divider.rectTransform, x, 274f, ColumnWidth, 12f);

            // Список прокручивается, если предложений больше, чем помещается.
            var view = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
            view.SetParent(_root, false);
            Pin(view, x, 292f, ColumnWidth, 668f);
            view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            var content = new GameObject("items", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            content.SetParent(view, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.GetComponent<ScrollRect>();
            scroll.viewport = view;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return content;
        }

        public void Show(string title, MarketingStatus status, List<OfferView> offers, Action<MarketingOffer> pick, Action leave)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _pick = pick;
            _leave = leave;
            _title.text = string.IsNullOrEmpty(title) ? "МАРКЕТИНГ" : title;
            if (status != null)
            {
                _cash.text = status.cash + " <size=18><color=#B7A9AB>УЕ</color></size>";
                _cash.color = Gold;
                _reputation.text = status.reputation + " <size=17><color=#B7A9AB>· " + status.tier + "</color></size>";
                float share = Mathf.Clamp01(status.reputation / 100f);
                _reputationFill.anchorMax = new Vector2(share, 1f);
                _reputationFill.GetComponent<Image>().color = share >= 0.6f ? Green : share >= 0.3f ? Gold : Red;
                bool full = status.contracts >= status.contractSlots;
                _contracts.text = status.contracts + " / " + status.contractSlots + (full ? " <size=17><color=#FF7A5C>· заняты</color></size>" : " <size=17><color=#B7A9AB>· свободно " + (status.contractSlots - status.contracts) + "</color></size>");
            }

            if (Time.unscaledTime > _noticeUntil)
                _noticePlate.gameObject.SetActive(false);
            float buyScroll = _buys.anchoredPosition.y;
            float dealScroll = _deals.anchoredPosition.y;
            Clear(_buys);
            Clear(_deals);
            int buys = 0;
            int deals = 0;
            foreach (var v in offers)
            {
                if (v.contract)
                {
                    Deal(_deals, v);
                    deals++;
                }
                else
                {
                    Buy(_buys, v);
                    buys++;
                }
            }

            if (buys == 0)
                Empty(_buys, "Сегодня ничего не продают.");
            if (deals == 0)
                Empty(_deals, "Спонсоров сегодня нет.");
            // После покупки витрина перерисовывается — прокрутка остаётся там, где была.
            Restore(_buys, buyScroll);
            Restore(_deals, dealScroll);
        }

        static void Restore(RectTransform content, float y)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            var view = (RectTransform)content.parent;
            float max = Mathf.Max(0f, content.rect.height - view.rect.height);
            content.anchoredPosition = new Vector2(0f, Mathf.Clamp(y, 0f, max));
        }

        // Подтверждение после покупки/подписи: что получили и что стало с деньгами.
        public void Notice(string text, bool ok)
        {
            _notice.text = text;
            _notice.color = ok ? Light : new Color(1f, 0.82f, 0.74f, 1f);
            _noticePlate.sprite = Sprite(ok ? "Badges/badge_success_blank" : "Badges/badge_warning_blank");
            _noticePlate.gameObject.SetActive(true);
            _noticeUntil = Time.unscaledTime + 6f;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (_noticePlate != null && _noticePlate.gameObject.activeSelf && Time.unscaledTime > _noticeUntil)
                _noticePlate.gameObject.SetActive(false);
        }

        // Старые карточки убираются из раскладки сразу (Destroy — только в конце кадра).
        static void Clear(RectTransform list)
        {
            for (int i = list.childCount - 1; i >= 0; i--)
            {
                var child = list.GetChild(i).gameObject;
                child.SetActive(false);
                child.transform.SetParent(null, false);
                Destroy(child);
            }
        }

        static void Empty(RectTransform list, string text)
        {
            var row = Item(list, 60f);
            var t = UiKit.Txt("empty", row, text, 20, Muted, TextAnchor.MiddleLeft, UiKit.Body);
            UiKit.Stretch(t.rectTransform, 8f);
        }

        // ---------- карточки ----------

        // Покупка: картинка (арт карты или значок бонуса), название, что получишь и сколько действует; справа цена и кнопка.
        void Buy(RectTransform list, OfferView v)
        {
            var row = Item(list, BuyHeight);
            var plate = Panel(row, "Panels/panel_option_gold_9slice", 2f);
            UiKit.Stretch(plate);
            Dim(row, v);
            Picture(plate, v, Gold, 18f, 18f, 168f, 100f);
            var title = UiKit.Txt("title", plate, v.title, 22, Light, TextAnchor.UpperLeft, UiKit.Body);
            title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 15;
            title.resizeTextMaxSize = 22;
            Pin(title.rectTransform, 204f, 16f, 450f, 30f);
            Line(plate, "icon_quality_tint", "<color=#F2C35C>Получите:</color> " + v.gets, 204f, 50f, 450f, 46f, Light);
            if (!string.IsNullOrEmpty(v.lifetime))
                Line(plate, null, "<color=#B7A9AB>Действует:</color> " + v.lifetime, 204f, 98f, 450f, 24f, Muted);
            Price(plate, v.price, Gold);
            Action(plate, v, "КУПИТЬ");
        }

        // Контракт: бренд и карта бренда, задача, что будет при успехе и при провале; справа выплата и кнопка.
        void Deal(RectTransform list, OfferView v)
        {
            var row = Item(list, DealHeight);
            var plate = Panel(row, "Panels/panel_option_green_9slice", 2f);
            UiKit.Stretch(plate);
            Dim(row, v);
            Picture(plate, v, Green, 18f, 18f, 168f, 100f);
            var brand = UiKit.Txt("brand", plate, string.IsNullOrEmpty(v.brand) ? v.title : v.brand.ToUpperInvariant() + "  <color=#F3EEE9><size=18>· " + v.title + "</size></color>",
                22, Green, TextAnchor.UpperLeft, UiKit.Body);
            brand.fontStyle = FontStyle.Bold;
            brand.supportRichText = true;
            brand.resizeTextForBestFit = true;
            brand.resizeTextMinSize = 14;
            brand.resizeTextMaxSize = 22;
            Pin(brand.rectTransform, 204f, 14f, 450f, 30f);
            Line(plate, "icon_quality_tint", "<color=#F2C35C>Задача:</color> " + v.task, 204f, 46f, 450f, 46f, Light);
            Line(plate, null, "<color=#B7A9AB>Карта «" + v.card + "»  ·  " + v.lifetime + "</color>", 204f, 94f, 450f, 24f, Muted);

            // Исход контракта — две плашки рядом: что получишь при успехе и чем рискуешь при провале.
            Outcome(plate, true, v.success, 18f, 132f, 410f);
            Outcome(plate, false, v.fail, 438f, 132f, 230f);
            if (!string.IsNullOrEmpty(v.requirement))
            {
                var need = UiKit.Txt("need", plate, "требование: " + v.requirement, 14, Muted, TextAnchor.LowerRight, UiKit.Body);
                need.rectTransform.anchorMin = need.rectTransform.anchorMax = new Vector2(1f, 0f);
                need.rectTransform.pivot = new Vector2(1f, 0f);
                need.rectTransform.anchoredPosition = new Vector2(-20f, 10f);
                need.rectTransform.sizeDelta = new Vector2(180f, 20f);
            }

            Price(plate, v.price, Green);
            Action(plate, v, "ПОДПИСАТЬ");
        }

        static RectTransform Item(RectTransform list, float height)
        {
            var row = new GameObject("offer", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            row.SetParent(list, false);
            row.GetComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        // Недоступное — приглушено, но читается: видно, чего не хватило.
        static void Dim(RectTransform row, OfferView v)
        {
            var group = row.gameObject.AddComponent<CanvasGroup>();
            group.alpha = v.available || v.done ? 1f : 0.6f;
        }

        static void Picture(RectTransform plate, OfferView v, Color accent, float x, float y, float w, float h)
        {
            var edge = UiKit.Img("edge", plate, null, new Color(accent.r, accent.g, accent.b, 0.7f));
            Pin(edge.rectTransform, x - 2f, y - 2f, w + 4f, h + 4f);
            var window = new GameObject("picture", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            window.SetParent(plate, false);
            Pin(window, x, y, w, h);
            var back = UiKit.Img("back", window, null, new Color(0.06f, 0.03f, 0.05f, 1f));
            UiKit.Stretch(back.rectTransform);
            var glow = UiKit.Img("glow", window, UiKit.Halo(), new Color(accent.r, accent.g, accent.b, 0.35f));
            UiKit.Stretch(glow.rectTransform);
            var art = v.cardDef != null ? CardVisuals.Art(v.cardDef) : null;
            if (art != null)
            {
                var img = UiKit.Img("art", window, art, Color.white);
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                var fit = img.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = art.rect.width / Mathf.Max(1f, art.rect.height);
            }
            else
            {
                var icon = UiKit.Img("icon", window, Sprite("Icons/" + (v.icon ?? "icon_drama_tint")), Color.white);
                icon.preserveAspect = true;
                icon.rectTransform.anchorMin = new Vector2(0.28f, 0.14f);
                icon.rectTransform.anchorMax = new Vector2(0.72f, 0.86f);
                icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
            }
        }

        // Строка карточки; icon — значок слева (null — без значка, текст с того же отступа).
        static void Line(RectTransform plate, string icon, string text, float x, float y, float w, float h, Color color)
        {
            if (!string.IsNullOrEmpty(icon))
            {
                var glyph = UiKit.Img("icon", plate, Sprite("Icons/" + icon), Color.white);
                glyph.preserveAspect = true;
                Pin(glyph.rectTransform, x, y + 2f, 18f, 18f);
            }

            var t = UiKit.Txt("line", plate, text, 17, color, TextAnchor.UpperLeft, UiKit.Body);
            t.supportRichText = true;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 12;
            t.resizeTextMaxSize = 17;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            Pin(t.rectTransform, x + 26f, y, w - 26f, h);
        }

        static void Outcome(RectTransform plate, bool success, string text, float x, float y, float w)
        {
            var chip = Panel(plate, success ? "Panels/panel_stats_green_9slice" : "Panels/panel_option_red_9slice", 3f);
            Pin(chip, x, y, w, 50f);
            var mark = UiKit.Txt("mark", chip, success ? "✓ УСПЕХ" : "✗ ПРОВАЛ", 14, success ? Green : Red, TextAnchor.UpperLeft, UiKit.Body);
            mark.fontStyle = FontStyle.Bold;
            Pin(mark.rectTransform, 14f, 6f, w - 20f, 18f);
            var t = UiKit.Txt("text", chip, text, 16, Light, TextAnchor.UpperLeft, UiKit.Body);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 11;
            t.resizeTextMaxSize = 16;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            Pin(t.rectTransform, 14f, 24f, w - 22f, 22f);
        }

        static void Price(RectTransform plate, string price, Color color)
        {
            var badge = UiKit.Img("price", plate, CardVisuals.Element("CostBadge"), Color.white);
            badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(1f, 1f);
            badge.rectTransform.pivot = new Vector2(1f, 1f);
            badge.rectTransform.anchoredPosition = new Vector2(-20f, -16f);
            badge.rectTransform.sizeDelta = new Vector2(170f, 52f);
            var t = UiKit.Txt("value", badge.rectTransform, price, 22, color, TextAnchor.MiddleCenter, UiKit.Body);
            t.fontStyle = FontStyle.Bold;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 13;
            t.resizeTextMaxSize = 22;
            UiKit.Stretch(t.rectTransform, 10f);
        }

        // Кнопка или состояние: КУПИТЬ / ПОДПИСАТЬ, штамп «куплено ✓», или почему нельзя.
        void Action(RectTransform plate, OfferView v, string verb)
        {
            if (v.available)
            {
                var b = Panel(plate, v.contract ? "Buttons/button_option_success_9slice" : "Buttons/button_option_primary_9slice", 1.2f);
                b.anchorMin = b.anchorMax = new Vector2(1f, 1f);
                b.pivot = new Vector2(1f, 1f);
                b.anchoredPosition = new Vector2(-20f, -76f);
                b.sizeDelta = new Vector2(170f, 52f);
                var button = b.gameObject.AddComponent<Button>();
                button.targetGraphic = b.Find("Frame").GetComponent<Image>();
                Hover(button);
                var t = UiKit.Txt("t", b, verb, 19, Light, TextAnchor.MiddleCenter, UiKit.Body);
                t.fontStyle = FontStyle.Bold;
                UiKit.Stretch(t.rectTransform);
                var offer = v.offer;
                button.onClick.AddListener(() => _pick?.Invoke(offer));
                return;
            }

            var stamp = UiKit.Img("state", plate, Sprite(v.done ? "Badges/badge_success_blank" : "Badges/badge_warning_blank"), Color.white);
            stamp.type = Image.Type.Sliced;
            stamp.rectTransform.anchorMin = stamp.rectTransform.anchorMax = new Vector2(1f, 1f);
            stamp.rectTransform.pivot = new Vector2(1f, 1f);
            stamp.rectTransform.anchoredPosition = new Vector2(-20f, -76f);
            stamp.rectTransform.sizeDelta = new Vector2(170f, 52f);
            string state = v.done ? (v.contract ? "ПРИНЯТ ✓" : "КУПЛЕНО ✓") : v.reason;
            var s = UiKit.Txt("text", stamp.rectTransform, state, v.done ? 19 : 15, v.done ? Green : new Color(1f, 0.7f, 0.5f, 1f), TextAnchor.MiddleCenter, UiKit.Body);
            s.fontStyle = FontStyle.Bold;
            s.resizeTextForBestFit = true;
            s.resizeTextMinSize = 11;
            s.resizeTextMaxSize = v.done ? 19 : 15;
            UiKit.Stretch(s.rectTransform, 8f);
        }

        // ---------- построение ----------

        // Окно пака: сплошная тёмная заливка, поверх — рамка 9-slice (k — во сколько раз тоньше родной кромки).
        static RectTransform Panel(RectTransform parent, string sprite, float k)
        {
            var holder = UiKit.Rect("panel", parent);
            var fill = UiKit.Img("Fill", holder, null, Fill, true);
            var art = Sprite(sprite);
            UiKit.Stretch(fill.rectTransform, art != null ? 6f : 0f);
            if (art != null)
            {
                var frame = UiKit.Img("Frame", holder, art, Color.white);
                frame.type = Image.Type.Sliced;
                frame.pixelsPerUnitMultiplier = k;
                UiKit.Stretch(frame.rectTransform);
            }
            else
            {
                fill.name = "Frame";
            }

            return holder;
        }

        static void Hover(Button button)
        {
            var colors = button.colors;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.colorMultiplier = 1.4f;
            button.colors = colors;
        }

        static void Pin(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }
    }
}
