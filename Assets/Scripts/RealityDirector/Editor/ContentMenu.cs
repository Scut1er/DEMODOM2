using System.Collections.Generic;
using System.IO;
using RealityDirector.Core;
using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Меню для дизайнеров: стартовый набор контента и проверка id.
    public static class ContentMenu
    {
        const string RoomsRoot = "Assets/Resources/Content/Rooms";
        const string DataRoot = "Assets/Data";
        const string MapPath = "Assets/Data/Map/EpisodeMap_Default.asset";
        const string SeasonPath = "Assets/Data/SeasonConfig.asset";

        [MenuItem("RealityDirector/Content/Create Default Content")]
        public static void CreateDefaults()
        {
            var opening = Situation("situation_opening", "НАЧАЛО", "Холодное открытие", MapNodeKind.Start, new Color(0.22f, 0.2f, 0.27f, 1f),
                "Первая комната выпуска: знакомим зрителя с домом и героями.", "Снять первый хайлайт и понять, кто здесь кто.", ShowMood.Drama, 0, 0);
            opening.weight = 0f;
            Save(opening, "Situations");

            Save(Situation("situation_meet", "СЦЕНА", "Знакомство", MapNodeKind.Scene, new Color(0.78f, 0.6f, 0.22f, 1f),
                "Герои притираются друг к другу. Спокойная съёмка — хороша для семейного тона.", "Поймать момент тепла или первую искру.", ShowMood.Family, 6, 0), "Situations");
            Save(Situation("situation_challenge", "ИСПЫТАНИЕ", "Командный конкурс", MapNodeKind.Challenge, new Color(0.28f, 0.42f, 0.82f, 1f),
                "Соревнование выводит характеры наружу.", "Снять, как кто-то проигрывает некрасиво.", ShowMood.Trash, 6, 0), "Situations");
            Save(Situation("situation_confession", "ИСПОВЕДЬ", "Комната признаний", MapNodeKind.Confession, new Color(0.2f, 0.6f, 0.52f, 1f),
                "Герои говорят на камеру то, что не скажут в лицо.", "Довести кого-то до слёз.", ShowMood.Drama, 8, 0), "Situations");
            Save(Situation("situation_secret", "СЕКРЕТ", "Утечка", MapNodeKind.Secret, new Color(0.7f, 0.55f, 0.28f, 1f),
                "Один секрет — и в доме уже никто никому не верит.", "Снять реакцию на раскрытый секрет.", ShowMood.Drama, 6, 0), "Situations");
            Save(Situation("situation_conflict", "КОНФЛИКТ", "Ссора в доме", MapNodeKind.Conflict, new Color(0.82f, 0.28f, 0.24f, 1f),
                "Искры летят — идеальный момент для драки в кадре.", "Снять драку или громкую ссору.", ShowMood.Trash, 8, 0), "Situations");
            Save(Situation("situation_date", "СВИДАНИЕ", "Романтический ужин", MapNodeKind.Date, new Color(0.88f, 0.32f, 0.58f, 1f),
                "Свидание под камерами — нежность или неловкость.", "Поймать тёплый момент или провал свидания.", ShowMood.Family, 8, 0), "Situations");
            Save(Situation("situation_party", "ВЕЧЕРИНКА", "Ночь у бассейна", MapNodeKind.Party, new Color(0.52f, 0.28f, 0.78f, 1f),
                "Музыка, бассейн и никаких тормозов.", "Снять хаос, пока он не закончился.", ShowMood.Trash, 8, 0), "Situations");

            var elimination = Situation("situation_elimination", "ЭЛИМИНАЦИЯ", "Кто уходит?", MapNodeKind.Elimination, new Color(0.38f, 0.38f, 0.44f, 1f),
                "Голосование держит всех в напряжении. Нужен каст побольше.", "Снять реакцию на вылет.", ShowMood.Trash, 10, 0);
            elimination.weight = 0.6f;
            elimination.conditions.Add(new Condition { type = ConditionType.CrewLevelAtLeast, key = nameof(CrewTrack.Cast), value = 2 });
            elimination.conditions.Add(new Condition { type = ConditionType.EpisodeAtLeast, value = 2 });
            Save(elimination, "Situations");

            var ev = Room<EventRoomDefinition>("event_mystery", "СОБЫТИЕ", "Что-то случилось", MapNodeKind.Mystery, new Color(0.45f, 0.3f, 0.6f, 1f),
                "Что-то происходит за кадром. Выбор с последствиями для следующих комнат.", "");
            ev.uniquePerEpisode = false;
            Save(ev, "Events");

            var marketing = Room<MarketingRoomDefinition>("marketing_default", "МАРКЕТИНГ", "Спонсоры и закупка", MapNodeKind.Shop, new Color(0.25f, 0.55f, 0.35f, 1f),
                "Купить карты за бюджет или подписать спонсорский контракт.", "");
            marketing.uniquePerEpisode = false;
            Save(marketing, "Marketing");

            var montage = Room<MontageRoomDefinition>("montage_default", "МОНТАЖ", "Финальная склейка", MapNodeKind.Climax, new Color(0.92f, 0.72f, 0.28f, 1f),
                "Выбираем лучшие клипы выпуска, ставим по порядку и выпускаем в эфир.", "Собрать сильный эфир из отснятого.");
            montage.weight = 0f;
            Save(montage, "Montage");

            var map = AssetDatabase.LoadAssetAtPath<EpisodeMapConfig>(MapPath);
            if (map != null)
            {
                map.opening = Load<SituationRoomDefinition>("Situations", "situation_opening");
                map.montage = Load<MontageRoomDefinition>("Montage", "montage_default");
                EditorUtility.SetDirty(map);
            }

            var season = AssetDatabase.LoadAssetAtPath<SeasonConfig>(SeasonPath);
            if (season == null)
            {
                season = ScriptableObject.CreateInstance<SeasonConfig>();
                Directory.CreateDirectory(DataRoot);
                AssetDatabase.CreateAsset(season, SeasonPath);
            }

            if (map != null && !season.maps.Contains(map))
            {
                season.maps.Add(map);
                EditorUtility.SetDirty(season);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Content: стартовый набор готов (" + RoomsRoot + "). Существующие ассеты не перезаписаны.");
        }

        // Встроенные карты (из кода) → ассеты. Совместимое имя для старых кнопок.
        public static void ExportCards()
        {
            SyncCards();
        }

        // Синхронизация с кодом: новые встроенные карты → ассеты; полям, которых в ассете ещё нет
        // (их добавили в EventDefinition после выгрузки), — встроенные значения. Значения дизайнера не трогаем.
        [MenuItem("RealityDirector/Content/Sync Built-in Cards")]
        public static void SyncCards()
        {
            var report = CardSync.Run(true);
            Debug.Log("Cards: " + report);
        }

        // Участники с артом художника → ассеты в Resources/Content/Characters.
        [MenuItem("RealityDirector/Content/Create Default Characters")]
        public static void CreateCharacters()
        {
            Character("npc_zloi", "Злой", "zloi", 0, RealityDirector.NPC.TraitId.Aggressive, RealityDirector.NPC.HiddenTrait.Prankster, "агрессивный");
            Character("npc_dobryak", "Добряк", "dobryak", 1, RealityDirector.NPC.TraitId.Panicker, RealityDirector.NPC.HiddenTrait.Kleptomaniac, "паникер");
            Character("npc_kira", "Кира", "kira", 2, RealityDirector.NPC.TraitId.Jealous, RealityDirector.NPC.HiddenTrait.None, "ревнивая", "body3");
            Character("npc_max", "Макс", "max", 3, RealityDirector.NPC.TraitId.Vain, RealityDirector.NPC.HiddenTrait.Singer, "тщеславный", "body2");
            Character("npc_lyusya", "Люся", "lyusya", 4, RealityDirector.NPC.TraitId.Shy, RealityDirector.NPC.HiddenTrait.None, "застенчивая");
            AssetDatabase.SaveAssets();
            DesignerData.Invalidate();
            Debug.Log("Characters: участники готовы (" + DesignerData.CharactersRoot + "). Существующие не тронуты.");
        }

        static void Character(string id, string displayName, string prefix, int order,
            RealityDirector.NPC.TraitId trait, RealityDirector.NPC.HiddenTrait hidden, string visible, string body = "body")
        {
            Directory.CreateDirectory(DesignerData.CharactersRoot);
            string path = DesignerData.CharactersRoot + "/" + id + ".asset";
            if (AssetDatabase.LoadAssetAtPath<ActorDefinition>(path) != null)
                return;
            var actor = ScriptableObject.CreateInstance<ActorDefinition>();
            actor.name = id;
            actor.SetId(id);
            actor.displayName = displayName;
            actor.artPrefix = prefix;
            actor.bodyPrefix = body;
            actor.order = order;
            actor.mainTrait = trait;
            actor.hiddenTrait = hidden;
            actor.visibleTraits.Add(visible);
            AssetDatabase.CreateAsset(actor, path);
        }

        [MenuItem("RealityDirector/Content/Validate Content IDs")]
        public static void Validate()
        {
            var seen = new Dictionary<string, ContentDefinition>();
            int problems = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:ContentDefinition"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ContentDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null)
                    continue;
                if (seen.TryGetValue(asset.Id, out var other))
                {
                    problems++;
                    Debug.LogError("Content: id '" + asset.Id + "' у двух ассетов: " + other.name + " и " + asset.name, asset);
                }
                else
                {
                    seen[asset.Id] = asset;
                }

                if (!AssetDatabase.GetAssetPath(asset).Contains("/Resources/Content/"))
                    Debug.LogWarning("Content: " + asset.name + " лежит не в Resources/Content — в пул сам не попадёт (только если указан напрямую).", asset);
            }

            Debug.Log(problems == 0 ? "Content: id в порядке (" + seen.Count + " ассетов)." : "Content: найдено повторов id: " + problems);
        }

        static SituationRoomDefinition Situation(string id, string title, string subtitle, MapNodeKind icon, Color color,
            string description, string goal, ShowMood mood, int tone, int budget)
        {
            var room = Room<SituationRoomDefinition>(id, title, subtitle, icon, color, description, goal);
            if (tone > 0)
                room.onEnter.Add(new Effect { type = EffectType.Tone, mood = mood, value = tone });
            if (budget != 0)
                room.onEnter.Add(new Effect { type = EffectType.Budget, value = budget });
            return room;
        }

        static T Room<T>(string id, string title, string subtitle, MapNodeKind icon, Color color, string description, string goal)
            where T : RoomDefinition
        {
            var room = ScriptableObject.CreateInstance<T>();
            room.name = id;
            room.SetId(id);
            room.title = title;
            room.subtitle = subtitle;
            room.icon = icon;
            room.color = color;
            room.description = description;
            room.goal = goal;
            return room;
        }

        static void Save(RoomDefinition room, string folder)
        {
            string dir = RoomsRoot + "/" + folder;
            Directory.CreateDirectory(dir);
            string path = dir + "/" + room.name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<RoomDefinition>(path) != null)
            {
                Object.DestroyImmediate(room);
                return;
            }

            AssetDatabase.CreateAsset(room, path);
        }

        static T Load<T>(string folder, string id) where T : RoomDefinition
        {
            return AssetDatabase.LoadAssetAtPath<T>(RoomsRoot + "/" + folder + "/" + id + ".asset");
        }
    }
}
