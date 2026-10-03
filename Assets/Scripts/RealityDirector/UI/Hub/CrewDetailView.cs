using System;
using RealityDirector.Meta;
using RealityDirector.Util;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Правая панель хаба: описание выбранной зоны и кнопка апгрейда.
    public class CrewDetailView : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Text title;
        [SerializeField] Text level;
        [SerializeField] Text description;
        [SerializeField] Text now;
        [SerializeField] Text next;
        [SerializeField] Text cost;
        [SerializeField] Button upgrade;
        [SerializeField] Text upgradeLabel;
        [Tooltip("Свои иконки по веткам: Cast, Operators, Writers. Пусто — заглушка.")]
        [SerializeField] Sprite[] icons = new Sprite[3];

        public event Action<CrewTrack> Upgrade;

        CrewTrack _track;

        void Awake()
        {
            if (upgrade != null)
                upgrade.onClick.AddListener(() => Upgrade?.Invoke(_track));
        }

        public void Show(CrewInfo info)
        {
            _track = info.track;
            if (icon != null)
                icon.sprite = Icon(info.track);
            if (title != null)
                title.text = info.title;
            if (level != null)
                level.text = "Уровень " + info.level + " из " + (info.maxLevel > 0 ? info.maxLevel : Progression.MaxLevel);
            if (description != null)
                description.text = info.description;
            if (now != null)
                now.text = info.now;
            if (next != null)
                next.text = info.next;
            if (cost != null)
                cost.text = info.cost;
            if (upgradeLabel != null)
                upgradeLabel.text = info.upgradeLabel;
            if (upgrade != null)
                upgrade.interactable = info.affordable && _upgradeOpen;
        }

        bool _upgradeOpen = true;

        public void SetUpgradeEnabled(bool on)
        {
            _upgradeOpen = on;
            if (upgrade != null && !on)
                upgrade.interactable = false;
        }

        Sprite Icon(CrewTrack track)
        {
            int i = (int)track;
            if (icons != null && i < icons.Length && icons[i] != null)
                return icons[i];
            switch (track)
            {
                case CrewTrack.Cast: return IllustratedArt.IconCast;
                case CrewTrack.Operators: return IllustratedArt.IconCamera;
                default: return IllustratedArt.IconPen;
            }
        }
    }
}
