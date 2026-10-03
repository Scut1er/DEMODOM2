using System.Collections.Generic;
using System.Linq;
using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Мастерская событий: список + редактор; «Связи» — какие теги и флаги события ставят и кто их проверяет.
    public class EventWorkshop : EditorWindow
    {
        static readonly string[] Modes = { "Событие", "Связи", "Обзор" };
        static readonly string[] Filters = { "Все", "Выпадают на карте", "Выключенные (вес 0)", "С риском (шанс)", "С ценой", "С проблемами" };

        int _mode;
        int _filter;
        string _search = "";
        EventRoomDefinition _selected;
        UnityEditor.Editor _editor;
        Vector2 _listScroll;
        Vector2 _editScroll;
        Vector2 _linksScroll;
        List<EventRoomDefinition> _events = new List<EventRoomDefinition>();

        [MenuItem("RealityDirector/Event Workshop", priority = 2)]
        public static void Open()
        {
            Open(null);
        }

        public static void Open(EventRoomDefinition e)
        {
            var window = GetWindow<EventWorkshop>("Мастерская событий");
            window.minSize = new Vector2(780, 500);
            if (e != null)
            {
                window._selected = e;
                window._mode = 0;
            }

            window.Reload();
        }

        void OnEnable()
        {
            Reload();
        }

        void OnFocus()
        {
            Reload();
        }

        void OnDisable()
        {
            if (_editor != null)
                DestroyImmediate(_editor);
        }

        void Reload()
        {
            DesignerData.Invalidate();
            _events = DesignerData.LoadAll<EventRoomDefinition>().OrderBy(e => e.title).ToList();
            if (_selected == null && _events.Count > 0)
                _selected = _events[0];
            Repaint();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _mode = GUILayout.Toolbar(_mode, Modes, EditorStyles.toolbarButton, GUILayout.Width(250));
                GUILayout.Space(8);
                _filter = EditorGUILayout.Popup(_filter, Filters, EditorStyles.toolbarPopup, GUILayout.Width(160));
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ Новое событие", EditorStyles.toolbarButton))
                    Select(EventActions.Create());
                using (new EditorGUI.DisabledScope(_selected == null))
                {
                    if (GUILayout.Button("Дублировать", EditorStyles.toolbarButton))
                        Select(EventActions.Duplicate(_selected));
                    if (GUILayout.Button("Удалить", EditorStyles.toolbarButton) && EventActions.Delete(_selected))
                    {
                        _selected = null;
                        Reload();
                    }

                    if (GUILayout.Button("▶ Проверить", EditorStyles.toolbarButton))
                        EventActions.PlayTest(_selected);
                }
            }

            switch (_mode)
            {
                case 0: EventMode(); break;
                case 1: Links(); break;
                default: Overview(); break;
            }
        }

        void Select(EventRoomDefinition e)
        {
            _selected = e;
            _mode = 0;
            Reload();
            GUI.FocusControl(null);
        }

        List<EventRoomDefinition> Visible()
        {
            IEnumerable<EventRoomDefinition> list = _events;
            switch (_filter)
            {
                case 1: list = list.Where(e => e.weight > 0f); break;
                case 2: list = list.Where(e => e.weight <= 0f); break;
                case 3: list = list.Where(e => e.choices.Any(c => c.chance < 100)); break;
                case 4: list = list.Where(e => e.choices.Any(c => c.costMoney > 0 || c.costCash > 0)); break;
                case 5: list = list.Where(e => Problems(e).Count > 0); break;
            }

            if (!string.IsNullOrEmpty(_search))
            {
                string q = _search.ToLowerInvariant();
                list = list.Where(e => (e.title ?? "").ToLowerInvariant().Contains(q) || e.Id.Contains(q) || (e.body ?? "").ToLowerInvariant().Contains(q));
            }

            return list.ToList();
        }

        // ---------- Событие ----------

        void EventMode()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(_listScroll, GUILayout.Width(260)))
                {
                    _listScroll = scroll.scrollPosition;
                    var visible = Visible();
                    if (visible.Count == 0)
                        EditorGUILayout.LabelField("Нет событий под фильтр.", EditorStyles.centeredGreyMiniLabel);
                    foreach (var e in visible)
                        ListRow(e);
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(_editScroll))
                {
                    _editScroll = scroll.scrollPosition;
                    if (_selected == null)
                    {
                        EditorGUILayout.HelpBox("Выберите событие слева или создайте новое.", MessageType.None);
                        return;
                    }

                    UnityEditor.Editor.CreateCachedEditor(_selected, typeof(EventRoomEditor), ref _editor);
                    _editor.OnInspectorGUI();
                }
            }
        }

        void ListRow(EventRoomDefinition e)
        {
            var rect = GUILayoutUtility.GetRect(10, 40, GUILayout.ExpandWidth(true));
            if (e == _selected)
                EditorGUI.DrawRect(rect, new Color(0.24f, 0.37f, 0.6f, 0.55f));
            EditorGUI.DrawRect(new Rect(rect.x + 4, rect.y + 5, 6, rect.height - 10), e.color);
            var title = new GUIStyle(EditorStyles.boldLabel);
            if (e.weight <= 0f)
                title.normal.textColor = Color.gray;
            GUI.Label(new Rect(rect.x + 16, rect.y + 3, rect.width - 60, 18), string.IsNullOrEmpty(e.title) ? e.name : e.title, title);
            string info = e.choices.Count + " вар.";
            if (e.choices.Any(c => c.chance < 100))
                info += " · риск";
            if (e.conditions.Count > 0)
                info += " · условий " + e.conditions.Count;
            if (e.weight <= 0f)
                info += " · выкл";
            GUI.Label(new Rect(rect.x + 16, rect.y + 20, rect.width - 40, 16), info, EditorStyles.miniLabel);
            if (Problems(e).Count > 0)
            {
                var warn = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleRight };
                warn.normal.textColor = new Color(1f, 0.6f, 0.3f);
                GUI.Label(new Rect(rect.xMax - 30, rect.y, 24, rect.height), new GUIContent("!", string.Join("\n", Problems(e))), warn);
            }

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selected = e;
                GUI.FocusControl(null);
                if (Event.current.clickCount == 2)
                    EditorGUIUtility.PingObject(e);
                Event.current.Use();
                Repaint();
            }
        }

        static List<string> Problems(EventRoomDefinition e)
        {
            var list = new List<string>();
            if (e.choices.Count == 0)
                list.Add("нет вариантов");
            if (string.IsNullOrEmpty(e.body))
                list.Add("нет текста");
            var keys = new HashSet<string>(e.roles.Where(r => r != null).Select(r => r.key));
            var texts = new List<string> { e.title, e.body };
            foreach (var c in e.choices)
                texts.AddRange(new[] { c.label, c.description, c.resultText, c.failText });
            foreach (var t in texts.Where(t => !string.IsNullOrEmpty(t)))
            {
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(t, @"\{([^}]+)\}"))
                {
                    if (!keys.Contains(m.Groups[1].Value))
                    {
                        list.Add("неизвестная роль {" + m.Groups[1].Value + "}");
                        return list;
                    }
                }
            }

            return list;
        }

        // ---------- Связи ----------

        class Link
        {
            public readonly List<string> setBy = new List<string>();
            public readonly List<string> readBy = new List<string>();
        }

        void Links()
        {
            EditorGUILayout.HelpBox("Сюжетные теги и флаги связывают события с остальным выпуском и сезоном.\n" +
                                    "Ставят: эффекты комнат, теги события и результаты выборов. Проверяют: условия комнат и вариантов.\n" +
                                    "Тег, который никто не проверяет, ничего не меняет; условие на тег, который никто не ставит, никогда не выполнится.", MessageType.None);
            var tags = new SortedDictionary<string, Link>();
            var flags = new SortedDictionary<string, Link>();
            var seasonFlags = new SortedDictionary<string, Link>();

            foreach (var room in DesignerData.LoadAll<RoomDefinition>())
            {
                string name = (room is EventRoomDefinition ? "событие " : "комната ") + "«" + room.title + "»";
                CollectEffects(room.onEnter, name, tags, flags, seasonFlags);
                CollectConditions(room.conditions, name, tags, flags, seasonFlags);
                if (room is EventRoomDefinition e)
                {
                    foreach (var t in e.eventTags)
                        Get(tags, t).setBy.Add(name + " (при входе)");
                    for (int i = 0; i < e.choices.Count; i++)
                    {
                        var c = e.choices[i];
                        string cn = name + " → «" + c.label + "»";
                        CollectEffects(c.effects, cn, tags, flags, seasonFlags);
                        CollectEffects(c.failEffects, cn + " (провал)", tags, flags, seasonFlags);
                        foreach (var t in c.resultTags)
                            Get(tags, t).setBy.Add(cn);
                        foreach (var t in c.failTags)
                            Get(tags, t).setBy.Add(cn + " (провал)");
                        CollectConditions(c.conditions, cn, tags, flags, seasonFlags);
                    }
                }
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_linksScroll))
            {
                _linksScroll = scroll.scrollPosition;
                Group("Сюжетные теги выпуска", tags);
                Group("Флаги выпуска", flags);
                Group("Флаги сезона", seasonFlags);
            }
        }

        static void CollectEffects(List<Effect> effects, string who, SortedDictionary<string, Link> tags, SortedDictionary<string, Link> flags, SortedDictionary<string, Link> season)
        {
            if (effects == null)
                return;
            foreach (var fx in effects)
            {
                if (fx == null || string.IsNullOrEmpty(fx.key))
                    continue;
                switch (fx.type)
                {
                    case EffectType.AddNarrativeTag: Get(tags, fx.key).setBy.Add(who); break;
                    case EffectType.SetEpisodeFlag: Get(flags, fx.key).setBy.Add(who); break;
                    case EffectType.ClearEpisodeFlag: Get(flags, fx.key).setBy.Add(who + " (снимает)"); break;
                    case EffectType.SetSeasonFlag: Get(season, fx.key).setBy.Add(who); break;
                    case EffectType.ClearSeasonFlag: Get(season, fx.key).setBy.Add(who + " (снимает)"); break;
                }
            }
        }

        static void CollectConditions(List<Condition> conditions, string who, SortedDictionary<string, Link> tags, SortedDictionary<string, Link> flags, SortedDictionary<string, Link> season)
        {
            if (conditions == null)
                return;
            foreach (var c in conditions)
            {
                if (c == null || string.IsNullOrEmpty(c.key))
                    continue;
                string w = who + (c.not ? " (НЕ)" : "");
                switch (c.type)
                {
                    case ConditionType.NarrativeTag: Get(tags, c.key).readBy.Add(w); break;
                    case ConditionType.EpisodeFlag: Get(flags, c.key).readBy.Add(w); break;
                    case ConditionType.SeasonFlag: Get(season, c.key).readBy.Add(w); break;
                }
            }
        }

        static Link Get(SortedDictionary<string, Link> dict, string key)
        {
            if (!dict.TryGetValue(key, out var link))
                dict[key] = link = new Link();
            return link;
        }

        static void Group(string title, SortedDictionary<string, Link> dict)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(title + " (" + dict.Count + ")", EditorStyles.boldLabel);
            if (dict.Count == 0)
            {
                EditorGUILayout.LabelField("Пока нет.", EditorStyles.miniLabel);
                return;
            }

            foreach (var kv in dict)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var head = new GUIStyle(EditorStyles.boldLabel);
                    string state = "";
                    if (kv.Value.setBy.Count == 0)
                    {
                        head.normal.textColor = new Color(1f, 0.45f, 0.4f);
                        state = "  — никто не ставит, условия не выполнятся";
                    }
                    else if (kv.Value.readBy.Count == 0)
                    {
                        head.normal.textColor = new Color(1f, 0.75f, 0.35f);
                        state = "  — никто не проверяет, ни на что не влияет";
                    }

                    EditorGUILayout.LabelField(kv.Key + state, head);
                    if (kv.Value.setBy.Count > 0)
                        EditorGUILayout.LabelField("ставят: " + string.Join("; ", kv.Value.setBy.Distinct()), EditorStyles.wordWrappedMiniLabel);
                    if (kv.Value.readBy.Count > 0)
                        EditorGUILayout.LabelField("проверяют: " + string.Join("; ", kv.Value.readBy.Distinct()), EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        // ---------- Обзор ----------

        Vector2 _overviewScroll;

        void Overview()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_overviewScroll))
            {
                _overviewScroll = scroll.scrollPosition;
                var on = _events.Where(e => e.weight > 0f).ToList();
                EditorGUILayout.LabelField("Событий: " + _events.Count + ", выпадают на карте: " + on.Count + " (по GDD нужно 8–10)", EditorStyles.boldLabel);
                if (on.Count > 0)
                {
                    float total = on.Sum(e => e.weight);
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Шанс выпасть среди событий (без учёта условий)", EditorStyles.miniBoldLabel);
                    foreach (var e in on.OrderByDescending(e => e.weight))
                    {
                        var r = GUILayoutUtility.GetRect(10, 18, GUILayout.ExpandWidth(true));
                        GUI.Label(new Rect(r.x, r.y, 200, r.height), e.title);
                        float share = e.weight / total;
                        EditorGUI.DrawRect(new Rect(r.x + 200, r.y + 3, (r.width - 260) * share, 12), e.color);
                        GUI.Label(new Rect(r.xMax - 55, r.y, 55, r.height), (share * 100f).ToString("0") + "%");
                    }
                }

                EditorGUILayout.Space(8);
                int choices = _events.Sum(e => e.choices.Count);
                EditorGUILayout.LabelField("Вариантов всего: " + choices + ", с риском: " + _events.Sum(e => e.choices.Count(c => c.chance < 100)) +
                                           ", с ценой: " + _events.Sum(e => e.choices.Count(c => c.costMoney > 0 || c.costCash > 0)));
                EditorGUILayout.LabelField("С условиями появления: " + _events.Count(e => e.conditions.Count > 0) +
                                           ", с ролями: " + _events.Count(e => e.roles.Count > 0));

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Проблемы", EditorStyles.boldLabel);
                int count = 0;
                foreach (var e in _events)
                {
                    foreach (var p in Problems(e))
                    {
                        count++;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(e.title, EditorStyles.linkLabel, GUILayout.Width(200)))
                                Select(e);
                            EditorGUILayout.LabelField(p, EditorStyles.miniLabel);
                        }
                    }
                }

                if (count == 0)
                    EditorGUILayout.HelpBox("Проблем не найдено.", MessageType.Info);
            }
        }
    }
}
