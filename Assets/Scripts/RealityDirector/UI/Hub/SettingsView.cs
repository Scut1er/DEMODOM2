using System;
using RealityDirector.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    public class SettingsView : MonoBehaviour
    {
        [SerializeField] Slider volume;
        [SerializeField] Toggle fullscreen;
        [SerializeField] Button back;

        public event Action Back;

        void Awake()
        {
            if (volume != null)
                volume.onValueChanged.AddListener(v => GameSettings.Volume = v);
            if (fullscreen != null)
                fullscreen.onValueChanged.AddListener(v => GameSettings.Fullscreen = v);
            if (back != null)
                back.onClick.AddListener(() =>
                {
                    GameSettings.Flush();
                    Back?.Invoke();
                });
        }

        public void Show()
        {
            if (volume != null)
                volume.SetValueWithoutNotify(GameSettings.Volume);
            if (fullscreen != null)
                fullscreen.SetIsOnWithoutNotify(GameSettings.Fullscreen);
        }
    }
}
