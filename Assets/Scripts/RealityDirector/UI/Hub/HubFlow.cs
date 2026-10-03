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
        AudioSource _bed;

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

            map.Select += id =>
            {
                Click();
                var node = _episode.Map.Map.Find(id);
                // Второй клик по уже выбранной доступной комнате — войти.
                if (node != null && node == _selected && _episode.Map.CanEnter(node))
                {
                    Shoot();
                    return;
                }

                _selected = node;
                RefreshMap();
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
            // В хаб посреди выпуска: выпуск не прерывается, «Начать съёмку» в хабе вернёт на карту.
            map.Back += () =>
            {
                Click();
                map.Deck.Hide();
                _runShop = false;
                GameSession.Save();
                ShowHub();
            };
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
            {
                RefreshHub();
                SyncBed();
            }
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
            bool toHub = GameSession.ExitToHub;
            GameSession.ExitToHub = false;
            // Квартира закрыла сцену. Карту выпуска открывает Resume (выпуск идёт), комнату закрывает RoomNodeId.
            // Вернулись со съёмки — комната пройдена (и по «Снято!», и по «Хаб»), дальше по карте выпуска.
            if (!string.IsNullOrEmpty(GameSession.RoomNodeId) && _episode.Active)
            {
                GameSession.RoomNodeId = null;
                _episode.FinishSceneRoom();
                GameSession.Save();
                if (toHub && !_episode.Current.Finished)
                    ShowHub();
                else
                    AfterStep();
                return;
            }

            GameSession.RoomNodeId = null;
            if (toHub)
            {
                ShowHub();
                return;
            }

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
            GameSession.State.wantsTutorial = true;
            GameSession.State.tutorialBeat = 0;
            Show(intro.gameObject);
            intro.Play();
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
                StopBed();
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
            if (_meta.GrantDevDeck() > 0)
                GameSession.Save();
            _meta.ClearReject();
            Show(hub.gameObject);
            hub.Deck.Hide();
            hub.ClearSelection();
            RefreshHub();
            var teach = new List<CoachStep>();
            BossCoach.Line(teach, BossMood.Aside, "Сверху — кр, рейтинг и тон сезона. кр тратишь здесь, на людей и карты. Hell Token — отдельные деньги, их жгут карты уже на площадке.", hub.StatsFocus());
            BossCoach.Line(teach, BossMood.Think, "Кастинг. Апгрейд даёт места в кадре и процент к чеку. С третьего уровня на карточке откроется скрытая черта. Раньше она закрыта.", hub.ZoneFocus(CrewTrack.Cast));
            BossCoach.Line(teach, BossMood.Annoyed, "Съёмочная. Каждый уровень — ещё один ролик за выпуск. Со второго монтаж подписывает общий тег соседних кадров. Слоты кончились — хоть потолок снимай, в эфир он не просится.", hub.ZoneFocus(CrewTrack.Operators));
            BossCoach.Line(teach, BossMood.Smug, "Сценарная. Больше карт берут в серию и открываются новые типы. На четвёртом уровне — второй рекламный контракт за выпуск.", hub.ZoneFocus(CrewTrack.Writers));
            BossCoach.Line(teach, BossMood.Shock, "Магазин. Платишь кр один раз. Карта остаётся в колоде до конца сезона. На площадке её сдадут в руку вместе с остальными.", hub.ShopFocus());
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
            BossCoach.Line(steps, BossMood.Stern, "Колода сезона. Стартовое и купленное. Сейчас только смотри, тыкать не надо.", hub.Deck.CardsFocus());
            BossCoach.Line(steps, BossMood.Aside, "На съёмке её тасуют и сдают в руку. Сыграл карту — добираешь следующую. К следующей съёмке сыгранные возвращаются.", hub.Deck.ExplainFocus());
            BossCoach.Line(steps, BossMood.Stern, "Счёт снизу. Сколько карт в колоде и сколько влезет в руку.", hub.Deck.FooterFocus());
            BossCoach.Guide(0, steps.ToArray(), CloseDeckThenStart);
        }

        void CloseDeckThenStart()
        {
            if (hub.Deck != null && hub.Deck.IsOpen)
                hub.Deck.Hide();
            var start = new List<CoachStep>();
            BossCoach.Line(start, BossMood.Mad, "Старт выпуска. Сначала каст — кого пустишь в кадр. Потом карта: комнаты по очереди, в конце монтаж и эфир. Назад по комнатам нельзя.", hub.StartFocus());
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
                // «← В хаб» — передумал запускать выпуск. В обучении кнопки нет: босс ведёт по шагам.
                System.Action backToHub = state.wantsTutorial && state.tutorialBeat < 6 ? null : (System.Action)(() =>
                {
                    Click();
                    BossCoach.Ensure().Hide();
                    ShowHub();
                });
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
                }, backToHub);
                var castTeach = new List<CoachStep>();
                BossCoach.Line(castTeach, BossMood.Grin, "Жми на карточку — взять или убрать. Минимум двое, больше мест даёт только апгрейд кастинга. Эти люди и будут в кадре весь выпуск.", _screens.Focus);
                BossCoach.Line(castTeach, BossMood.Smug, "Строка под именем — как они ломаются. Это крючок для карт, не биография. Жми в них тем, на что они уже злые.", _screens.Focus);
                BossCoach.Line(castTeach, BossMood.Aside, "Скрытая черта закрыта, пока кастинг ниже третьего. Апгрейд — и я шепну, кто клептоман, а кто пранкер. В кадре это потом всплывёт в комментариях.", _screens.Focus);
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
            BossCoach.Line(mapTeach, BossMood.Smug, "Выпуск — маршрут. Сегодня короткий, я сам собрал: съёмка, событие, монтаж. Со следующего будут развилки. Светящаяся комната — единственная, куда можно.", map.BoardFocus);
            BossCoach.Line(mapTeach, BossMood.Yell, "Съёмка. Колоду тасуют и сдают руку уже там. Карту кидаешь на человека или вещь, камера пишет ролик. Ролик — ещё не эфир.", map.NodeFocus(RoomType.Situation));
            BossCoach.Line(mapTeach, BossMood.Think, "Событие. Текст и выбор между съёмками. В библиотеку футажа не падает. На кнопке бывает шанс — может не выйти.", map.NodeFocus(RoomType.Event));
            BossCoach.Line(mapTeach, BossMood.Stern, "Монтаж — последняя дверь. Пока не соберёшь кат и не нажмёшь в эфир, выпуск не закрыт.", map.NodeFocus(RoomType.Montage));
            BossCoach.Line(mapTeach, BossMood.Aside, "Маркетинга сегодня нет. На следующих картах будет: берёшь контракт, играешь карту спонсора и оставляешь этот ролик в кате. Выкинул из монтажа — выплаты нет, репутация падает.", map.InfoFocus);
            BossCoach.Line(mapTeach, BossMood.Grin, "Справа цель выпуска и задачи зрителей. Задач пока нет. После эфира заказ можно взять в комментариях — тогда он появится здесь и на хабе.", map.PlanFocus);
            BossCoach.Line(mapTeach, BossMood.Mad, "Жми на съёмку, потом входи. Назад по карте нельзя. В хаб можно выйти, выпуск от этого не сбросится.", map.EnterFocus);
            if (mapTeach.Count > 0)
                BossCoach.Play(2, 3, mapTeach.ToArray());
        }

        void RefreshMap()
        {
            // Если идти можно только в одну комнату — она сразу выбрана, кнопка действия активна.
            if (_selected == null)
            {
                var open = _episode.Map.Available();
                if (open.Count == 1)
                    _selected = open[0];
            }

            map.Show(_episode.Map, _selected, _meta.Stats(), _episode.Current.Number, GameSession.State.seasonLength, _meta.TaskLines());
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

        // Съёмка: сразу в квартиру. Карты не выбираются у двери — колода тасуется и сдаёт руку уже на площадке (GDD 0.3).
        void StartFilming(MapNode node)
        {
            _runShop = false;
            _meta.ClearReject();
            BeginFilming(node);
        }

        void BeginFilming(MapNode node)
        {
            if (node == null || !_episode.Map.CanEnter(node))
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
            BossCoach.Ensure().Hide();
            GameSession.Save();
            StopBed();
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
            TeachEvent();
        }

        void TeachEvent()
        {
            var state = GameSession.State;
            if (state == null || !state.wantsTutorial || state.tutorialBeat >= 6 || _eventView == null || _eventView.Focus == null)
                return;
            BossCoach.Ensure().Freeze(
                BossMood.Think,
                "Событие. Это не клип: в библиотеку футажа ничего не падает. Читаешь, что случилось между съёмками.",
                () => BossCoach.Ensure().Freeze(
                    BossMood.Stern,
                    "Варианты справа. Если на кнопке процент — это шанс, может не выйти. «Уйти» закрывает комнату и ничего не меняет. Назад выбор не переигрывается.",
                    () => BossCoach.Ensure().Order(BossMood.Mad, "Выбери одну.", _eventView.Focus),
                    _eventView.Focus),
                _eventView.BodyFocus);
        }

        void OpenEvent(EventRoomDefinition def, string nodeId, RuleContext ctx, System.Action<int> applied, Action done)
        {
            var roles = EventResolver.CastRoles(def, ctx.episode, nodeId);
            Func nameOf = ActorName;
            var choices = EventResolver.Choices(def, ctx, roles, nameOf);
            Sfx.Play(Cue.Bell, 0.35f, 1.2f);
            // Подзаголовок «Событие» не повторяем после «СОБЫТИЕ».
            string sub = def.subtitle != null ? def.subtitle.Trim() : "";
            bool echo = sub.Length == 0 || sub.Equals("событие", System.StringComparison.OrdinalIgnoreCase);
            _eventView.Show("СОБЫТИЕ" + (echo ? "" : "  ·  " + sub.ToUpperInvariant()),
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
            BossCoach.Line(cutTeach, BossMood.Sigh, "Сверху библиотека выпуска. Клик по карточке кладёт ролик в кат или убирает. ▶ смотрит черновик, в эфир от этого ничего не уезжает.", _cut.LibraryFocus);
            BossCoach.Line(cutTeach, BossMood.Annoyed, "Снизу то, что увидит ад. Слотов мало. Выдели кадр: РАНЬШЕ и ПОЗЖЕ меняют порядок, УБРАТЬ выкидывает. Соседние про одно и то же поднимают связность. Про разное — это нарезка.", _cut.CutFocus);
            BossCoach.Line(cutTeach, BossMood.Grin, "Связность справа. Высокая — зритель видит историю. Низкая — крики без нитки. Со второго уровня съёмочной здесь ещё и общий тег соседей.", _cut.CoherenceFocus);
            BossCoach.Line(cutTeach, BossMood.Yell, "Строчка сверху — это я ору. Не подсказка. Давление. Игнорируешь — я не замолкаю, просто злюсь.", _cut.BossFocus);
            BossCoach.Line(cutTeach, BossMood.Mad, "В ЭФИР. Чего нет в кате — для зрителя не было. Рекламу, которую выкинул, я не оплачу, и репутация спонсора просядет.", _cut.AirFocus);
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
            BossCoach.Line(airTeach, BossMood.Aside, "Вот их экран. Не квартира и не библиотека. Только кадры, которые ты оставил в кате.", _cut.WatchFocus);
            BossCoach.Line(airTeach, BossMood.Stern, "Просмотры, лайки, связность, рейтинг. Черновики и вырезанное сюда не входят. Оценка кормит чек.", _cut.NumbersFocus);
            BossCoach.Line(airTeach, BossMood.Grin, "Комментарии. Злые — это рецензия, не кнопка. Если кто-то увидел скрытую черту, напишут прямо здесь.", _cut.CommentsFocus);
            BossCoach.Line(airTeach, BossMood.Smug, "Звезда и «взять» — заказ зрителей. Жми, и задача встанет на хаб и на карту следующего выпуска. Закроешь её кадром — чек ×1.3. Мимо — деньги есть, бонуса нет. Такой же заказ второй раз не берётся, мест четыре.", _cut.TaskFocus);
            BossCoach.Line(airTeach, BossMood.Sigh, "Чек справа. Реклама в кате платит кр и злит их, рейтинг падает. Реклама, которую выкинул в монтаже, — тишина и минус к репутации. Дальше хаб: со следующего выпуска карта с развилками, магазин и апгрейды уже твои. Тон копится до концовки сезона.", _cut.PayFocus);
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
                    cues = clip.cues ?? new List<string>(),
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
            SyncBed();
        }

        // Меню — MainMenu.mp3. Обучение до входа в съёмку — Intro.mp3. Дальше тишина.
        void SyncBed()
        {
            var src = Bed();
            bool menuUp = menu != null && menu.gameObject.activeSelf
                || settings != null && settings.gameObject.activeSelf && _back == menu.gameObject;
            var state = GameSession.State;
            bool introUp = !menuUp && state != null && state.wantsTutorial && state.tutorialBeat < 3;
            if (menuUp)
                PlayBed(src, "Music/MainMenu");
            else if (introUp)
                PlayBed(src, "Music/Intro");
            else
                StopBed();
        }

        AudioSource Bed()
        {
            if (_bed != null)
                return _bed;
            var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include);
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].GetComponent<MainMenuView>() != null)
                    continue;
                if (sources[i].gameObject.name != "MainMenu")
                    continue;
                _bed = sources[i];
                break;
            }

            if (_bed == null)
            {
                var go = new GameObject("BedMusic");
                go.transform.SetParent(transform, false);
                _bed = go.AddComponent<AudioSource>();
            }

            _bed.playOnAwake = false;
            _bed.loop = true;
            _bed.spatialBlend = 0f;
            return _bed;
        }

        static void PlayBed(AudioSource src, string path)
        {
            if (src == null)
                return;
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
                return;
            if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
            if (src.clip == clip && src.isPlaying)
                return;
            src.clip = clip;
            src.loop = true;
            src.Play();
        }

        void StopBed()
        {
            var src = Bed();
            if (src != null && src.isPlaying)
                src.Stop();
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
