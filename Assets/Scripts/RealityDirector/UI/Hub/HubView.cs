using System;
using System.Collections.Generic;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Продакшн-хаб: фон с зонами, панель зоны, участники, показатели, старт съёмки.
    public class HubView : MonoBehaviour
    {
        [Header("Шапка")]
        [SerializeField] Text subtitle;
        [SerializeField] StatsView stats;

        [Header("Зоны и панель")]
        [SerializeField] ZoneButtonView[] zones = new ZoneButtonView[3];
        [SerializeField] CrewDetailView detail;
        [SerializeField] RosterView roster;

        [Header("Задачи")]
        [SerializeField] GameObject tasksPanel;
        [SerializeField] Text tasks;

        [Header("Низ")]
        [SerializeField] Button start;
        [SerializeField] Text startCaption;
        [SerializeField] Button deckButton;
        [SerializeField] Text deckButtonLabel;
        [SerializeField] Button shopButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button menuButton;

        [SerializeField] DeckPanelView deck;

        public event Action<CrewTrack> Upgrade;
        public event Action<string> Toggle;
        public event Action<string> Buy;
        public event Action StartShoot;
        public event Action OpenSettings;
        public event Action Menu;
        public event Action<int> OpenDeck;
        public event Action CloseDeck;

        CrewTrack _selected = CrewTrack.Cast;

        public CrewTrack Selected => _selected;
        public DeckPanelView Deck => deck;

        void Awake()
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null)
                    zones[i].Clicked += t =>
                    {
                        _selected = t;
                        Select?.Invoke();
                    };
            }

            if (detail != null)
                detail.Upgrade += t => Upgrade?.Invoke(t);
            if (roster != null)
                roster.Invite += () =>
                {
                    _selected = CrewTrack.Cast;
                    Select?.Invoke();
                };
            if (deck != null)
            {
                deck.Toggle += id => Toggle?.Invoke(id);
                deck.Buy += id => Buy?.Invoke(id);
                deck.Close += () => CloseDeck?.Invoke();
            }

            if (start != null)
                start.onClick.AddListener(() => StartShoot?.Invoke());
            if (deckButton != null)
                deckButton.onClick.AddListener(() => OpenDeck?.Invoke(DeckPanelView.DeckTab));
            if (shopButton != null)
                shopButton.onClick.AddListener(() => OpenDeck?.Invoke(DeckPanelView.ShopTab));
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => OpenSettings?.Invoke());
            if (menuButton != null)
                menuButton.onClick.AddListener(() => Menu?.Invoke());
        }

        // Выбор зоны — перерисовка целиком, её делает HubFlow.
        public event Action Select;

        public void Show(PrepModel prep, StatsModel statsModel, CrewInfo[] crew, CastMember[] members, int castLevel, IList<string> taskLines)
        {
            if (subtitle != null)
                subtitle.text = "Сезон 1  ·  Выпуск " + prep.episodeNumber + "  ·  Продакшн-хаб";
            if (stats != null)
                stats.Show(statsModel);

            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] == null)
                    continue;
                var info = crew[(int)zones[i].Track];
                zones[i].Show(info.level, zones[i].Track == _selected);
            }

            if (detail != null)
                detail.Show(crew[(int)_selected]);
            if (roster != null)
                roster.Show(members, castLevel);

            bool hasTasks = taskLines != null && taskLines.Count > 0;
            if (tasksPanel != null)
                tasksPanel.SetActive(hasTasks);
            if (tasks != null)
            {
                var body = "ЗАДАЧИ ЗРИТЕЛЕЙ";
                if (hasTasks)
                {
                    for (int i = 0; i < taskLines.Count; i++)
                        body += "\n★  " + taskLines[i];
                }

                tasks.text = body;
            }

            if (deckButtonLabel != null)
                deckButtonLabel.text = "Колода " + prep.picked + "/" + prep.slots;
            if (shopButton != null)
                shopButton.gameObject.SetActive(true);
            if (startCaption != null)
                startCaption.text = "Выпуск " + prep.episodeNumber + "  ·  дальше карта эпизода";

            if (deck != null && deck.IsOpen)
                deck.Show(prep);
        }

        public void SetStartCaption(string text)
        {
            if (startCaption != null)
                startCaption.text = text;
        }

        public void SetSubtitle(string text)
        {
            if (subtitle != null)
                subtitle.text = text;
        }

        public RectTransform ZoneFocus(CrewTrack track)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null && zones[i].Track == track)
                    return zones[i].transform as RectTransform;
            }

            return null;
        }

        public RectTransform DeckFocus()
        {
            return deckButton != null ? deckButton.transform as RectTransform : null;
        }

        public RectTransform ShopFocus()
        {
            return shopButton != null ? shopButton.transform as RectTransform : null;
        }

        public RectTransform StartFocus()
        {
            return start != null ? start.transform as RectTransform : null;
        }

        // Обучение: пока босс говорит — ничего. После речи на хабе жива только кнопка старта.
        public void ApplyTutorial(bool teach, bool talking, bool startReady)
        {
            bool chrome = !teach;
            bool canStart = !teach || (startReady && !talking);
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null)
                    zones[i].SetEnabled(!teach && !talking);
            }

            if (detail != null)
                detail.SetUpgradeEnabled(chrome && !talking);
            if (deckButton != null)
                deckButton.interactable = chrome && !talking;
            if (shopButton != null)
                shopButton.interactable = chrome && !talking;
            if (settingsButton != null)
                settingsButton.interactable = chrome;
            if (menuButton != null)
                menuButton.interactable = chrome;
            if (start != null)
                start.interactable = canStart;
            if (deck != null)
                deck.SetLocked(teach && talking);
        }
    }
}
