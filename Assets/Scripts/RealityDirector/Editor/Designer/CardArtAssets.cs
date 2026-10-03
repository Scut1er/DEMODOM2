using System.Collections.Generic;
using RealityDirector.Events;
using RealityDirector.UI;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Внешний вид карт: ассет CardVisuals (рамка/иконка/цвет по категории) и арт карт по id.
    // Художнику достаточно положить PNG с именем id карты в Art/UI/Cards/Art — меню подключит его в поле «Арт».
    public static class CardArtAssets
    {
        public const string ConfigAsset = "Assets/Resources/" + CardVisuals.ConfigPath + ".asset";
        const string ArtRoot = "Assets/Resources/" + CardVisuals.ArtFolder;

        [MenuItem("RealityDirector/Карты: подключить арт по id", priority = 41)]
        public static void BindArt()
        {
            CreateConfig();
            int bound = 0;
            var missing = new List<string>();
            foreach (var card in DesignerData.LoadAll<EventDefinition>())
            {
                if (card == null || string.IsNullOrEmpty(card.id) || card.cardArt != null)
                    continue;
                var art = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + card.id + ".png");
                if (art == null)
                {
                    missing.Add(card.id);
                    continue;
                }

                Undo.RecordObject(card, "Арт карты");
                card.cardArt = art;
                EditorUtility.SetDirty(card);
                bound++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Карты: арт подключён — " + bound + ". Без арта: " + (missing.Count == 0 ? "нет" : string.Join(", ", missing)));
        }

        // Ассет внешнего вида с раскладкой пака (красная/зелёная/синяя рамка художницы).
        public static CardVisualConfig CreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CardVisualConfig>(ConfigAsset);
            if (config != null)
                return config;
            config = Object.Instantiate(CardVisuals.Defaults);
            config.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(config, ConfigAsset);
            AssetDatabase.SaveAssets();
            return config;
        }

        // Проблемы внешнего вида карты — для инспектора карты и Workshop.
        public static List<string> Problems(EventDefinition card)
        {
            var list = new List<string>();
            if (card == null)
                return list;
            if (CardVisuals.Art(card) == null)
                list.Add("нет арта: поле «Арт» пустое и нет файла " + CardVisuals.ArtFolder + card.id + ".png");
            var config = AssetDatabase.LoadAssetAtPath<CardVisualConfig>(ConfigAsset);
            if (config != null && !string.IsNullOrEmpty(card.category) && config.Find(card.category) == config.fallback)
                list.Add("категории «" + card.category + "» нет в CardVisuals — рамка «Остальные»");
            return list;
        }
    }

    [CustomEditor(typeof(CardVisualConfig))]
    public class CardVisualConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (CardVisualConfig)target;
            EditorGUILayout.HelpBox("Как выглядит карта в руке, колоде и магазине: рамка художницы, иконка и цвет — по категории. "
                                    + "Арт — в поле «Арт» у самой карты (или файл Art/UI/Cards/Art/<id>.png). Текст на карту не запекается: "
                                    + "название, описание, цена и кубик берутся из данных карты.", MessageType.None);
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Как выглядит каждая категория", EditorStyles.boldLabel);
            var cards = DesignerData.LoadAll<EventDefinition>();
            var styles = new List<CardVisualConfig.CategoryStyle>(config.categories) { config.fallback };
            var size = CardFace.SizeFor(132f);
            int perRow = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 30f) / (size.x + 8f)));
            for (int i = 0; i < styles.Count; i += perRow)
            {
                var row = GUILayoutUtility.GetRect(10, size.y + 22, GUILayout.ExpandWidth(true));
                for (int j = 0; j < perRow && i + j < styles.Count; j++)
                {
                    var style = styles[i + j];
                    var sample = cards.Find(c => c != null && c.category == style.category);
                    var r = new Rect(row.x + j * (size.x + 8f), row.y, size.x, size.y);
                    if (sample != null)
                        CardInsight.DrawCard(r, sample);
                    else if (style.frame != null)
                        CardInsight.DrawSprite(r, style.frame, false);
                    GUI.Label(new Rect(r.x, r.yMax + 2, r.width, 18), string.IsNullOrEmpty(style.category) ? "?" : style.category, EditorStyles.centeredGreyMiniLabel);
                }
            }

            foreach (var w in Warnings(config, cards))
                EditorGUILayout.HelpBox(w, MessageType.Warning);
            if (GUILayout.Button("Подключить арт карт по id"))
                CardArtAssets.BindArt();
        }

        static List<string> Warnings(CardVisualConfig config, List<EventDefinition> cards)
        {
            var list = new List<string>();
            var seen = new HashSet<string>();
            foreach (var style in config.categories)
            {
                if (style == null)
                    continue;
                if (string.IsNullOrEmpty(style.category))
                    list.Add("Строка без категории.");
                else if (!seen.Add(style.category))
                    list.Add("Категория «" + style.category + "» дважды — работает первая.");
                if (style.frame == null)
                    list.Add("«" + style.category + "»: нет рамки — возьмётся встроенная.");
                if (style.icon == null)
                    list.Add("«" + style.category + "»: нет иконки.");
            }

            if (config.fallback == null || config.fallback.frame == null)
                list.Add("«Остальные»: нет рамки.");
            var noArt = new List<string>();
            var noStyle = new HashSet<string>();
            foreach (var card in cards)
            {
                if (card == null || card.status == CardStatus.Disabled)
                    continue;
                if (CardVisuals.Art(card) == null)
                    noArt.Add(card.id);
                if (!string.IsNullOrEmpty(card.category) && config.Find(card.category) == config.fallback)
                    noStyle.Add(card.category);
            }

            if (noArt.Count > 0)
                list.Add("Карты без арта (" + noArt.Count + "): " + string.Join(", ", noArt) + " — на карте будет иконка категории.");
            if (noStyle.Count > 0)
                list.Add("Категории карт без строки здесь: " + string.Join(", ", noStyle) + ".");
            return list;
        }
    }
}
