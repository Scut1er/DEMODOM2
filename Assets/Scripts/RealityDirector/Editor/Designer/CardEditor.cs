using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Events;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Инспектор карты продюсера (EventDefinition из кора): поля по-русски, лишнее скрыто, предпросмотр и проверки.
    [CustomEditor(typeof(EventDefinition))]
    public class CardEditor : UnityEditor.Editor
    {
        static readonly string[] TargetNames = { "Объект в квартире", "Участник", "Весь дом сразу" };

        public override void OnInspectorGUI()
        {
            var card = (EventDefinition)target;
            serializedObject.Update();

            DrawPreview(card);
            EditorGUILayout.Space(6);

            Header("Карта");
            Field("id", "id", "Стабильный id. Сейвы и колода ссылаются на него — не меняйте после выхода карты в игру.");
            Field("displayName", "Название", "Крупно на карте.");
            Field("hint", "Подсказка", "Как играть: «клик по холодильнику», «сразу на весь дом».");

            Header("Куда играется");
            var targetType = serializedObject.FindProperty("targetType");
            targetType.enumValueIndex = EditorGUILayout.Popup(new GUIContent("Цель"), targetType.enumValueIndex, TargetNames);
            switch ((TargetType)targetType.enumValueIndex)
            {
                case TargetType.Object:
                    var obj = serializedObject.FindProperty("requiredObjectId");
                    DesignerData.PickerField(EditorGUILayout.GetControlRect(), new GUIContent("Объект", "id объекта квартиры."), obj,
                        DesignerData.ObjectIds, DesignerData.ObjectNames);
                    break;
                case TargetType.Actor:
                    var limit = serializedObject.FindProperty("limitTrait");
                    EditorGUILayout.PropertyField(limit, new GUIContent("Только на участника с чертой"));
                    if (limit.boolValue)
                        Field("targetTrait", "Черта", "На кого можно сыграть карту.");
                    break;
            }

            Header("Что происходит в кадре");
            DrawTags();
            Field("moods", "Тон карты", "Драма / трэш / семья — цвет рамки и тон сезона.");
            Field("rageSeconds", "Злость, сек", "Сколько секунд цель злится (0 — не злит).");
            Field("ignite", "Поджигает", "Цель загорается (как «Поджог» холодильника).");

            Header("Экономика");
            Field("cost", "Hell Token", "Сколько маны съедает розыгрыш на съёмке.");
            Field("price", "Цена в магазине", "Кредиты. 0 — карта не продаётся.");
            Field("starter", "В стартовой колоде", "Есть у игрока с начала сезона.");

            Header("Вид");
            Field("cardColor", "Цвет", "");
            Field("cardArt", "Арт", "Пусто — встроенная иконка (для встроенных карт) или только рамка.");

            Header("Заметки");
            Field("jamNote", "Для команды", "В игре не видно.");

            serializedObject.ApplyModifiedProperties();
            Warnings(card);
        }

        void DrawPreview(EventDefinition card)
        {
            var rect = GUILayoutUtility.GetRect(10, 86, GUILayout.ExpandWidth(true));
            var cardRect = new Rect(rect.x, rect.y, 64f, 86f);
            EditorGUI.DrawRect(cardRect, card.cardColor);
            if (card.cardArt != null)
                GUI.DrawTexture(new Rect(cardRect.x + 6, cardRect.y + 18, 52, 52), card.cardArt.texture, ScaleMode.ScaleToFit);

            var text = new Rect(rect.x + 74f, rect.y, rect.width - 74f, rect.height);
            var big = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
            GUI.Label(new Rect(text.x, text.y, text.width, 22), string.IsNullOrEmpty(card.displayName) ? "(без названия)" : card.displayName, big);
            GUI.Label(new Rect(text.x, text.y + 22, text.width, 18), card.hint, EditorStyles.label);
            string moods = card.moods != null && card.moods.Count > 0 ? string.Join(", ", card.moods) : "без тона";
            string economy = card.starter ? "стартовая" : card.price > 0 ? "в магазине за " + card.price + " кр" : "нигде не выдаётся";
            GUI.Label(new Rect(text.x, text.y + 42, text.width, 18), moods + "   ·   " + card.cost + " hell   ·   " + economy, EditorStyles.miniLabel);
            GUI.Label(new Rect(text.x, text.y + 60, text.width, 18), "теги: " + (card.tags != null && card.tags.Count > 0 ? string.Join(", ", card.tags) : "—"), EditorStyles.miniLabel);
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
                    var known = DesignerData.MomentTagList();
                    var path = tags.propertyPath;
                    var targets = serializedObject.targetObjects;
                    foreach (var tag in known)
                    {
                        string t = tag;
                        menu.AddItem(new GUIContent(t), false, () => AddTag(targets, path, t));
                    }

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Свой тег (пустая строка)"), false, () => AddTag(targets, path, ""));
                    menu.ShowAsContext();
                }
            }
        }

        static void AddTag(Object[] targets, string path, string tag)
        {
            var so = new SerializedObject(targets);
            var list = so.FindProperty(path);
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
        }

        void Warnings(EventDefinition card)
        {
            if (string.IsNullOrEmpty(card.id))
                EditorGUILayout.HelpBox("Пустой id — карта не загрузится.", MessageType.Error);
            if (System.Array.IndexOf(DesignerData.SpecialCardIds, card.id) >= 0)
                EditorGUILayout.HelpBox("У этой карты особое поведение в коде квартиры (по id). Числа и тексты менять можно, id — нельзя.", MessageType.Info);
            if (card.targetType == TargetType.Object && string.IsNullOrEmpty(card.requiredObjectId))
                EditorGUILayout.HelpBox("Цель — объект, но объект не выбран: карту можно будет навести на любой объект.", MessageType.Warning);
            if (!card.starter && card.price <= 0)
                EditorGUILayout.HelpBox("Не стартовая и без цены — игрок её никогда не получит (разве что временной картой из события).", MessageType.Warning);
            if (card.tags == null || card.tags.Count == 0)
                EditorGUILayout.HelpBox("Без тегов участники не отреагируют на карту.", MessageType.Warning);
            if (card.tags != null)
            {
                var known = DesignerData.MomentTagList();
                foreach (var tag in card.tags)
                {
                    if (!known.Contains(tag))
                        EditorGUILayout.HelpBox("Тег «" + tag + "» реакции участников пока не знают (известные: " + string.Join(", ", known) + ").", MessageType.Info);
                }
            }

            if (!DesignerData.InFolder(card, DesignerData.CardsRoot))
                EditorGUILayout.HelpBox("Карта лежит не в " + DesignerData.CardsRoot + " — в игру не попадёт.", MessageType.Warning);

            var dup = 0;
            foreach (var other in DesignerData.LoadAll<EventDefinition>())
            {
                if (other != card && other.id == card.id)
                    dup++;
            }

            if (dup > 0)
                EditorGUILayout.HelpBox("Ещё " + dup + " карт(ы) с таким же id.", MessageType.Error);
        }

        static void Header(string text)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }

        void Field(string name, string label, string tooltip)
        {
            var prop = serializedObject.FindProperty(name);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip), true);
        }
    }
}
