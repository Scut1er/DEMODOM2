using System;
using System.Collections.Generic;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Комната маркетинга: слева ПОКУПКИ (тратишь УЕ — получаешь карту или бонус), справа СПОНСОРСКИЕ КОНТРАКТЫ
    // (бренд, задача, карта, выплата, последствия, репутация). Каждое предложение — карточка с ценой, сроком
    // и причиной, если нельзя. После покупки — подтверждение сверху и состояние «куплено / принят».
    public class MarketingView : MonoBehaviour
    {
        const float ColumnWidth = 860f;
        RectTransform _root;
        Text _title;
        Text _status;
        Text _notice;
        RectTransform _buys;
        RectTransform _deals;
        Action<MarketingOffer> _pick;
        Action _leave;
        float _noticeUntil;

        public RectTransform Focus => _root;
        public RectTransform BuysFocus => _buys;
        public RectTransform DealsFocus => _deals;

        public static MarketingView Create(Transform canvas)
        {
            var go = new GameObject("MarketingRoom", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var view = go.AddComponent<MarketingView>();
            view.Build();
            go.SetActive(false);
            return view;
        }

        void Build()
        {
            _root = (RectTransform)transform;
            UiKit.Stretch(_root);
            UiKit.Backdrop(_root, "Art/Intro/bg/scene_3", new Rect(0f, 0.3f, 1f, 0.7f), 0.7f);
            var shade = UiKit.Img("shade", _root, null, new Color(0.03f, 0.015f, 0.03f, 0.82f), true);
            UiKit.Stretch(shade.rectTransform);

            _title = UiKit.Txt("title", _root, "МАРКЕТИНГ", 46, UiKit.Gold, TextAnchor.UpperLeft, UiKit.Body);
            _title.fontStyle = FontStyle.Bold;
            UiKit.Shadow(_title);
            UiKit.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -40f), new Vector2(1200f, 60f));
            _status = UiKit.Txt("status", _root, "", 21, UiKit.Paper, TextAnchor.UpperLeft, UiKit.Body);
            _status.supportRichText = true;
            UiKit.Place(_status.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -104f), new Vector2(1500f, 30f));
            _notice = UiKit.Txt("notice", _root, "", 22, UiKit.Good, TextAnchor.UpperLeft, UiKit.Body);
            _notice.fontStyle = FontStyle.Bold;
            _notice.supportRichText = true;
            UiKit.Place(_notice.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -140f), new Vector2(1700f, 32f));

            _buys = Column("ПОКУПКИ", "тратишь УЕ выпуска — получаешь карту или бонус", 80f);
            _deals = Column("СПОНСОРСКИЕ КОНТРАКТЫ", "бесплатно — платят, если кадр с брендом попадёт в эфир", 80f + ColumnWidth + 60f);

            var leave = UiKit.Img("leave", _root, null, Color.white, true);
            var leaveButton = leave.gameObject.AddComponent<Button>();
            leaveButton.targetGraphic = leave;
            UiKit.Place(leave.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(80f, 40f), new Vector2(420f, 74f));
            var leaveText = UiKit.Txt("t", leave.rectTransform, "ЗАКРЫТЬ КОМНАТУ  →", 22, UiKit.Paper, TextAnchor.MiddleCenter, UiKit.Body);
            leaveText.fontStyle = FontStyle.Bold;
            UiKit.Stretch(leaveText.rectTransform);
            UiKit.Primary(leaveButton, 22);
            leaveButton.onClick.AddListener(() =>
            {
                Hide();
                _leave?.Invoke();
            });
            var hint = UiKit.Txt("hint", _root, "Купленное и контракты действуют до эфира этого выпуска. В комнату не вернуться.", 18, UiKit.Muted, TextAnchor.MiddleLeft, UiKit.Body);
            UiKit.Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(530f, 58f), new Vector2(1200f, 40f));
        }

        RectTransform Column(string head, string sub, float x)
        {
            var title = UiKit.Txt("head", _root, head, 26, UiKit.Gold, TextAnchor.UpperLeft, UiKit.Body);
            title.fontStyle = FontStyle.Bold;
            UiKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -186f), new Vector2(ColumnWidth, 34f));
            var note = UiKit.Txt("sub", _root, sub, 17, UiKit.Muted, TextAnchor.UpperLeft, UiKit.Body);
            UiKit.Place(note.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -220f), new Vector2(ColumnWidth, 26f));
            var list = UiKit.Rect("list", _root);
            UiKit.Place(list, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -254f), new Vector2(ColumnWidth, 680f));
            return list;
        }

        public void Show(string title, string status, List<OfferView> offers, Action<MarketingOffer> pick, Action leave)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _pick = pick;
            _leave = leave;
            _title.text = string.IsNullOrEmpty(title) ? "МАРКЕТИНГ" : title;
            _status.text = status;
            if (Time.unscaledTime > _noticeUntil)
                _notice.text = "";
            Clear(_buys);
            Clear(_deals);
            float buyY = 0f;
            float dealY = 0f;
            foreach (var v in offers)
            {
                if (v.contract)
                    dealY += Card(_deals, v, dealY) + 12f;
                else
                    buyY += Card(_buys, v, buyY) + 12f;
            }

            if (buyY <= 0f)
                Empty(_buys, "Сегодня ничего не продают.");
            if (dealY <= 0f)
                Empty(_deals, "Спонсоров сегодня нет.");
        }

        // Подтверждение после покупки/подписи: что получили и что стало с деньгами.
        public void Notice(string text, bool ok)
        {
            _notice.text = text;
            _notice.color = ok ? UiKit.Good : UiKit.Ember;
            _noticeUntil = Time.unscaledTime + 6f;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        static void Clear(RectTransform list)
        {
            for (int i = list.childCount - 1; i >= 0; i--)
                Destroy(list.GetChild(i).gameObject);
        }

        static void Empty(RectTransform list, string text)
        {
            var t = UiKit.Txt("empty", list, text, 20, UiKit.Muted, TextAnchor.UpperLeft, UiKit.Body);
            UiKit.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(ColumnWidth, 30f));
        }

        float Card(RectTransform list, OfferView v, float y)
        {
            var lines = new List<string>();
            if (v.contract)
            {
                lines.Add("<color=#F2C35C>Задача:</color> " + v.task);
                lines.Add("<color=#F2C35C>Карта:</color> «" + v.card + "»  ·  " + v.lifetime);
                lines.Add("<color=#7FE08A>Успех:</color> " + v.success + "   <color=#FF7A5C>Провал:</color> " + v.fail);
                if (!string.IsNullOrEmpty(v.requirement))
                    lines.Add("<color=#A89F96>Требование: " + v.requirement + "</color>");
            }
            else
            {
                lines.Add("<color=#F2C35C>Получите:</color> " + v.gets);
                if (!string.IsNullOrEmpty(v.lifetime))
                    lines.Add("<color=#F2C35C>Действует:</color> " + v.lifetime);
            }

            var plate = UiKit.Img("offer", list, null, Color.white);
            UiKit.DressSolid(plate, v.done ? UiKit.Frame.TealTile : v.contract ? UiKit.Frame.Gold : UiKit.Frame.Dialog);
            var group = plate.gameObject.AddComponent<CanvasGroup>();
            group.alpha = v.available || v.done ? 1f : 0.62f;

            string head = v.contract && !string.IsNullOrEmpty(v.brand) ? v.brand.ToUpperInvariant() + "  ·  " + v.title : v.title;
            var title = UiKit.Txt("title", plate.rectTransform, head, 22, UiKit.Paper, TextAnchor.UpperLeft, UiKit.Body);
            title.fontStyle = FontStyle.Bold;
            UiKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -14f), new Vector2(ColumnWidth - 300f, 30f));
            var body = UiKit.Txt("body", plate.rectTransform, string.Join("\n", lines), 17, UiKit.Muted, TextAnchor.UpperLeft, UiKit.Body);
            body.supportRichText = true;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            // Высота карточки — по тексту с переносами: строки не налезают на кнопку и соседей.
            const float BodyWidth = ColumnWidth - 270f;
            UiKit.Place(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -48f), new Vector2(BodyWidth, 400f));
            float textHeight = body.cachedTextGenerator.GetPreferredHeight(body.text, body.GetGenerationSettings(new Vector2(BodyWidth, 0f))) / Mathf.Max(0.01f, body.pixelsPerUnit);
            float height = Mathf.Max(112f, 60f + textHeight + 12f);
            body.rectTransform.sizeDelta = new Vector2(BodyWidth, height - 54f);
            UiKit.Place(plate.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -y), new Vector2(ColumnWidth, height));

            var price = UiKit.Txt("price", plate.rectTransform, v.price, 24, v.contract ? UiKit.Good : UiKit.Gold, TextAnchor.UpperRight, UiKit.Body);
            price.fontStyle = FontStyle.Bold;
            UiKit.Place(price.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -14f), new Vector2(220f, 30f));

            // Кнопка или состояние: КУПИТЬ / ПОДПИСАТЬ, «куплено ✓», или почему нельзя.
            if (v.available)
            {
                var b = UiKit.Img("buy", plate.rectTransform, null, Color.white, true);
                var button = b.gameObject.AddComponent<Button>();
                button.targetGraphic = b;
                UiKit.Place(b.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 14f), new Vector2(200f, 50f));
                var bt = UiKit.Txt("t", b.rectTransform, v.contract ? "ПОДПИСАТЬ" : "КУПИТЬ", 19, UiKit.Paper, TextAnchor.MiddleCenter, UiKit.Body);
                bt.fontStyle = FontStyle.Bold;
                UiKit.Stretch(bt.rectTransform);
                UiKit.Primary(button, 19);
                var offer = v.offer;
                button.onClick.AddListener(() => _pick?.Invoke(offer));
            }
            else
            {
                string state = v.done ? (v.contract ? "ПРИНЯТ ✓" : "КУПЛЕНО ✓") : v.reason;
                var s = UiKit.Txt("state", plate.rectTransform, state, v.done ? 20 : 17, v.done ? UiKit.Good : UiKit.Ember, TextAnchor.LowerRight, UiKit.Body);
                s.fontStyle = FontStyle.Bold;
                UiKit.Place(s.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 16f), new Vector2(240f, 50f));
            }

            return height;
        }
    }
}
