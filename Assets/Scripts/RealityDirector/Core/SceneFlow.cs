using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealityDirector.Core
{
    public static class SceneFlow
    {
        public const string Hub = "Hub";
        public const string Episode = "Apartment_Pitch";

        public static void ToHub()
        {
            Load(Hub);
        }

        public static void ToEpisode()
        {
            Load(Episode);
        }

        // Сцена комнаты-съёмки (должна быть в Build Settings).
        public static void ToScene(string scene)
        {
            Load(string.IsNullOrEmpty(scene) ? Episode : scene);
        }

        static void Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }
    }
}
