using System.Collections.Generic;
using System.Linq;
using RealityDirector.Core;
using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Инспектор карты выпуска: настройки + живой предпросмотр + статистика по сотне карт.
    [CustomEditor(typeof(EpisodeMapConfig))]
    public class EpisodeMapConfigEditor : UnityEditor.Editor
    {
        static int _seed = 1;
        static int _episode = 1;
        static int _castLevel = 1;
        static bool _autoPreview = true;

        MapGraph _graph;
        RoomCatalog _catalog;
        string _stats;
        List<string> _warnings = new List<string>();

        public override void OnInspectorGUI()
        {
            var config = (EpisodeMapConfig)target;
            EditorGUILayout.HelpBox(
                "Форма карты одного выпуска. Старт — одна комната слева, монтаж — всегда справа. " +
                "Комнаты берутся из пула (или из Resources/Content/Rooms) по весу и условиям.", MessageType.None);

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Предпросмотр", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                _seed = EditorGUILayout.IntField(new GUIContent("Seed", "Любое число — вариант карты."), _seed);
                if (GUILayout.Button("Другая", GUILayout.Width(70)))
                    _seed = Random.Range(1, 99999);
                changed |= EditorGUI.EndChangeCheck();
            }

            EditorGUI.BeginChangeCheck();
            _episode = EditorGUILayout.IntSlider(new GUIContent("Как будто выпуск №", "Для условий комнат «с выпуска N»."), _episode, 1, 12);
            _castLevel = EditorGUILayout.IntSlider(new GUIContent("Уровень Кастинга", "Для условий «уровень команды»."), _castLevel, 1, 5);
            _autoPreview = EditorGUILayout.Toggle("Обновлять сразу", _autoPreview);
            changed |= EditorGUI.EndChangeCheck();

            if (_graph == null || (changed && _autoPreview) || GUILayout.Button("Сгенерировать"))
                Preview(config);

            foreach (var w in _warnings)
                EditorGUILayout.HelpBox(w, MessageType.Warning);
            DrawGraph();
            if (!string.IsNullOrEmpty(_stats))
                EditorGUILayout.HelpBox(_stats, MessageType.Info);

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Статистика по 200 картам (для балансировки весов)"))
                Statistics(config);
        }

        RuleContext Context()
        {
            var season = new SeasonState();
            season.Reset(null);
            season.episodeIndex = _episode - 1;
            season.castLevel = _castLevel;
            var episode = new EpisodeState { index = _episode - 1 };
            episode.cast.AddRange(new[] { "npc_zloi", "npc_dobryak" });
            return new RuleContext(season, episode, new SeasonTone());
        }

        void Preview(EpisodeMapConfig config)
        {
            DesignerData.Invalidate();
            _catalog?.DestroyPlaceholders();
            var catalog = _catalog = new RoomCatalog(config);
            _graph = MapGenerator.Generate(catalog, _seed, Context());
            _warnings = Check(config, catalog);

            var byType = new Dictionary<RoomType, int>();
            foreach (var n in _graph.nodes)
                byType[n.type] = (byType.TryGetValue(n.type, out var c) ? c : 0) + 1;
            _stats = "Комнат на карте: " + _graph.nodes.Count + "   ·   путь игрока: " + _graph.Layers + " комнат\n" +
                     string.Join("   ·   ", byType.Select(kv => RoomCatalog.TypeTitle(kv.Key).ToLowerInvariant() + ": " + kv.Value));
        }

        void OnDisable()
        {
            _catalog?.DestroyPlaceholders();
            _catalog = null;
        }

        List<string> Check(EpisodeMapConfig config, RoomCatalog catalog)
        {
            var list = new List<string>();
            var ctx = Context();
            if (catalog.Available(RoomType.Situation, ctx).Count == 0 && config.opening == null)
                list.Add("Нет ни одной съёмки для этого выпуска — на карте будут заглушки.");
            if (config.eventWeight > 0 && catalog.Available(RoomType.Event, ctx).Count == 0)
                list.Add("Вес событий > 0, но подходящих комнат-событий нет — событий на карте не будет.");
            if (config.marketingWeight > 0 && catalog.Available(RoomType.Marketing, ctx).Count == 0)
                list.Add("Вес маркетинга > 0, но подходящих комнат-маркетинга нет — маркетинга на карте не будет.");
            if (config.montage == null && catalog.Available(RoomType.Montage, ctx).Count == 0)
                list.Add("Не задан монтаж и в пуле нет комнаты-монтажа — будет заглушка.");
            if (config.guaranteedMarketingFloor >= config.floors - 1)
                list.Add("guaranteedMarketingFloor за пределами карты (последний ряд — монтаж).");
            if (config.paths > 1 && config.lanes == 1)
                list.Add("Одна дорожка — развилок не будет, сколько бы ни было путей.");
            return list;
        }

        void Statistics(EpisodeMapConfig config)
        {
            DesignerData.Invalidate();
            var catalog = new RoomCatalog(config);
            var ctx = Context();
            var rooms = new Dictionary<string, int>();
            var types = new Dictionary<RoomType, int>();
            int nodes = 0;
            const int Runs = 200;
            for (int s = 1; s <= Runs; s++)
            {
                var g = MapGenerator.Generate(catalog, s * 7919, ctx);
                nodes += g.nodes.Count;
                foreach (var n in g.nodes)
                {
                    string title = n.room != null ? n.room.title + " (" + n.roomId + ")" : n.roomId;
                    rooms[title] = (rooms.TryGetValue(title, out var c) ? c : 0) + 1;
                    types[n.type] = (types.TryGetValue(n.type, out var t) ? t : 0) + 1;
                }
            }

            catalog.DestroyPlaceholders();
            var lines = new List<string>
            {
                "В среднем комнат на карте: " + (nodes / (float)Runs).ToString("0.0"),
                "Доля типов: " + string.Join("  ·  ", types.Select(kv => RoomCatalog.TypeTitle(kv.Key).ToLowerInvariant() + " " + (100f * kv.Value / nodes).ToString("0") + "%")),
                "",
                "Сколько раз комната встречается на 200 картах:"
            };
            foreach (var kv in rooms.OrderByDescending(kv => kv.Value))
                lines.Add("  " + kv.Value.ToString().PadLeft(4) + "   " + kv.Key);
            _stats = string.Join("\n", lines);
        }

        void DrawGraph()
        {
            if (_graph == null || _graph.nodes.Count == 0)
                return;
            var area = GUILayoutUtility.GetRect(10, 230, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(area, new Color(0.13f, 0.12f, 0.15f));
            var pad = new Vector2(46f, 26f);
            var inner = new Rect(area.x + pad.x, area.y + pad.y, area.width - pad.x * 2, area.height - pad.y * 2);
            Vector2 Pos(MapNode n)
            {
                float x = _graph.Layers > 1 ? inner.x + inner.width * n.layer / (_graph.Layers - 1) : inner.center.x;
                float y = inner.center.y - n.row * inner.height * 0.5f;
                return new Vector2(x, y);
            }

            if (Event.current.type == EventType.Repaint)
            {
                Handles.BeginGUI();
                Handles.color = new Color(1f, 1f, 1f, 0.35f);
                foreach (var n in _graph.nodes)
                {
                    foreach (var id in n.next)
                    {
                        var to = _graph.Find(id);
                        if (to != null)
                            Handles.DrawAAPolyLine(2.5f, Pos(n), Pos(to));
                    }
                }

                Handles.EndGUI();
            }

            var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = 9 };
            style.normal.textColor = Color.white;
            foreach (var n in _graph.nodes)
            {
                var p = Pos(n);
                var box = new Rect(p.x - 38f, p.y - 15f, 76f, 30f);
                EditorGUI.DrawRect(box, n.color);
                EditorGUI.DrawRect(new Rect(box.x, box.y, box.width, 3f), TypeColor(n.type));
                GUI.Label(box, new GUIContent(n.title, n.subtitle + "\n" + n.description + "\nid: " + n.roomId), style);
            }
        }

        static Color TypeColor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Event: return new Color(0.7f, 0.5f, 1f);
                case RoomType.Marketing: return new Color(0.4f, 1f, 0.5f);
                case RoomType.Montage: return new Color(1f, 0.85f, 0.3f);
                default: return Color.white;
            }
        }
    }
}
