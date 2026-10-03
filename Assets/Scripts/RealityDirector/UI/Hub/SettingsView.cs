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
