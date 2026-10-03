using System;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] Button newSeason;
        [SerializeField] Button continueSeason;
        [SerializeField] Button settings;
        [SerializeField] Button quit;

        public event Action NewSeason;
        public event Action Continue;
        public event Action Settings;
        public event Action Quit;

        void Awake()
        {
            if (newSeason != null)
                newSeason.onClick.AddListener(() => NewSeason?.Invoke());
            if (continueSeason != null)
                continueSeason.onClick.AddListener(() => Continue?.Invoke());
            if (settings != null)
                settings.onClick.AddListener(() => Settings?.Invoke());
            if (quit != null)
                quit.onClick.AddListener(() => Quit?.Invoke());
            MenuCameraChrome.Mount(this);
            Show(false);
        }

        public void Show(bool canContinue)
        {
            if (continueSeason == null)
                return;
            continueSeason.interactable = canContinue;
            // Нечего продолжать — кнопки нет совсем, а не серая заглушка.
            continueSeason.gameObject.SetActive(canContinue);
            var colors = continueSeason.colors;
            colors.disabledColor = new Color(0.32f, 0.26f, 0.26f, 0.6f);
            continueSeason.colors = colors;

            // Одна главная кнопка: есть сейв — «Продолжить» первой и яркой, иначе «Новый сезон».
            // Остальные — тёмные, чтобы глаз сразу шёл к главному действию.
            var main = canContinue ? continueSeason : newSeason;
            if (canContinue)
                continueSeason.transform.SetSiblingIndex(0);
            else if (newSeason != null)
                newSeason.transform.SetSiblingIndex(0);
            foreach (var b in new[] { newSeason, continueSeason, settings, quit })
            {
                if (b == null)
                    continue;
                bool primary = b == main;
                var c = b.colors;
                c.normalColor = primary ? new Color(0.9f, 0.3f, 0.16f, 1f) : new Color(0.2f, 0.08f, 0.07f, 1f);
                c.highlightedColor = primary ? new Color(1f, 0.52f, 0.2f, 1f) : new Color(0.42f, 0.14f, 0.08f, 1f);
                c.selectedColor = c.normalColor;
                b.colors = c;
                var label = b.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.color = primary ? new Color(1f, 0.96f, 0.9f, 1f) : new Color(0.98f, 0.62f, 0.38f, 1f);
                    label.fontSize = primary ? 24 : 20;
                }
            }
        }
    }
}
