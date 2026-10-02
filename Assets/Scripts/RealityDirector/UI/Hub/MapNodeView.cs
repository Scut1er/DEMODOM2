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

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => _onClick?.Invoke());
        }

        public void Show(MapNode node, MapNodeState state, string lockReason, bool selected, Action onClick)
        {
            _onClick = onClick;
            if (title != null)
                title.text = node.title;
            if (subtitle != null)
            {
                string line = node.subtitle ?? "";
                if (node.toneGain > 0)
                    line += "  ·  " + MoodStyle.Paint(MoodStyle.Short(node.mood) + " +" + node.toneGain, node.mood);
                subtitle.text = line;
            }
            if (frame != null)
                frame.color = state == MapNodeState.Locked ? lockedColor : node.color;
            if (art != null)
                art.sprite = node.art != null ? node.art : Icon(node.kind);

            bool live = state == MapNodeState.Available || state == MapNodeState.Current || state == MapNodeState.Done
                        || state == MapNodeState.Locked;
            if (dimShade != null)
                dimShade.SetActive(!live);
            if (selectedRing != null)
                selectedRing.SetActive(selected);
            if (openRing != null)
                openRing.SetActive(state == MapNodeState.Available && !selected);
            if (doneBadge != null)
                doneBadge.SetActive(state == MapNodeState.Done);
            if (currentBadge != null)
                currentBadge.SetActive(state == MapNodeState.Current);
            if (lockBadge != null)
                lockBadge.SetActive(state == MapNodeState.Locked);
            if (lockLabel != null)
                lockLabel.text = lockReason ?? "";
        }

        static Sprite Icon(MapNodeKind kind)
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
