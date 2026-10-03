using System.Collections.Generic;
using RealityDirector.Events;
using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Съёмка: постановка (кто где и как начинает, что стоит в доме, какие карты можно) — с проверками.
    [CustomEditor(typeof(SituationRoomDefinition))]
    public class SituationRoomEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var s = (SituationRoomDefinition)target;
            EditorGUILayout.HelpBox("Съёмка на квартире. Блок «Постановка» делает её непохожей на другие: где стоят люди, "
                                    + "их эмоции и состояния, вражда пары, что уже стоит в доме, какие комнаты открыты и «без камер», "
                                    + "какие карты играются и сколько HellToken. Применяется один раз при входе в комнату карты выпуска.", MessageType.None);
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Как начнётся съёмка", EditorStyles.boldLabel);
            foreach (var line in Preview(s))
                EditorGUILayout.LabelField("• " + line, EditorStyles.wordWrappedLabel);
            foreach (var w in Warnings(s))
                EditorGUILayout.HelpBox(w, MessageType.Warning);
        }

        static List<string> Preview(SituationRoomDefinition s)
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(s.goal))
                list.Add("Задача: " + s.goal);
            if (s.actors != null)
            {
                foreach (var a in s.actors)
                {
                    if (a == null)
                        continue;
                    string who = a.castSlot < 0 ? "Все" : "Участник " + (a.castSlot + 1);
                    var parts = new List<string>();
                    if (a.room != HouseRoom.Any)
                        parts.Add(a.room.ToString());
                    Part(parts, "стресс", a.stress);
                    Part(parts, "злость", a.anger);
                    Part(parts, "грусть", a.sadness);
                    Part(parts, "влечение", a.attraction);
                    Part(parts, "уверенность", a.confidence);
                    Part(parts, "самоконтроль", a.selfControl);
                    if (a.states != null && a.states.Count > 0)
                        parts.Add(string.Join(", ", a.states));
                    list.Add(who + ": " + (parts.Count > 0 ? string.Join(", ", parts) : "как обычно"));
                }
            }

            if (s.hostility != 0 || s.trust != 0)
                list.Add("Пара: вражда " + s.hostility + ", доверие " + s.trust);
            if (s.props != null && s.props.Count > 0)
            {
                var names = new List<string>();
                foreach (var p in s.props)
                {
                    if (p != null)
                        names.Add(p.cardId + " → " + p.room);
                }

                list.Add("Стоит в доме: " + string.Join("; ", names));
            }

            if (s.startEvents != null && s.startEvents.Count > 0)
                list.Add("На старте: " + string.Join(", ", s.startEvents));
            if (s.openBedroom || s.openBathroom)
                list.Add("Открыто: " + (s.openBedroom ? "спальня " : "") + (s.openBathroom ? "ванная" : ""));
            if (s.allowedCategories != null && s.allowedCategories.Count > 0)
                list.Add("Играются только: " + string.Join(", ", s.allowedCategories));
            if (s.blockedCategories != null && s.blockedCategories.Count > 0)
                list.Add("Не играются: " + string.Join(", ", s.blockedCategories));
            list.Add("HellToken: " + (s.hellTokenBudget > 0f ? HellToken.Format(s.hellTokenBudget) : "как в SeasonConfig"));
            if (!s.HasSetup)
                list.Add("Постановки нет — съёмка начнётся как все.");
            return list;
        }

        static void Part(List<string> parts, string name, int value)
        {
            if (value != 0)
                parts.Add(name + (value > 0 ? " +" : " ") + value);
        }

        static List<string> Warnings(SituationRoomDefinition s)
        {
            var list = new List<string>();
            var cards = new Dictionary<string, EventDefinition>();
            foreach (var c in DesignerData.LoadAll<EventDefinition>())
            {
                if (c != null && !string.IsNullOrEmpty(c.id))
                    cards[c.id] = c;
            }

            if (s.props != null)
            {
                foreach (var p in s.props)
                {
                    if (p == null)
                        continue;
                    if (!cards.TryGetValue(p.cardId ?? "", out var card))
                    {
                        list.Add("Объект: карты «" + p.cardId + "» нет.");
                        continue;
                    }

                    bool prop = card.effects != null && card.effects.Exists(e => e != null && (e.type == CardEffectType.SpawnObject || e.type == CardEffectType.CreateAura));
                    if (!prop)
                        list.Add("Объект: у карты «" + card.displayName + "» нет эффекта «поставить объект» или «аура» — ставить нечего.");
                    if (p.room == HouseRoom.Bedroom && !s.openBedroom)
                        list.Add("Объект в спальне, но спальня закрыта (Open Bedroom).");
                    if (p.room == HouseRoom.Bathroom && !s.openBathroom)
                        list.Add("Объект в ванной, но ванная закрыта (Open Bathroom).");
                }
            }

            if (s.actors != null)
            {
                foreach (var a in s.actors)
                {
                    if (a == null)
                        continue;
                    if (a.room == HouseRoom.Bedroom && !s.openBedroom)
                        list.Add("Участник стартует в спальне, но спальня закрыта.");
                    if (a.room == HouseRoom.Bathroom && !s.openBathroom)
                        list.Add("Участник стартует в ванной, но ванная закрыта.");
                    if (a.castSlot >= 2)
                        list.Add("Участник " + (a.castSlot + 1) + " есть только при касте от " + (a.castSlot + 1) + " человек — в первом выпуске их двое.");
                }
            }

            var known = new List<string>(CardInsight.Categories);
            Categories(s.allowedCategories, known, list, "Разрешённые");
            Categories(s.blockedCategories, known, list, "Запрещённые");
            if (s.allowedCategories != null && s.blockedCategories != null)
            {
                foreach (var c in s.allowedCategories)
                {
                    if (s.blockedCategories.Contains(c))
                        list.Add("Категория «" + c + "» и разрешена, и запрещена — запрет главнее.");
                }
            }

            if (s.startEvents != null)
            {
                var tags = ReactionTags();
                foreach (var t in s.startEvents)
                {
                    if (!string.IsNullOrEmpty(t) && !tags.Contains(t))
                        list.Add("Тег «" + t + "» реакции участников пока не знают — на старте никто не отреагирует.");
                }
            }

            return list;
        }

        static List<string> _reactionTags;

        // Теги, на которые кто-то реагирует: MomentTags + теги всех правил реакций черт.
        static List<string> ReactionTags()
        {
            if (_reactionTags != null)
                return _reactionTags;
            _reactionTags = new List<string>(DesignerData.MomentTagList());
            var content = PitchContent.Create();
            foreach (var set in content.AllRuleSets())
            {
                if (set == null || set.rules == null)
                    continue;
                foreach (var r in set.rules)
                {
                    if (r != null && !string.IsNullOrEmpty(r.eventTag) && !_reactionTags.Contains(r.eventTag))
                        _reactionTags.Add(r.eventTag);
                }
            }

            content.DestroyAssets(true);
            return _reactionTags;
        }

        static void Categories(List<string> list, List<string> known, List<string> warnings, string title)
        {
            if (list == null)
                return;
            foreach (var c in list)
            {
                if (!known.Contains(c))
                    warnings.Add(title + " категории: «" + c + "» — такой категории карт нет.");
            }
        }
    }
}
