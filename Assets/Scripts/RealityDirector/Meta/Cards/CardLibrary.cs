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

        public static EventDefinition[] Merge(EventDefinition[] builtIn)
        {
            var result = new List<EventDefinition>(builtIn);
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

            return result.ToArray();
        }
    }
}
