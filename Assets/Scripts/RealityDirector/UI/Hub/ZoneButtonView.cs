using System;
using RealityDirector.Meta;
using RealityDirector.Util;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Карточка-станция хаба (Кастинг / Съёмочная / Сценарная): иконка, уровень ромбиками, цена следующего.
    public class ZoneButtonView : MonoBehaviour
    {
        public static readonly Vector2 CardSize = new Vector2(340f, 270f);

        [SerializeField] CrewTrack track;
        [SerializeField] Button button;
        [SerializeField] Image icon;
        [SerializeField] Text level;
        [SerializeField] GameObject selected;

        public event Action<CrewTrack> Clicked;

        public CrewTrack Track => track;

        Text _title;
        Text _ribbon;
        Image _ribbonPlate;
        RectTransform _pips;
        bool _built;

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => Clicked?.Invoke(track));
            Build();
        }

        // Табличку из сцены перекладываем в карточку: панель с рамкой, крупная иконка, заголовок, ромбики.
        void Build()
        {
            if (_built)
                return;
            _built = true;
            var rt = (RectTransform)transform;
            rt.sizeDelta = CardSize;

            var plate = transform.Find("Plate")?.GetComponent<Image>();
            UiKit.Dress(plate, UiKit.Frame.Gold, 1.1f);
            if (button != null && plate != null)
                button.targetGraphic = plate;

            if (selected != null)
            {
                // Выбранная станция — яркая золотая кайма вокруг карточки.
                var ring = selected.GetComponent<Image>();
                UiKit.Dress(ring, UiKit.Frame.GoldTile, 0.7f);
                UiKit.Stretch((RectTransform)selected.transform, -9f);
            }

            if (icon != null)
            {
                UiKit.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(112f, 112f));
                icon.preserveAspect = true;
                Paint();
            }

            _title = transform.Find("Title")?.GetComponent<Text>();
            if (_title != null)
            {
                UiKit.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(CardSize.x - 32f, 44f));
                if (UiKit.Display != null)
                    _title.font = UiKit.Display;
                _title.fontSize = 34;
                _title.alignment = TextAnchor.MiddleCenter;
                _title.color = UiKit.Gold;
                UiKit.Shadow(_title);
            }

            if (level != null)
            {
                UiKit.Place(level.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(CardSize.x - 32f, 24f));
                level.alignment = TextAnchor.MiddleCenter;
                level.fontSize = 16;
                level.color = UiKit.Muted;
            }

            _pips = UiKit.Pips(transform, "Pips", 1, Progression.MaxLevel, UiKit.Gold, 13f, 9f);
            UiKit.Place(_pips, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), new Vector2(Progression.MaxLevel * 22f, 16f));
            _pips.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

            _ribbonPlate = UiKit.Img("Ribbon", transform, null, Color.white);
            UiKit.Dress(_ribbonPlate, UiKit.Frame.Dark);
            UiKit.Place(_ribbonPlate.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(CardSize.x - 40f, 34f));
            _ribbon = UiKit.Txt("Label", _ribbonPlate.transform, "", 16, UiKit.Muted, TextAnchor.MiddleCenter);
            UiKit.Stretch(_ribbon.rectTransform);
            _ribbon.fontStyle = FontStyle.Bold;
        }

        void Paint()
        {
            // Своя иконка в сцене важнее пака.
            if (icon.sprite != null)
                return;
            Sprite s;
            switch (track)
            {
                case CrewTrack.Cast:
                    s = GameArt.Head("npc_kira", Face.Neutral) ?? IllustratedArt.IconCast;
                    var second = GameArt.Head("npc_max", Face.Neutral);
                    if (second != null && icon.transform.Find("Second") == null)
                    {
                        var other = UiKit.Img("Second", icon.transform, second, Color.white);
                        other.preserveAspect = true;
                        UiKit.Place(other.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46f, -6f), new Vector2(96f, 96f));
                        other.transform.SetAsFirstSibling();
                        icon.rectTransform.anchoredPosition += new Vector2(-24f, 0f);
                    }

                    break;
                case CrewTrack.Operators:
                    s = UiKit.Icon("icon_camera") ?? IllustratedArt.IconCamera;
                    break;
                default:
                    s = UiKit.Icon("icon_library") ?? IllustratedArt.IconPen;
                    break;
            }

            icon.sprite = s;
            icon.color = track == CrewTrack.Cast ? Color.white : UiKit.Gold;
        }

        public void SetEnabled(bool on)
        {
            if (button != null)
                button.interactable = on;
        }

        public void Show(int lvl, bool isSelected)
        {
            if (level != null)
                level.text = "уровень " + lvl + " из " + Progression.MaxLevel;
            if (_pips != null)
                UiKit.Pips(transform, "Pips", lvl, Progression.MaxLevel, UiKit.Gold, 13f, 9f);
            SetChosen(isSelected);
        }

        // Полная карточка: цена следующего уровня и можно ли его взять прямо сейчас.
        public void Show(CrewInfo info, bool isSelected)
        {
            int max = info.maxLevel > 0 ? info.maxLevel : Progression.MaxLevel;
            if (level != null)
                level.text = "уровень " + info.level + " из " + max;
            if (_pips != null)
                UiKit.Pips(transform, "Pips", info.level, max, UiKit.Gold, 13f, 9f);
            SetChosen(isSelected);
            if (_ribbon == null)
                return;
            if (info.maxed)
            {
                _ribbon.text = "МАКСИМУМ";
                _ribbon.color = UiKit.Gold;
                UiKit.Dress(_ribbonPlate, UiKit.Frame.GoldTile);
            }
            else if (info.affordable)
            {
                _ribbon.text = "▲  УЛУЧШИТЬ  ·  " + info.cost;
                _ribbon.color = UiKit.Paper;
                UiKit.Dress(_ribbonPlate, UiKit.Frame.RedTile);
            }
            else
            {
                _ribbon.text = "след. уровень  ·  " + info.cost;
                _ribbon.color = UiKit.Muted;
                UiKit.Dress(_ribbonPlate, UiKit.Frame.Dark);
            }
        }

        public void SetChosen(bool on)
        {
            if (selected != null && selected.activeSelf != on)
                selected.SetActive(on);
        }
    }
}
