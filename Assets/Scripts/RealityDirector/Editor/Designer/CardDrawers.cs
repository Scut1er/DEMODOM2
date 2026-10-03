using RealityDirector.Events;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Компактные рисовалки данных карты: подписи по-русски, только нужные поля.
    static class Row
    {
        public const float Line = 18f;
        public const float Gap = 2f;

        public static Rect First(Rect position)
        {
            return new Rect(position.x, position.y + Gap, position.width, Line);
        }

        public static Rect Next(Rect r)
        {
            return new Rect(r.x, r.y + Line + Gap, r.width, Line);
        }

        // Делит строку на части с подписями; подпись не «ползунок» — только ввод.
        public static Rect[] Split(Rect r, params float[] weights)
        {
            float total = 0f;
            foreach (var w in weights)
                total += w;
            var parts = new Rect[weights.Length];
            float x = r.x;
            float free = r.width - (weights.Length - 1) * 4f;
            for (int i = 0; i < weights.Length; i++)
            {
                float w = free * weights[i] / total;
                parts[i] = new Rect(x, r.y, w, r.height);
                x += w + 4f;
            }

            return parts;
        }

        public static void Labeled(Rect r, string label, SerializedProperty prop, float labelWidth = 70f)
        {
            var l = new Rect(r.x, r.y, Mathf.Min(labelWidth, r.width * 0.5f), r.height);
            var f = new Rect(l.xMax + 2f, r.y, r.width - l.width - 2f, r.height);
            EditorGUI.LabelField(l, label, EditorStyles.miniLabel);
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = EditorGUI.IntField(f, prop.intValue);
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = EditorGUI.FloatField(f, prop.floatValue);
                    break;
                default:
                    EditorGUI.PropertyField(f, prop, GUIContent.none);
                    break;
            }
        }
    }

    [CustomPropertyDrawer(typeof(TargetFilter))]
    public class TargetFilterDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Row.Line * 2 + Row.Gap * 3;
        }

        public override void OnGUI(Rect position, SerializedProperty p, GUIContent label)
        {
            var r = Row.First(position);
            var type = p.FindPropertyRelative("type");
            EditorGUI.PropertyField(r, type, GUIContent.none);
            r = Row.Next(r);
            switch ((TargetFilterType)type.enumValueIndex)
            {
                case TargetFilterType.ActorStatAtLeast:
                case TargetFilterType.ActorStatAtMost:
                    var s = Row.Split(r, 1, 1);
                    Row.Labeled(s[0], "Эмоция", p.FindPropertyRelative("stat"));
                    Row.Labeled(s[1], "Значение", p.FindPropertyRelative("value"));
                    break;
                case TargetFilterType.ActorHasTrait:
                case TargetFilterType.ActorLacksTrait:
                    Row.Labeled(r, "Черта", p.FindPropertyRelative("key"));
                    break;
                case TargetFilterType.ActorHasState:
                case TargetFilterType.ActorLacksState:
                    Row.Labeled(r, "Состояние", p.FindPropertyRelative("key"));
                    break;
                default:
                    Row.Labeled(r, "Тег", p.FindPropertyRelative("key"));
                    break;
            }
        }
    }

    [CustomPropertyDrawer(typeof(CardEffect))]
    public class CardEffectDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var type = (CardEffectType)property.FindPropertyRelative("type").enumValueIndex;
            bool state = type == CardEffectType.AddState;
            return Row.Line * (state ? 3 : 2) + Row.Gap * (state ? 4 : 3);
        }

        public override void OnGUI(Rect position, SerializedProperty p, GUIContent label)
        {
            var r = Row.First(position);
            var type = p.FindPropertyRelative("type");
            var top = Row.Split(r, 3, 2);
            EditorGUI.PropertyField(top[0], type, GUIContent.none);
            var t = (CardEffectType)type.enumValueIndex;
            bool actor = t <= CardEffectType.MoveActor;
            if (actor)
                EditorGUI.PropertyField(top[1], p.FindPropertyRelative("receiver"), GUIContent.none);

            r = Row.Next(r);
            switch (t)
            {
                case CardEffectType.ChangeStat:
                {
                    var s = Row.Split(r, 1, 1);
                    Row.Labeled(s[0], "Эмоция", p.FindPropertyRelative("stat"));
                    Row.Labeled(s[1], "На сколько", p.FindPropertyRelative("amount"));
                    break;
                }
                case CardEffectType.ChangeRelationship:
                {
                    var s = Row.Split(r, 1, 1);
                    Row.Labeled(s[0], "Ось", p.FindPropertyRelative("axis"));
                    Row.Labeled(s[1], "На сколько", p.FindPropertyRelative("amount"));
                    break;
                }
                case CardEffectType.AddState:
                {
                    Row.Labeled(r, "Состояние", p.FindPropertyRelative("key"));
                    r = Row.Next(r);
                    var s = Row.Split(r, 3, 2);
                    Row.Labeled(s[0], "Длится", p.FindPropertyRelative("duration"));
                    if ((StateDuration)p.FindPropertyRelative("duration").enumValueIndex == StateDuration.Timed)
                        Row.Labeled(s[1], "Секунд", p.FindPropertyRelative("seconds"));
                    break;
                }
                case CardEffectType.RemoveState:
                    Row.Labeled(r, "Состояние", p.FindPropertyRelative("key"));
                    break;
                case CardEffectType.WorldEvent:
                    Row.Labeled(r, "Теги", p.FindPropertyRelative("key"));
                    break;
                case CardEffectType.MoveActor:
                    Row.Labeled(r, "Куда (id)", p.FindPropertyRelative("key"));
                    break;
                case CardEffectType.ReduceCost:
                case CardEffectType.IncreaseCost:
                    Row.Labeled(r, "На $", p.FindPropertyRelative("amount"));
                    break;
                default:
                    Row.Labeled(r, "Сколько карт", p.FindPropertyRelative("amount"));
                    break;
            }
        }
    }

    [CustomPropertyDrawer(typeof(DiceStepUp))]
    public class DiceStepUpDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Row.Line + Row.Gap * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty p, GUIContent label)
        {
            var r = Row.First(position);
            var condition = p.FindPropertyRelative("condition");
            var s = Row.Split(r, 4, 3, 2);
            EditorGUI.PropertyField(s[0], condition, GUIContent.none);
            if ((StepUpCondition)condition.enumValueIndex == StepUpCondition.TargetStatAtLeast)
            {
                var half = Row.Split(s[1], 1, 1);
                EditorGUI.PropertyField(half[0], p.FindPropertyRelative("stat"), GUIContent.none);
                var v = p.FindPropertyRelative("value");
                v.intValue = EditorGUI.IntField(half[1], v.intValue);
            }
            else
            {
                EditorGUI.PropertyField(s[1], p.FindPropertyRelative("key"), GUIContent.none);
            }

            Row.Labeled(s[2], "+ступ.", p.FindPropertyRelative("steps"), 40f);
        }
    }

    [CustomPropertyDrawer(typeof(DiceEffect))]
    public class DiceEffectDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Row.Line * 2 + Row.Gap * 3 + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("stepUps"), true) + Row.Gap;
        }

        public override void OnGUI(Rect position, SerializedProperty p, GUIContent label)
        {
            var r = Row.First(position);
            var s = Row.Split(r, 3, 2, 2, 2);
            Row.Labeled(s[0], "Эмоция", p.FindPropertyRelative("stat"), 50f);
            Row.Labeled(s[1], "Кол-во", p.FindPropertyRelative("count"), 44f);
            Row.Labeled(s[2], "Кубик", p.FindPropertyRelative("die"), 40f);
            Row.Labeled(s[3], "+", p.FindPropertyRelative("bonus"), 14f);
            r = Row.Next(r);
            Row.Labeled(r, "Бросок", p.FindPropertyRelative("roll"), 50f);
            var steps = p.FindPropertyRelative("stepUps");
            var listRect = new Rect(position.x, r.yMax + Row.Gap, position.width, EditorGUI.GetPropertyHeight(steps, true));
            EditorGUI.PropertyField(listRect, steps, new GUIContent("Step Up (кубик растёт, если…)"), true);
        }
    }

    [CustomPropertyDrawer(typeof(AuraStatModifier))]
    public class AuraStatDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty p, GUIContent label)
        {
            var s = Row.Split(new Rect(position.x, position.y, position.width, Row.Line), 1, 1);
            Row.Labeled(s[0], "Эмоция", p.FindPropertyRelative("stat"));
            Row.Labeled(s[1], "В секунду", p.FindPropertyRelative("perSecond"));
        }
    }

    [CustomPropertyDrawer(typeof(AuraDiceModifier))]
    public class AuraDiceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty p, GUIContent label)
        {
            var s = Row.Split(new Rect(position.x, position.y, position.width, Row.Line), 1, 1);
            Row.Labeled(s[0], "Броски", p.FindPropertyRelative("stat"));
            Row.Labeled(s[1], "+ступеней", p.FindPropertyRelative("steps"));
        }
    }

    [CustomPropertyDrawer(typeof(BehaviourWeight))]
    public class BehaviourWeightDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty p, GUIContent label)
        {
            var s = Row.Split(new Rect(position.x, position.y, position.width, Row.Line), 3, 2);
            Row.Labeled(s[0], "Поведение", p.FindPropertyRelative("behaviour"));
            Row.Labeled(s[1], "× вес", p.FindPropertyRelative("multiplier"), 40f);
        }
    }
}
