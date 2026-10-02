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

        public event Action<string> Toggle;
        public event Action<string> Buy;
        public event Action Close;

        int _tab;
        PrepModel _model;

        public bool IsOpen => gameObject.activeSelf;

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
        }

        public void Open(int tab, PrepModel model)
        {
            gameObject.SetActive(true);
            _tab = Mathf.Clamp(tab, 0, tabPages.Length - 1);
            Show(model);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetTab(int tab)
        {
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
                return "Купленная карта попадает в колоду. Сыгранная сгорает до конца сезона.";
            if (model.available == 0)
                return "Колода пуста. Загляни в магазин.";
            if (!model.canStart)
                return "Возьми " + Mathf.Min(model.slots, model.available) + " несыгранных. Неиспользованные вернутся.";
            return "Набор собран. Неиспользованные карты вернутся в колоду.";
        }
    }
}
