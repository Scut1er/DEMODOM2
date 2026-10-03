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
        public string unit;
        public bool temporary;
        public bool picked;
        public bool affordable;
        public Color color;
        public Sprite art;
        public ShowMood[] moods;
        public string category;
    }

    public class CrewButton
    {
        public string title;
        public int level;
        public string detail;
        public string costLabel;
        public bool affordable;
        public bool maxed;
    }

    // Правая панель хаба: что даёт зона сейчас и на следующем уровне.
    public class CrewInfo
    {
        public CrewTrack track;
        public string title;
        public int level;
        public string description;
        public string now;
        public string next;
        public string cost;
        public string upgradeLabel;
        public bool affordable;
        public bool maxed;
    }

    public class StatsModel
    {
        public string rating;
        public string budget;
        public string episode;
        public float drama;
        public float trash;
        public float family;
        public int dramaValue;
        public int trashValue;
        public int familyValue;
    }

    public class PrepModel
    {
        public int episodeNumber;
        public int money;
        public string moneyText;
        public string shopFooter;
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
