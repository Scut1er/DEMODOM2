using System.Collections.Generic;
using RealityDirector.NPC;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Черты в ассетах: дизайнер правит описание и склонности для карточки кандидата (и чувствительность) в инспекторе.
    // Ассет с тем же traitId заменяет встроенную черту целиком — поэтому создаём его копией встроенной.
    public static class TraitAssets
    {
        const string Folder = "Assets/Resources/Content/Traits";

        [MenuItem("RealityDirector/Черты: создать недостающие ассеты", priority = 40)]
        public static void CreateMissing()
        {
            var have = new HashSet<TraitId>();
            foreach (var t in DesignerData.LoadAll<TraitDefinition>())
                have.Add(t.traitId);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Resources/Content", "Traits");
            var content = PitchContent.Create();
            int made = 0;
            foreach (TraitId id in System.Enum.GetValues(typeof(TraitId)))
            {
                if (have.Contains(id))
                    continue;
                var source = content.TraitOf(id);
                var asset = Object.Instantiate(source);
                asset.shortDescription = TraitText.Description(source);
                asset.gameplayHints = TraitText.Hints(source, content.RulesFor(id));
                AssetDatabase.CreateAsset(asset, Folder + "/trait_" + id.ToString().ToLowerInvariant() + ".asset");
                made++;
            }

            content.DestroyAssets(true);
            AssetDatabase.SaveAssets();
            Debug.Log("Черты: создано ассетов — " + made + " (" + Folder + ").");
        }
    }

    [CustomEditor(typeof(TraitDefinition))]
    public class TraitDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var trait = (TraitDefinition)target;
            EditorGUILayout.HelpBox("Черта: как участник реагирует (чувствительность, склонности) и как его видит игрок на экране каста.", MessageType.None);
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Карточка кандидата", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(TraitText.Description(trait), EditorStyles.wordWrappedLabel);
            foreach (var line in TraitText.Hints(trait, null))
                EditorGUILayout.LabelField("•  " + line, EditorStyles.wordWrappedMiniLabel);
            if (string.IsNullOrEmpty(trait.shortDescription))
                EditorGUILayout.HelpBox("Описание пустое — на карточке будет встроенная фраза.", MessageType.Info);
            if (trait.gameplayHints != null && trait.gameplayHints.Count > 3)
                EditorGUILayout.HelpBox("На карточке помещаются 3 склонности — остальные не видны.", MessageType.Warning);
            if (trait.gameplayHints != null && trait.gameplayHints.Exists(h => h != null && h.Length > 48))
                EditorGUILayout.HelpBox("Склонность длиннее 48 знаков займёт две строки.", MessageType.Info);
            if (!DesignerData.InFolder(trait, "Assets/Resources/Content"))
                EditorGUILayout.HelpBox("Ассет вне Resources/Content — игра его не увидит.", MessageType.Warning);
        }
    }
}
