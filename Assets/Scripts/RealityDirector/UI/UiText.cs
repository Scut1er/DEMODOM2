using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI
{
    // Группа текста для типографики (UiTypography). Размер и начертание текста берутся из группы —
    // менять их у самого Text бесполезно: поменяйте группу или настройки группы.
    [ExecuteAlways]
    [RequireComponent(typeof(Text))]
    [DisallowMultipleComponent]
    public class UiText : MonoBehaviour
    {
        // По умолчанию «не трогать»: добавление компонента ничего не меняет, пока группа не выбрана.
        public TextRole role = TextRole.Custom;

        void OnEnable()
        {
            Apply();
        }

        void OnValidate()
        {
            Apply();
        }

        public void Apply()
        {
            if (role != TextRole.Custom)
                UiTypography.Apply(GetComponent<Text>(), role);
        }
    }
}
