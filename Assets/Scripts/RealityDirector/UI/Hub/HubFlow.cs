using System.Collections;
using System.Collections.Generic;
using Action = System.Action;
using Func = System.Func<string, string>;
using RealityDirector.Capture;
using RealityDirector.Core;
using RealityDirector.Meta;
using RealityDirector.Persistence;
using RealityDirector.UI;
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
        bool _runShop;
        MarketingRoomDefinition _marketing;
        CutFlowUi _cut;
        HubOverlays _screens;
        ViewerWishId _airWish;
        string _airWishLabel;
        CastMember[] _cast;
        GameObject _back;
        EventRoomView _eventView;

        void Awake()
        {
            _content = PitchContent.Create();
            _cast = CastRoster.All();
            if (season == null)
                season = SeasonConfig.CreateDefault();
            EnsureEventSystem();
            Sfx.Bind(gameObject);
            _eventView = EventRoomView.Create(map.transform.parent, TitleFont());

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
            hub.OpenDeck += tab =>
            {
                Click();
                _meta.ClearReject();
                bool shop = tab == DeckPanelView.ShopTab;
                hub.Deck.Open(tab, _meta.BuildPrep(), shop, true);
            };
            hub.CloseDeck += () =>
            {
                Click();
                hub.Deck.Hide();
                RefreshHub();
            };
            hub.StartShoot += () => { Click(); StartOrResumeEpisode(); };
            hub.OpenSettings += () => { Click(); ShowSettings(hub.gameObject); };
            hub.Menu += () => { Click(); GameSession.Save(); ShowMenu(); };

            map.Select += id => { Click(); _selected = _episode.Map.Map.Find(id); RefreshMap(); };
            // Выбор карт для съёмки — прямо на карте сезона.
            map.Deck.Toggle += id =>
            {
                bool ok = _meta.TogglePick(id);
                Sfx.Play(ok ? Cue.Click : Cue.Miss, ok ? 0.3f : 0.45f);
                if (ok)
                    GameSession.Save();
                map.Deck.Show(_runShop ? _meta.BuildRunShop() : _meta.BuildPrep());
                if (ok)
                    OnDeckPicked(id);
            };
            map.Deck.Close += () =>
            {
                Click();
                if (_runShop)
                {
                    map.Deck.Hide();
                    LeaveRunShop();
                    return;
                }

                BeginFilming(_selected);
            };
            map.Deck.Cancel += () =>
            {
                Click();
                map.Deck.Hide();
                if (_runShop)
                    LeaveRunShop();
            };
            map.Deck.Buy += id =>
            {
                bool ok = _meta.TryBuyRun(id);
                Sfx.Play(ok ? Cue.Coin : Cue.Miss, ok ? 0.5f : 0.45f);
                if (ok)
                    GameSession.Save();
                map.Deck.Show(_meta.BuildRunShop());
            };
            map.Random += PickRandom;
            // Выпуск уже идёт — в хаб только после эфира.
            map.Back += () => { };
            map.Shoot += Shoot;
            _cut = CutFlowUi.Create();
            _screens = HubOverlays.Create();
            _cut.Confirm += ConfirmCut;
            _cut.Next += CloseAir;
        }

        void Update()
        {
            ApplyTutorialLocks();
            if (_cut == null || !_cut.AirVisible)
                return;
            var state = GameSession.State;
            var coach = BossCoach.Ensure();
            if (state != null && state.wantsTutorial && state.tutorialBeat < 6 && coach != null && coach.IsOpen)
                return;
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                CloseAir();
        }

        bool _teachWas;

        void ApplyTutorialLocks()
        {
            var state = GameSession.State;
            bool teach = state != null && state.wantsTutorial && state.tutorialBeat < 6;
            bool talking = false;
            if (teach)
            {
                var coach = BossCoach.Ensure();
                talking = coach != null && coach.IsOpen && !coach.Ordering;
            }
            if (hub != null)
                hub.ApplyTutorial(teach, talking, state != null && state.tutorialBeat >= 1);
            if (map != null)
                map.ApplyTutorial(teach, talking);
            if (_screens != null)
                _screens.SetPageLocked(talking);
            if (_cut != null)
                _cut.SetLocked(talking);
            if (_teachWas && !teach)
                RefreshHub();
            _teachWas = teach;
        }

        void Start()
        {
#if UNITY_EDITOR
            // Мастерская событий → «Проверить»: сразу открыть событие.
            string testEvent = EventResolver.TakeTestEvent();
            if (!string.IsNullOrEmpty(testEvent))
            {
                OpenTestEvent(testEvent);
                return;
            }
#endif
            if (!GameSession.Active)
            {
                ShowMenu();
                return;
            }

            Bind();
            GameSession.ReturnToMap = false;
            if (GameSession.ExitToHub)
            {
                GameSession.ExitToHub = false;
                GameSession.RoomNodeId = null;
                ShowHub();
                return;
            }
            // Квартира закрыла сцену. Карту выпуска открывает Resume (выпуск идёт), комнату закрывает RoomNodeId.
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
            BossCoach.Dismiss();
        }

        void BeginSeason()
        {
            GameSession.NewSeason(_content.StarterIds(), season);
            Bind();
            _screens.AskName("", (name, teach) =>
            {
                GameSession.State.producerName = name;
                GameSession.State.wantsTutorial = teach;
                GameSession.State.tutorialBeat = 0;
                GameSession.Commit();
                Show(intro.gameObject);
            });
        }

        void ContinueSeason()
        {
            if (!GameSession.Continue())
            {
                ShowMenu();
                return;
            }

            Bind();
            if (GameSession.Embarked && GameSession.InEpisode && !string.IsNullOrEmpty(GameSession.RoomNodeId))
            {
                SceneFlow.ToScene(string.IsNullOrEmpty(GameSession.SceneId) ? SceneFlow.Episode : GameSession.SceneId);
                return;
            }

            Resume();
        }

        // Итоги сезона, карта начатого выпуска или хаб.
        void Resume()
        {
            if (GameSession.SeasonOver)
                ShowSeasonEnd();
            else if (_episode.Active && _episode.Current.Finished)
                AirEpisode();
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
            hub.ClearSelection();
            RefreshHub();
            var teach = new List<CoachStep>();
            BossCoach.Line(teach, "Кастинг. Кого пустишь к камере. И когда я разрешу подсмотреть, что они от тебя прячут.", hub.ZoneFocus(CrewTrack.Cast));
            BossCoach.Line(teach, "Съёмочная. Сколько роликов влезет в выпуск. Слоты кончились — хоть потолок снимай, в эфир он не просится.", hub.ZoneFocus(CrewTrack.Operators));
            BossCoach.Line(teach, "Сценарная. Отсюда новые карты. Дорастёт — дам второй рекламный контракт. Проценты к чеку оставь бухгалтерии.", hub.ZoneFocus(CrewTrack.Writers));
            BossCoach.Line(teach, "Магазин. Тратишь кр. Карта остаётся на весь сезон. Я от себя такой щедрости не ждал.", hub.ShopFocus());
            if (teach.Count > 0)
                BossCoach.Guide(0, teach.ToArray(), () => StartCoroutine(ShowDeckLesson()));
        }

        IEnumerator ShowDeckLesson()
        {
            if (GameSession.State == null || !GameSession.State.wantsTutorial || GameSession.State.tutorialBeat != 0)
                yield break;
            hub.Deck.Open(DeckPanelView.DeckTab, _meta.BuildPrep(), false, true);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var steps = new List<CoachStep>();
            BossCoach.Line(steps, "Колода. Всё, что уже купил. Сейчас только смотри. Тыкать не надо.", hub.Deck.CardsFocus());
            BossCoach.Line(steps, "Пояснение. В съёмку отсюда ничего не уезжает. Выбор будет перед дверью на карте.", hub.Deck.ExplainFocus());
            BossCoach.Line(steps, "Снизу счёт. Сколько карт есть и сколько пустят с собой.", hub.Deck.FooterFocus());
            BossCoach.Guide(0, steps.ToArray(), CloseDeckThenStart);
        }

        void CloseDeckThenStart()
        {
            if (hub.Deck != null && hub.Deck.IsOpen)
                hub.Deck.Hide();
            var start = new List<CoachStep>();
            BossCoach.Line(start, "Старт. Сначала люди. Потом карта выпуска. Не перепутай, второй раз я это рассказывать не буду.", hub.StartFocus());
            if (start.Count == 0 && GameSession.State != null)
            {
                GameSession.State.tutorialBeat = 1;
                GameSession.Save();
                return;
            }

            BossCoach.Play(0, 1, start.ToArray());
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
                ? "Выпуск " + number + " из " + state.seasonLength + " идёт  ·  вернуться на карту эпизода"
                : "Выпуск " + number + " из " + state.seasonLength + "  ·  дальше карта эпизода: комнаты и монтаж");
            hub.SetSubtitle("Сезон " + state.seasonNumber + "  ·  Выпуск " + number + " из " + state.seasonLength + "  ·  Продакшн-хаб"
                            + (string.IsNullOrEmpty(_hubNotice) ? "" : "\n" + _hubNotice));
        }

        // Начало выпуска: каст из хаба, новая карта. Если выпуск уже идёт — просто на карту.
        void StartOrResumeEpisode()
        {
            if (!_episode.Active)
            {
                int seats = Mathf.Min(CastRoster.Seats(_meta.CastLevel), Mathf.Max(2, season.castMax));
                int min = Mathf.Clamp(season.castMin, 2, seats);
                var state = GameSession.State;
                if (state.wantsTutorial && state.tutorialBeat < 1)
                    state.tutorialBeat = 1;
                BossCoach.Ensure().Hide();
                _screens.PickCast(_cast, state.castPick, min, seats, state.castLevel >= 3, ids =>
                {
                    state.castPick.Clear();
                    state.castPick.AddRange(ids);
                    if (state.wantsTutorial && state.tutorialBeat < 2)
                        state.tutorialBeat = 2;
                    BossCoach.Ensure().Hide();
                    _episode.Begin(ids);
                    GameSession.Save();
                    int number = state.episodeIndex + 1;
                    _screens.Slate(number, state.producerName, () =>
                    {
                        _hubNotice = null;
                        OpenMap();
                    });
                });
                var castTeach = new List<CoachStep>();
                BossCoach.Line(castTeach, "Двое. Минимум. Из одного человека шоу не соберёшь, это уже исповедь.", _screens.Focus);
                BossCoach.Line(castTeach, "Черта под именем. Вот так они и сломаются, когда ты начнёшь.", _screens.Focus);
                BossCoach.Line(castTeach, "Скрытое пока закрыто. Кастинг подрастёт — шепну. Раньше не выпрашивай.", _screens.Focus);
                if (castTeach.Count > 0)
                    BossCoach.Play(1, 2, castTeach.ToArray());
                return;
            }

            _hubNotice = null;
            OpenMap();
        }

        void OpenMap()
        {
            _selected = _episode.Map.CurrentChoice;
            Show(map.gameObject);
            RefreshMap();
            var mapTeach = new List<CoachStep>();
            BossCoach.Line(mapTeach, "Сегодня маршрут короткий. Я сам его собрал: съёмка, событие, монтаж.", map.BoardFocus);
            BossCoach.Line(mapTeach, "Съёмка. Заходи сюда. Здесь заставлю тебя кинуть карту.", map.NodeFocus(RoomType.Situation));
            BossCoach.Line(mapTeach, "Потом событие. Почитаешь и выберешь. В эфир это само не прыгнет.", map.NodeFocus(RoomType.Event));
            BossCoach.Line(mapTeach, "И монтаж. Последняя дверь. Мимо неё выпуск не выходит.", map.NodeFocus(RoomType.Montage));
            BossCoach.Line(mapTeach, "Жми на съёмку, потом входи. Назад я не пускаю.", map.EnterFocus);
            if (mapTeach.Count > 0)
                BossCoach.Play(2, 3, mapTeach.ToArray());
        }

        void RefreshMap()
        {
            map.Show(_episode.Map, _selected, _meta.Stats(), _episode.Current.Number, GameSession.State.seasonLength, _meta.TaskLines());
        }

        void PickRandom()
        {
            if (GameSession.State != null && GameSession.State.wantsTutorial && GameSession.State.tutorialBeat < 6)
                return;
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
            _runShop = false;
            _meta.TrimPicked();
            _meta.ClearReject();
            map.Deck.Open(DeckPanelView.DeckTab, _meta.BuildPrep());
            if (!DeckLesson())
                return;
            map.Deck.SetCancelEnabled(false);
            map.Deck.SetCloseEnabled(false);
            map.Deck.SetOnly("fridge_fire");
            StartCoroutine(PromptDeckCard());
        }

        bool DeckLesson()
        {
            var state = GameSession.State;
            return state != null && state.wantsTutorial && state.tutorialBeat >= 2 && state.tutorialBeat < 4;
        }

        IEnumerator PromptDeckCard()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (!DeckLesson())
                yield break;
            if (GameSession.State.picked.Contains("fridge_fire"))
            {
                ExplainPick();
                yield break;
            }

            BossCoach.Ensure().Order("Колода перед дверью. Жми «Поджог». Остальные сегодня не трогай.", map.Deck.CardRect("fridge_fire"));
        }

        void OnDeckPicked(string id)
        {
            if (!DeckLesson() || _runShop || id != "fridge_fire")
                return;
            if (!GameSession.State.picked.Contains(id))
            {
                map.Deck.SetOnly("fridge_fire");
                map.Deck.SetCloseEnabled(false);
                BossCoach.Ensure().Order("Верни её. «Поджог». Без неё урок пустой.", map.Deck.CardRect("fridge_fire"));
                return;
            }

            ExplainPick();
        }

        void ExplainPick()
        {
            map.Deck.SetCardsEnabled(false);
            map.Deck.SetCloseEnabled(false);
            BossCoach.Ensure().Hide();
            BossCoach.Ensure().Freeze(
                "Видишь «В СЕРИИ». Она едет на эту съёмку. Колода на месте, уехала только она.",
                () =>
                {
                    map.Deck.SetCloseEnabled(true);
                    BossCoach.Ensure().Order("Начать съёмку. Отмеченное едет на площадку.", map.Deck.CloseFocus());
                },
                map.Deck.CardRect("fridge_fire"));
        }

        void BeginFilming(MapNode node)
        {
            if (node == null || !_episode.Map.CanEnter(node) || !_meta.CanStart())
            {
                Sfx.Play(Cue.Miss, 0.4f);
                return;
            }

            if (DeckLesson() && !GameSession.State.picked.Contains("fridge_fire"))
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
                GameSession.Save();
                Sfx.Play(Cue.Miss, 0.4f);
                map.Deck.Hide();
                RefreshMap();
                return;
            }

            Sfx.Play(Cue.Card, 0.45f, 0.8f);
            var situation = node.room as SituationRoomDefinition;
            GameSession.Embarked = true;
            GameSession.SceneTitle = node.title;
            GameSession.SceneId = situation != null && !string.IsNullOrEmpty(situation.scene) ? situation.scene : SceneFlow.Episode;
            GameSession.RoomNodeId = node.id;
            if (situation != null && _episode.Current != null)
                _episode.Current.roleBrief = situation.roleBrief;
            if (DeckLesson())
                BossCoach.Ensure().Hide();
            GameSession.Save();
            SceneFlow.ToScene(GameSession.SceneId);
        }

        // Покупка за нал или контракт без денег. Выплата контракта ждёт монтаж.
        void EnterMarketing(MapNode node)
        {
            if (!_episode.Map.Choose(node))
            {
                Sfx.Play(Cue.Miss, 0.4f);
                return;
            }

            _meta.ClearReject();
            _marketing = node.room as MarketingRoomDefinition;
            Sfx.Play(Cue.Coin, 0.4f);
            GameSession.Save();
            RefreshDeals();
        }

        // Предложения комнаты маркетинга из её ассета; пусто — стандартный набор.
        List<MarketingOffer> Deals()
        {
            int reputation = GameSession.State.sponsorReputation;
            if (_marketing == null || _marketing.offers == null || _marketing.offers.Count == 0)
                return JamContent.Offers(reputation);
            var list = new List<MarketingOffer>();
            foreach (var offer in _marketing.offers)
            {
                if (offer != null && !string.IsNullOrEmpty(offer.cardId) && reputation >= offer.minReputation)
                    list.Add(offer);
            }

            return list;
        }

        void RefreshDeals()
        {
            if (!_episode.Active)
                return;
            _screens.ShowDeals(_meta.ReputationLine(), Deals(), offer =>
            {
                bool ok = offer.kind == OfferKind.Contract
                    ? _meta.TryTakeContract(offer.cardId, offer.payout, offer.scoreHit)
                    : _meta.TryBuyRun(offer.cardId);
                Sfx.Play(ok ? Cue.Coin : Cue.Miss, ok ? 0.5f : 0.45f);
                if (ok)
                    GameSession.Save();
                RefreshDeals();
            }, LeaveRunShop);
        }

        void LeaveRunShop()
        {
            _runShop = false;
            _screens.Hide();
            if (!_episode.Active)
            {
                RefreshMap();
                return;
            }

            _episode.CompleteRoom();
            GameSession.Save();
            AfterStep();
        }

        // Событие: текст и 2–3 выбора с последствиями. Выбор сразу закрывает комнату и сохраняется,
        // поэтому после выхода из игры событие нельзя «переиграть».
        void RunEvent(MapNode node)
        {
            if (!_episode.Map.Choose(node))
                return;
            var def = node.room as EventRoomDefinition;
            if (def == null || def.choices == null || def.choices.Count == 0)
            {
                Sfx.Play(Cue.Blip, 0.5f);
                _episode.CompleteRoom();
                GameSession.Save();
                AfterStep(node.title, "У этого события нет вариантов — сцена засчитана. Добавьте выборы в мастерской событий.");
                return;
            }

            var ctx = _episode.Context;
            EventResolver.Enter(def, ctx);
            OpenEvent(def, node.id, ctx, index =>
            {
                BossCoach.Ensure().Hide();
                _episode.CompleteRoom();
                GameSession.Save();
            }, () =>
            {
                _eventView.Hide();
                AfterStep();
            });
            if (GameSession.State != null && GameSession.State.wantsTutorial && GameSession.State.tutorialBeat < 6 && _eventView.Focus != null)
                BossCoach.Ensure().Order("Выбери одну. Это не клип. Это то, с чем они придут дальше.", _eventView.Focus);
        }

        void OpenEvent(EventRoomDefinition def, string nodeId, RuleContext ctx, System.Action<int> applied, Action done)
        {
            var roles = EventResolver.CastRoles(def, ctx.episode, nodeId);
            Func nameOf = ActorName;
            var choices = EventResolver.Choices(def, ctx, roles, nameOf);
            Sfx.Play(Cue.Bell, 0.35f, 1.2f);
            _eventView.Show("СОБЫТИЕ" + (string.IsNullOrEmpty(def.subtitle) ? "" : "  ·  " + def.subtitle.ToUpperInvariant()),
                EventResolver.Fill(def.title, roles, nameOf), EventResolver.Fill(def.body, roles, nameOf),
                def.art != null ? def.art : MapNodeView.Icon(def.icon), def.color, choices, "Уйти", index =>
                {
                    EventOutcome outcome;
                    bool chancy = false;
                    if (index < 0 || index >= def.choices.Count)
                    {
                        outcome = new EventOutcome { success = true, text = "Вы уходите, ничего не решив.", summary = "" };
                    }
                    else
                    {
                        var choice = def.choices[index];
                        chancy = choice.chance < 100;
                        outcome = EventResolver.Resolve(choice, ctx, roles, nameOf, CardName);
                    }

                    applied?.Invoke(index);
                    Sfx.Play(outcome.success ? Cue.Coin : Cue.Miss, 0.5f, outcome.success ? 1.05f : 0.9f);
                    _eventView.ShowResult(outcome, chancy, () =>
                    {
                        Click();
                        done?.Invoke();
                    });
                });
        }

        string ActorName(string id)
        {
            for (int i = 0; i < _cast.Length; i++)
            {
                if (_cast[i].id == id)
                    return _cast[i].name;
            }

            return id;
        }

        string CardName(string id)
        {
            var def = _meta != null ? _meta.Find(id) : null;
            return def != null ? def.displayName : id;
        }

        // Шрифт заголовков хаба (Metal Mania), если он есть на канвасе.
        Font TitleFont()
        {
            foreach (var text in map.transform.parent.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                if (text.font != null && text.font.name.Contains("Metal"))
                    return text.font;
            }

            return null;
        }

