using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    [CustomEditor(typeof(SeasonConfig))]
    public class SeasonConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var c = (SeasonConfig)target;
            EditorGUILayout.HelpBox("Главные числа сезона. Хаб читает их при каждом запуске — менять можно и посреди сезона.", MessageType.None);
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Как будет выглядеть сезон", EditorStyles.boldLabel);
            for (int i = 0; i < c.episodes; i++)
            {
                var map = c.MapFor(i);
                string shape = map != null ? map.name + "  ·  " + map.floors + " комнат по пути" : "карта по умолчанию (не задана)";
                EditorGUILayout.LabelField("Выпуск " + (i + 1), shape);
            }

            // Стартовая колода: кто отмечен галочкой у карты + из чего тянутся случайные.
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Стартовая колода нового сезона", EditorStyles.boldLabel);
            var cards = DesignerData.LoadAll<RealityDirector.Events.EventDefinition>();
            int starters = 0;
            foreach (var card in cards)
            {
                if (card == null || !card.starter)
                    continue;
                starters++;
                bool works = RealityDirector.Cards.CardRuntime.Playable(card);
                EditorGUILayout.LabelField("• " + card.displayName + "  (" + card.category + ")", works ? "" : "NOT RUNTIME SUPPORTED");
            }

            if (starters == 0)
                EditorGUILayout.HelpBox("Ни одна карта не отмечена «В стартовой колоде» — игрок начнёт только со случайными.", MessageType.Warning);
            int pool = 0;
            foreach (var card in cards)
            {
                if (card == null || card.starter || card.sponsor || card.status == RealityDirector.Events.CardStatus.Disabled)
                    continue;
                if (card.effects == null || card.effects.Count == 0)
                    continue;
                if (card.status != RealityDirector.Events.CardStatus.Ready && !(c.randomStarterIncludeTesting && card.status == RealityDirector.Events.CardStatus.Testing))
                    continue;
                if (c.randomStarterExclude != null && c.randomStarterExclude.Contains(card.category))
                    continue;
                if (RealityDirector.Cards.CardRuntime.Playable(card))
                    pool++;
            }

            EditorGUILayout.LabelField("+ " + c.randomStarterCardCount + " случайные", "из " + pool + " подходящих карт");
            if (pool < c.randomStarterCardCount)
                EditorGUILayout.HelpBox("Подходящих карт меньше, чем случайных в старте: выдадим сколько есть.", MessageType.Warning);
            EditorGUILayout.LabelField("Отмечать стартовые — в Card Workshop (фильтр «Стартовые», колонка «старт» в таблице).", EditorStyles.wordWrappedMiniLabel);

            if (c.maps == null || c.maps.Count == 0)
                EditorGUILayout.HelpBox("Не задано ни одной карты — будет встроенная по умолчанию.", MessageType.Warning);
            if (c.castMax > CastRoster.MaxSeats)
                EditorGUILayout.HelpBox("castMax больше максимума мест в хабе (" + CastRoster.MaxSeats + ").", MessageType.Warning);
        }
    }
}
