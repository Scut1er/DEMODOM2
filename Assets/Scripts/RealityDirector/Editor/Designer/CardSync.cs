using System.Collections.Generic;
using System.IO;
using RealityDirector.Events;
using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Ассеты карт ↔ встроенные карты из кода (PitchContent).
    // Поле, которого нет в YAML ассета, добавили в код после выгрузки — его значение берём из кода.
    // Поле, которое в ассете есть (даже 0), — решение дизайнера, его не трогаем.
    public static class CardSync
    {
        static readonly string[] Ignore = { "m_Script", "m_Name", "m_EditorClassIdentifier", "cardArt" };

        public class Status
        {
            public readonly List<string> missingAssets = new List<string>();
            public readonly Dictionary<string, List<string>> missingFields = new Dictionary<string, List<string>>();
            public int Problems => missingAssets.Count + missingFields.Count;
        }

        // Что расходится с кодом — для плашки в мастерской.
        public static Status Check()
        {
            return Compare(false, out _);
        }

        public static string Run(bool apply)
        {
            var status = Compare(apply, out int created);
            int fields = 0;
            foreach (var kv in status.missingFields)
                fields += kv.Value.Count;
            return apply
                ? "новых ассетов " + created + ", дописано полей " + fields + " в " + status.missingFields.Count + " карт(ы)."
                : "нет ассетов: " + status.missingAssets.Count + ", карт без новых полей: " + status.missingFields.Count;
        }

        static Status Compare(bool apply, out int created)
        {
            created = 0;
            var status = new Status();
            var builtIn = BuiltIn();
            Directory.CreateDirectory(DesignerData.CardsRoot);
            foreach (var card in builtIn)
            {
                string path = DesignerData.CardsRoot + "/" + card.id + ".asset";
                var asset = AssetDatabase.LoadAssetAtPath<EventDefinition>(path) ?? FindAsset(card.id);
                if (asset == null)
                {
                    status.missingAssets.Add(card.id);
                    if (apply)
                    {
                        var copy = Object.Instantiate(card);
                        copy.name = card.id;
                        copy.cardArt = null; // встроенные иконки рисуются кодом — в ассет их не сохранить
                        AssetDatabase.CreateAsset(copy, path);
                        created++;
                    }

                    continue;
                }

                var missing = MissingFields(asset);
                if (missing.Count == 0)
                    continue;
                status.missingFields[card.id] = missing;
                if (!apply)
                    continue;
                var from = new SerializedObject(card);
                var to = new SerializedObject(asset);
                foreach (var field in missing)
                {
                    var prop = from.FindProperty(field);
                    if (prop != null)
                        to.CopyFromSerializedProperty(prop);
                }

                to.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }

            foreach (var card in builtIn)
                Object.DestroyImmediate(card);
            if (apply)
            {
                AssetDatabase.SaveAssets();
                DesignerData.Invalidate();
            }

            return status;
        }

        // Поля EventDefinition, которых нет в файле ассета.
        public static List<string> MissingFields(EventDefinition asset)
        {
            var missing = new List<string>();
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return missing;
            string yaml = File.ReadAllText(path);
            var so = new SerializedObject(asset);
            var it = so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (System.Array.IndexOf(Ignore, it.name) >= 0)
                    continue;
                if (!yaml.Contains("\n  " + it.name + ":"))
                    missing.Add(it.name);
            }

            return missing;
        }

        // Встроенные карты в чистом виде (без наложения ассетов). Вызывающий уничтожает их сам.
        public static List<EventDefinition> BuiltIn()
        {
            CardLibrary.SkipAssets = true;
            PitchContent content;
            try
            {
                content = PitchContent.Create();
            }
            finally
            {
                CardLibrary.SkipAssets = false;
            }

            Object.DestroyImmediate(content.Aggressive);
            Object.DestroyImmediate(content.Sentimental);
            Object.DestroyImmediate(content.AggressiveRules);
            Object.DestroyImmediate(content.SentimentalRules);
            return new List<EventDefinition>(content.All);
        }

        static List<string> _builtInIds;

        public static List<string> BuiltInIds()
        {
            if (_builtInIds != null)
                return _builtInIds;
            _builtInIds = new List<string>();
            foreach (var card in BuiltIn())
            {
                _builtInIds.Add(card.id);
                Object.DestroyImmediate(card);
            }

            return _builtInIds;
        }

        static EventDefinition FindAsset(string id)
        {
            foreach (var card in DesignerData.LoadAll<EventDefinition>())
            {
                if (card.id == id)
                    return card;
            }

            return null;
        }
    }
}
