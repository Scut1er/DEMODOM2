using System;
using RealityDirector.Core;
using RealityDirector.Meta;
using RealityDirector.Util;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Карта в колоде и магазине хаба. Выглядит так же, как в руке на съёмке: рамка категории из пака,
    // арт, название, подсказка; в магазине — цена плашкой, на что не хватает денег — приглушено.
    public class EventCardView : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(176f, 236f);

        [SerializeField] Button button;
        [SerializeField] Image frame;
        [Tooltip("Тёмная подложка под прозрачным окном рамки художника.")]
        [SerializeField] Image window;
        [SerializeField] Image art;
        [SerializeField] Text title;
        [SerializeField] Text status;
        [SerializeField] GameObject pickedMark;
        [SerializeField] Image[] moodIcons = new Image[0];
        [SerializeField] Color pickedColor = new Color(0.95f, 0.78f, 0.32f, 1f);
        [SerializeField] Color pickedInk = new Color(0.06f, 0.05f, 0.07f, 1f);
        [SerializeField] Color ink = new Color(0.96f, 0.93f, 0.88f, 1f);
        [SerializeField] Color poorInk = new Color(0.62f, 0.56f, 0.52f, 1f);

        Action _onClick;
        Image _pill;
        Text _price;
        CanvasGroup _group;

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
            if (window != null)
                window.gameObject.SetActive(false);
            if (title != null)
            {
                Top(title.rectTransform, -30f, 36f, 16f);
                title.alignment = TextAnchor.MiddleCenter;
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 10;
                title.resizeTextMaxSize = 16;
                title.verticalOverflow = VerticalWrapMode.Truncate;
                title.fontStyle = FontStyle.Bold;
                UiKit.Shadow(title);
            }

            if (art != null)
            {
                Top(art.rectTransform, -70f, 92f, 26f);
                art.preserveAspect = true;
                _pill = UiKit.Img("Price", art.transform, UiKit.Load("Art/UI/CoreGameplay/UI/HUD/cost_badge"), Color.white);
                UiKit.Place(_pill.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(4f, -4f), new Vector2(76f, 26f));
                _price = UiKit.Txt("Label", _pill.transform, "", 15, UiKit.Gold, TextAnchor.MiddleCenter);
                UiKit.Stretch(_price.rectTransform);
                _price.fontStyle = FontStyle.Bold;
            }

            if (status != null)
            {
                var r = status.rectTransform;
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(1f, 0f);
                r.pivot = new Vector2(0.5f, 0f);
                r.anchoredPosition = new Vector2(0f, 22f);
                r.sizeDelta = new Vector2(-34f, 52f);
                status.alignment = TextAnchor.MiddleCenter;
                status.resizeTextForBestFit = true;
                status.resizeTextMinSize = 10;
                status.resizeTextMaxSize = 14;
                status.verticalOverflow = VerticalWrapMode.Truncate;
            }

            for (int i = 0; i < moodIcons.Length; i++)
            {
                if (moodIcons[i] == null)
                    continue;
                UiKit.Place(moodIcons[i].rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f - i * 22f, -9f), new Vector2(20f, 20f));
                moodIcons[i].preserveAspect = true;
            }
        }

        // Полоса по ширине карты на высоте y от верха.
        static void Top(RectTransform r, float y, float height, float inset)
        {
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, y);
            r.sizeDelta = new Vector2(-inset * 2f, height);
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
            // Рамка категории как в руке на съёмке; нет пака — рамка по тону, нет и её — цветная плашка.
            var frameArt = CoreGameplayArt.Sprite(CoreGameplayArt.Frame(card.category));
            if (frameArt == null && card.moods != null && card.moods.Length > 0)
                frameArt = GameArt.CardFrame(card.moods[0]);
            if (frame != null)
            {
                if (frameArt != null)
                {
                    frame.sprite = frameArt;
                    frame.type = Image.Type.Simple;
                    frame.color = card.picked ? pickedColor : Color.white;
                }
                else
                {
                    frame.color = card.picked ? pickedColor : card.color;
                }
            }

            if (art != null)
            {
                art.sprite = card.art != null ? card.art : CoreGameplayArt.Sprite(CoreGameplayArt.CardArt(card.category));
                art.enabled = art.sprite != null;
            }

            bool poor = shop && !card.affordable;
            if (title != null)
            {
                title.text = (card.title ?? "").ToUpperInvariant();
                title.color = ink;
            }

            if (status != null)
            {
                status.text = card.temporary ? "В РУКЕ  ·  " + card.hint : card.hint;
                status.color = poor ? poorInk : new Color(0.9f, 0.86f, 0.8f, 1f);
            }

            if (_pill != null)
            {
                string unit = string.IsNullOrEmpty(card.unit) ? "кр" : card.unit;
                _pill.gameObject.SetActive(shop);
                _pill.color = poor ? new Color(1f, 0.45f, 0.45f, 1f) : Color.white;
                _price.text = card.price + " " + unit;
                _price.color = poor ? new Color(1f, 0.6f, 0.55f, 1f) : UiKit.Gold;
            }

            // На что не хватает — видно сразу, но карту всё равно можно рассмотреть.
            if (_group != null)
                _group.alpha = poor ? 0.55f : 1f;

            if (pickedMark != null)
                pickedMark.SetActive(card.picked);

            for (int i = 0; i < moodIcons.Length; i++)
            {
                if (moodIcons[i] == null)
                    continue;
                bool on = card.moods != null && i < card.moods.Length;
                moodIcons[i].gameObject.SetActive(on);
                if (on)
                {
                    moodIcons[i].sprite = UiKit.MoodIcon(card.moods[i]) ?? MoodIcon(card.moods[i]);
                    moodIcons[i].color = MoodStyle.ColorOf(card.moods[i]);
                }
            }
        }

        static Sprite MoodIcon(ShowMood mood)
        {
            switch (mood)
            {
                case ShowMood.Drama: return IllustratedArt.IconTear;
                case ShowMood.Trash: return IllustratedArt.IconDevil;
                default: return IllustratedArt.IconFamily;
            }
        }
    }
}
