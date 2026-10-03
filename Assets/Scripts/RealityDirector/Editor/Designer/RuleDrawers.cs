using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Условие в инспекторе: тип по-русски и только нужные ему поля.
    [CustomPropertyDrawer(typeof(Condition))]
    public class ConditionDrawer : PropertyDrawer
    {
        static readonly string[] TypeNames =
        {
            "Выпуск не раньше №",
            "Выпуск не позже №",
            "Бюджет не меньше",
            "Участников не меньше",
            "Участников не больше",
            "В касте есть участник",
            "Флаг выпуска стоит",
            "Флаг сезона стоит",
            "Сюжетный тег выпуска есть",
            "Комната уже пройдена в выпуске",
            "Уровень команды не меньше",
            "Спонсорский контракт активен",
            "Тон сезона не меньше",
            "Нал выпуска не меньше"
        };

        // Что видно в списке. «Спонсорский контракт активен» скрыт: контрактов в игре пока нет, условие всегда ложно.
        static readonly ConditionType[] Shown =
        {
            ConditionType.EpisodeAtLeast, ConditionType.EpisodeAtMost, ConditionType.BudgetAtLeast,
            ConditionType.CastAtLeast, ConditionType.CastAtMost, ConditionType.CastHasActor,
            ConditionType.EpisodeFlag, ConditionType.SeasonFlag, ConditionType.NarrativeTag,
            ConditionType.RoomVisited, ConditionType.CrewLevelAtLeast, ConditionType.ToneAtLeast,
            ConditionType.CashAtLeast
        };

        static string[] _shownNames;

        static int TypePopup(Rect rect, int current)
        {
            if (_shownNames == null)
            {
                _shownNames = new string[Shown.Length];
                for (int i = 0; i < Shown.Length; i++)
                    _shownNames[i] = TypeNames[(int)Shown[i]];
            }

            int index = System.Array.IndexOf(Shown, (ConditionType)current);
            if (index < 0)
                return EditorGUI.Popup(rect, current, TypeNames); // старый ассет со скрытым типом — показать как есть
            return (int)Shown[EditorGUI.Popup(rect, index, _shownNames)];
        }

        const float Line = 18f;
        const float Gap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Line * 3 + Gap * 4;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var type = property.FindPropertyRelative("type");
            var key = property.FindPropertyRelative("key");
            var value = property.FindPropertyRelative("value");
            var mood = property.FindPropertyRelative("mood");
            var not = property.FindPropertyRelative("not");
            var failText = property.FindPropertyRelative("failText");

            var r = new Rect(position.x, position.y + Gap, position.width, Line);
            var typeRect = new Rect(r.x, r.y, r.width - 70f, Line);
            var notRect = new Rect(r.xMax - 66f, r.y, 66f, Line);
            type.enumValueIndex = TypePopup(typeRect, type.enumValueIndex);
            not.boolValue = EditorGUI.ToggleLeft(notRect, new GUIContent("НЕ", "Инвертировать: выполнено, если проверка НЕ прошла."), not.boolValue);

            r.y += Line + Gap;
            switch ((ConditionType)type.enumValueIndex)
            {
                case ConditionType.EpisodeAtLeast:
                case ConditionType.EpisodeAtMost:
                    EditorGUI.PropertyField(r, value, new GUIContent("Номер выпуска (с 1)"));
                    break;
                case ConditionType.BudgetAtLeast:
                    EditorGUI.PropertyField(r, value, new GUIContent("Кредитов"));
                    break;
                case ConditionType.CashAtLeast:
                    EditorGUI.PropertyField(r, value, new GUIContent("Нала"));
                    break;
                case ConditionType.CastAtLeast:
                case ConditionType.CastAtMost:
                    EditorGUI.PropertyField(r, value, new GUIContent("Участников"));
                    break;
                case ConditionType.CastHasActor:
                    DesignerData.PickerField(r, new GUIContent("id участника"), key, DesignerData.ActorIds());
                    break;
                case ConditionType.EpisodeFlag:
                    DesignerData.PickerField(r, new GUIContent("Флаг выпуска"), key, DesignerData.EpisodeFlags());
                    break;
                case ConditionType.SeasonFlag:
                    DesignerData.PickerField(r, new GUIContent("Флаг сезона"), key, DesignerData.SeasonFlags());
                    break;
                case ConditionType.NarrativeTag:
                    DesignerData.PickerField(r, new GUIContent("Сюжетный тег"), key, DesignerData.NarrativeTags());
                    break;
                case ConditionType.RoomVisited:
                    DesignerData.PickerField(r, new GUIContent("id комнаты"), key, DesignerData.RoomIds());
                    break;
                case ConditionType.CrewLevelAtLeast:
                    Split(r, out var left, out var right);
                    DesignerData.PickerField(left, new GUIContent("Команда"), key, DesignerData.CrewKeys, DesignerData.CrewNames);
                    EditorGUI.PropertyField(right, value, new GUIContent("Уровень"));
                    break;
                case ConditionType.ContractActive:
                    EditorGUI.PropertyField(r, key, new GUIContent("id оффера"));
                    break;
                case ConditionType.ToneAtLeast:
                    Split(r, out var l, out var rr);
                    EditorGUI.PropertyField(l, mood, new GUIContent("Тон"));
                    EditorGUI.PropertyField(rr, value, new GUIContent("Очков"));
                    break;
            }

            r.y += Line + Gap;
            EditorGUI.PropertyField(r, failText, new GUIContent("Текст, если нельзя", "Что увидит игрок на закрытой комнате. Пусто — сгенерируется."));
            EditorGUI.EndProperty();
        }

        public static void Split(Rect r, out Rect left, out Rect right)
        {
            float half = (r.width - 6f) * 0.5f;
            left = new Rect(r.x, r.y, half, r.height);
            right = new Rect(r.x + half + 6f, r.y, half, r.height);
        }
    }

    // Эффект в инспекторе: тип по-русски и только нужные ему поля.
    [CustomPropertyDrawer(typeof(Effect))]
    public class EffectDrawer : PropertyDrawer
    {
        // Ключи, которые квартира читает из модификаторов следующей съёмки (PitchFlow).
        static readonly string[] ModifierKeys = { "stress", "anger", "sadness", "hostility" };
        static readonly string[] ModifierNames = { "Стресс", "Злость", "Грусть", "Вражда" };

        // «Модификатор эфира» скрыт: его пока никто не читает.
        static readonly EffectType[] Shown =
        {
            EffectType.Budget, EffectType.Cash, EffectType.Tone,
            EffectType.SetEpisodeFlag, EffectType.ClearEpisodeFlag, EffectType.SetSeasonFlag, EffectType.ClearSeasonFlag,
            EffectType.AddNarrativeTag, EffectType.AddTempCard, EffectType.RemoveTempCard,
            EffectType.AddDeckCard, EffectType.RemoveDeckCard, EffectType.NextRoomModifier
        };

        static string[] _shownNames;

        static int TypePopup(Rect rect, int current)
        {
            if (_shownNames == null)
            {
                _shownNames = new string[Shown.Length];
                for (int i = 0; i < Shown.Length; i++)
                    _shownNames[i] = TypeNames[(int)Shown[i]];
            }

            int index = System.Array.IndexOf(Shown, (EffectType)current);
            if (index < 0)
                return EditorGUI.Popup(rect, current, TypeNames);
            return (int)Shown[EditorGUI.Popup(rect, index, _shownNames)];
        }

        static readonly string[] TypeNames =
        {
            "Бюджет +/-",
            "Тон сезона +",
            "Поставить флаг выпуска",
            "Снять флаг выпуска",
            "Поставить флаг сезона",
            "Снять флаг сезона",
            "Добавить сюжетный тег",
            "Дать временную карту (на выпуск)",
            "Забрать временную карту",
            "Следующая съёмка: актёры начнут с…",
            "Модификатор эфира",
            "Нал +/-",
            "Карта в колоду навсегда",
            "Убрать карту из колоды навсегда"
        };

        const float Line = 18f;
        const float Gap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Line * 2 + Gap * 3;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var type = property.FindPropertyRelative("type");
            var key = property.FindPropertyRelative("key");
            var value = property.FindPropertyRelative("value");
            var mood = property.FindPropertyRelative("mood");

            var r = new Rect(position.x, position.y + Gap, position.width, Line);
            type.enumValueIndex = TypePopup(r, type.enumValueIndex);

            r.y += Line + Gap;
            switch ((EffectType)type.enumValueIndex)
            {
                case EffectType.NextRoomModifier:
                    ConditionDrawer.Split(r, out var ml, out var mr);
                    DesignerData.PickerField(ml, new GUIContent("Что"), key, ModifierKeys, ModifierNames);
                    EditorGUI.PropertyField(mr, value, new GUIContent("Сколько"));
                    break;
                case EffectType.Budget:
                    EditorGUI.PropertyField(r, value, new GUIContent("Кредиты (минус — списать)"));
                    break;
                case EffectType.Tone:
                    ConditionDrawer.Split(r, out var l, out var rr);
                    EditorGUI.PropertyField(l, mood, new GUIContent("Тон"));
                    EditorGUI.PropertyField(rr, value, new GUIContent("Очков"));
                    break;
                case EffectType.SetEpisodeFlag:
                case EffectType.ClearEpisodeFlag:
                    DesignerData.PickerField(r, new GUIContent("Флаг выпуска"), key, DesignerData.EpisodeFlags());
                    break;
                case EffectType.SetSeasonFlag:
                case EffectType.ClearSeasonFlag:
                    DesignerData.PickerField(r, new GUIContent("Флаг сезона"), key, DesignerData.SeasonFlags());
                    break;
                case EffectType.AddNarrativeTag:
                    DesignerData.PickerField(r, new GUIContent("Сюжетный тег"), key, DesignerData.NarrativeTags());
                    break;
                case EffectType.AddTempCard:
                case EffectType.RemoveTempCard:
                case EffectType.AddDeckCard:
                case EffectType.RemoveDeckCard:
                    DesignerData.PickerField(r, new GUIContent("id карты"), key, DesignerData.CardIds());
                    break;
                case EffectType.Cash:
                    EditorGUI.PropertyField(r, value, new GUIContent("Нал (минус — списать)"));
                    break;
            }

            EditorGUI.EndProperty();
        }
    }
}
