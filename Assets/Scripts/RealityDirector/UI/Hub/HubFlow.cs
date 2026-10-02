using RealityDirector.Core;
using RealityDirector.Meta;
using RealityDirector.Persistence;
using RealityDirector.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace RealityDirector.UI.Hub
{
    // Главное меню → хаб → карта сезона → квартира → снова хаб.
    public class HubFlow : MonoBehaviour
    {
        [SerializeField] MainMenuView menu;
        [SerializeField] IntroView intro;
        [SerializeField] HubView hub;
        [SerializeField] MapView map;
        [SerializeField] SeasonEndView seasonEnd;
        [SerializeField] SettingsView settings;
        [Tooltip("Карта сезона. Пусто — встроенная по умолчанию.")]
        [SerializeField] EpisodeMap episodeMap;

        PitchContent _content;
        MetaService _meta;
        MapService _map;
        MapNode _selected;
        CastMember[] _cast;
        GameObject _back;

        void Awake()
        {
            _content = PitchContent.Create();
            _cast = CastRoster.All();
            if (episodeMap == null)
                episodeMap = EpisodeMap.CreateDefault();
            EnsureEventSystem();
            Sfx.Bind(gameObject);

            menu.NewSeason += () => { Click(); BeginSeason(); };
            menu.Continue += () => { Click(); ContinueSeason(); };
            menu.Settings += () => { Click(); ShowSettings(menu.gameObject); };
            menu.Quit += Quit;
            intro.Next += () => { Click(); ShowHub(); };
            settings.Back += () => { Click(); Show(_back != null ? _back : menu.gameObject); if (_back == hub.gameObject) ShowHub(); };
            seasonEnd.NewSeason += () => { Click(); BeginSeason(); };
            seasonEnd.Menu += () => { Click(); ShowMenu(); };

            hub.Select += () => { Click(); RefreshHub(); };
            hub.Upgrade += track => Act(_meta.TryUpgrade(track), Cue.Coin, 0.45f, 0.9f);
            hub.Toggle += id => Act(_meta.TogglePick(id), Cue.Click, 0.3f);
            hub.Buy += id => Act(_meta.TryBuy(id), Cue.Coin, 0.5f);
            hub.OpenDeck += tab => { Click(); _meta.ClearReject(); hub.Deck.Open(tab, _meta.BuildPrep()); };
            hub.CloseDeck += () => { Click(); hub.Deck.Hide(); RefreshHub(); };
            hub.StartShoot += OpenMap;
            hub.OpenSettings += () => { Click(); ShowSettings(hub.gameObject); };
            hub.Menu += () => { Click(); ShowMenu(); };

            map.Select += id => { Click(); _selected = episodeMap.Find(id); RefreshMap(); };
            map.Random += PickRandom;
            map.Back += () => { Click(); ShowHub(); };
            map.Shoot += Shoot;
        }

        void Start()
        {
            // Вернулись из квартиры — сразу в хаб или к итогам сезона.
            if (GameSession.Active)
            {
                Bind();
                if (GameSession.SeasonOver)
                    ShowSeasonEnd();
                else
                    ShowHub();
                return;
            }

            ShowMenu();
        }

        void OnDestroy()
        {
            if (_content != null)
                _content.DestroyAssets();
        }

        void BeginSeason()
        {
            GameSession.NewSeason(_content.StarterIds());
            Bind();
            Show(intro.gameObject);
        }

        void ContinueSeason()
        {
            if (!GameSession.Continue())
            {
                ShowMenu();
                return;
            }

            Bind();
            if (GameSession.SeasonOver)
                ShowSeasonEnd();
            else
                ShowHub();
        }

        void Bind()
        {
            _meta = new MetaService(GameSession.State, GameSession.Tone, _content.All);
            _map = new MapService(GameSession.State, GameSession.Tone, episodeMap);
        }

        void ShowMenu()
        {
            Show(menu.gameObject);
            menu.Show(SaveSystem.HasSave);
        }

        void ShowSettings(GameObject backTo)
        {
            _back = backTo;
            Show(settings.gameObject);
            settings.Show();
        }

        void ShowHub()
        {
            _meta.TrimPicked();
            _meta.ClearReject();
            Show(hub.gameObject);
            hub.Deck.Hide();
            RefreshHub();
        }

        void RefreshHub()
        {
            var crew = new[]
            {
                _meta.Crew(CrewTrack.Cast),
                _meta.Crew(CrewTrack.Operators),
                _meta.Crew(CrewTrack.Writers)
            };
            hub.Show(_meta.BuildPrep(), _meta.Stats(), crew, _cast, _meta.CastLevel, _meta.TaskLines());
        }

        void OpenMap()
        {
            if (!_meta.CanStart())
            {
                Sfx.Play(Cue.Miss, 0.4f);
                hub.Deck.Open(DeckPanelView.DeckTab, _meta.BuildPrep());
                return;
            }

            Click();
            _selected = _map.CurrentChoice;
            Show(map.gameObject);
            RefreshMap();
        }

        void RefreshMap()
        {
            map.Show(_map, _selected, _meta.Stats(), GameSession.State.episodeIndex + 1, _meta.TaskLines());
        }

        void PickRandom()
        {
            var open = _map.Available();
            if (open.Count == 0)
            {
                Sfx.Play(Cue.Miss, 0.4f);
                return;
            }

            Sfx.Play(Cue.Blip, 0.5f);
            _selected = open[Random.Range(0, open.Count)];
            RefreshMap();
        }

        void Shoot()
        {
            if (!_map.CanShoot(_selected) || !_map.Choose(_selected))
            {
                Sfx.Play(Cue.Miss, 0.4f);
                return;
            }

            if (!_meta.TryEmbark(GameSession.Hand))
            {
                // Колода развалилась (например, после правок в другом окне) — назад собирать.
                GameSession.Save();
                Sfx.Play(Cue.Miss, 0.4f);
                ShowHub();
                return;
            }

            Sfx.Play(Cue.Card, 0.45f, 0.8f);
            GameSession.SceneTitle = _selected.title;
            GameSession.Save();
            SceneFlow.ToEpisode();
        }

        void ShowSeasonEnd()
        {
            Show(seasonEnd.gameObject);
            seasonEnd.Show(_meta.SeasonEndText());
            GameSession.EndSeason();
        }

        void Act(bool ok, Cue cue, float volume, float pitch = 1f)
        {
            Sfx.Play(ok ? cue : Cue.Miss, ok ? volume : 0.45f, ok ? pitch : 1f);
            if (ok)
                GameSession.Save();
            RefreshHub();
        }

        void Show(GameObject screen)
        {
            menu.gameObject.SetActive(screen == menu.gameObject);
            intro.gameObject.SetActive(screen == intro.gameObject);
            hub.gameObject.SetActive(screen == hub.gameObject);
            map.gameObject.SetActive(screen == map.gameObject);
            seasonEnd.gameObject.SetActive(screen == seasonEnd.gameObject);
            settings.gameObject.SetActive(screen == settings.gameObject);
        }

        static void Click()
        {
            Sfx.Play(Cue.Click, 0.3f);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            if (InputSystem.actions != null)
                module.actionsAsset = InputSystem.actions;
        }
    }
}
