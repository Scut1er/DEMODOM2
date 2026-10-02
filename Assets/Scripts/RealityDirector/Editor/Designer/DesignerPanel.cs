using System.Collections.Generic;
using RealityDirector.Events;
using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Одно окно для геймдизайнера: сезон, карты выпусков, комнаты, карты продюсера, участники.
    // Открыть: RealityDirector → Designer Panel.
    public class DesignerPanel : EditorWindow
    {
        Vector2 _scroll;
        int _tab;
        static readonly string[] Tabs = { "Сезон и карта", "Комнаты", "Карты продюсера", "Участники", "Инструменты" };

        [MenuItem("RealityDirector/Designer Panel", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<DesignerPanel>("Дизайнер");
            window.minSize = new Vector2(420, 360);
        }

        void OnFocus()
        {
            DesignerData.Invalidate();
        }

        void OnGUI()
        {
            _tab = GUILayout.Toolbar(_tab, Tabs);
            EditorGUILayout.Space(6);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case 0: SeasonTab(); break;
                case 1: RoomsTab(); break;
                case 2: CardsTab(); break;
                case 3: CharactersTab(); break;
                default: ToolsTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        void SeasonTab()
        {
            Hint("Сезон = несколько выпусков. Каждый выпуск — своя карта комнат (как в Slay the Spire), в конце — монтаж.\n" +
                 "Выделите ассет — его настройки откроются в инспекторе. У карты там есть предпросмотр и статистика.");
            var season = AssetDatabase.LoadAssetAtPath<SeasonConfig>(DesignerData.SeasonPath);
            Section("Сезон");
            if (season != null)
                Row(season, season.episodes + " выпусков  ·  каст " + season.castMin + "–" + season.castMax + "  ·  в эфир " + season.finalCutSize + " из " + season.footageLimit + " клипов");
            else if (GUILayout.Button("Создать SeasonConfig"))
                DesignerData.CreateAsset<SeasonConfig>("Assets/Data", "SeasonConfig");

            Section("Карты выпусков");
            foreach (var map in DesignerData.LoadAll<EpisodeMapConfig>())
            {
                bool used = season != null && season.maps.Contains(map);
                Row(map, map.floors + " рядов  ·  " + map.lanes + " дорожки  ·  " + map.paths + " пути" + (used ? "" : "   (не в сезоне)"));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Новая карта выпуска"))
                {
                    var src = DesignerData.LoadAll<EpisodeMapConfig>();
                    var map = DesignerData.CreateAsset<EpisodeMapConfig>(DesignerData.MapsRoot, "EpisodeMap_New", m =>
                    {
                        if (src.Count > 0)
                        {
                            EditorUtility.CopySerialized(src[0], m);
                        }
                    });
                    if (season != null)
                    {
                        Undo.RecordObject(season, "Add map");
                        season.maps.Add(map);
                        EditorUtility.SetDirty(season);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }

        void RoomsTab()
        {
            Hint("Одна комната = один ассет в " + DesignerData.RoomsRoot + ". Новая комната сразу попадает в пул карт.\n" +
                 "Условия решают, когда комната может выпасть; эффекты — что случится при входе.");
            var rooms = DesignerData.LoadAll<RoomDefinition>();
            RoomGroup<SituationRoomDefinition>("Съёмки", rooms, "Situations", "Situation_New");
            RoomGroup<EventRoomDefinition>("События", rooms, "Events", "Event_New");
            RoomGroup<MarketingRoomDefinition>("Маркетинг", rooms, "Marketing", "Marketing_New");
            RoomGroup<MontageRoomDefinition>("Монтаж", rooms, "Montage", "Montage_New");
        }

        void RoomGroup<T>(string title, List<RoomDefinition> rooms, string folder, string baseName) where T : RoomDefinition
        {
            Section(title);
            foreach (var room in rooms)
            {
                if (!(room is T))
                    continue;
                string extra = "вес " + room.weight;
                if (room.conditions.Count > 0)
                    extra += "  ·  условий: " + room.conditions.Count;
                if (room.onEnter.Count > 0)
                    extra += "  ·  эффектов: " + room.onEnter.Count;
                Row(room, (string.IsNullOrEmpty(room.title) ? room.name : room.title) + "  ·  " + extra);
            }

            if (GUILayout.Button("+ " + title + ": новая комната"))
                DesignerData.CreateAsset<T>(DesignerData.RoomsRoot + "/" + folder, baseName, r =>
                {
                    r.title = "НОВАЯ";
                    r.subtitle = "подзаголовок";
                });
        }

        void CardsTab()
        {
            Hint("Карты продюсера — ассеты в " + DesignerData.CardsRoot + ".\n" +
                 "Ассет с id встроенной карты меняет её; ассет с новым id — новая карта (в магазине, если есть цена, или в стартовой колоде).\n" +
                 "Новая карта работает через общие поля: цель, теги, тон, злость, поджог. Особая механика — только у встроенных.");
            var cards = DesignerData.LoadAll<EventDefinition>();
            Section("Карты в ассетах");
            if (cards.Count == 0)
                EditorGUILayout.LabelField("Пока все карты встроены в код. Нажмите «Выгрузить встроенные», чтобы их править.", EditorStyles.wordWrappedMiniLabel);
            foreach (var card in cards)
            {
                string economy = card.starter ? "стартовая" : card.price > 0 ? card.price + " кр" : "не выдаётся";
                Row(card, card.displayName + "  ·  " + card.id + "  ·  " + economy);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Выгрузить встроенные карты"))
                    ContentMenu.ExportCards();
                if (GUILayout.Button("+ Новая карта"))
                    DesignerData.CreateAsset<EventDefinition>(DesignerData.CardsRoot, "card_new", c =>
                    {
                        c.id = ContentDefinition.MakeId(c.name);
                        c.displayName = "Новая карта";
                        c.hint = "сразу на весь дом";
                        c.targetType = TargetType.Global;
                        c.cardColor = new Color(0.4f, 0.3f, 0.5f, 1f);
                        c.price = 100;
                    });
            }
        }

        void CharactersTab()
        {
            Hint("Участники — ассеты в " + DesignerData.CharactersRoot + ". Отсюда берутся список в хабе и кастинг.\n" +
                 "Арт: файлы <artPrefix>_happy / _mad / _sad / _love в Resources/Art/Characters.");
            var actors = DesignerData.LoadAll<ActorDefinition>();
            Section("Участники");
            if (actors.Count == 0)
                EditorGUILayout.LabelField("Ассетов нет — в хабе двое встроенных. Нажмите «Создать стартовых», чтобы их править.", EditorStyles.wordWrappedMiniLabel);
            foreach (var actor in actors)
                Row(actor, actor.displayName + "  ·  " + actor.archetype + (actor.available ? "" : "   (выключен)"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Создать стартовых (Злой, Добряк)"))
                    ContentMenu.CreateCharacters();
                if (GUILayout.Button("+ Новый участник"))
                    DesignerData.CreateAsset<ActorDefinition>(DesignerData.CharactersRoot, "npc_new", a =>
                    {
                        a.order = actors.Count;
                    });
            }
        }

        void ToolsTab()
        {
            Section("Контент");
            if (GUILayout.Button("Создать стартовый набор комнат"))
                ContentMenu.CreateDefaults();
            if (GUILayout.Button("Проверить id (повторы, не та папка)"))
                ContentMenu.Validate();
            Section("Игра");
            if (GUILayout.Button("Удалить сейв сезона"))
            {
                Persistence.SaveSystem.Delete();
                Debug.Log("Сейв удалён.");
            }

            if (GUILayout.Button("Открыть сцену хаба"))
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Hub.unity");
            Section("Справка");
            if (GUILayout.Button("Открыть Designer_Guide.md"))
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Docs/Designer_Guide.md"));
        }

        static void Section(string title)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        static void Hint(string text)
        {
            EditorGUILayout.HelpBox(text, MessageType.None);
        }

        static void Row(Object asset, string text)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(asset.name, EditorStyles.linkLabel, GUILayout.Width(170)))
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }

                EditorGUILayout.LabelField(text, EditorStyles.miniLabel);
            }
        }
    }
}
