using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Строка участника в списке хаба (префаб CastRow).
    public class CastRowView : MonoBehaviour
    {
        [SerializeField] Image portrait;
        [SerializeField] Text displayName;
        [SerializeField] Text traits;
        [SerializeField] GameObject lockedShade;

        public void Show(string name, string traitLines, Sprite sprite, bool locked)
        {
            if (displayName != null)
                displayName.text = name;
            if (traits != null)
                traits.text = traitLines;
            if (portrait != null)
            {
                portrait.sprite = sprite;
                portrait.enabled = sprite != null;
            }

            if (lockedShade != null)
                lockedShade.SetActive(locked);
        }
    }
}