#if UNITY_EDITOR
        // Проверка события из редактора: открыть поверх карты; результат не сохраняется в сейв.
        void OpenTestEvent(string id)
        {
            var def = ContentLibrary.Find<RoomDefinition>(id) as EventRoomDefinition;
            if (def == null)
            {
                Debug.LogWarning("Проверка события: не найдено событие «" + id + "».");
                ShowMenu();
                return;
            }

            if (!GameSession.Active && !GameSession.Continue())
                GameSession.NewSeason(_content.StarterIds(), season);
            Bind();
            if (!_episode.Active)
            {
                // Без выбора каста: прошлый каст игрока или первые участники.
                var ids = new List<string>(GameSession.State.castPick);
                for (int i = 0; i < _cast.Length && ids.Count < Mathf.Max(2, season.castMin); i++)
                {
                    if (!ids.Contains(_cast[i].id))
                        ids.Add(_cast[i].id);
                }

                _episode.Begin(ids);
            }

            OpenMap();
            Debug.Log("Проверка события «" + def.title + "»: результат не сохраняется в сейв.");
            OpenEvent(def, "test_" + id, _episode.Context, null, () =>
            {
                _eventView.Hide();
                RefreshMap();
            });
        }
#endif

        void RunMontage(MapNode node)
        {
            if (!_episode.Map.Choose(node))
                return;
            map.Deck.Hide();
            _episode.Current.EnsureLists();
            Sfx.Play(Cue.Blip, 0.5f);
            int slots = season != null && season.finalCutSize > 0 ? season.finalCutSize : 3;
            _cut.ShowMontage(Library(_episode.Current), slots);
            var cutTeach = new List<CoachStep>();
            BossCoach.Line(cutTeach, "Сверху — всё, что наснимал. И удачное, и то, за что мне за тебя стыдно.", _cut.LibraryFocus);
            BossCoach.Line(cutTeach, "Снизу — что увидит ад. Кадров мало. Порядок уже история, не куча.", _cut.CutFocus);
            BossCoach.Line(cutTeach, "Соседи про одно и то же — связность. Про разное — нарезка. Зритель тупой. Но не всегда.", _cut.CoherenceFocus);
            BossCoach.Line(cutTeach, "Строчка сверху — это я ору. Я не подсказка. Я давление.", _cut.BossFocus);
            BossCoach.Line(cutTeach, "В эфир. Вырезанное для них не случалось. Рекламу, которую выкинул, я тебе не оплачу.", _cut.AirFocus);
            if (cutTeach.Count > 0)
                BossCoach.Play(4, 5, cutTeach.ToArray());
        }

        void ConfirmCut(List<string> ids)
        {
            if (!_episode.Active)
                return;
            _episode.Current.finalCut = ids != null ? new List<string>(ids) : new List<string>();
            _episode.CompleteRoom();
            GameSession.Save();
            _cut.Hide();
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

        // Эфир: деньги и HellTube только по финальному кату. Комната карты к этому моменту уже закрыта.
        void AirEpisode()
        {
            if (!_episode.Active)
                return;
            var episode = _episode.Current;
            episode.EnsureLists();
            var cut = LoadCut(episode);
            int coherence = MontageCut.Coherence(cut);
            var moments = Shells(cut);
            if (!episode.settled)
                Settle(episode, cut, moments, coherence);
            int hit = AiredHit(episode);
            var result = FeedbackGenerator.BuildCut(moments, GameSession.Tone, coherence, hit > 0);
            if (hit > 0)
                result = FeedbackGenerator.ApplySponsor(result, hit);
            _airWish = result.nextWish;
            _airWishLabel = result.wish;
            int scene = episode.Number;
            string name = string.IsNullOrEmpty(GameSession.SceneTitle) ? "Без названия" : GameSession.SceneTitle;
            _cut.ShowAir(result, cut, "Серия " + scene + ". «" + name + "»", episode.settledPay, coherence, episode.settledLine);
            var airTeach = new List<CoachStep>();
            BossCoach.Line(airTeach, "Вот что они видят. Не квартиру. Только то, что ты сам оставил в кате.", _cut.WatchFocus);
            BossCoach.Line(airTeach, "Просмотры, лайки, связность. Черновики я в эту арифметику не кладу.", _cut.NumbersFocus);
            BossCoach.Line(airTeach, "Комментарии. Им можно быть злыми. Считай это рецензией.", _cut.CommentsFocus);
            BossCoach.Line(airTeach, "Чек. Реклама в кате платит и злит их. Реклама на полу монтажной — тишина.", _cut.PayFocus);
            if (airTeach.Count > 0)
                BossCoach.Play(5, 6, airTeach.ToArray());
        }

        void Settle(EpisodeState episode, List<FootageClip> cut, List<CapturedMoment> moments, int coherence)
        {
            int sponsorMoney = 0;
            int hit = 0;
            bool playedAd = false;
            var aired = new HashSet<string>();
            for (int i = 0; i < cut.Count; i++)
                aired.Add(cut[i].id);
            for (int i = 0; i < episode.contracts.Count; i++)
            {
                var contract = episode.contracts[i];
                if (contract.status != ContractStatus.Active)
                    continue;
                if (contract.cardWasPlayed)
                    playedAd = true;
                bool inCut = false;
                for (int m = 0; m < contract.matchingFootageIds.Count; m++)
                {
                    if (aired.Contains(contract.matchingFootageIds[m]))
                        inCut = true;
                }

                if (contract.cardWasPlayed && inCut)
                {
                    contract.status = ContractStatus.Fulfilled;
                    contract.footageWasAired = true;
                    sponsorMoney += contract.payout;
                    hit += contract.scoreHit;
                    GameSession.State.sponsorReputation = Mathf.Clamp(GameSession.State.sponsorReputation + 12, 0, 100);
                }
                else
                {
                    contract.status = ContractStatus.Failed;
                    GameSession.State.sponsorReputation = Mathf.Clamp(GameSession.State.sponsorReputation - 15, 0, 100);
                }
            }

            var result = FeedbackGenerator.BuildCut(moments, GameSession.Tone, coherence, hit > 0);
            if (hit > 0)
                result = FeedbackGenerator.ApplySponsor(result, hit);
            bool hadTasks = GameSession.State.tasks.Count > 0;
            bool wishDone = GameSession.State.Resolve(moments, GameSession.Tone);
            int pay = Progression.Payout(result.score, GameSession.State.castLevel, wishDone) + sponsorMoney;
            GameSession.State.money += pay;
            GameSession.State.ratingSum += result.score;
            GameSession.State.rated++;
            string line = PayLine(pay, hadTasks, wishDone);
            if (sponsorMoney > 0)
                line += "   ·   спонсор +" + sponsorMoney + " кр, отзывы −" + hit;
            else if (playedAd)
                line += "   ·   реклама не в эфире, выплаты нет";
            episode.settled = true;
            episode.settledPay = pay;
            episode.settledSponsor = sponsorMoney;
            episode.settledLine = line;
            GameSession.Save();
        }

        void CloseAir()
        {
            if (_cut == null || !_cut.AirVisible || !_episode.Active)
                return;
            if (_cut.TaskTaken)
                GameSession.State.Accept(_airWish, _airWishLabel);
            var episode = _episode.Current;
            FootageReel.ReleaseEpisode(episode);
            _episode.End();
            _cut.Hide();
            GameSession.Save();
            if (GameSession.SeasonOver)
                ShowSeasonEnd();
            else
                ShowHub();
        }

        List<FootageClip> Library(EpisodeState episode)
        {
            var list = new List<FootageClip>();
            if (episode == null || episode.footage == null)
                return list;
            for (int i = 0; i < episode.footage.Count; i++)
            {
                var clip = FootageReel.Resolve(episode.footage[i]);
                if (clip != null)
                    list.Add(clip);
            }

            return list;
        }

        List<FootageClip> LoadCut(EpisodeState episode)
        {
            var list = new List<FootageClip>();
            if (episode.finalCut == null)
                return list;
            var library = Library(episode);
            for (int i = 0; i < episode.finalCut.Count; i++)
            {
                for (int j = 0; j < library.Count; j++)
                {
                    if (library[j].id == episode.finalCut[i])
                        list.Add(library[j]);
                }
            }

            return list;
        }

        static List<CapturedMoment> Shells(List<FootageClip> cut)
        {
            var list = new List<CapturedMoment>(cut.Count);
            for (int i = 0; i < cut.Count; i++)
            {
                var clip = cut[i];
                list.Add(new CapturedMoment
                {
                    actorNames = clip.actorNames,
                    tags = clip.tags,
                    mood = clip.mood,
                    grade = clip.grade,
                    exposed = clip.exposed,
                    duration = clip.duration,
                    photo = clip.photo,
                    time = clip.time
                });
            }

            return list;
        }

        static int AiredHit(EpisodeState episode)
        {
            int hit = 0;
            for (int i = 0; i < episode.contracts.Count; i++)
            {
                if (episode.contracts[i].footageWasAired)
                    hit += episode.contracts[i].scoreHit;
            }

            return hit;
        }

        static string PayLine(int pay, bool hadTasks, bool wishDone)
        {
            if (!hadTasks)
                return "+" + pay + " кр   ·   задач не было";
            if (wishDone)
                return "+" + pay + " кр   ·   задача закрыта ×1.3";
            return "+" + pay + " кр   ·   задачи открыты, мимо";
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
            if (_eventView != null)
                _eventView.Hide();
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
