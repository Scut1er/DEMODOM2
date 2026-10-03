using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using RealityDirector.Core;
using RealityDirector.Events;
using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealityDirector.EditorTools
{
    // Общие справочники и помощники для инспекторов дизайнера.
    public static class DesignerData
    {
        public const string RoomsRoot = "Assets/Resources/Content/Rooms";
        public const string CardsRoot = "Assets/Resources/Content/Cards";
        public const string CharactersRoot = "Assets/Resources/Content/Characters";
        public const string MapsRoot = "Assets/Data/Map";
        public const string SeasonPath = "Assets/Data/SeasonConfig.asset";

        public static readonly string[] CrewKeys = { nameof(CrewTrack.Cast), nameof(CrewTrack.Operators), nameof(CrewTrack.Writers) };
        public static readonly string[] CrewNames = { "Кастинг", "Съёмочная", "Сценарная" };

        // Объекты квартиры, на которые можно навести карту (задаются в коре).
        public static readonly string[] ObjectIds = { "fridge", "bath_door", "bed_door" };
        public static readonly string[] ObjectNames = { "Холодильник", "Дверь ванной", "Дверь спальни" };

        // У этих карт есть особое поведение в коде квартиры — менять id нельзя, остальное можно.
        public static readonly string[] SpecialCardIds = { "open_bathroom", "open_bedroom", "meditation_bell" };

        static List<string> _tags;
        static List<string> _cardIds;
        static List<string> _roomIds;
        static List<string> _actorIds;
        static List<string> _episodeFlags;
        static List<string> _seasonFlags;
        static List<string> _narrativeTags;

        static DesignerData()
        {
            EditorApplication.projectChanged += Invalidate;
        }

        public static void Invalidate()
        {
            _cardIds = null;
            _roomIds = null;
            _actorIds = null;
            _episodeFlags = null;
            _seasonFlags = null;
            _narrativeTags = null;
            ContentLibrary.Clear();
        }

        // Теги моментов из MomentTags (Fire, Conflict, Crying...) — их понимают реакции участников.
        public static List<string> MomentTagList()
        {
            if (_tags != null)
                return _tags;
            _tags = new List<string>();
            foreach (var f in typeof(MomentTags).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.IsLiteral && f.FieldType == typeof(string))
                    _tags.Add((string)f.GetRawConstantValue());
            }

            return _tags;
        }

        public static List<string> CardIds()
        {
            if (_cardIds != null)
                return _cardIds;
            _cardIds = new List<string>();
            var content = PitchContent.Create();
            foreach (var card in content.All)
            {
                if (card != null && !_cardIds.Contains(card.id))
                    _cardIds.Add(card.id);
                Object.DestroyImmediate(card);
            }

            Object.DestroyImmediate(content.Aggressive);
            Object.DestroyImmediate(content.Sentimental);
            Object.DestroyImmediate(content.Panicker);
            Object.DestroyImmediate(content.AggressiveRules);
            Object.DestroyImmediate(content.SentimentalRules);
            Object.DestroyImmediate(content.PanickerRules);
            return _cardIds;
        }

        public static List<string> RoomIds()
        {
            if (_roomIds == null)
                _roomIds = Ids<RoomDefinition>();
            return _roomIds;
        }

        public static List<string> ActorIds()
        {
            if (_actorIds != null)
                return _actorIds;
            _actorIds = Ids<ActorDefinition>();
            if (!_actorIds.Contains("npc_zloi"))
                _actorIds.Add("npc_zloi");
            if (!_actorIds.Contains("npc_dobryak"))
                _actorIds.Add("npc_dobryak");
            return _actorIds;
        }

        public static List<string> EpisodeFlags()
        {
            if (_episodeFlags == null)
                CollectKeys();
            return _episodeFlags;
        }

        public static List<string> SeasonFlags()
        {
            if (_seasonFlags == null)
                CollectKeys();
            return _seasonFlags;
        }

        public static List<string> NarrativeTags()
        {
            if (_narrativeTags == null)
                CollectKeys();
            return _narrativeTags;
        }

        // Флаги и теги, которые уже где-то выставляются эффектами, — чтобы условия не ловили опечатки.
        static void CollectKeys()
        {
            _episodeFlags = new List<string>();
            _seasonFlags = new List<string>();
            _narrativeTags = new List<string>();
            foreach (var asset in LoadAll<ContentDefinition>())
            {
                var so = new SerializedObject(asset);
                var it = so.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType != SerializedPropertyType.Generic || it.type != nameof(Effect))
                        continue;
                    var type = (EffectType)it.FindPropertyRelative("type").enumValueIndex;
                    string key = it.FindPropertyRelative("key").stringValue;
                    if (string.IsNullOrEmpty(key))
                        continue;
                    if (type == EffectType.SetEpisodeFlag)
                        AddOnce(_episodeFlags, key);
                    else if (type == EffectType.SetSeasonFlag)
                        AddOnce(_seasonFlags, key);
                    else if (type == EffectType.AddNarrativeTag)
                        AddOnce(_narrativeTags, key);
                }
            }
        }

        static void AddOnce(List<string> list, string key)
        {
            if (!list.Contains(key))
                list.Add(key);
        }

        public static List<string> Ids<T>() where T : ContentDefinition
        {
            var ids = new List<string>();
            foreach (var asset in LoadAll<T>())
                AddOnce(ids, asset.Id);
            return ids;
        }

        public static List<T> LoadAll<T>() where T : Object
        {
            var list = new List<T>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    list.Add(asset);
            }

            return list;
        }

        // Создаёт ассет с уникальным именем, выделяет его в Project и открывает в инспекторе.
        public static T CreateAsset<T>(string folder, string baseName, Action<T> init = null) where T : ScriptableObject
        {
            Directory.CreateDirectory(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + baseName + ".asset");
            var asset = ScriptableObject.CreateInstance<T>();
            string fileName = Path.GetFileNameWithoutExtension(path);
            asset.name = fileName;
            init?.Invoke(asset);
            asset.name = fileName; // init мог скопировать чужое имя (CopySerialized)
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Invalidate();
            return asset;
        }

        // Текстовое поле + кнопка ▾ с известными значениями (id, флаги, теги).
        public static void PickerField(Rect rect, GUIContent label, SerializedProperty prop, IList<string> options, IList<string> names = null)
        {
            var field = new Rect(rect.x, rect.y, rect.width - 22f, rect.height);
            var button = new Rect(rect.xMax - 20f, rect.y, 20f, rect.height);
            EditorGUI.PropertyField(field, prop, label);
            using (new EditorGUI.DisabledScope(options == null || options.Count == 0))
            {
                if (GUI.Button(button, "▾", EditorStyles.miniButton))
                {
                    var targets = prop.serializedObject.targetObjects;
                    string path = prop.propertyPath;
                    var menu = new GenericMenu();
                    for (int i = 0; i < options.Count; i++)
                    {
                        string value = options[i];
                        string shown = names != null && i < names.Count ? names[i] + "  (" + value + ")" : value;
                        menu.AddItem(new GUIContent(shown), prop.stringValue == value, () =>
                        {
                            var so = new SerializedObject(targets);
                            so.FindProperty(path).stringValue = value;
                            so.ApplyModifiedProperties();
                        });
                    }

                    menu.DropDown(button);
                }
            }
        }

        public static bool InFolder(Object asset, string folder)
        {
            return AssetDatabase.GetAssetPath(asset).Replace('\\', '/').StartsWith(folder + "/");
        }
    }
}
