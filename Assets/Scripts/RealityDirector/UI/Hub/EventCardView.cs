using System;
using RealityDirector.Core;
using RealityDirector.Meta;
using RealityDirector.Util;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Префаб карты ивента в хабе (колода и магазин). Внешний вид — в префабе.
    public class EventCardView : MonoBehaviour
    {
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

        public string CardId { get; private set; }

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => _onClick?.Invoke());
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
            // Рамка по тону карты (красная — трэш, зелёная — семья, синяя — драма). Нет арта — цветная плашка.
            var frameArt = card.moods != null && card.moods.Length > 0 ? GameArt.CardFrame(card.moods[0]) : null;
            bool framed = frameArt != null;
            if (frame != null)
            {
                if (framed)
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

            if (window != null)
                window.gameObject.SetActive(framed);
            if (art != null)
            {
                art.sprite = card.art;
                art.enabled = card.art != null;
            }

            // На тёмном окне рамки текст всегда светлый.
            Color titleInk = card.picked && !framed ? pickedInk : ink;
            Color text = card.picked && !framed ? pickedInk : shop && !card.affordable ? poorInk : ink;
            if (title != null)
            {
                title.text = card.title;
                title.color = titleInk;
            }

            if (status != null)
            {
                string unit = string.IsNullOrEmpty(card.unit) ? "кр" : card.unit;
                status.text = shop ? card.price + " " + unit
                    : card.temporary ? "В РУКЕ"
                    : card.picked ? "В СЕРИИ" : card.hint;
                status.color = text;
            }

            if (pickedMark != null)
                pickedMark.SetActive(card.picked);

            for (int i = 0; i < moodIcons.Length; i++)
            {
                if (moodIcons[i] == null)
                    continue;
                bool on = card.moods != null && i < card.moods.Length;
                moodIcons[i].gameObject.SetActive(on);
                if (on)
                    moodIcons[i].sprite = MoodIcon(card.moods[i]);
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
