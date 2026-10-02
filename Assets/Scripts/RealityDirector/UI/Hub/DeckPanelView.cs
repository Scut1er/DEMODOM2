using System;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Окно поверх хаба: «Колода» (карты в серию) и «Магазин».
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
        }

        public void Open(int tab, PrepModel model, bool shopOpen = false)
        {
            gameObject.SetActive(true);
            _shopOpen = shopOpen;
            if (ShopTab < tabButtons.Length && tabButtons[ShopTab] != null)
                tabButtons[ShopTab].gameObject.SetActive(shopOpen);
            _tab = shopOpen ? Mathf.Clamp(tab, 0, tabPages.Length - 1) : DeckTab;
            Show(model);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
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
            if (deckTabLabel != null)
                deckTabLabel.text = "КОЛОДА  " + model.picked + "/" + model.slots;
            if (money != null)
                money.text = model.money + " кр";
            for (int i = 0; i < tabPages.Length; i++)
            {
                if (tabPages[i] != null)
                    tabPages[i].SetActive(i == _tab);
                if (i < tabSelected.Length && tabSelected[i] != null)
                    tabSelected[i].SetActive(i == _tab);
            }

            if (deckInfo != null)
                deckInfo.text = model.slotsLabel;
            Fill(deckRoot, deckEmpty, model.deck, false);
            Fill(shopRoot, shopEmpty, model.shop, true);
            if (footer != null)
                footer.text = Footer(model);
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
                    view.Bind(cards[i], true, () => Buy?.Invoke(id));
                else
                    view.Bind(cards[i], false, () => Toggle?.Invoke(id));
            }
        }

        string Footer(PrepModel model)
        {
            if (!string.IsNullOrEmpty(model.reject))
                return model.reject;
            if (_tab == ShopTab)
                return "Купленная карта попадает в колоду. «Готово» — уйти из магазина и продолжить сценарий.";
            if (model.available == 0)
                return "Колода пуста — снимать можно и без карт. Новые карты — в магазине сценария.";
            if (model.picked == 0)
                return "Можно снимать и без карт, но провоцировать будет нечем. Лимит: " + model.slots + ".";
            return "В серию: " + model.picked + " из " + model.slots + ". Неиспользованные карты вернутся в колоду.";
        }
    }
}
