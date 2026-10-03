using System.Collections.Generic;
using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Комментарии HellTube из кода — в ассет пула, чтобы дизайнер правил тексты, авторов, приоритеты и условия.
    public static class CommentPoolAssets
    {
        const string Path = "Assets/Resources/Content/HellTubeComments.asset";

        [MenuItem("RealityDirector/HellTube: сохранить комментарии в ассет", priority = 42)]
        public static void Export()
        {
            var pool = AssetDatabase.LoadAssetAtPath<HellTubeCommentPool>(Path);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<HellTubeCommentPool>();
                AssetDatabase.CreateAsset(pool, Path);
            }

            var have = new HashSet<string>();
            foreach (var c in pool.comments)
            {
                if (c != null && !string.IsNullOrEmpty(c.id))
                    have.Add(c.id);
            }

            int added = 0;
            foreach (var c in HellTubeComments.All)
            {
                if (c == null || have.Contains(c.id))
                    continue;
                pool.comments.Add(new HellTubeComment
                {
                    id = c.id, persona = c.persona, category = c.category, template = c.template, tone = c.tone,
                    priority = c.priority, weight = c.weight, required = c.required, group = c.group
                });
                added++;
            }

            EditorUtility.SetDirty(pool);
            AssetDatabase.SaveAssets();
            Debug.Log("HellTube: в пуле " + pool.comments.Count + " комментариев (добавлено " + added + ").");
        }
    }

    [CustomEditor(typeof(HellTubeCommentPool))]
    public class HellTubeCommentPoolEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var pool = (HellTubeCommentPool)target;
            var categories = new SortedDictionary<string, int>();
            var ids = new HashSet<string>();
            var warnings = new List<string>();
            foreach (var c in pool.comments)
            {
                if (c == null)
                    continue;
                categories.TryGetValue(c.category ?? "", out int n);
                categories[c.category ?? ""] = n + 1;
                if (!ids.Add(c.id ?? ""))
                    warnings.Add("Повтор id «" + c.id + "».");
                if (string.IsNullOrEmpty(c.template))
                    warnings.Add(c.id + ": пустой текст.");
                if (string.IsNullOrEmpty(c.group))
                    warnings.Add(c.id + ": нет группы — может встать рядом с похожим.");
            }

            EditorGUILayout.HelpBox("Комментарии HellTube: в ленту эфира идут 5–6 — сначала самые конкретные (комбо, последовательности), "
                                    + "потом реакции на людей и события, потом тон и монтаж, общие — не больше двух. "
                                    + "Из одной группы и от одного автора — по одному. Имена и события подставляются только из того, что было в эфире.", MessageType.None);
            EditorGUILayout.LabelField("Всего: " + pool.comments.Count, EditorStyles.boldLabel);
            foreach (var kv in categories)
                EditorGUILayout.LabelField("  " + (kv.Key.Length > 0 ? kv.Key : "(без категории)"), kv.Value.ToString());
            foreach (var w in warnings)
                EditorGUILayout.HelpBox(w, MessageType.Warning);
            EditorGUILayout.Space(6);
            DrawDefaultInspector();
        }
    }
}
