using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    public class PrepCard
    {
        public string id;
        public string title;
        public string hint;
        public int price;
        public bool picked;
        public Color color;
        public Sprite art;
        public ShowMood[] moods;
    }

    public class CrewButton
    {
        public string title;
        public string detail;
        public string costLabel;
        public bool affordable;
        public bool maxed;
    }

    public class PrepModel
    {
        public int episodeNumber;
        public int money;
        public int slots;
        public int picked;
        public int available;
        public bool canStart;
        public string slotsLabel;
        public string reject;
        public CrewButton[] crew;
        public PrepCard[] deck;
        public PrepCard[] shop;
    }
}
