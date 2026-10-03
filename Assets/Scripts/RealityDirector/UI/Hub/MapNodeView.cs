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
            if (frame != null)
                frame.color = state == MapNodeState.Locked ? lockedColor : node.color;
            if (art != null)
                art.sprite = node.art != null ? node.art : Icon(node.kind);

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
