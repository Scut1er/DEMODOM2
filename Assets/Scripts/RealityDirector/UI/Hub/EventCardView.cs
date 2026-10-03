using System;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Карта в колоде и магазине хаба — то же лицо карты (CardFace), что в руке на съёмке. В магазине — цена
    // в кр/нал, на что не хватает денег — приглушено; карта в серии — с золотым свечением.
    public class EventCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static readonly Vector2 Size = CardFace.SizeFor(168f);

        [SerializeField] Button button;
        [Tooltip("Старая плашка карты из префаба: теперь только ловит клики, рисует CardFace.")]
        [SerializeField] Image frame;
        [SerializeField] Image window;
        [SerializeField] Image art;
        [SerializeField] Text title;
        [SerializeField] Text status;
        [SerializeField] GameObject pickedMark;
        [SerializeField] Image[] moodIcons = new Image[0];

        Action _onClick;
        CanvasGroup _group;
        CardFace _face;

        public string CardId { get; private set; }

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => _onClick?.Invoke());
            Layout();
        }

        void Layout()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null)
                _group = gameObject.AddComponent<CanvasGroup>();
            // Прежние части префаба не рисуются — лицо карты одно на всю игру.
            foreach (var old in new Component[] { window, art, title, status })
            {
                if (old != null)
                    old.gameObject.SetActive(false);
            }

            if (pickedMark != null)
                pickedMark.SetActive(false);
            foreach (var icon in moodIcons)
            {
                if (icon != null)
                    icon.gameObject.SetActive(false);
            }

            if (frame != null)
                frame.color = new Color(0f, 0f, 0f, 0f);
            _face = CardFace.Build(UiKit.Rect("Face", transform));
            UiKit.Stretch(_face.Rect);
            if (button != null)
            {
                button.targetGraphic = _face.Back;
                button.transition = Selectable.Transition.None;
            }
        }

        public void SetInteractable(bool on)
        {
            if (button != null)
                button.interactable = on;
        }

        public void Bind(PrepCard card, bool shop, Action onClick)
        {
            CardId = card.id;
            _onClick = onClick;
            if (_face == null)
                Layout();
            if (card.def != null)
                _face.Show(card.def);
            _face.SetMoods(card.moods);
            _face.SetSelected(card.picked);

            bool poor = shop && !card.affordable;
            if (shop)
                _face.SetCost(card.price + " " + (string.IsNullOrEmpty(card.unit) ? "кр" : card.unit == "нал" ? "касса" : card.unit), poor);
            _face.SetBanner(card.temporary ? "В РУКЕ" : null, UiKit.Gold);
            if (!string.IsNullOrEmpty(card.hint))
                _face.SetHint(card.hint);

            // На что не хватает — видно сразу, но карту всё равно можно рассмотреть.
            if (_group != null)
                _group.alpha = poor ? 0.55f : 1f;
            _face.SetDisabled(poor);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_face != null)
                _face.SetHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_face != null)
                _face.SetHover(false);
        }
    }
}
