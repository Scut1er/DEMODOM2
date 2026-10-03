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

            if (c.maps == null || c.maps.Count == 0)
                EditorGUILayout.HelpBox("Не задано ни одной карты — будет встроенная по умолчанию.", MessageType.Warning);
            if (c.castMax > CastRoster.MaxSeats)
                EditorGUILayout.HelpBox("castMax больше максимума мест в хабе (" + CastRoster.MaxSeats + ").", MessageType.Warning);
        }
    }
}
