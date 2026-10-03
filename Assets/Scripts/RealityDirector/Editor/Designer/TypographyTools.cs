using System.Collections.Generic;
using RealityDirector.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.EditorTools
{
    // Типографика: каждому тексту сцены хаба и UI-префабов — группа (UiText), размеры групп — в одном ассете.
    public static class TypographyTools
    {
        public const string AssetPath = "Assets/Resources/UiTypography.asset";
        const string PrefabFolder = "Assets/Prefabs/UI";

        public static UiTypography Asset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UiTypography>(AssetPath);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<UiTypography>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        // Разметить тексты без группы и применить стили. Уже размеченные — только переприменить.
        [MenuItem("RealityDirector/UI/Apply Text Styles (Hub scene + UI prefabs)")]
        public static void TagAll()
        {
            Asset();
            int tagged = 0, total = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                Tag(root, false, ref tagged, ref total);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                    Tag(root, true, ref tagged, ref total);
                EditorSceneManager.MarkSceneDirty(scene);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Типографика: текстов " + total + ", размечено новых " + tagged + ". Размеры — в " + AssetPath + ".");
        }

        static void Tag(GameObject root, bool skipPrefabInstances, ref int tagged, ref int total)
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                if (skipPrefabInstances && PrefabUtility.IsPartOfPrefabInstance(text))
                    continue;
                total++;
                var ui = text.GetComponent<UiText>();
                if (ui == null)
                {
                    // Группу считаем ДО добавления компонента — по исходному размеру текста.
                    var role = Guess(text);
                    ui = Undo.AddComponent<UiText>(text.gameObject);
                    ui.role = role;
                    tagged++;
                }

                ui.Apply();
                EditorUtility.SetDirty(text);
            }
        }

        // Группа по контексту: поле ввода, кнопка, логотип; иначе — по прежнему размеру.
        public static TextRole Guess(Text text)
        {
            if (text.font != null && text.font.name.Contains("Metal") && text.fontSize >= 60)
                return TextRole.Custom;
            var input = text.GetComponentInParent<InputField>(true);
            if (input != null && (input.textComponent == text || input.placeholder == text))
                return TextRole.Input;
            var button = text.GetComponentInParent<Button>(true);
            if (button != null && Depth(text.transform, button.transform) <= 2 && button.GetComponentsInChildren<Text>(true).Length == 1)
                return TextRole.Button;
            return UiTypography.ForSize(text.fontSize);
        }

        static int Depth(Transform child, Transform parent)
        {
            int d = 0;
            for (var t = child; t != null && t != parent; t = t.parent)
                d++;
            return d;
        }

        public static Dictionary<TextRole, int> Count()
        {
            var counts = new Dictionary<TextRole, int>();
            foreach (var ui in Object.FindObjectsByType<UiText>(FindObjectsInactive.Include))
                counts[ui.role] = (counts.TryGetValue(ui.role, out var c) ? c : 0) + 1;
            return counts;
        }
    }

    [CustomEditor(typeof(UiTypography))]
    public class UiTypographyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Размер и начертание для каждой группы текста во всей игре.\n" +
                                    "Группу тексту задаёт компонент UiText (в сцене) — поменяйте его, если текст попал не в ту группу.", MessageType.None);
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
                Reapply();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Тексты в открытой сцене", EditorStyles.boldLabel);
            foreach (var kv in TypographyTools.Count())
                EditorGUILayout.LabelField(CardInsight.Enum(kv.Key), kv.Value.ToString());
            if (GUILayout.Button("Разметить новые тексты и применить ко всем"))
                TypographyTools.TagAll();
        }

        static void Reapply()
        {
            foreach (var ui in Object.FindObjectsByType<UiText>(FindObjectsInactive.Include))
                ui.Apply();
        }
    }
}
