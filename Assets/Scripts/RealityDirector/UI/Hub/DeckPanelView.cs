using System;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Окно поверх хаба: «Колода» и «Магазин».
    // В хабе колода — только просмотр (browse): карты в съёмку выбираются перед съёмкой на карте выпуска.
    public class DeckPanelView : MonoBehaviour
    {
        public const int DeckTab = 0;
        public const int ShopTab = 1;

        [SerializeField] Button[] tabButtons = new Button[2];
        [SerializeField] GameObject[] tabPages = new GameObject[2];
        [SerializeField] GameObject[] tabSelected = new GameObject[2];
        [SerializeField] Text deckTabLabel;
        [SerializeField] Text money;

        [SerializeField] EventCardView cardPrefab;
        [SerializeField] RectTransform deckRoot;
        [SerializeField] RectTransform shopRoot;
        [SerializeField] GameObject deckEmpty;
        [SerializeField] GameObject shopEmpty;
        [SerializeField] Text deckInfo;
        [SerializeField] Text footer;
        [SerializeField] Button close;
        [Tooltip("Необязательно: вторая кнопка («Назад») — например, в выборе карт на карте сезона.")]
        [SerializeField] Button cancel;

        public event Action<string> Toggle;
        public event Action<string> Buy;
        public event Action Close;
        public event Action Cancel;

        int _tab;
        PrepModel _model;
        bool _shopOpen;
        bool _browse;
        string _closeCaption;
        bool _hardLock;
        bool _cardsOn = true;
        bool _closeOn = true;
        string _onlyId;

        public bool IsOpen => gameObject.activeSelf;
        // Магазин доступен только на узле «Магазин» карты сезона.
        public bool ShopOpen => _shopOpen;

        void Awake()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int tab = i;
                if (tabButtons[i] != null)
                    tabButtons[i].onClick.AddListener(() => SetTab(tab));
            }

            if (close != null)
                close.onClick.AddListener(() => Close?.Invoke());
            if (cancel != null)
                cancel.onClick.AddListener(() => Cancel?.Invoke());
            _deckScroll = Scrollable(deckRoot);
            _shopScroll = Scrollable(shopRoot);
        }

        ScrollRect _deckScroll;
        ScrollRect _shopScroll;

        // Сетка карт прокручивается колесом и перетаскиванием: карт может быть сколько угодно.
        static ScrollRect Scrollable(RectTransform content)
        {
            if (content == null)
                return null;
            var existing = content.GetComponentInParent<ScrollRect>(true);
            if (existing != null)
                return existing;

            var view = new GameObject(content.name + "View", typeof(RectTransform), typeof(RectMask2D), typeof(Image), typeof(ScrollRect));
            var viewRect = (RectTransform)view.transform;
            viewRect.SetParent(content.parent, false);
            viewRect.SetSiblingIndex(content.GetSiblingIndex());
            viewRect.anchorMin = content.anchorMin;
            viewRect.anchorMax = content.anchorMax;
            viewRect.pivot = content.pivot;
            viewRect.offsetMin = content.offsetMin;
            viewRect.offsetMax = content.offsetMax;
            // Прозрачный фон ловит колесо мыши между карт.
            view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            content.SetParent(viewRect, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = view.GetComponent<ScrollRect>();
            scroll.viewport = viewRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return scroll;
        }

        public void Open(int tab, PrepModel model, bool shopOpen = false, bool browse = false)
        {
            gameObject.SetActive(true);
            if (_deckScroll != null)
                _deckScroll.verticalNormalizedPosition = 1f;
            if (_shopScroll != null)
                _shopScroll.verticalNormalizedPosition = 1f;
            _shopOpen = shopOpen;
            _browse = browse;
            var closeLabel = close != null ? close.GetComponentInChildren<Text>(true) : null;
            if (closeLabel != null)
            {
                if (_closeCaption == null)
                    _closeCaption = closeLabel.text;
                closeLabel.text = browse ? "ЗАКРЫТЬ" : _closeCaption;
            }

            if (ShopTab < tabButtons.Length && tabButtons[ShopTab] != null)
                tabButtons[ShopTab].gameObject.SetActive(shopOpen);
            _tab = shopOpen ? Mathf.Clamp(tab, 0, tabPages.Length - 1) : DeckTab;
            Show(model);
        }

        public void Hide()
        {
            _hardLock = false;
            _cardsOn = true;
            _closeOn = true;
            _onlyId = null;
            gameObject.SetActive(false);
        }

        public RectTransform CardsFocus()
        {
            return deckRoot != null ? deckRoot : transform as RectTransform;
        }

        public RectTransform ExplainFocus()
        {
            if (deckInfo != null)
                return deckInfo.rectTransform;
            return FooterFocus();
        }

        public RectTransform FooterFocus()
        {
            if (footer != null)
                return footer.rectTransform;
            return CardsFocus();
        }

        public RectTransform CloseFocus()
        {
            return close != null ? close.transform as RectTransform : null;
        }

        public RectTransform CardRect(string id)
        {
            if (deckRoot == null)
                return CardsFocus();
            var views = deckRoot.GetComponentsInChildren<EventCardView>(true);
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] != null && views[i].CardId == id)
                    return views[i].transform as RectTransform;
            }

            return views.Length > 0 ? views[0].transform as RectTransform : CardsFocus();
        }

        public void SetOnly(string cardId)
        {
            _onlyId = cardId;
            _cardsOn = true;
            PaintGates();
        }

        public void SetCardsEnabled(bool on)
        {
            _cardsOn = on;
            PaintGates();
        }

        public void SetCloseEnabled(bool on)
        {
            _closeOn = on;
            PaintGates();
        }

        public void SetLocked(bool locked)
        {
            _hardLock = locked;
            PaintGates();
        }

        public void SetCancelEnabled(bool on)
        {
            if (cancel != null)
                cancel.interactable = on && !_hardLock;
        }

        void PaintGates()
        {
            if (close != null)
                close.interactable = !_hardLock && _closeOn;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] != null)
                    tabButtons[i].interactable = !_hardLock && _onlyId == null && _cardsOn;
            }

            var views = GetComponentsInChildren<EventCardView>(true);
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] == null)
                    continue;
                bool allow = !_hardLock && _cardsOn && (_onlyId == null || views[i].CardId == _onlyId);
                views[i].SetInteractable(allow);
            }
        }

        public void SetTab(int tab)
        {
            if (tab == ShopTab && !_shopOpen)
                return;
            _tab = Mathf.Clamp(tab, 0, tabPages.Length - 1);
            if (_model != null)
                Show(_model);
        }

        public void Show(PrepModel model)
        {
            _model = model;
            int owned = model.deck != null ? model.deck.Length : 0;
            if (deckTabLabel != null)
                deckTabLabel.text = _browse ? "КОЛОДА  ·  " + owned : "КОЛОДА  " + model.picked + "/" + model.slots;
            if (money != null)
                money.text = string.IsNullOrEmpty(model.moneyText) ? model.money + " кр" : model.moneyText;
            for (int i = 0; i < tabPages.Length; i++)
            {
                if (tabPages[i] != null)
                    tabPages[i].SetActive(i == _tab);
                if (i < tabSelected.Length && tabSelected[i] != null)
                    tabSelected[i].SetActive(i == _tab);
            }

            if (deckInfo != null)
                deckInfo.text = _browse
                    ? "Все твои карты. Какие взять в съёмку — выбираешь перед каждой съёмкой на карте выпуска."
                    : model.slotsLabel;
            Fill(deckRoot, deckEmpty, model.deck, false);
            Fill(shopRoot, shopEmpty, model.shop, true);
            if (footer != null)
                footer.text = Footer(model);
            PaintGates();
        }

        void Fill(RectTransform root, GameObject empty, PrepCard[] cards, bool shop)
        {
            if (root == null || cardPrefab == null)
                return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var old = root.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }

            bool none = cards == null || cards.Length == 0;
            if (empty != null)
                empty.SetActive(none);
            if (none)
                return;

            for (int i = 0; i < cards.Length; i++)
            {
                string id = cards[i].id;
                var view = Instantiate(cardPrefab, root);
                view.name = cardPrefab.name + "_" + id;
                if (shop)
                {
                    view.Bind(cards[i], true, () => Buy?.Invoke(id));
                }
                else if (_browse)
                {
                    // Просмотр: без отметки «в серии» и без выбора.
                    var card = cards[i];
                    bool picked = card.picked;
                    card.picked = false;
                    view.Bind(card, false, null);
                    card.picked = picked;
                }
                else
                {
                    view.Bind(cards[i], false, () => Toggle?.Invoke(id));
                }
            }
        }

        string Footer(PrepModel model)
        {
            if (!string.IsNullOrEmpty(model.reject))
                return model.reject;
            if (_tab == ShopTab)
                return string.IsNullOrEmpty(model.shopFooter)
                    ? "Купленная карта попадает в колоду."
                    : model.shopFooter;
            if (_browse)
                return model.available == 0
                    ? "Колода пуста. Новые карты — в магазине хаба, разовые — у спонсоров выпуска."
                    : "Карт в колоде: " + model.available + ". В съёмку берётся до " + model.slots + " — выбор перед съёмкой.";
            if (model.available == 0)
                return "Колода пуста — снимать можно и без карт. Новые карты — в магазине хаба, разовые — у спонсоров выпуска.";
            if (model.picked == 0)
                return "Можно снимать и без карт, но провоцировать будет нечем. Лимит: " + model.slots + ".";
            return "В серию: " + model.picked + " из " + model.slots + ". Неиспользованные карты вернутся в колоду.";
        }
    }
}
