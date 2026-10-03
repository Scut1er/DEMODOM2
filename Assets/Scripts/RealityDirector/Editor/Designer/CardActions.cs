using RealityDirector.Events;
using RealityDirector.Meta;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Действия над картами: создать, дублировать, удалить, проверить в квартире.
    public static class CardActions
    {
        const string ApartmentScene = "Assets/Scenes/Apartment_Pitch.unity";

        public static EventDefinition Create()
        {
            return DesignerData.CreateAsset<EventDefinition>(DesignerData.CardsRoot, "card_new", c =>
            {
                c.id = ContentDefinition.MakeId(c.name);
                c.displayName = "Новая карта";
                c.hint = "сразу на весь дом";
                c.targetType = TargetType.Global;
                c.cardColor = new Color(0.4f, 0.3f, 0.5f, 1f);
                c.tags.Add(Core.MomentTags.Conflict);
                c.moods.Add(Core.ShowMood.Drama);
                c.price = 100;
            });
        }

        public static EventDefinition Duplicate(EventDefinition source)
        {
            string baseName = (source.id ?? source.name) + "_copy";
            var json = EditorJsonUtility.ToJson(source);
            return DesignerData.CreateAsset<EventDefinition>(DesignerData.CardsRoot, baseName, c =>
            {
                string file = c.name;
                EditorJsonUtility.FromJsonOverwrite(json, c);
                c.name = file;
                c.id = ContentDefinition.MakeId(file);
                c.displayName = source.displayName + " (копия)";
                c.starter = false;
            });
        }

        public static bool Delete(EventDefinition card)
        {
            if (card == null)
                return false;
            string path = AssetDatabase.GetAssetPath(card);
            bool builtIn = CardSync.BuiltInIds().Contains(card.id);
            string message = builtIn
                ? "Это встроенная карта «" + card.displayName + "». Ассет удалится, и карта вернётся к значениям из кода."
                : "Удалить карту «" + card.displayName + "» (" + card.id + ")? Из колод и сейвов она пропадёт.";
            if (!EditorUtility.DisplayDialog("Удалить карту", message, "Удалить", "Отмена"))
                return false;
            AssetDatabase.DeleteAsset(path);
            DesignerData.Invalidate();
            return true;
        }

        // Квартира откроется с этой картой первой в руке (через MetaService.TryEmbark).
        public static void PlayTest(EventDefinition card)
        {
            if (card == null || string.IsNullOrEmpty(card.id))
                return;
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Проверка карты", "Сначала остановите Play.", "Ок");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            AssetDatabase.SaveAssets();
            EditorPrefs.SetString(CardLibrary.TestCardPref, card.id);
            EditorSceneManager.OpenScene(ApartmentScene);
            EditorApplication.EnterPlaymode();
        }
    }
}
