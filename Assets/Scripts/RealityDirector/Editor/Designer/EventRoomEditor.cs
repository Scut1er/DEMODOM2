using System.Collections.Generic;
using System.Text.RegularExpressions;
using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Инспектор события: предпросмотр как в игре (с песочницей), текст с ролями, выборы и их последствия, проверки.
    [CustomEditor(typeof(EventRoomDefinition))]
    public class EventRoomEditor : UnityEditor.Editor
    {
        static readonly EventActions.Sandbox Sandbox = new EventActions.Sandbox();
        static bool _sandboxOpen;
        string _simulation;

        public override void OnInspectorGUI()
        {
            var e = (EventRoomDefinition)target;
            serializedObject.Update();

            DrawPreview(e);

            Section("На карте выпуска");
            Field("id", "ID", "Стабильный id. Пусто — из имени ассета. Не менять после выхода в игру.");
            Field("title", "Заголовок", "На карте и крупно на экране события (можно {роли}).");
            Field("subtitle", "Подзаголовок", "Строка под заголовком на карте.");
            Field("description", "Подпись на карте", "Коротко — видно при выборе узла на карте выпуска.");
            Field("icon", "Иконка", "Если нет арта.");
            Field("color", "Цвет", "Рамка узла и акцент экрана события.");
            Field("art", "Иллюстрация", "Картинка на карте и на экране события.");

            Section("Когда выпадает");
            Number("weight", "Вес", "Шанс среди событий. 0 — событие не выпадает само.", true);
            Field("conditions", "Условия", "Номер выпуска, бюджет, нал, флаги, сюжетные теги, пройденные комнаты, уровень команды, тон.");
            Field("uniquePerEpisode", "Не повторять на пути", "Не встретится дважды за выпуск на одном пути.");

            Section("При входе");
            Field("onEnter", "Эффекты", "Срабатывают сразу, как игрок вошёл в событие — до выбора.");
            Field("eventTags", "Сюжетные теги", "Выпуск получает их при входе — их видят условия следующих комнат и событий.");

            Section("Текст события");
            DrawRoles();
            Field("body", "Текст", "Основной текст на экране. {роль} заменяется именем участника.");

            Section("Выборы");
            DrawChoices();

            serializedObject.ApplyModifiedProperties();
            Warnings(e);
            Buttons(e);
        }

        // ---------- предпросмотр ----------

        void DrawPreview(EventRoomDefinition e)
        {
            var ctx = Sandbox.Context();
            var roles = EventResolver.CastRoles(e, ctx.episode, "preview");
            var choices = EventResolver.Choices(e, ctx, roles, EventActions.ActorName);

            var area = GUILayoutUtility.GetRect(10, 250 + Mathf.Max(1, choices.Count) * 34, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(area, new Color(0.12f, 0.11f, 0.15f));
            EditorGUI.DrawRect(new Rect(area.x, area.y, area.width, 4), e.color);

            var art = new Rect(area.x + 12, area.y + 16, 150, 150);
            EditorGUI.DrawRect(art, e.color);
            EditorGUI.DrawRect(new Rect(art.x + 3, art.y + 3, art.width - 6, art.height - 6), new Color(0.08f, 0.07f, 0.1f));
            if (e.art != null)
                CardInsight.DrawSprite(new Rect(art.x + 8, art.y + 8, art.width - 16, art.height - 16), e.art);

            var gold = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, wordWrap = true };
            gold.normal.textColor = new Color(0.93f, 0.76f, 0.36f);
            var light = new GUIStyle(EditorStyles.wordWrappedLabel);
            light.normal.textColor = new Color(0.96f, 0.93f, 0.88f);
            var muted = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
            muted.normal.textColor = new Color(0.62f, 0.58f, 0.66f);

            float x = art.xMax + 14;
            float w = area.xMax - x - 12;
            GUI.Label(new Rect(x, area.y + 12, w, 22), EventResolver.Fill(e.title, roles, EventActions.ActorName), gold);
            GUI.Label(new Rect(x, area.y + 38, w, 190), EventResolver.Fill(e.body, roles, EventActions.ActorName), light);

            float y = area.y + 236;
            if (choices.Count == 0)
                GUI.Label(new Rect(area.x + 12, y, area.width - 24, 30), "Нет вариантов — игрок увидит только «Уйти».", muted);
            foreach (var c in choices)
            {
                var row = new Rect(area.x + 12, y, area.width - 24, 30);
                EditorGUI.DrawRect(row, c.available ? new Color(0.2f, 0.18f, 0.24f) : new Color(0.14f, 0.13f, 0.16f));
                var label = new GUIStyle(EditorStyles.boldLabel);
                label.normal.textColor = c.available ? new Color(0.96f, 0.93f, 0.88f) : new Color(0.62f, 0.58f, 0.66f);
                GUI.Label(new Rect(row.x + 8, row.y + 1, row.width * 0.6f, 16), c.label, label);
                GUI.Label(new Rect(row.x + 8, row.y + 15, row.width * 0.75f, 14), c.available ? c.description : "Закрыто: " + c.reason, muted);
                var side = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
                side.normal.textColor = new Color(0.93f, 0.76f, 0.36f);
                GUI.Label(new Rect(row.xMax - 170, row.y, 160, row.height), (c.costLabel + "  " + c.chanceLabel).Trim(), side);
                y += 34;
            }

            _sandboxOpen = EditorGUILayout.Foldout(_sandboxOpen, "Песочница: как будто выпуск " + Sandbox.episode + ", " + Sandbox.money + " кр, " + Sandbox.cash + " нал", true);
            if (_sandboxOpen)
            {
                EditorGUI.indentLevel++;
                Sandbox.episode = Mathf.Max(1, EditorGUILayout.IntField("Выпуск №", Sandbox.episode));
                Sandbox.money = Mathf.Max(0, EditorGUILayout.IntField("Бюджет, кр", Sandbox.money));
                Sandbox.cash = Mathf.Max(0, EditorGUILayout.IntField("Нал", Sandbox.cash));
                Sandbox.castLevel = Mathf.Clamp(EditorGUILayout.IntField("Уровень Кастинга", Sandbox.castLevel), 1, 5);
                string tags = EditorGUILayout.TextField(new GUIContent("Сюжетные теги", "Через запятую — как будто выпуск их уже получил."), string.Join(", ", Sandbox.tags));
                Sandbox.tags = new List<string>();
                foreach (var t in tags.Split(','))
                {
                    if (t.Trim().Length > 0)
                        Sandbox.tags.Add(t.Trim());
                }

                EditorGUILayout.LabelField("Разыграть вариант (в песочнице, игра не меняется):", EditorStyles.miniBoldLabel);
                for (int i = 0; i < e.choices.Count; i++)
                {
                    var c = e.choices[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField((i + 1) + ". " + c.label, GUILayout.MinWidth(120));
                        if (GUILayout.Button(c.chance < 100 ? "Успех" : "Выбрать", EditorStyles.miniButton, GUILayout.Width(70)))
                            _simulation = Describe(EventActions.Simulate(e, i, true, Sandbox), c.chance < 100);
                        using (new EditorGUI.DisabledScope(c.chance >= 100))
                        {
                            if (GUILayout.Button("Провал", EditorStyles.miniButton, GUILayout.Width(70)))
                                _simulation = Describe(EventActions.Simulate(e, i, false, Sandbox), true);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(_simulation))
                    EditorGUILayout.HelpBox(_simulation, MessageType.None);
                EditorGUI.indentLevel--;
            }
        }

        static string Describe(EventOutcome o, bool chancy)
        {
            string head = chancy ? (o.success ? "ПОЛУЧИЛОСЬ\n" : "ПРОВАЛ\n") : "";
            return head + o.text + (string.IsNullOrEmpty(o.summary) ? "" : "\n\n" + Regex.Replace(o.summary, "<.*?>", ""));
        }

        // ---------- роли ----------

        void DrawRoles()
        {
            var roles = serializedObject.FindProperty("roles");
            EditorGUILayout.LabelField(new GUIContent("Роли", "{ключ} в тексте → имя участника. Пустой участник — случайный из каста, без повторов."));
            for (int i = 0; i < roles.arraySize; i++)
            {
                var role = roles.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.HorizontalScope())
                {
                    var key = role.FindPropertyRelative("key");
                    EditorGUILayout.LabelField("{", GUILayout.Width(10));
                    key.stringValue = EditorGUILayout.TextField(key.stringValue, GUILayout.Width(110));
                    EditorGUILayout.LabelField("}  =", GUILayout.Width(28));
                    var actor = role.FindPropertyRelative("actorId");
                    var rect = EditorGUILayout.GetControlRect(GUILayout.MinWidth(140));
                    DesignerData.PickerField(rect, GUIContent.none, actor, DesignerData.ActorIds());
                    if (string.IsNullOrEmpty(actor.stringValue))
                        EditorGUILayout.LabelField("случайный", EditorStyles.miniLabel, GUILayout.Width(60));
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        roles.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }

            if (GUILayout.Button("+ роль", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                roles.arraySize++;
                var r = roles.GetArrayElementAtIndex(roles.arraySize - 1);
                r.FindPropertyRelative("key").stringValue = roles.arraySize == 1 ? "актёр" : "актёр" + roles.arraySize;
                r.FindPropertyRelative("actorId").stringValue = "";
            }
        }

        // ---------- выборы ----------

        void DrawChoices()
        {
            var choices = serializedObject.FindProperty("choices");
            for (int i = 0; i < choices.arraySize; i++)
            {
                var c = choices.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        c.isExpanded = EditorGUILayout.Foldout(c.isExpanded, "Вариант " + (i + 1) + ":  " + c.FindPropertyRelative("label").stringValue, true, EditorStyles.foldoutHeader);
                        using (new EditorGUI.DisabledScope(i == 0))
                        {
                            if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(24)))
                            {
                                choices.MoveArrayElement(i, i - 1);
                                break;
                            }
                        }

                        using (new EditorGUI.DisabledScope(i == choices.arraySize - 1))
                        {
                            if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(24)))
                            {
                                choices.MoveArrayElement(i, i + 1);
                                break;
                            }
                        }

                        if (GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(24)))
                        {
                            choices.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }

                    if (!c.isExpanded)
                        continue;

                    Sub(c, "label", "Кнопка", "Текст кнопки (можно {роли}).");
                    Sub(c, "description", "Пояснение", "Мелко под кнопкой.");
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Доступность", EditorStyles.miniBoldLabel);
                    Sub(c, "conditions", "Условия", "Невыполненное условие закрывает вариант с пояснением.");
                    Sub(c, "hideIfUnavailable", "Скрыть, если закрыт", "Иначе вариант виден, но закрыт — игрок знает, чего не хватило.");
                    SubNumber(c, "costMoney", "Цена, кр", 0, 100000);
                    SubNumber(c, "costCash", "Цена, нал", 0, 100000);
                    SubNumber(c, "chance", "Шанс успеха, %", 1, 100);

                    EditorGUILayout.Space(2);
                    bool chancy = c.FindPropertyRelative("chance").intValue < 100;
                    EditorGUILayout.LabelField(chancy ? "Если успех" : "Последствия", EditorStyles.miniBoldLabel);
                    Sub(c, "effects", "Эффекты", "Бюджет, нал, тон, флаги, сюжетные теги, карты (на выпуск или в колоду навсегда).");
                    Sub(c, "resultText", "Текст результата", "Что видит игрок после выбора (можно {роли}).");
                    Sub(c, "resultTags", "Сюжетные теги", "Их проверяют условия следующих комнат и событий.");
                    if (chancy)
                    {
                        EditorGUILayout.Space(2);
                        EditorGUILayout.LabelField("Если провал", EditorStyles.miniBoldLabel);
                        Sub(c, "failEffects", "Эффекты", "");
                        Sub(c, "failText", "Текст провала", "Пусто — тот же текст, что при успехе.");
                        Sub(c, "failTags", "Сюжетные теги", "");
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ вариант", GUILayout.Width(110)))
                {
                    choices.arraySize++;
                    var c = choices.GetArrayElementAtIndex(choices.arraySize - 1);
                    c.FindPropertyRelative("label").stringValue = "Вариант " + choices.arraySize;
                    c.FindPropertyRelative("chance").intValue = 100;
                    c.isExpanded = true;
                }

                if (choices.arraySize > 3)
                    EditorGUILayout.LabelField("По GDD — 2–3 варианта.", EditorStyles.miniLabel);
            }
        }

        // ---------- проверки ----------

        void Warnings(EventRoomDefinition e)
        {
            EditorGUILayout.Space(6);
            if (e.choices.Count == 0)
                EditorGUILayout.HelpBox("Нет вариантов — событие засчитается без выбора.", MessageType.Warning);
            else if (e.choices.Count == 1)
                EditorGUILayout.HelpBox("Один вариант — это не выбор. По GDD — 2–3 варианта.", MessageType.Info);
            if (string.IsNullOrEmpty(e.body))
                EditorGUILayout.HelpBox("Нет текста события.", MessageType.Warning);
            if (e.weight <= 0f)
                EditorGUILayout.HelpBox("Вес 0 — событие не выпадает на карте.", MessageType.Info);

            var known = new HashSet<string>();
            foreach (var r in e.roles)
            {
                if (r != null && !string.IsNullOrEmpty(r.key))
                    known.Add(r.key);
            }

            var texts = new List<string> { e.title, e.body };
            foreach (var c in e.choices)
            {
                texts.Add(c.label);
                texts.Add(c.description);
                texts.Add(c.resultText);
                texts.Add(c.failText);
            }

            var unknown = new HashSet<string>();
            foreach (var t in texts)
            {
                if (string.IsNullOrEmpty(t))
                    continue;
                foreach (Match m in Regex.Matches(t, @"\{([^}]+)\}"))
                {
                    if (!known.Contains(m.Groups[1].Value))
                        unknown.Add(m.Groups[1].Value);
                }
            }

            if (unknown.Count > 0)
                EditorGUILayout.HelpBox("В тексте есть {" + string.Join("}, {", unknown) + "}, но таких ролей нет — игрок увидит скобки.", MessageType.Error);
            if (e.roles.Count > 2)
                EditorGUILayout.HelpBox("Ролей больше двух, а в касте сейчас двое — лишние роли получат повтор.", MessageType.Info);

            for (int i = 0; i < e.choices.Count; i++)
            {
                var c = e.choices[i];
                string n = "Вариант " + (i + 1) + ": ";
                if (string.IsNullOrEmpty(c.label))
                    EditorGUILayout.HelpBox(n + "пустая кнопка.", MessageType.Warning);
                if (string.IsNullOrEmpty(c.resultText))
                    EditorGUILayout.HelpBox(n + "нет текста результата.", MessageType.Info);
                if (c.effects.Count == 0 && c.resultTags.Count == 0 && c.costMoney == 0 && c.costCash == 0)
                    EditorGUILayout.HelpBox(n + "ни последствий, ни цены — выбор ни на что не влияет.", MessageType.Info);
                if (c.chance < 100 && c.failEffects.Count == 0 && string.IsNullOrEmpty(c.failText))
                    EditorGUILayout.HelpBox(n + "шанс " + c.chance + "%, но провал ничего не делает и не описан.", MessageType.Info);
            }

            if (!DesignerData.InFolder(e, DesignerData.RoomsRoot))
                EditorGUILayout.HelpBox("Событие лежит не в " + DesignerData.RoomsRoot + " — само на карту не попадёт.", MessageType.Warning);
        }

        void Buttons(EventRoomDefinition e)
        {
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶ Проверить в игре", GUILayout.Height(26)))
                    EventActions.PlayTest(e);
                if (GUILayout.Button("Дублировать", GUILayout.Height(26)))
                    EventActions.Duplicate(e);
                if (GUILayout.Button("Мастерская событий", GUILayout.Height(26)))
                    EventWorkshop.Open(e);
            }
        }

        // ---------- помощники ----------

        static void Section(string text)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }

        void Field(string name, string label, string tooltip)
        {
            var prop = serializedObject.FindProperty(name);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip), true);
        }

        // Число без «ползунка» на подписи — только ввод.
        void Number(string name, string label, string tooltip, bool isFloat)
        {
            var prop = serializedObject.FindProperty(name);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(EditorGUIUtility.labelWidth - 2));
                if (isFloat)
                    prop.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(prop.floatValue));
                else
                    prop.intValue = Mathf.Max(0, EditorGUILayout.IntField(prop.intValue));
            }
        }

        static void Sub(SerializedProperty parent, string name, string label, string tooltip)
        {
            var prop = parent.FindPropertyRelative(name);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip), true);
        }

        static void SubNumber(SerializedProperty parent, string name, string label, int min, int max)
        {
            var prop = parent.FindPropertyRelative(name);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - 2));
                prop.intValue = Mathf.Clamp(EditorGUILayout.IntField(prop.intValue), min, max);
            }
        }
    }
}
