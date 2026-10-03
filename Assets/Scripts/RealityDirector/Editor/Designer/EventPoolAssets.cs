using System.Collections.Generic;
using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Пул событий OWM (OwmEvents, из таблицы дизайна) — в ассеты EventRoomDefinition, чтобы дизайнер правил их
    // в инспекторе и мастерской событий. Ассет с тем же id главнее кода: JamContent берёт код только для недостающих.
    public static class EventPoolAssets
    {
        const string Folder = "Assets/Resources/Content/Rooms/Events";

        [MenuItem("RealityDirector/События: сохранить пул OWM в ассеты", priority = 41)]
        public static void Export()
        {
            var have = new HashSet<string>();
            foreach (var e in DesignerData.LoadAll<EventRoomDefinition>())
            {
                if (e != null && !string.IsNullOrEmpty(e.Id))
                    have.Add(e.Id);
            }

            int made = 0;
            foreach (var source in OwmEvents.All())
            {
                if (source == null || have.Contains(source.Id))
                {
                    if (source != null)
                        Object.DestroyImmediate(source);
                    continue;
                }

                var asset = Object.Instantiate(source);
                asset.hideFlags = HideFlags.None;
                foreach (var c in asset.choices)
                {
                    if (c == null)
                        continue;
                    // Строки таблицы («+80 кр; Trash +6; EpisodeFlag: …») — заметка для команды, а не текст для игрока:
                    // игрок видит последствия, собранные из эффектов автоматически.
                    string note = string.IsNullOrEmpty(c.resultText) ? "" : "Успех: " + c.resultText;
                    if (!string.IsNullOrEmpty(c.failText))
                        note += (note.Length > 0 ? "\n" : "") + "Провал: " + c.failText;
                    c.designNote = note;
                    c.resultText = "";
                    c.failText = "";
                }

                AssetDatabase.CreateAsset(asset, Folder + "/" + source.Id + ".asset");
                Object.DestroyImmediate(source);
                made++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("События: сохранено в ассеты — " + made + " (" + Folder + ").");
        }
    }
}
