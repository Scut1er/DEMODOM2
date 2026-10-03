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
            int lines = type == CardEffectType.AddState ? 4 : Kind(type) == EffectKind.Flag ? 2 : 3;
            return Row.Line * lines + Row.Gap * (lines + 1);
        }

        enum EffectKind
        {
            Stat,
            Relationship,
            State,
            Key,
            Amount,
            Flag
        }

        // Какие параметры нужны эффекту.
        static EffectKind Kind(CardEffectType t)
        {
            switch (t)
            {
                case CardEffectType.ChangeStat:
                case CardEffectType.ChangeHighestNegative:
                    return EffectKind.Stat;
                case CardEffectType.ChangeRelationship:
                    return EffectKind.Relationship;
                case CardEffectType.AddState:
                case CardEffectType.RemoveState:
                    return EffectKind.State;
                case CardEffectType.WorldEvent:
                case CardEffectType.MoveActor:
                case CardEffectType.AddContext:
                case CardEffectType.Check:
                case CardEffectType.RevealSecret:
                case CardEffectType.InviteActor:
                case CardEffectType.EventCandidate:
                case CardEffectType.BehaviourWeights:
                case CardEffectType.NextCaptureBonus:
                    return EffectKind.Key;
                case CardEffectType.ReturnHandCardToLibrary:
                case CardEffectType.DrawRandom:
                case CardEffectType.SearchLibrary:
                case CardEffectType.RecoverUsedCard:
                case CardEffectType.ProtectCard:
                case CardEffectType.RetainCard:
                case CardEffectType.ReduceCost:
                case CardEffectType.IncreaseCost:
                case CardEffectType.ModifyNextCard:
                case CardEffectType.DuplicateEffect:
                case CardEffectType.MoveHandCardToUsed:
                case CardEffectType.PeekLibrary:
                    return EffectKind.Amount;
                default:
                    return EffectKind.Flag;
            }
        }

        static bool ToActors(CardEffectType t)
        {
            var k = Kind(t);
            return k == EffectKind.Stat || k == EffectKind.Relationship || k == EffectKind.State
                   || t == CardEffectType.WorldEvent || t == CardEffectType.MoveActor || t == CardEffectType.AddContext
                   || t == CardEffectType.Check || t == CardEffectType.RevealSecret || t == CardEffectType.InviteActor;
        }

        static string KeyLabel(CardEffectType t)
        {
            switch (t)
            {
                case CardEffectType.WorldEvent: return "Теги";
                case CardEffectType.MoveActor: return "Куда (id)";
                case CardEffectType.AddContext: return "Контекст";
                case CardEffectType.Check: return "Что";
                case CardEffectType.RevealSecret: return "Как";
                case CardEffectType.InviteActor: return "Кого";
                case CardEffectType.EventCandidate: return "Событие";
                case CardEffectType.BehaviourWeights: return "Поведение";
                case CardEffectType.NextCaptureBonus: return "Бонус";
                default: return "Ключ";
            }
        }

        public override void OnGUI(Rect position, SerializedProperty p, GUIContent label)
        {
            var r = Row.First(position);
            var type = p.FindPropertyRelative("type");
            var top = Row.Split(r, 3, 2);
            EditorGUI.PropertyField(top[0], type, GUIContent.none);
            var t = (CardEffectType)type.enumValueIndex;
            if (ToActors(t))
                EditorGUI.PropertyField(top[1], p.FindPropertyRelative("receiver"), GUIContent.none);

            var kind = Kind(t);
            if (kind != EffectKind.Flag)
            {
                r = Row.Next(r);
                switch (kind)
                {
                    case EffectKind.Stat:
                    {
                        var s = Row.Split(r, 1, 1);
                        if (t == CardEffectType.ChangeStat)
                            Row.Labeled(s[0], "Эмоция", p.FindPropertyRelative("stat"));
                        Row.Labeled(s[1], "На сколько", p.FindPropertyRelative("amount"));
                        break;
                    }
                    case EffectKind.Relationship:
                    {
                        var s = Row.Split(r, 1, 1);
                        Row.Labeled(s[0], "Ось", p.FindPropertyRelative("axis"));
                        Row.Labeled(s[1], "На сколько", p.FindPropertyRelative("amount"));
                        break;
                    }
                    case EffectKind.State:
                        Row.Labeled(r, "Состояние", p.FindPropertyRelative("key"));
                        if (t == CardEffectType.AddState)
                        {
                            r = Row.Next(r);
                            var s = Row.Split(r, 3, 2);
                            Row.Labeled(s[0], "Длится", p.FindPropertyRelative("duration"));
                            if ((StateDuration)p.FindPropertyRelative("duration").enumValueIndex == StateDuration.Timed)
                                Row.Labeled(s[1], "Секунд", p.FindPropertyRelative("seconds"));
                        }

                        break;
                    case EffectKind.Key:
                    {
                        var s = Row.Split(r, 3, 2);
                        Row.Labeled(s[0], KeyLabel(t), p.FindPropertyRelative("key"));
                        Row.Labeled(s[1], "Сила", p.FindPropertyRelative("amount"), 40f);
                        break;
                    }
                    default:
                        Row.Labeled(r, t == CardEffectType.ReduceCost || t == CardEffectType.IncreaseCost ? "На $" : "Сколько карт", p.FindPropertyRelative("amount"));
                        break;
                }
            }

            r = Row.Next(r);
            Row.Labeled(r, "Только если", p.FindPropertyRelative("onlyIf"));
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
            return Row.Line * 3 + Row.Gap * 4 + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("stepUps"), true) + Row.Gap;
        }

        public override void OnGUI(Rect position, SerializedProperty p, GUIContent label)
        {
            var r = Row.First(position);
            var subject = p.FindPropertyRelative("subject");
            var top = Row.Split(r, 2, 3, 2);
            EditorGUI.PropertyField(top[0], subject, GUIContent.none);
            switch ((DiceSubject)subject.enumValueIndex)
            {
                case DiceSubject.Relationship:
                    EditorGUI.PropertyField(top[1], p.FindPropertyRelative("axis"), GUIContent.none);
                    break;
                case DiceSubject.Check:
                    Row.Labeled(top[1], "Что", p.FindPropertyRelative("check"), 30f);
                    break;
                default:
                    EditorGUI.PropertyField(top[1], p.FindPropertyRelative("stat"), GUIContent.none);
                    break;
            }

            var lower = p.FindPropertyRelative("lower");
            lower.boolValue = EditorGUI.Popup(top[2], lower.boolValue ? 1 : 0, new[] { "повышает", "снижает" }) == 1;
            r = Row.Next(r);
            var s = Row.Split(r, 2, 2, 1, 3);
            Row.Labeled(s[0], "Кол-во", p.FindPropertyRelative("count"), 44f);
            Row.Labeled(s[1], "Кубик", p.FindPropertyRelative("die"), 40f);
            Row.Labeled(s[2], "+", p.FindPropertyRelative("bonus"), 14f);
            Row.Labeled(s[3], "Бросок", p.FindPropertyRelative("roll"), 46f);
            r = Row.Next(r);
            Row.Labeled(r, "Только если", p.FindPropertyRelative("onlyIf"));
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
