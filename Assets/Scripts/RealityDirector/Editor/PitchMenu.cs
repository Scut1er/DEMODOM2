#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RealityDirector.Editor
{
    public static class PitchMenu
    {
        [MenuItem("RealityDirector/Open Pitch Scene")]
        public static void Open()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Apartment_Pitch.unity");
        }

        [MenuItem("RealityDirector/Open Hub Scene")]
        public static void OpenHub()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Hub.unity");
        }

        [MenuItem("RealityDirector/Delete Season Save")]
        public static void DeleteSave()
        {
            RealityDirector.Persistence.SaveSystem.Delete();
        }
    }
}
#endif
