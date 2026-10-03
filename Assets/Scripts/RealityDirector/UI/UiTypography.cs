using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI
{
    // Группа текста. Внутри группы у всех текстов один размер и начертание.
    public enum TextRole
    {
        [InspectorName("Заголовок экрана")] Title,
        [InspectorName("Заголовок блока")] Heading,
        [InspectorName("Основной текст")] Body,
        [InspectorName("Кнопка")] Button,
        [InspectorName("Подпись / строка данных")] Label,
        [InspectorName("Мелкий текст / пояснение")] Caption,
        [InspectorName("Поле ввода")] Input,
        [InspectorName("Не трогать (логотип и особые)")] Custom
    }

    // Типографика всей игры: размер и начертание для каждой группы текста. Один ассет — Resources/UiTypography.
    // Тексты сцены получают группу компонентом UiText, экраны из кода — через Apply/ForSize.
    [CreateAssetMenu(menuName = "RealityDirector/UI Typography", fileName = "UiTypography")]
    public class UiTypography : ScriptableObject
    {
        [System.Serializable]
        public class Style
        {
            [Min(8)] public int size = 20;
            public FontStyle fontStyle = FontStyle.Normal;
            [Tooltip("Межстрочный интервал.")]
            [Min(0.5f)] public float lineSpacing = 1f;
        }

        [Tooltip("Крупные заголовки экранов: «СЦЕНАРИЙ», «ТРУДОВОЙ ДОГОВОР».")]
        public Style title = new Style { size = 40, fontStyle = FontStyle.Bold };
        [Tooltip("Заголовки панелей и блоков.")]
        public Style heading = new Style { size = 28, fontStyle = FontStyle.Bold };
        [Tooltip("Абзацы текста.")]
        public Style body = new Style { size = 22, lineSpacing = 1.1f };
        [Tooltip("Надписи на кнопках.")]
        public Style button = new Style { size = 20, fontStyle = FontStyle.Bold };
        [Tooltip("Подписи, строки данных, статусы.")]
        public Style label = new Style { size = 19 };
        [Tooltip("Мелкие пояснения и подсказки.")]
        public Style caption = new Style { size = 16 };
        [Tooltip("Текст в полях ввода.")]
        public Style input = new Style { size = 24 };

        static UiTypography _current;
        static UiTypography _fallback;

        public static UiTypography Current
        {
            get
            {
                if (_current == null)
                    _current = Resources.Load<UiTypography>("UiTypography");
                if (_current != null)
                    return _current;
                if (_fallback == null)
                {
                    _fallback = CreateInstance<UiTypography>();
                    _fallback.hideFlags = HideFlags.DontSave;
                }

                return _fallback;
            }
        }

        public Style Get(TextRole role)
        {
            switch (role)
            {
                case TextRole.Title: return title;
                case TextRole.Heading: return heading;
                case TextRole.Body: return body;
                case TextRole.Button: return button;
                case TextRole.Label: return label;
                case TextRole.Caption: return caption;
                case TextRole.Input: return input;
                default: return null;
            }
        }

        // Группа по прежнему «ручному» размеру — для экранов, собранных кодом.
        public static TextRole ForSize(int size)
        {
            if (size >= 34)
                return TextRole.Title;
            if (size >= 26)
                return TextRole.Heading;
            if (size >= 21)
                return TextRole.Body;
            if (size >= 18)
                return TextRole.Label;
            return TextRole.Caption;
        }

        public static void Apply(Text text, TextRole role)
        {
            if (text == null)
                return;
            var style = Current.Get(role);
            if (style == null)
                return;
            text.fontSize = style.size;
            text.fontStyle = style.fontStyle;
            text.lineSpacing = style.lineSpacing;
        }
    }
}
