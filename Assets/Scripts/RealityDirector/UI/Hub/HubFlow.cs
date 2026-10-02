using System.Collections.Generic;
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
    // Главное меню → хаб → карта выпуска → комнаты (съёмка в квартире, событие, маркетинг) → монтаж → хаб.
    public class HubFlow : MonoBehaviour
    {
        [SerializeField] MainMenuView menu;
        [SerializeField] IntroView intro;
        [SerializeField] HubView hub;
        [SerializeField] MapView map;
        [SerializeField] SeasonEndView seasonEnd;
        [SerializeField] SettingsView settings;
        [Tooltip("Настройки сезона: число выпусков, карты выпусков, лимиты. Пусто — встроенные по умолчанию.")]
        [SerializeField] SeasonConfig season;

        PitchContent _content;
        MetaService _meta;
        EpisodeService _episode;
        MapNode _selected;
        string _hubNotice;
        CastMember[] _cast;
        GameObject _back;

        void Awake()
        {
            _content = PitchContent.Create();
            _cast = CastRoster.All();
            if (season == null)
                season = SeasonConfig.CreateDefault();
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
            hub.StartShoot += () => { Click(); StartOrResumeEpisode(); };
            hub.OpenSettings += () => { Click(); ShowSettings(hub.gameObject); };
            hub.Menu += () => { Click(); ShowMenu(); };

            map.Select += id => { Click(); _selected = _episode.Map.Map.Find(id); RefreshMap(); };
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
            if (!GameSession.Active)
            {
                ShowMenu();
                return;
            }

            Bind();
            // Квартира закрыла сцену. Карту выпуска открывает Resume (выпуск идёт), комнату закрывает RoomNodeId.
            GameSession.ReturnToMap = false;
            // Вернулись со съёмки — комната пройдена, дальше по карте выпуска.
            if (!string.IsNullOrEmpty(GameSession.RoomNodeId) && _episode.Active)
            {
                GameSession.RoomNodeId = null;
                _episode.FinishSceneRoom();
                GameSession.Save();
                AfterStep();
                return;
            }

            GameSession.RoomNodeId = null;
            Resume();
        }

        void OnDestroy()
        {
            if (_content != null)
                _content.DestroyAssets();
            _episode?.Dispose();
        }

        void BeginSeason()
        {
            GameSession.NewSeason(_content.StarterIds(), season);
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
            Resume();
        }

        // Итоги сезона, карта начатого выпуска или хаб.
        void Resume()
        {
            if (GameSession.SeasonOver)
                ShowSeasonEnd();
            else if (_episode.Active)
                OpenMap();
            else
                ShowHub();
        }

        void Bind()
        {
            var state = GameSession.State;
            // Длина сезона всегда из конфига — дизайнер может поменять её посреди сезона.
            state.seasonLength = Mathf.Max(1, season.episodes);
            _meta = new MetaService(state, GameSession.Tone, _content.All);
            _episode?.Dispose();
            _episode = new EpisodeService(state, GameSession.Tone, season);
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
            var state = GameSession.State;
            int number = state.episodeIndex + 1;
            hub.SetStartCaption(_episode.Active
                ? "Выпуск " + number + " из " + state.seasonLength + " идёт  ·  вернуться на карту выпуска"
                : "Выпуск " + number + " из " + state.seasonLength + "  ·  дальше карта выпуска: комнаты и монтаж");
            hub.SetSubtitle("Сезон " + state.seasonNumber + "  ·  Выпуск " + number + " из " + state.seasonLength + "  ·  Продакшн-хаб"
                            + (string.IsNullOrEmpty(_hubNotice) ? "" : "\n" + _hubNotice));
        }

        // Начало выпуска: каст из хаба, новая карта. Если выпуск уже идёт — просто на карту.
        void StartOrResumeEpisode()
        {
            if (!_episode.Active)
            {
                _episode.Begin(CastIds());
                GameSession.Save();
            }

            _hubNotice = null;
            OpenMap();
        }

        // Пока выбора каста нет: в выпуск идут первые участники по числу мест (уровень Кастинга, не больше castMax).
        List<string> CastIds()
        {
            int seats = Mathf.Min(CastRoster.Seats(_meta.CastLevel), Mathf.Max(1, season.castMax));
            var ids = new List<string>();
            for (int i = 0; i < _cast.Length && ids.Count < seats; i++)
                ids.Add(_cast[i].id);
            return ids;
        }

        void OpenMap()
        {
            _selected = _episode.Map.CurrentChoice;
            Show(map.gameObject);
            RefreshMap();
        }

        void RefreshMap()
        {
            map.Show(_episode.Map, _selected, _meta.Stats(), _episode.Current.Number, GameSession.State.seasonLength, _meta.TaskLines());
        }

        void PickRandom()
        {
            var open = _episode.Map.Available();
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
            if (!_episode.Map.CanEnter(_selected))
            {
                Sfx.Play(Cue.Miss, 0.4f);
                return;
            }

            switch (_selected.type)
            {
                case RoomType.Marketing: EnterMarketing(_selected); break;
                case RoomType.Event: RunEvent(_selected); break;
                case RoomType.Montage: RunMontage(_selected); break;
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
            if (node == null || !_episode.Map.CanEnter(node) || !_meta.CanStart())
            {
                Sfx.Play(Cue.Miss, 0.4f);
                return;
            }

            if (!_episode.Map.Choose(node))
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
            GameSession.RoomNodeId = node.id;
            GameSession.Save();
            var situation = node.room as SituationRoomDefinition;
            SceneFlow.ToScene(situation != null && !string.IsNullOrEmpty(situation.scene) ? situation.scene : SceneFlow.Episode);
        }

        // Маркетинг: пока это старый магазин карт (окно колоды). «Готово» закрывает комнату (LeaveShop).
        // TODO: офферы и спонсорские контракты — шаг Marketing room.
        void EnterMarketing(MapNode node)
        {
            if (!_episode.Map.Choose(node))
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
            if (!_episode.Active)
            {
                RefreshHub();
                return;
            }

            _episode.CompleteRoom();
            GameSession.Save();
            AfterStep();
        }

        // Событие — пока не реализовано: комната засчитывается сразу.
        void RunEvent(MapNode node)
        {
            if (!_episode.Map.Choose(node))
                return;
            // TODO: экран события по EventRoomDefinition (текст, 2–3 выбора, эффекты) — шаг Event room.
            Sfx.Play(Cue.Blip, 0.5f);
            _episode.CompleteRoom();
            GameSession.Save();
            AfterStep("Событие", "Пока не реализовано — сцена засчитана. Здесь будет выбор с последствиями.");
        }

        // Монтаж — всегда последняя комната. Пока не реализован: выпуск сразу уходит в эфир.
        void RunMontage(MapNode node)
        {
            if (!_episode.Map.Choose(node))
                return;
            // TODO: выбор и порядок клипов, оценка монтажа, эфир и HellTube — шаги Montage и HellTube.
            Sfx.Play(Cue.Blip, 0.5f);
            _episode.CompleteRoom();
            GameSession.Save();
            AfterStep();
        }

        // После комнаты: выпуск закончен — в эфир, иначе снова карта (с пояснением, если есть).
        void AfterStep(string noticeTitle = null, string noticeBody = null)
        {
            if (_episode.Active && _episode.Current.Finished)
            {
                AirEpisode();
                return;
            }

            _selected = null;
            Show(map.gameObject);
            RefreshMap();
            if (noticeTitle != null)
                map.Notice(noticeTitle, noticeBody);
        }

        // Эфир выпуска. Экран итогов (монтаж, HellTube) — следующие шаги, пока только строка в хабе.
        void AirEpisode()
        {
            int number = _episode.Current.Number;
            _episode.End();
            GameSession.Save();
            if (GameSession.SeasonOver)
            {
                ShowSeasonEnd();
                return;
            }

            _hubNotice = "Выпуск " + number + " вышел в эфир. Итоги монтажа и HellTube появятся здесь.";
            ShowHub();
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
