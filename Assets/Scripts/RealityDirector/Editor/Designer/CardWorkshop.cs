using System.Collections.Generic;
using System.Linq;
using RealityDirector.Core;
using RealityDirector.Events;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Мастерская карт продюсера: все карты в одном окне.
    // «Карточка» — список + полный редактор; «Таблица» — правка цифр всех карт сразу; «Обзор» — баланс колоды.
    public class CardWorkshop : EditorWindow
    {
        static readonly string[] Modes = { "Карточка", "Таблица", "Обзор" };
        static readonly string[] Filters = { "Все", "Стартовые", "Магазин хаба (кр)", "Магазин выпуска (нал)", "Спонсоры", "Негде взять", "Драма", "Трэш", "Семья", "Черновики", "На тесте", "Готовые", "Выключенные" };

        int _mode;
        int _filter;
        string _search = "";
        EventDefinition _selected;
        UnityEditor.Editor _editor;
        Vector2 _listScroll;
        Vector2 _editScroll;
        Vector2 _tableScroll;
        List<EventDefinition> _cards = new List<EventDefinition>();
        CardSync.Status _sync;

        [MenuItem("RealityDirector/Card Workshop", priority = 1)]
        public static void Open()
        {
            Open(null);
        }

        public static void Open(EventDefinition card)
        {
            var window = GetWindow<CardWorkshop>("Мастерская карт");
            window.minSize = new Vector2(760, 480);
            if (card != null)
            {
                window._selected = card;
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
            _cards = DesignerData.LoadAll<EventDefinition>()
                .Where(c => DesignerData.InFolder(c, DesignerData.CardsRoot))
                .OrderBy(c => SourceOrder(c)).ThenBy(c => c.displayName).ToList();
            _sync = CardSync.Check();
            if (_selected == null && _cards.Count > 0)
                _selected = _cards[0];
            Repaint();
        }

        static int SourceOrder(EventDefinition c)
        {
            if (c.starter) return 0;
            if (c.sponsor) return 3;
            if (c.price > 0) return 1;
            if (c.runPrice > 0) return 2;
            return 4;
        }

        void OnGUI()
        {
            Toolbar();
            SyncBanner();
            switch (_mode)
            {
                case 0: CardMode(); break;
                case 1: TableMode(); break;
                default: Overview(); break;
            }
        }

        void Toolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _mode = GUILayout.Toolbar(_mode, Modes, EditorStyles.toolbarButton, GUILayout.Width(260));
                GUILayout.Space(8);
                _filter = EditorGUILayout.Popup(_filter, Filters, EditorStyles.toolbarPopup, GUILayout.Width(170));
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Импорт из таблицы", "Карты из .xlsx (лист «Карты»). Повторный импорт обновит их по таблице."), EditorStyles.toolbarButton))
                {
                    CardSheetImport.ImportWithDialog();
                    Reload();
                }

                if (GUILayout.Button("+ Новая карта", EditorStyles.toolbarButton))
                    Select(CardActions.Create());
                using (new EditorGUI.DisabledScope(_selected == null))
                {
                    if (GUILayout.Button("Дублировать", EditorStyles.toolbarButton))
                        Select(CardActions.Duplicate(_selected));
                    if (GUILayout.Button("Удалить", EditorStyles.toolbarButton) && CardActions.Delete(_selected))
                    {
                        _selected = null;
                        Reload();
                    }

                    if (GUILayout.Button("▶ Проверить", EditorStyles.toolbarButton))
                        CardActions.PlayTest(_selected);
                }
            }
        }

        void SyncBanner()
        {
            if (_sync == null || _sync.Problems == 0)
                return;
            using (new EditorGUILayout.HorizontalScope())
            {
                string text = "";
                if (_sync.missingAssets.Count > 0)
                    text += "В коде есть карты без ассетов: " + string.Join(", ", _sync.missingAssets) + ". ";
                if (_sync.missingFields.Count > 0)
                    text += "У " + _sync.missingFields.Count + " карт нет новых полей (их добавили в код после выгрузки). ";
                EditorGUILayout.HelpBox(text + "Синхронизация добавит недостающее и не тронет ваши значения.", MessageType.Warning);
                if (GUILayout.Button("Синхронизировать", GUILayout.Width(140), GUILayout.Height(38)))
                {
                    Debug.Log("Cards: " + CardSync.Run(true));
                    Reload();
                }
            }
        }

        List<EventDefinition> Visible()
        {
            IEnumerable<EventDefinition> list = _cards;
            switch (_filter)
            {
                case 1: list = list.Where(c => c.starter); break;
                case 2: list = list.Where(c => c.price > 0 && !c.sponsor); break;
                case 3: list = list.Where(c => c.runPrice > 0); break;
                case 4: list = list.Where(c => c.sponsor); break;
                case 5: list = list.Where(c => !CardInsight.Obtainable(c)); break;
                case 6: list = list.Where(c => HasMood(c, ShowMood.Drama)); break;
                case 7: list = list.Where(c => HasMood(c, ShowMood.Trash)); break;
                case 8: list = list.Where(c => HasMood(c, ShowMood.Family)); break;
                case 9: list = list.Where(c => c.status == CardStatus.Draft); break;
                case 10: list = list.Where(c => c.status == CardStatus.Testing); break;
                case 11: list = list.Where(c => c.status == CardStatus.Ready); break;
                case 12: list = list.Where(c => c.status == CardStatus.Disabled); break;
            }

            if (!string.IsNullOrEmpty(_search))
            {
                string q = _search.ToLowerInvariant();
                list = list.Where(c => (c.displayName ?? "").ToLowerInvariant().Contains(q) || (c.id ?? "").Contains(q)
                                       || (c.tags != null && c.tags.Any(t => t.ToLowerInvariant().Contains(q))));
            }

            return list.ToList();
        }

        static bool HasMood(EventDefinition c, ShowMood mood)
        {
            return c.moods != null && c.moods.Take(CardInsight.MoodLimit).Contains(mood);
        }

        void Select(EventDefinition card)
        {
            _selected = card;
            _mode = 0;
            Reload();
            GUI.FocusControl(null);
        }

        // ---------- Карточка ----------

        void CardMode()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(_listScroll, GUILayout.Width(250)))
                {
                    _listScroll = scroll.scrollPosition;
                    var visible = Visible();
                    if (visible.Count == 0)
                        EditorGUILayout.LabelField("Нет карт под фильтр.", EditorStyles.centeredGreyMiniLabel);
                    foreach (var card in visible)
                        ListRow(card);
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(_editScroll))
                {
                    _editScroll = scroll.scrollPosition;
                    if (_selected == null)
                    {
                        EditorGUILayout.HelpBox("Выберите карту слева или создайте новую.", MessageType.None);
                        return;
                    }

                    UnityEditor.Editor.CreateCachedEditor(_selected, typeof(CardEditor), ref _editor);
                    _editor.OnInspectorGUI();
                }
            }
        }

        void ListRow(EventDefinition card)
        {
            var rect = GUILayoutUtility.GetRect(10, 38, GUILayout.ExpandWidth(true));
            bool selected = card == _selected;
            if (selected)
                EditorGUI.DrawRect(rect, new Color(0.24f, 0.37f, 0.6f, 0.55f));
            EditorGUI.DrawRect(new Rect(rect.x + 4, rect.y + 5, 6, rect.height - 10), card.cardColor);
            for (int i = 0; card.moods != null && i < card.moods.Count && i < CardInsight.MoodLimit; i++)
                EditorGUI.DrawRect(new Rect(rect.x + 12, rect.y + 6 + i * 13, 4, 11), MoodStyle.ColorOf(card.moods[i]));
            GUI.Label(new Rect(rect.x + 22, rect.y + 3, rect.width - 90, 18), card.displayName, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + 22, rect.y + 19, rect.width - 90, 16), card.id + "  ·  " + CardInsight.TargetNames[(int)card.targetType], EditorStyles.miniLabel);
            var badge = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            if (!CardInsight.Obtainable(card))
                badge.normal.textColor = new Color(1f, 0.45f, 0.4f);
            GUI.Label(new Rect(rect.xMax - 70, rect.y + 3, 64, 16), CardInsight.Badge(card), badge);
            string price = card.sponsor ? "+" + card.sponsorPay + " кр" : card.runPrice > 0 ? card.runPrice + " нал" : card.price > 0 ? card.price + " кр" : "";
            GUI.Label(new Rect(rect.xMax - 70, rect.y + 19, 64, 16), price, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight });
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selected = card;
                GUI.FocusControl(null);
                if (Event.current.clickCount == 2)
                    EditorGUIUtility.PingObject(card);
                Event.current.Use();
                Repaint();
            }
        }

        // ---------- Таблица ----------

        static readonly (string title, float width, string tip)[] Columns =
        {
            ("Название", 150, "Двойной клик по строке — открыть карточку."),
            ("Цель", 120, ""),
            ("Тон 1", 70, "Первый тон: +6 к тону сезона и цвет рамки."),
            ("Тон 2", 70, "Второй тон: ещё +6."),
            ("Старт", 42, "В стартовой колоде."),
            ("Хаб, кр", 60, "Цена в магазине хаба. 0 — не продаётся."),
            ("Выпуск, нал", 76, "Цена в магазине выпуска. 0 — не продаётся."),
            ("Спонсор", 54, ""),
            ("Платит", 54, "Спонсор: кр после сцены."),
            ("Отзывы −", 60, "Спонсор: минус к каждому отзыву."),
            ("Злость", 50, "Секунд злости цели (только «участник»)."),
            ("Огонь", 42, "Поджигает (только «объект»)."),
            ("Статус", 90, "«Выключена» — карты нет в игре."),
            ("Категория", 170, "Открывает карту уровнем Сценаристов."),
            ("Tier", 44, "◇"),
            ("Редкость", 96, "◇"),
            ("HellToken", 70, "Сколько HellToken съедает розыгрыш на съёмке."),
        };

        void TableMode()
        {
            var visible = Visible();
            EditorGUILayout.HelpBox("Правка цифр сразу у всех карт. Изменения сохраняются в ассеты; Ctrl+Z работает.", MessageType.None);
            using (var scroll = new EditorGUILayout.ScrollViewScope(_tableScroll))
            {
                _tableScroll = scroll.scrollPosition;
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Space(10);
                    foreach (var col in Columns)
                        GUILayout.Label(new GUIContent(col.title, col.tip), EditorStyles.miniBoldLabel, GUILayout.Width(col.width));
                }

                foreach (var card in visible)
                    TableRow(card);
            }
        }

        void TableRow(EventDefinition card)
        {
            var so = new SerializedObject(card);
            so.Update();
            using (new EditorGUILayout.HorizontalScope())
            {
                var swatch = GUILayoutUtility.GetRect(6, 18, GUILayout.Width(6));
                EditorGUI.DrawRect(swatch, card.cardColor);
                GUILayout.Space(4);
                var name = so.FindProperty("displayName");
                name.stringValue = EditorGUILayout.TextField(name.stringValue, GUILayout.Width(Columns[0].width));
                var last = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2 && last.Contains(Event.current.mousePosition))
                {
                    Select(card);
                    Event.current.Use();
                }

                var target = so.FindProperty("targetType");
                target.enumValueIndex = EditorGUILayout.Popup(target.enumValueIndex, CardInsight.TargetNames, GUILayout.Width(Columns[1].width));
                MoodCell(so.FindProperty("moods"), 0, Columns[2].width);
                MoodCell(so.FindProperty("moods"), 1, Columns[3].width);
                Toggle(so, "starter", Columns[4].width);
                using (new EditorGUI.DisabledScope(card.sponsor))
                    Int(so, "price", Columns[5].width);
                Int(so, "runPrice", Columns[6].width);
                Toggle(so, "sponsor", Columns[7].width);
                using (new EditorGUI.DisabledScope(!card.sponsor))
                {
                    Int(so, "sponsorPay", Columns[8].width);
                    Int(so, "sponsorScoreHit", Columns[9].width);
                }

                using (new EditorGUI.DisabledScope(card.PlayTarget != TargetType.Actor))
                {
                    var rage = so.FindProperty("rageSeconds");
                    rage.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(rage.floatValue, GUILayout.Width(Columns[10].width)));
                }

                using (new EditorGUI.DisabledScope(card.PlayTarget != TargetType.Object))
                    Toggle(so, "ignite", Columns[11].width);
                EditorGUILayout.PropertyField(so.FindProperty("status"), GUIContent.none, GUILayout.Width(Columns[12].width));
                var cat = so.FindProperty("category");
                int ci = CardInsight.CategoryIndex(cat.stringValue);
                int cn = EditorGUILayout.Popup(Mathf.Max(0, ci), CardInsight.CategoryNames, GUILayout.Width(Columns[13].width));
                if (ci < 0 ? cn != 0 : cn != ci)
                    cat.stringValue = CardInsight.Categories[cn];
                EditorGUILayout.PropertyField(so.FindProperty("tier"), GUIContent.none, GUILayout.Width(Columns[14].width));
                EditorGUILayout.PropertyField(so.FindProperty("rarity"), GUIContent.none, GUILayout.Width(Columns[15].width));
                var cost = so.FindProperty("cost");
                cost.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(cost.floatValue, GUILayout.Width(Columns[16].width)));
                if (!CardInsight.Obtainable(card))
                    GUILayout.Label(new GUIContent("⚠", "Карту негде взять."), GUILayout.Width(16));
            }

            so.ApplyModifiedProperties();
        }

        static readonly string[] MoodOptions = { "—", "Драма", "Трэш", "Семья" };

        static void MoodCell(SerializedProperty moods, int index, float width)
        {
            int current = index < moods.arraySize ? moods.GetArrayElementAtIndex(index).enumValueIndex + 1 : 0;
            int chosen = EditorGUILayout.Popup(current, MoodOptions, GUILayout.Width(width));
            if (chosen == current)
                return;
            if (chosen == 0)
            {
                if (index < moods.arraySize)
                    moods.DeleteArrayElementAtIndex(index);
                return;
            }

            // ShowMood: Drama, Trash, Family — порядок как в MoodOptions без «—».
            var value = (ShowMood)System.Enum.Parse(typeof(ShowMood), new[] { "Drama", "Trash", "Family" }[chosen - 1]);
            while (moods.arraySize <= index)
            {
                moods.arraySize++;
                moods.GetArrayElementAtIndex(moods.arraySize - 1).enumValueIndex = (int)value;
            }

            moods.GetArrayElementAtIndex(index).enumValueIndex = (int)value;
        }

        static void Int(SerializedObject so, string name, float width)
        {
            var p = so.FindProperty(name);
            p.intValue = Mathf.Max(0, EditorGUILayout.IntField(p.intValue, GUILayout.Width(width)));
        }

        static void Toggle(SerializedObject so, string name, float width)
        {
            var p = so.FindProperty(name);
            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(width)))
            {
                GUILayout.FlexibleSpace();
                p.boolValue = EditorGUILayout.Toggle(p.boolValue, GUILayout.Width(16));
                GUILayout.FlexibleSpace();
            }
        }

        // ---------- Обзор ----------

        Vector2 _overviewScroll;

        void Overview()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_overviewScroll))
            {
                _overviewScroll = scroll.scrollPosition;
                var all = _cards;
                Header("Колода целиком: " + all.Count + " карт");

                Row("Стартовые", all.Count(c => c.starter), "у игрока с первого выпуска");
                Row("Магазин хаба (кр)", all.Count(c => c.price > 0 && !c.sponsor), Avg(all.Where(c => c.price > 0 && !c.sponsor).Select(c => c.price), "кр"));
                Row("Магазин выпуска (нал)", all.Count(c => c.runPrice > 0), Avg(all.Where(c => c.runPrice > 0).Select(c => c.runPrice), "нал"));
                Row("Спонсоры", all.Count(c => c.sponsor), Avg(all.Where(c => c.sponsor).Select(c => c.sponsorPay), "кр платят"));
                Row("Негде взять", all.Count(c => !CardInsight.Obtainable(c)), "не попадут к игроку");

                Header("Тон (по первым двум меткам)");
                foreach (ShowMood mood in System.Enum.GetValues(typeof(ShowMood)))
                {
                    int count = all.Count(c => HasMood(c, mood));
                    var r = GUILayoutUtility.GetRect(10, 20, GUILayout.ExpandWidth(true));
                    GUI.Label(new Rect(r.x, r.y, 160, r.height), MoodStyle.Full(mood));
                    float max = Mathf.Max(1, all.Count);
                    EditorGUI.DrawRect(new Rect(r.x + 160, r.y + 4, (r.width - 220) * count / max, 12), MoodStyle.ColorOf(mood));
                    GUI.Label(new Rect(r.xMax - 50, r.y, 50, r.height), count.ToString());
                }

                Header("Статус");
                foreach (CardStatus st in System.Enum.GetValues(typeof(CardStatus)))
                    Row(CardInsight.Enum(st), all.Count(c => c.status == st), "");

                Header("Категории (открываются Сценаристами) и цена в HellToken");
                foreach (var cat in CardInsight.Categories)
                {
                    var inCat = all.Where(c => c.category == cat && c.status != CardStatus.Disabled).ToList();
                    string costs = inCat.Count > 0
                        ? HellToken.Format(inCat.Average(c => c.cost)) + " HellToken в среднем (от " + HellToken.Format(inCat.Min(c => c.cost)) + " до " + HellToken.Format(inCat.Max(c => c.cost)) + ")"
                        : "нет карт";
                    Row(CardInsight.CategoryName(cat), inCat.Count, costs);
                }

                Header("Редкость ◇");
                foreach (CardRarity r in System.Enum.GetValues(typeof(CardRarity)))
                    Row(CardInsight.Enum(r), all.Count(c => c.rarity == r && c.status != CardStatus.Disabled), "");

                Header("Цели");
                for (int t = 0; t < CardInsight.TargetNames.Length; t++)
                    Row(CardInsight.TargetNames[t], all.Count(c => (int)c.targetType == t), "");

                Header("Теги и кто на них реагирует");
                foreach (var tag in DesignerData.MomentTagList())
                {
                    int count = all.Count(c => c.tags != null && c.tags.Contains(tag));
                    Row(tag, count, count == 0 ? "ни одна карта не использует" : "");
                }

                Header("Проблемы");
                int problems = 0;
                foreach (var c in all)
                {
                    if (c.status != CardStatus.Disabled && !CardInsight.Obtainable(c)) { Problem(c, "негде взять"); problems++; }
                    if (!c.sponsor && (c.tags == null || c.tags.Count == 0)) { Problem(c, "без тегов — участники не реагируют"); problems++; }
                    if (c.sponsor && c.sponsorPay <= 0) { Problem(c, "спонсор без оплаты"); problems++; }
                    if (CardSync.MissingFields(c).Count > 0) { Problem(c, "нет новых полей — синхронизировать"); problems++; }
                }

                var ids = all.GroupBy(c => c.id).Where(g => g.Count() > 1);
                foreach (var g in ids)
                {
                    EditorGUILayout.HelpBox("Повтор id «" + g.Key + "»: " + string.Join(", ", g.Select(c => c.name)), MessageType.Error);
                    problems++;
                }

                if (problems == 0)
                    EditorGUILayout.HelpBox("Проблем не найдено.", MessageType.Info);
            }
        }

        void Problem(EventDefinition c, string text)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(c.displayName, EditorStyles.linkLabel, GUILayout.Width(160)))
                    Select(c);
                EditorGUILayout.LabelField(text, EditorStyles.miniLabel);
            }
        }

        static void Header(string text)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }

        static void Row(string label, int count, string extra)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(180));
                GUILayout.Label(count.ToString(), EditorStyles.boldLabel, GUILayout.Width(40));
                GUILayout.Label(extra, EditorStyles.miniLabel);
            }
        }

        static string Avg(IEnumerable<int> values, string unit)
        {
            var list = values.ToList();
            if (list.Count == 0)
                return "";
            return "в среднем " + Mathf.RoundToInt((float)list.Average()) + " " + unit + " (от " + list.Min() + " до " + list.Max() + ")";
        }
    }
}
