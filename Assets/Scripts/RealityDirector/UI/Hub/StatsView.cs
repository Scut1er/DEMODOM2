using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Плашка показателей (правый верх хаба и карты).
    public class StatsView : MonoBehaviour
    {
        [SerializeField] Text rating;
        [SerializeField] Text budget;
        [SerializeField] Text episode;
        [Tooltip("Image с типом Filled — fillAmount 0..1")]
        [SerializeField] Image dramaBar;
        [SerializeField] Image trashBar;
        [SerializeField] Image familyBar;

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
        }
    }
}
