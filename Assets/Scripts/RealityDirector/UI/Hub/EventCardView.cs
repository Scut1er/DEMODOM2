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

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => _onClick?.Invoke());
        }

        public void Bind(PrepCard card, bool shop, Action onClick)
        {
            _onClick = onClick;
            if (frame != null)
                frame.color = card.picked ? pickedColor : card.color;
            if (art != null)
            {
                art.sprite = card.art;
                art.enabled = card.art != null;
            }

            Color text = card.picked ? pickedInk : shop && !card.affordable ? poorInk : ink;
            if (title != null)
            {
                title.text = card.title;
                title.color = card.picked ? pickedInk : ink;
            }

            if (status != null)
            {
                status.text = shop ? card.price + " кр" : card.picked ? "В СЕРИИ" : card.hint;
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
