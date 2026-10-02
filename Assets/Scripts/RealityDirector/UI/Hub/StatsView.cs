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
