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

        static void Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }
    }
}
