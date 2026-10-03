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
            var colors = continueSeason.colors;
            colors.disabledColor = new Color(0.32f, 0.26f, 0.26f, 0.6f);
            continueSeason.colors = colors;
        }
    }
}
