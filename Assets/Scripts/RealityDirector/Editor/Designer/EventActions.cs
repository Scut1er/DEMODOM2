using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Meta;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Действия над событиями: создать, дублировать, удалить, проверить в игре; песочница для предпросмотра.
    public static class EventActions
    {
        public const string Folder = DesignerData.RoomsRoot + "/Events";
        const string HubScene = "Assets/Scenes/Hub.unity";

        public static EventRoomDefinition Create()
        {
            return DesignerData.CreateAsset<EventRoomDefinition>(Folder, "event_new", e =>
            {
                e.SetId(ContentDefinition.MakeId(e.name));
                e.title = "НОВОЕ СОБЫТИЕ";
                e.subtitle = "что-то случилось";
                e.description = "Коротко для карты выпуска.";
                e.icon = MapNodeKind.Mystery;
                e.color = new Color(0.45f, 0.3f, 0.6f, 1f);
                e.uniquePerEpisode = true;
                e.body = "{актёр} заходит в аппаратную и смотрит прямо в камеру. Что будем делать?";
                e.roles.Add(new EventRole { key = "актёр" });
                e.choices.Add(new EventChoice { label = "Снять это", resultText = "{актёр} довольно улыбается. Материал будет." });
                e.choices.Add(new EventChoice { label = "Выключить свет", resultText = "Темнота. {актёр} уходит обиженным." });
            });
        }

        public static EventRoomDefinition Duplicate(EventRoomDefinition source)
        {
            var json = EditorJsonUtility.ToJson(source);
            return DesignerData.CreateAsset<EventRoomDefinition>(Folder, source.Id + "_copy", e =>
            {
                string file = e.name;
                EditorJsonUtility.FromJsonOverwrite(json, e);
                e.name = file;
                e.SetId(ContentDefinition.MakeId(file));
                e.title = source.title + " (копия)";
            });
        }

        public static bool Delete(EventRoomDefinition e)
        {
            if (e == null || !EditorUtility.DisplayDialog("Удалить событие", "Удалить «" + e.title + "» (" + e.Id + ")?", "Удалить", "Отмена"))
                return false;
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(e));
            DesignerData.Invalidate();
            return true;
        }

        // Хаб откроется и сразу покажет событие поверх карты выпуска. Результат не сохраняется в сейв.
        public static void PlayTest(EventRoomDefinition e)
        {
            if (e == null)
                return;
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Проверка события", "Сначала остановите Play.", "Ок");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            AssetDatabase.SaveAssets();
            EditorPrefs.SetString(EventResolver.TestEventPref, e.Id);
            EditorSceneManager.OpenScene(HubScene);
            EditorApplication.EnterPlaymode();
        }

        // ---------- песочница предпросмотра ----------

        public class Sandbox
        {
            public int episode = 1;
            public int money = 200;
            public int cash = 90;
            public int castLevel = 1;
            public List<string> tags = new List<string>();

            public RuleContext Context()
            {
                var season = new SeasonState();
                season.Reset(null);
                season.episodeIndex = episode - 1;
                season.money = money;
                season.castLevel = castLevel;
                var ep = new EpisodeState { index = episode - 1, cash = cash, mapSeed = 12345 };
                foreach (var m in CastRoster.All())
                    ep.cast.Add(m.id);
                foreach (var t in tags)
                    ep.AddTag(t);
                return new RuleContext(season, ep, new SeasonTone());
            }
        }

        public static string ActorName(string id)
        {
            foreach (var m in CastRoster.All())
            {
                if (m.id == id)
                    return m.name;
            }

            return id;
        }

        public static string CardName(string id)
        {
            foreach (var card in DesignerData.LoadAll<Events.EventDefinition>())
            {
                if (card.id == id)
                    return card.displayName;
            }

            return id;
        }

        // Разыграть вариант в песочнице с заданным исходом броска.
        public static EventOutcome Simulate(EventRoomDefinition e, int choice, bool success, Sandbox sandbox)
        {
            var ctx = sandbox.Context();
            var roles = EventResolver.CastRoles(e, ctx.episode, "preview");
            var outcome = EventResolver.Resolve(e.choices[choice], ctx, roles, ActorName, CardName, new FixedRoll(success ? 0 : 99));
            return outcome;
        }

        class FixedRoll : System.Random
        {
            readonly int _value;

            public FixedRoll(int value)
            {
                _value = value;
            }

            public override int Next(int maxValue)
            {
                return Mathf.Clamp(_value, 0, maxValue - 1);
            }
        }
    }
}
