using System.Collections.Generic;
using RealityDirector.Events;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Карты продюсера из ассетов Resources/Content/Cards (EventDefinition).
    // Ассет с id встроенной карты переопределяет её значения; ассет с новым id — новая карта.
    // Все карты в игре — копии (ассеты на диске не меняются и не удаляются).
    public static class CardLibrary
    {
        public const string Folder = ContentLibrary.Root + "/Cards";

        // Только для инструментов редактора: получить встроенные значения карт без ассетов (синхронизация).
        public static bool SkipAssets;

#if UNITY_EDITOR
        // «Проверить в квартире» из мастерской карт: карта попадёт в руку при запуске квартиры из редактора.
        public const string TestCardPref = "RealityDirector.TestCardId";

        public static string TakeTestCard()
        {
            string id = UnityEditor.EditorPrefs.GetString(TestCardPref, "");
            if (!string.IsNullOrEmpty(id))
                UnityEditor.EditorPrefs.DeleteKey(TestCardPref);
            return id;
        }
#endif

        public static EventDefinition[] Merge(EventDefinition[] builtIn)
        {
            var result = new List<EventDefinition>(builtIn);
            if (SkipAssets)
                return result.ToArray();
            var assets = Resources.LoadAll<EventDefinition>(Folder);
            for (int i = 0; i < assets.Length; i++)
            {
                var asset = assets[i];
                if (asset == null || string.IsNullOrEmpty(asset.id))
                {
                    Debug.LogWarning("Cards: у карты " + (asset != null ? asset.name : "?") + " пустой id — пропущена.", asset);
                    continue;
                }

                int index = result.FindIndex(c => c != null && c.id == asset.id);
                if (index >= 0)
                {
                    var target = result[index];
                    var keepArt = target.cardArt;
                    string keepName = target.name;
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(asset), target);
                    target.name = keepName;
                    // Встроенные иконки рисуются кодом — в ассет их не сохранить. Пустой арт ассета их не стирает.
                    if (asset.cardArt == null)
                        target.cardArt = keepArt;
                }
                else
                {
                    var copy = Object.Instantiate(asset);
                    copy.name = asset.name;
                    result.Add(copy);
                }
            }

            // Статус «Выключена» — карты нет в игре (ни в колоде, ни в магазинах).
            for (int i = result.Count - 1; i >= 0; i--)
            {
                if (result[i] != null && result[i].status == CardStatus.Disabled)
                {
                    var off = result[i];
                    result.RemoveAt(i);
                    if (Application.isPlaying)
                        Object.Destroy(off);
                    else
                        Object.DestroyImmediate(off);
                }
            }

            return result.ToArray();
        }
    }
}
