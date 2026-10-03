using RealityDirector.Core;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Плашка показателей (правый верх хаба и сценария выпуска).
    public class StatsView : MonoBehaviour
    {
        [SerializeField] Text rating;
        [SerializeField] Text budget;
        [SerializeField] Text episode;
        [Tooltip("Image с типом Filled — fillAmount 0..1")]
        [SerializeField] Image dramaBar;
        [SerializeField] Image trashBar;
        [SerializeField] Image familyBar;

        Text _drama;
        Text _trash;
        Text _family;

        void Awake()
        {
            _drama = PaintLabel("Mood0", ShowMood.Drama);
            _trash = PaintLabel("Mood1", ShowMood.Trash);
            _family = PaintLabel("Mood2", ShowMood.Family);
        }

        bool _dressed;

        // Панель в рамке пака, тоны — иконка + цветная полоса на подложке.
        public void Dress()
        {
            if (_dressed)
                return;
            _dressed = true;
            UiKit.DressSolid(GetComponent<Image>(), UiKit.Frame.Dialog);
            DressMood("Mood0", "Bar0", dramaBar, ShowMood.Drama);
            DressMood("Mood1", "Bar1", trashBar, ShowMood.Trash);
            DressMood("Mood2", "Bar2", familyBar, ShowMood.Family);
            foreach (var value in new[] { rating, budget, episode })
            {
                if (value == null)
                    continue;
                value.fontStyle = FontStyle.Bold;
                value.color = UiKit.Gold;
            }
        }

        void DressMood(string labelName, string barName, Image fill, ShowMood mood)
        {
            var label = transform.Find(labelName) as RectTransform;
            var bar = transform.Find(barName) as RectTransform;
            if (label == null || bar == null)
                return;
            var icon = UiKit.Img("Icon", transform, UiKit.MoodIcon(mood), MoodStyle.ColorOf(mood));
            icon.preserveAspect = true;
            UiKit.Place(icon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(label.anchoredPosition.x, label.anchoredPosition.y - 12f), new Vector2(18f, 18f));
            label.anchoredPosition += new Vector2(24f, 0f);
            var labelText = label.GetComponent<Text>();
            if (labelText != null)
                labelText.fontStyle = FontStyle.Bold;

            bar.sizeDelta = new Vector2(bar.sizeDelta.x, 14f);
            var bg = bar.GetComponent<Image>();
            var back = UiKit.Load("Art/UI/CoreGameplay/UI/HUD/stat_bar_bg");
            if (bg != null && back != null)
            {
                bg.sprite = back;
                bg.type = Image.Type.Simple;
                bg.color = Color.white;
            }

            var white = UiKit.Load("Art/UI/CoreGameplay/UI/HUD/stat_bar_fill_white");
            if (fill != null && white != null)
            {
                fill.sprite = white;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.color = MoodStyle.ColorOf(mood);
            }
        }

        public void Show(StatsModel stats)
        {
            if (rating != null)
                rating.text = stats.rating;
            if (budget != null)
                budget.text = stats.budget;
            if (episode != null)
                episode.text = stats.episode;
            if (dramaBar != null)
                dramaBar.fillAmount = stats.drama;
            if (trashBar != null)
                trashBar.fillAmount = stats.trash;
            if (familyBar != null)
                familyBar.fillAmount = stats.family;
            Write(_drama, ShowMood.Drama, stats.dramaValue);
            Write(_trash, ShowMood.Trash, stats.trashValue);
            Write(_family, ShowMood.Family, stats.familyValue);
        }

        Text PaintLabel(string childName, ShowMood mood)
        {
            var child = transform.Find(childName);
            if (child == null)
                return null;
            var text = child.GetComponent<Text>();
            if (text == null)
                return null;
            text.color = MoodStyle.ColorOf(mood);
            text.supportRichText = true;
            return text;
        }

        static void Write(Text text, ShowMood mood, int value)
        {
            if (text == null)
                return;
            text.text = MoodStyle.Short(mood) + "  " + value;
        }
    }
}
