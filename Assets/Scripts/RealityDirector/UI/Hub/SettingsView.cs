using System;
using RealityDirector.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    public class SettingsView : MonoBehaviour
    {
        [SerializeField] Slider volume;
        [SerializeField] Toggle fullscreen;
        [SerializeField] Button back;
        [Tooltip("Необязательно: громкость в процентах рядом с ползунком.")]
        [SerializeField] Text volumeValue;
        [Tooltip("Необязательно: «Вкл» / «Выкл» рядом с переключателем.")]
        [SerializeField] Text fullscreenState;

        public event Action Back;

        void Awake()
        {
            if (volume != null)
                volume.onValueChanged.AddListener(v =>
                {
                    GameSettings.Volume = v;
                    RefreshLabels();
                });
            if (fullscreen != null)
                fullscreen.onValueChanged.AddListener(v =>
                {
                    GameSettings.Fullscreen = v;
                    RefreshLabels();
                });
            if (back != null)
                back.onClick.AddListener(Close);
            Dress();
        }

        // Окно настроек — та же рамка пака на фоне офиса канала.
        void Dress()
        {
            UiKit.Backdrop((RectTransform)transform, "Art/Intro/bg/scene_2", new Rect(0f, 0.3f, 1f, 0.7f), 0.7f);
            var card = transform.Find("Card");
            if (card == null)
                return;
            UiKit.DressSolid(card.GetComponent<Image>(), UiKit.Frame.Gold, 10f);
            var accent = card.Find("Accent");
            if (accent != null)
                accent.gameObject.SetActive(false);
            foreach (var row in new[] { "VolumeRow", "FullscreenRow" })
                UiKit.Dress(card.Find(row)?.GetComponent<Image>(), UiKit.Frame.Dark);
            var title = card.Find("Title")?.GetComponent<Text>();
            if (title != null && UiKit.Display != null)
            {
                title.font = UiKit.Display;
                title.fontSize = 48;
                UiKit.Shadow(title);
            }

            UiKit.Primary(back, 22);
        }

        void Update()
        {
            // Esc — назад, как кнопка «Назад».
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Close();
        }

        void Close()
        {
            GameSettings.Flush();
            Back?.Invoke();
        }

        public void Show()
        {
            if (volume != null)
                volume.SetValueWithoutNotify(GameSettings.Volume);
            if (fullscreen != null)
                fullscreen.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            RefreshLabels();
        }

        void RefreshLabels()
        {
            if (volumeValue != null && volume != null)
                volumeValue.text = Mathf.RoundToInt(volume.normalizedValue * 100f) + "%";
            if (fullscreenState != null && fullscreen != null)
                fullscreenState.text = fullscreen.isOn ? "Вкл" : "Выкл";
        }
    }
}
