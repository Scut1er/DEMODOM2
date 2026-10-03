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

        bool _dressed;

        void Dress()
        {
            if (_dressed)
                return;
            _dressed = true;
            UiKit.Dress(GetComponent<Image>(), UiKit.Frame.Dark);
            UiKit.Dress(transform.Find("PortraitFrame")?.GetComponent<Image>(), UiKit.Frame.Actor);
            if (displayName != null)
                displayName.fontStyle = FontStyle.Bold;
        }

        public void Show(string name, string traitLines, Sprite sprite, bool locked)
        {
            Dress();
            if (displayName != null)
                displayName.color = locked ? UiKit.Faint : UiKit.Paper;
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
