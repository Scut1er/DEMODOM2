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
        [Tooltip("Настройки генерации карты сезона. Пусто — встроенные по умолчанию.")]
        [SerializeField] SeasonMapConfig mapConfig;

        PitchContent _content;
        MetaService _meta;
        MapService _map;
        MapGraph _graph;
        MapNode _selected;
        CastMember[] _cast;
        GameObject _back;

        void Awake()
        {
            _content = PitchContent.Create();
            _cast = CastRoster.All();
            if (mapConfig == null)
                mapConfig = SeasonMapConfig.CreateDefault();
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
            hub.CloseDeck += () =>
            {
                Click();
                bool leavingShop = hub.Deck.ShopOpen;
                hub.Deck.Hide();
                if (leavingShop)
                    LeaveShop();
                else
                    RefreshHub();
            };
            hub.StartShoot += OpenMap;
            hub.OpenSettings += () => { Click(); ShowSettings(hub.gameObject); };
            hub.Menu += () => { Click(); ShowMenu(); };

            map.Select += id => { Click(); _selected = _graph.Find(id); RefreshMap(); };
            // Выбор карт для съёмки — прямо на карте сезона.
            map.Deck.Toggle += id =>
            {
                bool ok = _meta.TogglePick(id);
                Sfx.Play(ok ? Cue.Click : Cue.Miss, ok ? 0.3f : 0.45f);
                if (ok)
                    GameSession.Save();
                map.Deck.Show(_meta.BuildPrep());
            };
            map.Deck.Close += () => { Click(); BeginFilming(_selected); };
            map.Deck.Cancel += () => { Click(); map.Deck.Hide(); };
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
            var state = GameSession.State;
            _meta = new MetaService(state, GameSession.Tone, _content.All);

            // Карта строится из seed: тот же seed + конфиг = та же карта, поэтому «Продолжить» её восстанавливает.
            if (state.mapSeed == 0)
                state.mapSeed = Random.Range(1, int.MaxValue);
            int seed = mapConfig.seed != 0 ? mapConfig.seed : state.mapSeed;
            _graph = MapGenerator.Generate(mapConfig, seed);
            state.mapFloors = _graph.Layers;
            _map = new MapService(state, GameSession.Tone, _graph);
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

        // Карта открыта всегда; колода нужна только для узла-съёмки.
        void OpenMap()
        {
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

        // Кнопка действия на карте: зайти в выбранный узел по его типу.
        void Shoot()
        {
            if (!_map.CanEnter(_selected))
            {
                Sfx.Play(Cue.Miss, 0.4f);
                return;
            }

            switch (_selected.type)
            {
                case MapNodeType.Shop: EnterShop(_selected); break;
                case MapNodeType.RandomEvent: RunRandomEvent(_selected); break;
                case MapNodeType.Editing: RunEditing(_selected); break;
                default: StartFilming(_selected); break;
            }
        }

        // Съёмка: сначала выбор карт прямо на карте, «Начать съёмку» — сразу в квартиру.
        void StartFilming(MapNode node)
        {
            _meta.TrimPicked();
            _meta.ClearReject();
            map.Deck.Open(DeckPanelView.DeckTab, _meta.BuildPrep());
        }

        void BeginFilming(MapNode node)
        {
            if (node == null || !_map.CanEnter(node) || !_meta.CanStart())
            {
                Sfx.Play(Cue.Miss, 0.4f);
                return;
            }

            if (!_map.Choose(node))
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
            GameSession.Embarked = true;
            GameSession.SceneTitle = node.title;
            GameSession.Save();
            SceneFlow.ToEpisode();
        }

        // Магазин: окно колоды с вкладкой магазина. «Готово» закрывает шаг (LeaveShop).
        void EnterShop(MapNode node)
        {
            if (!_map.Choose(node))
            {
                Sfx.Play(Cue.Miss, 0.4f);
                return;
            }

            Sfx.Play(Cue.Coin, 0.4f);
            GameSession.Save();
            ShowHub();
            hub.Deck.Open(DeckPanelView.ShopTab, _meta.BuildPrep(), true);
        }

        void LeaveShop()
        {
            _map.CompleteStep();
            GameSession.Save();
            AfterStep();
        }

        // Случайное событие — пока не реализовано: шаг засчитывается сразу.
        void RunRandomEvent(MapNode node)
        {
            if (!_map.Choose(node))
                return;
            // TODO: здесь будет экран события (выбор вариантов, эффекты на героев/бюджет/тон).
            Sfx.Play(Cue.Blip, 0.5f);
            _map.CompleteStep();
            GameSession.Save();
            AfterStep("Случайное событие", "Пока не реализовано — шаг засчитан. Здесь будет выбор с последствиями.");
        }

        // Монтаж — пока не реализовано: шаг засчитывается сразу.
        void RunEditing(MapNode node)
        {
            if (!_map.Choose(node))
                return;
            // TODO: здесь будет перемонтаж отснятых кадров (улучшение оценки прошлых выпусков).
            Sfx.Play(Cue.Blip, 0.5f);
            _map.CompleteStep();
            GameSession.Save();
            AfterStep("Монтаж", "Пока не реализовано — шаг засчитан. Здесь будет перемонтаж отснятого.");
        }

        // После не-съёмочного шага: конец сезона или снова карта (с пояснением, если есть).
        void AfterStep(string noticeTitle = null, string noticeBody = null)
        {
            if (GameSession.SeasonOver)
            {
                ShowSeasonEnd();
                return;
            }

            _selected = null;
            Show(map.gameObject);
            RefreshMap();
            if (noticeTitle != null)
                map.Notice(noticeTitle, noticeBody);
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
