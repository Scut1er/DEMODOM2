using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Events;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Инспектор карты продюсера (EventDefinition из кора): поля по-русски, лишнее скрыто,
    // карта как в игре, «что произойдёт», прогноз реакций, проверки и кнопка «проверить в квартире».
    [CustomEditor(typeof(EventDefinition))]
    public class CardEditor : UnityEditor.Editor
    {
        static bool _showForecast = true;

        public override void OnInspectorGUI()
        {
            var card = (EventDefinition)target;
            serializedObject.Update();

            DrawHeader(card);
            EditorGUILayout.LabelField("◇ — данные для съёмки по GDD: квартира их пока не читает, но они сохраняются и попадут в игру вместе с ядром карт.",
                EditorStyles.wordWrappedMiniLabel);

            // ID, Название, Статус, Категория, Теги, Tier, Редкость, HellToken
            Section("Паспорт", false);
            Field("id", "ID", "Стабильный id. Колода и сейвы ссылаются на него — не меняйте после выхода карты в игру.");
            Field("displayName", "Название", "Крупно на карте.");
            Field("status", "Статус", "Черновик / на тесте / готова — для команды. «Выключена» — карты нет в игре.");
            CategoryPopup();
            DrawTags();
            DrawMoods();
            Field("tier", "Tier ◇", "Ступень улучшения. Улучшенные версии — отдельные карты (блок «Улучшения»).");
            Field("rarity", "Редкость ◇", "");
            Number("cost", "HellToken $", "Сколько долларов HellToken съедает розыгрыш на съёмке ($0.75). Не хватает — карту не сыграть.");

            Section("Цель", false);
            var targetType = serializedObject.FindProperty("targetType");
            targetType.enumValueIndex = EditorGUILayout.Popup(new GUIContent("Цель", "Объект — клик по предмету, участник — клик по человеку, весь дом — сразу."),
                targetType.enumValueIndex, CardInsight.TargetNames);
            var playAs = card.PlayTarget;
            if (playAs != card.targetType)
                EditorGUILayout.LabelField("◇ В квартире пока играется как «" + CardInsight.TargetNames[(int)playAs] + "».", EditorStyles.miniLabel);
            switch (playAs)
            {
                case TargetType.Object:
                    DesignerData.PickerField(EditorGUILayout.GetControlRect(), new GUIContent("Объект", "Пусто — любой объект квартиры."),
                        serializedObject.FindProperty("requiredObjectId"), DesignerData.ObjectIds, DesignerData.ObjectNames);
                    break;
                case TargetType.Actor:
                    var limit = serializedObject.FindProperty("limitTrait");
                    EditorGUILayout.PropertyField(limit, new GUIContent("Только на черту", "Карту можно сыграть только на участника с этой чертой."));
                    if (limit.boolValue)
                        Field("targetTrait", "Черта", "");
                    break;
            }

            Field("targetFilters", "Фильтры цели ◇", "Дополнительные условия: черта, эмоция, состояние, тег рядом.");

            Section("Описание для игрока", false);
            Field("hint", "Подсказка", "Коротко на карте: «клик по холодильнику», «сразу на весь дом». Показывается и при неверной цели.");
            Field("description", "Описание ◇", "Полный текст карты для игрока.");

            Section("Эффекты / правила", false);
            if (card.PlayTarget == TargetType.Actor)
                Number("rageSeconds", "Злость, сек", "Сколько секунд цель злится (0 — не злит). Злость открывает реакции «если уже злится».");
            if (card.PlayTarget == TargetType.Object)
                Field("ignite", "Поджигает", "Объект загорается.");
            Field("effects", "Эффекты ◇", "Эмоции, отношения, временные состояния, события, управление колодой.");

            Section("Dice effects", true);
            Field("diceEffects", "Броски", "Например «Злость +1d6»; Step Up растит кубик d6 → d8 рядом с алкоголем.");
            foreach (var line in CardInsight.DiceLines(card))
                EditorGUILayout.LabelField("🎲 " + line, EditorStyles.miniLabel);

            Section("Environment / Prefab", true);
            Field("environmentPrefab", "Префаб", "Объект, который карта ставит в мир.");
            Field("environmentId", "Id объекта", "Имя объекта из таблицы (AlcoholCrate). Нужен, пока префаба нет.");
            if (card.environmentPrefab != null || !string.IsNullOrEmpty(card.environmentId))
            {
                Field("environmentLifetime", "Живёт", "");
                if (card.environmentLifetime == EnvironmentLifetime.Seconds)
                    Number("environmentSeconds", "Секунд", "");
            }

            Section("Aura / влияние локации", true);
            var aura = serializedObject.FindProperty("aura");
            var auraOn = aura.FindPropertyRelative("enabled");
            EditorGUILayout.PropertyField(auraOn, new GUIContent("Есть аура", "Зона вокруг объекта карты меняет эмоции, кубики и поведение актёров."));
            if (auraOn.boolValue)
            {
                EditorGUI.indentLevel++;
                Sub(aura, "radius", "Радиус");
                Sub(aura, "tags", "Теги зоны");
                Sub(aura, "actorModifiers", "Эмоции в зоне");
                Sub(aura, "diceModifiers", "Кубики в зоне");
                Sub(aura, "behaviourWeights", "Веса поведения");
                EditorGUI.indentLevel--;
            }

            Section("Влияние на Footage", true);
            var footage = serializedObject.FindProperty("footage");
            Sub(footage, "tags", "Теги кадра");
            Sub(footage, "valueBonus", "Ценность +");
            Sub(footage, "commercialValue", "Коммерческая");
            Sub(footage, "technicalQuality", "Тех. качество +");

            Section("Sponsor", false);
            var sponsor = serializedObject.FindProperty("sponsor");
            EditorGUILayout.PropertyField(sponsor, new GUIContent("Продакт-плейсмент", "Сыгранная карта платит кр после сцены, но режет отзывы зрителей."));
            if (sponsor.boolValue)
            {
                Number("sponsorPay", "Платит, кр", "Кредиты после сцены.");
                Number("sponsorScoreHit", "Отзывы −", "На сколько ниже каждая оценка зрителей.");
                Field("sponsorId", "Бренд ◇", "Для контрактов и комментариев HellTube.");
            }

            Section("Где игрок её берёт", false);
            Field("starter", "В стартовой колоде", "Есть у игрока с начала сезона.");
            using (new EditorGUI.DisabledScope(card.sponsor))
                Number("price", "Магазин хаба, кр", card.sponsor ? "Спонсорские карты в хабе не продаются." : "Покупка в колоду навсегда. 0 — не продаётся.");
            Number("runPrice", "Магазин выпуска, нал", "Покупка на карте выпуска. Карта живёт только до эфира. 0 — не продаётся.");

            Section("Special rules / Lifecycle", true);
            Field("lifetime", "Жизненный цикл", "Что с картой после розыгрыша.");
            Field("specialRules", "Особые правила", "Удерживается, всегда в стартовой руке, не теряется при катастрофе...");

            Section("Upgrade Tier II", true);
            Upgrade(card, "upgradeTier2", "craftBudgetTier2", CardTier.II);
            Section("Upgrade Tier III", true);
            Upgrade(card, "upgradeTier3", "craftBudgetTier3", CardTier.III);

            Section("Вид", false);
            Field("cardColor", "Цвет", "Фон арта и вспышка при розыгрыше. Рамка карты — по первому тону.");
            Field("cardArt", "Арт", "Пусто — встроенная иконка (у встроенных карт) или только цвет.");

            Section("Design intent", false);
            Field("designIntent", "Зачем карта", "Для команды: какую ситуацию карта должна создавать. В игре не видно.");

            DrawSheet();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            _showForecast = EditorGUILayout.Foldout(_showForecast, "Прогноз реакций участников", true);
            if (_showForecast)
            {
                var forecast = CardInsight.Forecast(card);
                if (forecast.Count == 0)
                    EditorGUILayout.HelpBox("Ни одно правило реакций не отзовётся на эти теги. Участники не заметят карту.", MessageType.Info);
                foreach (var line in forecast)
                    EditorGUILayout.LabelField("• " + line, EditorStyles.wordWrappedLabel);
            }

            Warnings(card);
            Buttons(card);
        }

        static bool _showSheet;

        // Колонки таблицы дизайна как есть: спецификация для кора там, где структурные поля пока не всё выражают.
        void DrawSheet()
        {
            var sheet = serializedObject.FindProperty("sheet");
            if (sheet == null)
                return;
            EditorGUILayout.Space(8);
            _showSheet = EditorGUILayout.Foldout(_showSheet, new GUIContent("Из таблицы дизайна (текст колонок)",
                "Заполняется импортом из .xlsx. Структурные поля выше собраны из этого текста; повторный импорт перезапишет оба."), true);
            if (!_showSheet)
                return;
            EditorGUI.indentLevel++;
            Sub(sheet, "targetFilters", "Фильтры цели");
            Sub(sheet, "effects", "Эффекты / правила");
            Sub(sheet, "dice", "Dice effects");
            Sub(sheet, "aura", "Aura");
            Sub(sheet, "footage", "Footage");
            Sub(sheet, "lifecycle", "Lifecycle");
            Sub(sheet, "upgradeTier2", "Upgrade Tier II");
            Sub(sheet, "upgradeTier3", "Upgrade Tier III");
            EditorGUI.indentLevel--;
        }

        // Категория открывает карту уровнем Сценаристов (как считает игра).
        void CategoryPopup()
        {
            var prop = serializedObject.FindProperty("category");
            int index = CardInsight.CategoryIndex(prop.stringValue);
            var names = new List<string>(CardInsight.CategoryNames);
            if (index < 0)
            {
                names.Add("«" + prop.stringValue + "» — неизвестная, откроется на ур. 4");
                index = names.Count - 1;
            }

            int chosen = EditorGUILayout.Popup(new GUIContent("Категория", "Открывает карту в колоде и магазине по уровню Сценаристов."), index, names.ToArray());
            if (chosen < CardInsight.Categories.Length)
                prop.stringValue = CardInsight.Categories[chosen];
        }

        void Upgrade(EventDefinition card, string field, string budget, CardTier tier)
        {
            var prop = serializedObject.FindProperty(field);
            EditorGUILayout.PropertyField(prop, new GUIContent("Карта Tier " + tier, "Отдельный ассет-карта со своими значениями (кубик больше, дешевле, доп. эффект)."));
            Number(budget, "Крафт, кр", "3 × текущая карта + столько кр → эта карта.");
            if (prop.objectReferenceValue == null && GUILayout.Button("Создать карту Tier " + tier + " из этой", EditorStyles.miniButton))
            {
                serializedObject.ApplyModifiedProperties();
                var up = CardActions.Duplicate(card);
                up.tier = tier;
                up.displayName = card.displayName + " " + tier;
                up.upgradeTier2 = null;
                up.upgradeTier3 = null;
                EditorUtility.SetDirty(up);
                serializedObject.Update();
                serializedObject.FindProperty(field).objectReferenceValue = up;
                serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Selection.activeObject = card;
            }
        }

        void Sub(SerializedProperty parent, string name, string label)
        {
            var prop = parent.FindPropertyRelative(name);
            if (prop == null)
                return;
            if (prop.propertyType == SerializedPropertyType.Integer || prop.propertyType == SerializedPropertyType.Float)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - 2));
                    if (prop.propertyType == SerializedPropertyType.Float)
                        prop.floatValue = EditorGUILayout.FloatField(prop.floatValue);
                    else
                        prop.intValue = EditorGUILayout.IntField(prop.intValue);
                }

                return;
            }

            EditorGUILayout.PropertyField(prop, new GUIContent(label), true);
        }

        void DrawHeader(EventDefinition card)
        {
            var rect = GUILayoutUtility.GetRect(10, 214, GUILayout.ExpandWidth(true));
            var cardRect = new Rect(rect.x, rect.y + 4, 150, 206);
            CardInsight.DrawCard(cardRect, card);

            var text = new Rect(cardRect.xMax + 12, rect.y + 4, rect.width - cardRect.width - 12, rect.height);
            var head = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            GUI.Label(new Rect(text.x, text.y, text.width, 18), "Где взять:  " + CardInsight.Sources(card), head);
            var body = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { fontSize = 11 };
            GUI.Label(new Rect(text.x, text.y + 19, text.width, 16), CardInsight.Passport(card), EditorStyles.miniLabel);
            float y = text.y + 40;
            GUI.Label(new Rect(text.x, y, text.width, 16), "Что произойдёт:", EditorStyles.miniBoldLabel);
            y += 16;
            foreach (var line in CardInsight.Play(card))
            {
                float h = body.CalcHeight(new GUIContent("• " + line), text.width);
                GUI.Label(new Rect(text.x, y, text.width, h), "• " + line, body);
                y += h + 1;
            }
        }

        void DrawTags()
        {
            var tags = serializedObject.FindProperty("tags");
            EditorGUILayout.LabelField(new GUIContent("Теги события", "На них реагируют участники (злость, слёзы, драка...) и по ним оцениваются кадры."));
            EditorGUI.indentLevel++;
            for (int i = 0; i < tags.arraySize; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(tags.GetArrayElementAtIndex(i), GUIContent.none);
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        tags.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }

            EditorGUI.indentLevel--;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (GUILayout.Button("+ тег", EditorStyles.miniButton))
                {
                    var menu = new GenericMenu();
                    var path = tags.propertyPath;
                    var targets = serializedObject.targetObjects;
                    foreach (var tag in DesignerData.MomentTagList())
                    {
                        string t = tag;
                        menu.AddItem(new GUIContent(t + "  —  " + TagHint(t)), false, () => AddString(targets, path, t));
                    }

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Свой тег (пустая строка)"), false, () => AddString(targets, path, ""));
                    menu.ShowAsContext();
                }
            }
        }

        void DrawMoods()
        {
            var moods = serializedObject.FindProperty("moods");
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(new GUIContent("Тон", "Каждый из первых двух тонов даёт +" + SeasonTone.CardGain + " к тону сезона. Первый — цвет рамки карты."));
                for (int i = 0; i < moods.arraySize; i++)
                {
                    var m = moods.GetArrayElementAtIndex(i);
                    var old = GUI.backgroundColor;
                    GUI.backgroundColor = MoodStyle.ColorOf((ShowMood)m.enumValueIndex);
                    if (GUILayout.Button(MoodStyle.Short((ShowMood)m.enumValueIndex) + "  ✕", EditorStyles.miniButton))
                    {
                        moods.DeleteArrayElementAtIndex(i);
                        GUI.backgroundColor = old;
                        break;
                    }

                    GUI.backgroundColor = old;
                }

                if (moods.arraySize < CardInsight.MoodLimit && GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(24)))
                {
                    var menu = new GenericMenu();
                    var path = moods.propertyPath;
                    var targets = serializedObject.targetObjects;
                    foreach (ShowMood mood in System.Enum.GetValues(typeof(ShowMood)))
                    {
                        var mm = mood;
                        menu.AddItem(new GUIContent(MoodStyle.Full(mm)), false, () =>
                        {
                            var so = new SerializedObject(targets);
                            var list = so.FindProperty(path);
                            list.arraySize++;
                            list.GetArrayElementAtIndex(list.arraySize - 1).enumValueIndex = (int)mm;
                            so.ApplyModifiedProperties();
                        });
                    }

                    menu.ShowAsContext();
                }

                GUILayout.FlexibleSpace();
            }
        }

        static string TagHint(string tag)
        {
            switch (tag)
            {
                case MomentTags.Fire: return "огонь: агрессивный лезет в драку, сентиментальный в панике";
                case MomentTags.Conflict: return "конфликт: злит агрессивного";
                case MomentTags.Misery: return "быт/неудобство: ворчит сентиментальный";
                case MomentTags.Warmth: return "тепло/уют: радует сентиментального";
                case MomentTags.Crying: return "слёзы: паника сентиментального (на него)";
                case MomentTags.Chaos: return "хаос: для кадров и оценок";
                case MomentTags.Fight: return "драка: обычно рождается сама";
                case MomentTags.Slap: return "пощёчина";
                case MomentTags.Hug: return "объятия";
                default: return "";
            }
        }

        static void AddString(Object[] targets, string path, string value)
        {
            var so = new SerializedObject(targets);
            var list = so.FindProperty(path);
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = value;
            so.ApplyModifiedProperties();
        }

        void Warnings(EventDefinition card)
        {
            EditorGUILayout.Space(6);
            if (string.IsNullOrEmpty(card.id))
                EditorGUILayout.HelpBox("Пустой id — карта не загрузится.", MessageType.Error);
            if (card.status != CardStatus.Disabled && !CardInsight.Obtainable(card))
                EditorGUILayout.HelpBox("Карту негде взять: не стартовая, без цены в хабе и в выпуске.", MessageType.Warning);
            if (!card.sponsor && (card.tags == null || card.tags.Count == 0))
                EditorGUILayout.HelpBox("Без тегов участники не отреагируют на карту.", MessageType.Warning);
            if (card.moods == null || card.moods.Count == 0)
                EditorGUILayout.HelpBox("Без тона карта не двигает тон сезона, рамка будет «трэш».", MessageType.Info);
            if (card.moods != null && card.moods.Count > CardInsight.MoodLimit)
                EditorGUILayout.HelpBox("Тон считается только по первым двум меткам.", MessageType.Info);
            if (card.PlayTarget == TargetType.Object && string.IsNullOrEmpty(card.requiredObjectId))
                EditorGUILayout.HelpBox("Объект не выбран — карту можно навести на любой объект.", MessageType.Info);
            if (card.sponsor && card.sponsorPay <= 0)
                EditorGUILayout.HelpBox("Спонсор, который ничего не платит.", MessageType.Warning);
            if (card.sponsor && card.runPrice <= 0 && !card.starter)
                EditorGUILayout.HelpBox("Спонсорскую карту обычно берут в магазине выпуска — задайте цену в нале.", MessageType.Info);
            if (card.tags != null)
            {
                var known = DesignerData.MomentTagList();
                foreach (var tag in card.tags)
                {
                    if (!known.Contains(tag))
                        EditorGUILayout.HelpBox("Тег «" + tag + "» реакции участников пока не знают.", MessageType.Info);
                }
            }

            if (!DesignerData.InFolder(card, DesignerData.CardsRoot))
                EditorGUILayout.HelpBox("Карта лежит не в " + DesignerData.CardsRoot + " — в игру не попадёт.", MessageType.Warning);
            int dup = 0;
            foreach (var other in DesignerData.LoadAll<EventDefinition>())
            {
                if (other != card && other.id == card.id)
                    dup++;
            }

            if (dup > 0)
                EditorGUILayout.HelpBox("Ещё " + dup + " карт(ы) с таким же id.", MessageType.Error);
            var missing = CardSync.MissingFields(card);
            if (missing.Count > 0)
                EditorGUILayout.HelpBox("В ассете нет новых полей (" + string.Join(", ", missing) + ") — нажмите «Синхронизировать» в мастерской карт.", MessageType.Warning);
        }

        void Buttons(EventDefinition card)
        {
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶ Проверить в квартире", GUILayout.Height(26)))
                    CardActions.PlayTest(card);
                if (GUILayout.Button("Дублировать", GUILayout.Height(26)))
                    CardActions.Duplicate(card);
                if (GUILayout.Button("Мастерская карт", GUILayout.Height(26)))
                    CardWorkshop.Open(card);
            }
        }

        static void Section(string text, bool pending)
        {
            EditorGUILayout.Space(8);
            var content = new GUIContent(pending ? text + "  ◇" : text, pending ? "Данные для съёмки по GDD: квартира их пока не читает." : "");
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
        }

        // Число без «ползунка»: подпись отдельно, поэтому значение не меняется перетаскиванием мыши — только вводом.
        void Number(string name, string label, string tooltip)
        {
            var prop = serializedObject.FindProperty(name);
            if (prop == null)
                return;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(EditorGUIUtility.labelWidth - 2));
                if (prop.propertyType == SerializedPropertyType.Float)
                    prop.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(prop.floatValue));
                else
                    prop.intValue = Mathf.Max(0, EditorGUILayout.IntField(prop.intValue));
            }
        }

        void Field(string name, string label, string tooltip)
        {
            var prop = serializedObject.FindProperty(name);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip), true);
        }
    }
}
