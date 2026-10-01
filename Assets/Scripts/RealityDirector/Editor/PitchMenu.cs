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
    }
}
#endif
