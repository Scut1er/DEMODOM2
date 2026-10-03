using System;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    public class SeasonEndView : MonoBehaviour
    {
        [SerializeField] Text body;
        [SerializeField] Button newSeason;
        [SerializeField] Button menu;

        public event Action NewSeason;
        public event Action Menu;

        void Awake()
        {
            if (newSeason != null)
                newSeason.onClick.AddListener(() => NewSeason?.Invoke());
            if (menu != null)
                menu.onClick.AddListener(() => Menu?.Invoke());
            Dress();
        }

        // Финал сезона — команда и босс на фоне канала, итог в рамке.
        void Dress()
        {
            UiKit.Backdrop((RectTransform)transform, "Art/Intro/bg/scene_7", new Rect(0f, 0.3f, 1f, 0.7f), 0.55f);
            var title = transform.Find("Title")?.GetComponent<Text>();
            if (title != null)
            {
                if (UiKit.Display != null)
                    title.font = UiKit.Display;
                title.fontSize = 72;
                title.color = UiKit.Gold;
                UiKit.Shadow(title, 3f);
                title.rectTransform.anchoredPosition += new Vector2(0f, 50f);
            }

            if (body != null)
            {
                var plate = UiKit.Img("BodyPlate", transform, null, Color.white);
                UiKit.DressSolid(plate, UiKit.Frame.Dialog, 8f);
                plate.rectTransform.SetSiblingIndex(body.transform.GetSiblingIndex());
                var r = body.rectTransform;
                UiKit.Place(plate.rectTransform, r.anchorMin, r.pivot, r.anchoredPosition, r.sizeDelta + new Vector2(80f, 60f));
                body.alignment = TextAnchor.MiddleCenter;
            }

            UiKit.Primary(newSeason, 22);
            UiKit.Secondary(menu, 20);
        }

        public void Show(string text)
        {
            if (body != null)
                body.text = text;
        }
    }
}
