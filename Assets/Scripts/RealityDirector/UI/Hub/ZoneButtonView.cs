using System;
using RealityDirector.Meta;
using RealityDirector.Util;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Табличка-зона поверх фона хаба (Кастинг / Съёмочная / Сценарная). Ставится руками над артом.
    public class ZoneButtonView : MonoBehaviour
    {
        [SerializeField] CrewTrack track;
        [SerializeField] Button button;
        [SerializeField] Image icon;
        [SerializeField] Text level;
        [SerializeField] GameObject selected;

        public event Action<CrewTrack> Clicked;

        public CrewTrack Track => track;

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => Clicked?.Invoke(track));
            // Своя иконка в префабе/сцене важнее заглушки.
            if (icon != null && icon.sprite == null)
            {
                switch (track)
                {
                    case CrewTrack.Cast: icon.sprite = IllustratedArt.IconCast; break;
                    case CrewTrack.Operators: icon.sprite = IllustratedArt.IconCamera; break;
                    default: icon.sprite = IllustratedArt.IconPen; break;
                }
            }
        }

        public void SetEnabled(bool on)
        {
            if (button != null)
                button.interactable = on;
        }

        public void Show(int lvl, bool isSelected)
        {
            if (level != null)
                level.text = "Уровень " + lvl;
            SetChosen(isSelected);
        }

        public void SetChosen(bool on)
        {
            if (selected != null && selected.activeSelf != on)
                selected.SetActive(on);
        }
    }
}
