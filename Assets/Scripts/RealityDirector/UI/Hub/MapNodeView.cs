using System;
using RealityDirector.Core;
using RealityDirector.Meta;
using RealityDirector.Util;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Карточка узла на карте сезона (префаб MapNode).
    public class MapNodeView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image frame;
        [SerializeField] Image art;
        [SerializeField] Text title;
        [SerializeField] Text subtitle;
        [Tooltip("Тёмная накладка для узлов, которые сейчас не выбрать.")]
        [SerializeField] GameObject dimShade;
        [SerializeField] GameObject selectedRing;
        [SerializeField] GameObject openRing;
        [SerializeField] GameObject doneBadge;
        [SerializeField] GameObject currentBadge;
        [SerializeField] GameObject lockBadge;
        [SerializeField] Text lockLabel;
        [SerializeField] Color lockedColor = new Color(0.3f, 0.3f, 0.33f, 1f);

        Action _onClick;
        Image _shadeImage;
        Image _openImage;
        bool _pulse;

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => _onClick?.Invoke());
            if (dimShade != null)
                _shadeImage = dimShade.GetComponent<Image>();
            if (openRing != null)
                _openImage = openRing.GetComponent<Image>();
            // Подзаголовок с тоном не переносится на арт: шрифт ужимается под ширину.
            if (subtitle != null)
            {
                subtitle.resizeTextForBestFit = true;
                subtitle.resizeTextMinSize = 11;
                subtitle.resizeTextMaxSize = subtitle.fontSize;
                subtitle.verticalOverflow = VerticalWrapMode.Truncate;
            }

            if (title != null)
            {
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 12;
                title.resizeTextMaxSize = title.fontSize;
                // Без обрезки по высоте best fit не ужимает — длинное название налезало на подзаголовок.
                title.verticalOverflow = VerticalWrapMode.Truncate;
                title.fontStyle = FontStyle.Bold;
                UiKit.Shadow(title);
            }

            Dress();
        }

        Image _header;
        Image _medallion;
        RectTransform _window;
        Image _illustration;
        AspectRatioFitter _illustrationFit;

        // Карточка комнаты: тёмная с золотой кромкой, цветная шапка по типу, иконка на светящемся медальоне.
        void Dress()
        {
            UiKit.Dress(frame, UiKit.Frame.Dialog, 1.8f);
            _header = frame != null ? frame.transform.Find("Header")?.GetComponent<Image>() : null;
            if (_header != null)
            {
                var hr = _header.rectTransform;
                hr.anchorMin = new Vector2(0f, 1f);
                hr.anchorMax = new Vector2(1f, 1f);
                hr.pivot = new Vector2(0.5f, 1f);
                hr.offsetMin = new Vector2(5f, -54f);
                hr.offsetMax = new Vector2(-5f, -5f);
            }

            // Иллюстрация комнаты на всё тело карточки под шапкой (обрезка без растяжения).
            _window = UiKit.Rect("Illustration", transform);
            _window.anchorMin = Vector2.zero;
            _window.anchorMax = Vector2.one;
            _window.offsetMin = new Vector2(6f, 6f);
            _window.offsetMax = new Vector2(-6f, -54f);
            _window.gameObject.AddComponent<RectMask2D>();
            _illustration = UiKit.Img("Art", _window, null, Color.white);
            _illustration.rectTransform.anchorMin = _illustration.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _illustrationFit = _illustration.gameObject.AddComponent<AspectRatioFitter>();
            _illustrationFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (frame != null)
                _window.SetSiblingIndex(frame.transform.GetSiblingIndex() + 1);

            if (art != null)
            {
                _medallion = UiKit.Img("Medallion", transform, UiKit.Halo(), Color.white);
                _medallion.transform.SetSiblingIndex(art.transform.GetSiblingIndex());
                var mr = _medallion.rectTransform;
                mr.anchorMin = mr.anchorMax = art.rectTransform.anchorMin;
                mr.anchoredPosition = art.rectTransform.anchoredPosition;
                mr.sizeDelta = art.rectTransform.sizeDelta * 1.7f;
                art.preserveAspect = true;
            }

            if (_openImage != null)
            {
                _openImage.sprite = UiKit.Halo();
                ((RectTransform)openRing.transform).offsetMin = new Vector2(-34f, -34f);
                ((RectTransform)openRing.transform).offsetMax = new Vector2(34f, 34f);
            }

            var ring = selectedRing != null ? selectedRing.GetComponent<Image>() : null;
            if (ring != null)
            {
                UiKit.Dress(ring, UiKit.Frame.GoldTile);
                ((RectTransform)selectedRing.transform).offsetMin = new Vector2(-9f, -9f);
                ((RectTransform)selectedRing.transform).offsetMax = new Vector2(9f, 9f);
            }

            Badge(doneBadge, UiKit.Frame.TealTile);
            Badge(currentBadge, UiKit.Frame.RedTile);
            if (subtitle != null)
                subtitle.color = UiKit.Paper;
        }

        static void Badge(GameObject badge, UiKit.Frame frame)
        {
            if (badge == null)
                return;
            UiKit.Dress(badge.GetComponent<Image>(), frame);
            var text = badge.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.color = UiKit.Paper;
                text.fontStyle = FontStyle.Bold;
            }
        }

        // Доступная комната мягко «дышит» рамкой — видно, куда можно идти.
        void Update()
        {
            if (!_pulse || _openImage == null)
                return;
            var c = _openImage.color;
            c.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
            _openImage.color = c;
        }

        public void SetEnabled(bool on)
        {
            if (button != null)
                button.interactable = on;
        }

        public void Show(MapNode node, MapNodeState state, string lockReason, bool selected, Action onClick)
        {
            _onClick = onClick;
            if (title != null)
                title.text = (node.title ?? "").ToUpperInvariant();
            if (subtitle != null)
            {
                string line = Capital(node.subtitle ?? "");
                if (node.toneGain > 0)
                    line += "  ·  " + MoodStyle.Paint(MoodStyle.Short(node.mood) + " +" + node.toneGain, node.mood);
                subtitle.text = line;
            }
            // Цвет типа — шапка и свечение медальона; тело карточки всегда тёмное, текст читается.
            Color tint = state == MapNodeState.Locked ? lockedColor : node.color;
            if (_header != null)
                _header.color = new Color(tint.r * 0.8f, tint.g * 0.8f, tint.b * 0.8f, 0.95f);
            else if (frame != null)
                frame.color = tint;
            if (_medallion != null)
                _medallion.color = new Color(tint.r, tint.g, tint.b, 0.55f);
            if (_openImage != null)
                _openImage.color = UiKit.Ember;
            // Арт комнаты (поле «Иллюстрация») или иллюстрация её типа; нет и её — значок на медальоне.
            var picture = node.art != null ? node.art : Art(node.kind);
            if (_illustration != null)
            {
                _illustration.sprite = picture;
                _illustration.enabled = picture != null;
                if (picture != null)
                    _illustrationFit.aspectRatio = picture.rect.width / Mathf.Max(1f, picture.rect.height);
                _illustration.color = state == MapNodeState.Locked ? new Color(0.45f, 0.45f, 0.48f, 1f) : Color.white;
            }

            if (_medallion != null)
                _medallion.gameObject.SetActive(picture == null);
            if (art != null)
            {
                art.sprite = Icon(node.kind);
                art.enabled = picture == null;
            }

            // Состояние читается с первого взгляда: можно идти — ярко и «дышит», впереди — приглушено,
            // путь закрыт — почти не видно, снято — с галочкой. Затемнение — непрозрачной накладкой,
            // чтобы линии маршрута не просвечивали сквозь карточку.
            float shade;
            switch (state)
            {
                case MapNodeState.Available:
                case MapNodeState.Current: shade = 0f; break;
                case MapNodeState.Done: shade = 0.35f; break;
                case MapNodeState.Locked: shade = 0.35f; break;
                case MapNodeState.Future: shade = 0.6f; break;
                default: shade = 0.82f; break;
            }

            if (dimShade != null)
            {
                dimShade.SetActive(shade > 0f);
                if (_shadeImage != null)
                    _shadeImage.color = new Color(0.05f, 0.04f, 0.07f, shade);
            }
            if (selectedRing != null)
                selectedRing.SetActive(selected);
            _pulse = state == MapNodeState.Available && !selected;
            if (openRing != null)
                openRing.SetActive(_pulse);
            if (doneBadge != null)
                doneBadge.SetActive(state == MapNodeState.Done);
            if (currentBadge != null)
                currentBadge.SetActive(state == MapNodeState.Current);
            if (lockBadge != null)
                lockBadge.SetActive(state == MapNodeState.Locked);
            if (lockLabel != null)
                lockLabel.text = lockReason ?? "";
        }

        static string Capital(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return s.Substring(0, 1).ToUpperInvariant() + s.Substring(1).ToLowerInvariant();
        }

        // Иллюстрации типов комнат (пак EpisodeMap и Icons_1): Art/UI/EpisodeMap/Nodes/<имя>.png.
        public static Sprite Art(MapNodeKind kind)
        {
            return UiKit.Load("Art/UI/EpisodeMap/Nodes/" + ArtName(kind));
        }

        static string ArtName(MapNodeKind kind)
        {
            switch (kind)
            {
                case MapNodeKind.Start: return "finale";
                case MapNodeKind.Scene: return "scene";
                case MapNodeKind.Confession: return "confession";
                case MapNodeKind.Conflict: return "conflict";
                case MapNodeKind.Date: return "date";
                case MapNodeKind.Challenge: return "challenge";
                case MapNodeKind.Secret: return "secret";
                case MapNodeKind.Sponsor: return "ad";
                case MapNodeKind.Edit: return "montage";
                case MapNodeKind.Elimination: return "elimination";
                case MapNodeKind.Party: return "opening";
                case MapNodeKind.Climax: return "montage";
                case MapNodeKind.Shop: return "marketing";
                default: return "provocation";
            }
        }

        public static Sprite Icon(MapNodeKind kind)
        {
            switch (kind)
            {
                case MapNodeKind.Scene: return IllustratedArt.IconCast;
                case MapNodeKind.Confession: return IllustratedArt.IconTear;
                case MapNodeKind.Conflict: return IllustratedArt.IconAnger;
                case MapNodeKind.Date: return IllustratedArt.IconFamily;
                case MapNodeKind.Challenge: return IllustratedArt.IconFire;
                case MapNodeKind.Secret: return IllustratedArt.IconDevil;
                case MapNodeKind.Sponsor: return IllustratedArt.IconWater;
                case MapNodeKind.Edit: return IllustratedArt.IconPen;
                case MapNodeKind.Elimination: return IllustratedArt.IconDevil;
                case MapNodeKind.Party: return IllustratedArt.IconFire;
                case MapNodeKind.Climax: return IllustratedArt.IconCamera;
                case MapNodeKind.Shop: return IllustratedArt.IconCast;
                case MapNodeKind.Mystery: return IllustratedArt.IconDevil;
                default: return IllustratedArt.IconCamera;
            }
        }
    }
}
