using UnityEngine;

namespace RealityDirector.Core
{
    public static class GameSettings
    {
        const string VolumeKey = "settings.volume";

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set
            {
                float v = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(VolumeKey, v);
                AudioListener.volume = v;
            }
        }

        public static bool Fullscreen
        {
            get => Screen.fullScreen;
            set => Screen.fullScreen = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Apply()
        {
            AudioListener.volume = Volume;
        }

        public static void Flush()
        {
            PlayerPrefs.Save();
        }
    }
}
