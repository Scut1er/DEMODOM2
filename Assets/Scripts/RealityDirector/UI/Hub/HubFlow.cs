using System.Collections;
using System.Collections.Generic;
using Action = System.Action;
using Func = System.Func<string, string>;
using RealityDirector.Capture;
using RealityDirector.Core;
using RealityDirector.Meta;
using RealityDirector.NPC;
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
        MarketingRoomDefinition _marketing;
        MarketingView _market;
        string _marketNode;
        CutFlowUi _cut;
        HubOverlays _screens;
        int _montageCoach;
        ViewerWishId _airWish;
        string _airWishLabel;
        CastMember[] _cast;
        GameObject _back;
        EventRoomView _eventView;

        void Awake()
        {
            SilenceMenuSource();
            _content = PitchContent.Create();
            _cast = CastRoster.All();
            if (season == null)
                season = SeasonConfig.CreateDefault();
            // Хаб обещает столько мест, сколько реально займут: конфиг, кандидаты выпуска, живой ростер.
            CastRoster.SeatCap = Mathf.Min(season.castMax, Mathf.Min(season.castCandidates, _cast.Length));
            EnsureEventSystem();
            Sfx.Bind(gameObject);
            _eventView = EventRoomView.Create(map.transform.parent, TitleFont());
            _market = MarketingView.Create(map.transform.parent);

            menu.NewSeason += () => { Click(); BeginSeason(true); };
            menu.Continue += () => { Click(); ContinueSeason(); };
            menu.Settings += () => { Click(); ShowSettings(menu.gameObject); };
            menu.Quit += Quit;
            intro.Next += () => { Click(); ShowHub(); };
            settings.Back += () => { Click(); Show(_back != null ? _back : menu.gameObject); if (_back == hub.gameObject) ShowHub(); };
            seasonEnd.NewSeason += () => { Click(); BeginSeason(false); };
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
            // В хаб посреди выпуска: выпуск не прерывается, «Начать съёмку» в хабе вернёт на карту.
            map.Back += () =>
            {
                Click();
                GameSession.Save();
                ShowHub();
            };
            map.Shoot += Shoot;
            _cut = CutFlowUi.Create();
            _screens = HubOverlays.Create();
            _cut.Edited += OnCutEdited;
            _cut.Confirm += ConfirmCut;
            _cut.Extra = MontageExtra;
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
                hub.ApplyTutorial(teach, talking, true);
            if (map != null)
                map.ApplyTutorial(teach, talking);
            if (_screens != null)
                _screens.SetPageLocked(talking);
            if (_cut != null)
                _cut.SetLocked(talking);
            if (_eventView != null)
                _eventView.SetChoicesLocked(talking);
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

            // Пауза съёмки → «В главное меню»: съёмка осталась открытой в сейве, комнату не закрываем.
            if (GameSession.ToMenu)
            {
                GameSession.ToMenu = false;
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

        // Новая игра из меню всегда начинается с обучения (не нужно — «Пропустить обучение» у босса).
        // Следующий сезон после итогов — без обучения: игрок его уже видел, студию тоже не объясняем.
        void BeginSeason(bool tutorial)
        {
            GameSession.NewSeason(_content.SeasonDeck(season), season);
            Bind();
            GameSession.State.wantsTutorial = tutorial;
            GameSession.State.tutorialBeat = tutorial ? 0 : 6;
            if (!tutorial)
                GameSession.State.SetFlag(StudioTaught);
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
            if (GameSession.InEpisode && !string.IsNullOrEmpty(GameSession.RoomNodeId))
            {
                if (GameSession.Embarked)
                {
                    MusicBed.Play(MusicBed.Scene);
                    SceneFlow.ToScene(string.IsNullOrEmpty(GameSession.SceneId) ? SceneFlow.Episode : GameSession.SceneId);
                    return;
                }

                // Съёмку уже сдали («СНЯТО!» / «Хаб»), но игра закрылась до того, как хаб засчитал комнату (Start).
                // Засчитываем сейчас — иначе комнату можно снять второй раз, а её кадры уже в библиотеке.
                GameSession.RoomNodeId = null;
                _episode.FinishSceneRoom();
                GameSession.Save();
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
            var state = GameSession.State;
            if (state != null && state.wantsTutorial && state.tutorialBeat == 0)
                BossCoach.Ensure().Order("Жми «Начать съёмку». Каст первого выпуска я уже собрал.", hub.StartFocus());
            if (hub.FirstEpisode)
                return;

            // Обучение пропустили — экскурсии по студии не будет, но что такое ЕБ, сказать надо.
            if (state != null && state.flags.Contains(TutorialSkipped) && !state.flags.Contains(StudioTaught))
                TipOnce("money", "Сверху — бюджет в ЕБ. Это «единицы бюджета», а не то, что ты подумал. Копятся с эфиров, тратишь здесь, на людей и карты.");

            // Студия открылась после первого эфира — один раз рассказать, что тут к чему.
            if (state == null || state.flags.Contains(StudioTaught) || state.flags.Contains(TutorialSkipped))
                return;
            state.flags.Add(StudioTaught);
            GameSession.Save();
            var teach = new List<CoachStep>();
            BossCoach.Line(teach, BossMood.Smug, "Первый эфир позади. Студия открыта: теперь деньги с эфира можно вложить.", hub.StatsFocus());
            BossCoach.Line(teach, BossMood.Aside, "Сверху — бюджет, рейтинг и тон сезона. Бюджет в ЕБ — «единицах бюджета», а не то, что ты подумал. Тратишь здесь, на людей и карты. Hell Token — отдельные деньги, их жгут карты уже на площадке.", hub.StatsFocus());
            BossCoach.Line(teach, BossMood.Think, "Кастинг. Апгрейд даёт места в кадре и процент к чеку. С третьего уровня на карточке откроется скрытая черта. Раньше она закрыта.", hub.ZoneFocus(CrewTrack.Cast));
            BossCoach.Line(teach, BossMood.Annoyed, "Съёмочная. На сцене всегда 5 слотов футажа. Два апгрейда, каждый добавляет ещё один слот на сцену. В монтаже берёшь 3 кадра из всего, что снял за выпуск. Со второго уровня здесь ещё и общий тег соседних кадров.", hub.ZoneFocus(CrewTrack.Operators));
            BossCoach.Line(teach, BossMood.Smug, "Сценарная. Открывает новые типы карт в магазине. Рука на площадке от неё не растёт. На четвёртом уровне — второй рекламный контракт за выпуск.", hub.ZoneFocus(CrewTrack.Writers));
            BossCoach.Line(teach, BossMood.Shock, "Магазин. Платишь ЕБ один раз. Карта остаётся в колоде до конца сезона. На площадке её сдадут в руку вместе с остальными.", hub.ShopFocus());
            // Обучение к этому моменту закончено (эфир), поэтому не Guide, а прямая цепочка реплик.
            if (teach.Count > 0)
                BossCoach.Ensure().Tell(teach.ToArray(), () => StartCoroutine(ShowDeckLesson()));
        }

        // Флаги сезона: студию уже объяснили / обучение пропущено.
        public const string StudioTaught = "tut_studio";
        public const string TutorialSkipped = "tut_skipped";

        IEnumerator ShowDeckLesson()
        {
            if (GameSession.State == null)
                yield break;
            hub.Deck.Open(DeckPanelView.DeckTab, _meta.BuildPrep(), false, true);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var steps = new List<CoachStep>();
            BossCoach.Line(steps, BossMood.Stern, "Колода сезона. Стартовое и купленное. Сейчас только смотри, тыкать не надо.", hub.Deck.CardsFocus());
            BossCoach.Line(steps, BossMood.Aside, "На съёмке её тасуют и сдают в руку. Сыграл карту — добираешь следующую. К следующей съёмке сыгранные возвращаются.", hub.Deck.ExplainFocus());
            BossCoach.Line(steps, BossMood.Stern, "Счёт снизу. Сколько карт в колоде и сколько влезет в руку.", hub.Deck.FooterFocus());
            if (steps.Count > 0)
                BossCoach.Ensure().Tell(steps.ToArray(), CloseDeck);
            else
                CloseDeck();
        }

        void CloseDeck()
        {
            if (hub.Deck != null && hub.Deck.IsOpen)
                hub.Deck.Hide();
        }

        void RefreshHub()
        {
            var crew = new[]
            {
                _meta.Crew(CrewTrack.Cast),
                _meta.Crew(CrewTrack.Operators),
                _meta.Crew(CrewTrack.Writers)
            };
            var state = GameSession.State;
            // Онбординг: до первого эфира — упрощённый хаб (только старт и настройки).
            hub.SetFirstEpisode(state != null && state.episodeIndex == 0);
            hub.Show(_meta.BuildPrep(), _meta.Stats(), crew, _cast, _meta.CastLevel, _meta.TaskLines());
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
                bool firstLesson = state.wantsTutorial && state.episodeIndex == 0;
                CastMember[] roster;
                List<string> prefill;
                if (firstLesson)
                {
                    roster = TutorialCast(_cast);
                    prefill = new List<string>();
                    for (int i = 0; i < roster.Length; i++)
                        prefill.Add(roster[i].id);
                    min = roster.Length;
                    seats = roster.Length;
                }
                else
                {
                    roster = CastRoster.Candidates(_cast, state, season.castCandidates);
                    foreach (var member in roster)
                    {
                        var trait = _content.TraitOf(member.trait);
                        member.traitText = TraitText.Description(trait);
                        member.hints = TraitText.Hints(trait, _content.RulesFor(member.trait)).ToArray();
                    }
                    prefill = state.castPick;
                    if (state.castLevel < 3)
                        TipOnce("secret", "Скрытая черта закрыта, пока кастинг ниже третьего. Апгрейд кастинга — и на карточке будет видно, кто клептоман, а кто пранкер.");
                }

                GameSession.Save();
                _screens.PickCast(roster, prefill, min, seats, state.castLevel >= 3, ids =>
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
                if (firstLesson)
                    BossCoach.Ensure().Order("Злой и Добряк. На первом выпуске других нет. Жми «Утвердить каст».", _screens.ConfirmFocus != null ? _screens.ConfirmFocus : _screens.Focus);
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
            GuideMap();
        }

        // Обучение на карте первого выпуска: съёмка → событие → монтаж. Босс говорит, куда дальше.
        void GuideMap()
        {
            var state = GameSession.State;
            if (state == null || !state.wantsTutorial || _selected == null)
                return;
            var coach = BossCoach.Ensure();
            var enter = map.EnterFocus != null ? map.EnterFocus : map.NodeFocus(_selected.type);
            if (state.tutorialBeat == 2)
                coach.Freeze(BossMood.Think,
                    "Карта выпуска. Комнаты идут слева направо: съёмка, событие, монтаж. Назад по карте не ходят.",
                    () => coach.Order("Жми «Снимать». Светящаяся комната — та, куда можно сейчас.", enter),
                    map.BoardFocus);
            else if (state.tutorialBeat == 4 && _selected.type == RoomType.Event)
                coach.Order(BossMood.Smug, "Снято. Следующая комната — событие. Жми «Пройти».", enter);
            else if (state.tutorialBeat == 4 && _selected.type == RoomType.Montage)
                coach.Order("Последняя комната — монтажная. Там из снятого собирают серию. Жми «Монтаж».", enter);
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
            {
                _episode.Current.roleBrief = situation.roleBrief;
                _episode.Current.situationId = situation.Id;
            }
            BossCoach.Ensure().Hide();
            GameSession.Save();
            MusicBed.Play(MusicBed.Scene);
            SceneFlow.ToScene(GameSession.SceneId);
        }

        // Покупка за УЕ или контракт без денег. Выплата контракта ждёт монтаж.
        void EnterMarketing(MapNode node)
        {
            if (!_episode.Map.Choose(node))
            {
                Sfx.Play(Cue.Miss, 0.4f);
                return;
            }

            _meta.ClearReject();
            _marketing = node.room as MarketingRoomDefinition;
            _marketNode = node.id;
            Sfx.Play(Cue.Coin, 0.4f);
            GameSession.Save();
            RefreshDeals();
            // Обучение: первый маркетинг — что слева, что справа и как платит спонсор.
            var state = GameSession.State;
            if (state != null && state.wantsTutorial && !state.flags.Contains("tut_market"))
            {
                state.flags.Add("tut_market");
                var steps = new List<CoachStep>();
                BossCoach.Line(steps, BossMood.Smug, "Маркетинг. Слева покупки: тратишь УЕ — «условные единицы», деньги этого выпуска. Получаешь карту или бонус на следующие съёмки. Написано, что дадут и сколько действует.", _market.BuysFocus);
                BossCoach.Line(steps, BossMood.Aside, "Справа контракты. Бренд даёт свою карту. Сыграй её, сними бренд в кадре и оставь кадр в монтаже — заплатят и репутация вырастет. Нет кадра в эфире — штраф.", _market.DealsFocus);
                BossCoach.Line(steps, BossMood.Mad, "Купил, подписал — закрывай комнату. Сюда не вернуться.", _market.BuysFocus);
                BossCoach.Ensure().Tell(steps.ToArray(), null);
            }
        }

        // Витрина комнаты маркетинга: предложения из её ассета (пул OWM, если ассет пуст), часть — по сиду узла.
        // Предложения, до которых не хватает репутации, видны, но закрыты с причиной.
        List<MarketingOffer> Deals()
        {
            var ep = GameSession.State.episode;
            int seed = (ep != null ? ep.mapSeed : 0) ^ (_marketNode ?? "").GetHashCode();
            return MarketingDesk.Pick(_marketing, OwmOffers.All(int.MaxValue), seed);
        }

        void RefreshDeals()
        {
            if (!_episode.Active)
                return;
            var state = GameSession.State;
            TipOnce("market", "Маркетинг. УЕ, «условные единицы», — деньги этого выпуска: тратишь здесь, в хаб они не переходят. ЕБ копятся на сезон и тратятся в хабе. Контракт не платит сразу: сыграй карту спонсора и оставь этот ролик в монтаже.");
            var ep = state.episode;
            int slots = Progression.ContractSlots(state.writerLevel);
            var views = new List<OfferView>();
            foreach (var offer in Deals())
                views.Add(MarketingDesk.Describe(offer, state, ep, slots, _meta.Find));
            string title = _marketing != null && !string.IsNullOrEmpty(_marketing.title) ? _marketing.title.ToUpperInvariant() : "МАРКЕТИНГ";
            if (_marketing != null && !string.IsNullOrEmpty(_marketing.subtitle))
                title += "  ·  " + _marketing.subtitle;
            string status = "На выпуск: <b>" + (ep != null ? ep.cash : 0) + " УЕ</b>   ·   " + _meta.ReputationLine().Replace("\n", "   ·   ");
            _market.Show(title, status, views, offer =>
            {
                int before = ep != null ? ep.cash : 0;
                bool ok = _meta.TryBuyOffer(offer);
                Sfx.Play(ok ? Cue.Coin : Cue.Miss, ok ? 0.5f : 0.45f);
                if (ok)
                {
                    GameSession.Save();
                    var v = MarketingDesk.Describe(offer, state, ep, slots, _meta.Find);
                    _market.Notice(offer.kind == OfferKind.Contract
                        ? "Контракт подписан: «" + offer.title + "». Карта «" + v.card + "» в колоде выпуска — снимите бренд и вставьте кадр в эфир."
                        : "Куплено: «" + offer.title + "» — " + v.gets + ".  УЕ: " + before + " → " + (ep != null ? ep.cash : 0), true);
                }
                else
                {
                    _market.Notice("Не вышло: " + (_meta.Reject ?? "предложение недоступно"), false);
                }

                RefreshDeals();
            }, LeaveMarketing);
        }

        void LeaveMarketing()
        {
            _screens.Hide();
            if (_market != null)
                _market.Hide();
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
            TeachEvent(def);
        }

        bool TeachingEvent()
        {
            var state = GameSession.State;
            return state != null && state.wantsTutorial && state.tutorialBeat < 5 && _eventView != null && _eventView.Focus != null;
        }

        // Обучение: что такое событие, как читать варианты, что такое процент.
        void TeachEvent(EventRoomDefinition def)
        {
            if (!TeachingEvent())
                return;
            bool chancy = false;
            foreach (var choice in def.choices)
                chancy |= choice.chance < 100;
            var coach = BossCoach.Ensure();
            var steps = new List<CoachStep>();
            BossCoach.Line(steps, BossMood.Think, "Событие — то, что стряслось за кадром, между съёмками. Камеры тут нет: ты не снимаешь, а решаешь. В монтаж отсюда ничего не попадёт.", _eventView.BodyFocus);
            BossCoach.Line(steps, BossMood.Aside, "Под каждым вариантом написано, что он изменит: тон шоу, нервы участников, деньги, карты в колоде. Это последствия — они догонят тебя на съёмках и в эфире.", _eventView.Focus);
            string pick = chancy
                ? "Процент справа — шанс. Не повезёт — сработает строка «Если нет». Выбирай. Переиграть нельзя."
                : "Здесь всё без риска: что написано, то и будет. Выбирай. Переиграть нельзя.";
            coach.Tell(steps.ToArray(), () => coach.Order(BossMood.Mad, pick, _eventView.Focus));
        }

        // Обучение: итог события — что изменилось и куда дальше.
        void TeachEventResult()
        {
            if (!TeachingEvent() || _eventView.ResultFocus == null)
                return;
            BossCoach.Ensure().Order(BossMood.Smug, "Сделано. Штамп — повезло или нет, ниже — что изменилось. Это уже в сезоне. Жми «Дальше».", _eventView.ResultFocus);
        }

        void OpenEvent(EventRoomDefinition def, string nodeId, RuleContext ctx, System.Action<int> applied, Action done)
        {
            var roles = EventResolver.CastRoles(def, ctx.episode, nodeId);
            Func nameOf = ActorName;
            var choices = EventResolver.Choices(def, ctx, roles, nameOf, CardName);
            Sfx.PlayHellCall();
            // Подзаголовок «Событие» не повторяем после «СОБЫТИЕ».
            string sub = def.subtitle != null ? def.subtitle.Trim() : "";
            bool echo = sub.Length == 0 || sub.Equals("событие", System.StringComparison.OrdinalIgnoreCase);
            _eventView.Show("СОБЫТИЕ" + (echo ? "" : "  ·  " + sub.ToUpperInvariant()),
                EventResolver.Fill(def.title, roles, nameOf), EventResolver.Fill(def.body, roles, nameOf),
                def.art != null ? def.art : MapNodeView.Art(def.icon), def.color, choices, EventResolver.Stakes(def), "Уйти", index =>
                {
                    EventOutcome outcome;
                    // Результат всегда со штампом: «получилось» или «не получилось» и что изменилось.
                    bool chancy = index >= 0;
                    if (index < 0 || index >= def.choices.Count)
                    {
                        outcome = new EventOutcome { success = true, text = "Вы уходите, ничего не решив.", summary = "" };
                    }
                    else
                    {
                        var choice = def.choices[index];
                        outcome = EventResolver.Resolve(choice, ctx, roles, nameOf, CardName);
                    }

                    applied?.Invoke(index);
                    if (map != null && map.gameObject.activeInHierarchy)
                        RefreshMap();
                    Sfx.Play(outcome.success ? Cue.Coin : Cue.Miss, 0.5f, outcome.success ? 1.05f : 0.9f);
                    _eventView.ShowResult(outcome, chancy, () =>
                    {
                        Click();
                        done?.Invoke();
                    });
                    TeachEventResult();
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
                GameSession.NewSeason(_content.SeasonDeck(season), season);
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
            _episode.Current.EnsureLists();
            Sfx.Play(Cue.Blip, 0.5f);
            int slots = Progression.AirSlots;
            _cut.ShowMontage(Library(_episode.Current), slots);
            var state = GameSession.State;
            bool lesson = state != null && state.wantsTutorial && state.tutorialBeat == 4 && _cut.LibraryCount >= 1;
            _cut.HoldAir(lesson);
            if (!lesson)
                return;
            // Обучение: библиотека и слоты эфира, потом игрок сам кладёт кадры.
            _montageCoach = 1;
            var coach = BossCoach.Ensure();
            var steps = new List<CoachStep>();
            BossCoach.Line(steps, BossMood.Think, "Монтажная. Сверху всё, что ты снял за выпуск. Наведи на ролик — увидишь, чем он цепляет, ▶ — посмотреть.", _cut.LibraryFocus);
            BossCoach.Line(steps, BossMood.Aside, "Под ними — эфир: три слота. Зритель увидит только то, что лежит в слотах, и в том же порядке. Остальное — в корзину.", _cut.CutFocus);
            string put = MontageNeed() >= 2 ? "Кликни ролик сверху — он ляжет в слот. Положи хотя бы два." : "Кликни ролик сверху — он ляжет в слот.";
            coach.Tell(steps.ToArray(), () => coach.Order(put, _cut.LibraryFocus));
        }

        int MontageNeed()
        {
            return Mathf.Min(2, _cut.LibraryCount);
        }

        // Обучение: кадры в слотах — связи, порядок, прогноз аудитории, затем эфир.
        void OnCutEdited()
        {
            if (_cut == null || _montageCoach == 0)
                return;
            var state = GameSession.State;
            if (state == null || !state.wantsTutorial || state.tutorialBeat != 4)
                return;
            // Вынул кадр — «В ЭФИР» снова погашена, пока в слотах меньше, чем просил босс.
            if (_cut.CutCount < MontageNeed())
            {
                _cut.HoldAir(true);
                return;
            }

            _cut.HoldAir(false);
            if (_montageCoach != 1)
                return;
            _montageCoach = 2;
            var coach = BossCoach.Ensure();
            var steps = new List<CoachStep>();
            if (_cut.CutCount >= 2)
            {
                BossCoach.Line(steps, BossMood.Think, "Между соседними кадрами — связь. ХОРОШАЯ: тот же человек, общая тема, ссора и её последствия. РЕЗКИЙ ПЕРЕХОД: другие люди и другой тон. Наведи на связь — скажет почему.", _cut.CutFocus);
                BossCoach.Line(steps, BossMood.Aside, "Порядок решает: завязка, потом взрыв, потом последствия. Наоборот — история задом наперёд. Кликни кадр в слоте и двигай кнопками РАНЬШЕ и ПОЗЖЕ.", _cut.CutFocus, _cut.MoveFocus);
            }

            BossCoach.Line(steps, BossMood.Smug, "Справа прогноз: как серию увидит аудитория — тон, история, комбо. Это прогноз, точная оценка будет в эфире.", _cut.AudienceFocus);
            BossCoach.Line(steps, BossMood.Stern, "Снизу — из чего сложилась связность: плюсы зелёным, минусы красным. Выше связность — выше рейтинг.", _cut.BreakdownFocus);
            coach.Tell(steps.ToArray(), () => coach.Order("Переставь, если хочешь, и жми «В ЭФИР». Чего нет в слотах — для зрителя не было.", _cut.CutFocus, _cut.MoveFocus, _cut.AirFocus));
        }

        void ConfirmCut(List<string> ids)
        {
            if (!_episode.Active)
                return;
            int count = ids != null ? ids.Count : 0;
            var state = GameSession.State;
            if (state != null && state.wantsTutorial && state.tutorialBeat == 4 && _montageCoach != 0 && count < MontageNeed())
            {
                _cut.HoldAir(true);
                return;
            }

            _episode.Current.finalCut = ids != null ? new List<string>(ids) : new List<string>();
            if (state != null && state.wantsTutorial && state.tutorialBeat < 5)
                state.tutorialBeat = 5;
            _montageCoach = 0;
            _cut.HoldAir(false);
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
            GuideMap();
        }

        // Эфир: деньги и HellTube только по финальному кату. Комната карты к этому моменту уже закрыта.
        void AirEpisode()
        {
            if (!_episode.Active)
                return;
            var episode = _episode.Current;
            episode.EnsureLists();
            var cut = LoadCut(episode);
            // Монтаж V2: связность, комбо и повторы считаются по всей склейке и двигают оценку эфира.
            var report = CutAnalysis.Analyze(cut);
            int coherence = report.coherence;
            var moments = Shells(cut);
            if (!episode.settled)
                Settle(episode, cut, moments, coherence);
            int hit = AiredHit(episode);
            var result = FeedbackGenerator.BuildCut(moments, GameSession.Tone, coherence, hit > 0, report, Brand(episode));
            if (hit > 0)
                result = FeedbackGenerator.ApplySponsor(result, hit);
            result = AirRating(result, episode, cut.Count);
            _airWish = result.nextWish;
            _airWishLabel = result.wish;
            int scene = episode.Number;
            string name = string.IsNullOrEmpty(GameSession.SceneTitle) ? "Без названия" : GameSession.SceneTitle;
            _cut.ShowAir(result, cut, "Серия " + scene + ". «" + name + "»", episode.settledPay, coherence, episode.settledLine);
            var state = GameSession.State;
            if (state == null || !state.wantsTutorial || state.tutorialBeat != 5)
                return;
            BossCoach.Ensure().Freeze(
                "Эфир. Зрители видят только кат. «Нравится» — та же оценка в процентах, не отдельные деньги. Рейтинг кормит чек.",
                () => BossCoach.Ensure().Freeze(
                    BossMood.Smug,
                    "Доход — в ЕБ. Это «единицы бюджета», а не то, что ты подумал. Копятся за сезон, тратишь в студии на людей и карты.",
                    () => BossCoach.Ensure().Freeze(
                        "Обучение окончено. Дальше сам.",
                        () =>
                        {
                            state.tutorialBeat = 6;
                            state.wantsTutorial = false;
                            GameSession.MarkTutorialDone();
                            GameSession.Save();
                        }),
                    _cut.IncomeFocus),
                _cut.LikesFocus);
        }

        static CastMember[] TutorialCast(CastMember[] all)
        {
            var want = new[] { "npc_zloi", "npc_dobryak" };
            var list = new List<CastMember>();
            if (all != null)
            {
                for (int w = 0; w < want.Length; w++)
                {
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (all[i] != null && all[i].id == want[w])
                            list.Add(all[i]);
                    }
                }
            }

            if (list.Count >= 2)
                return list.ToArray();
            int n = all == null ? 0 : Mathf.Min(2, all.Length);
            var fallback = new CastMember[n];
            for (int i = 0; i < n; i++)
                fallback[i] = all[i];
            return fallback;
        }

        void TipOnce(string id, string line)
        {
            var state = GameSession.State;
            if (state == null || state.wantsTutorial || state.tips.Contains(id))
                return;
            BossCoach.Ensure().Freeze(line, () =>
            {
                if (!state.tips.Contains(id))
                    state.tips.Add(id);
                GameSession.Save();
            });
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
                    if (episode.HasFlag("SponsorShield"))
                    {
                        episode.flags.Remove("SponsorShield");
                    }
                    else
                        GameSession.State.sponsorReputation = Mathf.Clamp(GameSession.State.sponsorReputation - 15, 0, 100);
                }
            }

            var result = FeedbackGenerator.BuildCut(moments, GameSession.Tone, coherence, hit > 0, CutAnalysis.Analyze(cut), Brand(episode));
            if (hit > 0)
                result = FeedbackGenerator.ApplySponsor(result, hit);
            result = AirRating(result, episode, cut.Count);
            bool hadTasks = GameSession.State.tasks.Count > 0;
            bool wishDone = GameSession.State.Resolve(moments, GameSession.Tone);
            // Пустой эфир не оплачивается: зрителю нечего было смотреть.
            bool empty = cut.Count == 0;
            // Модификаторы эфира из событий: доплата спонсора — только если реклама вышла, выплата за эфир — всегда.
            int bonus = empty ? 0 : EpisodeState.Sum(episode.broadcastModifiers, EpisodeState.AirPay);
            if (sponsorMoney > 0)
                sponsorMoney = Mathf.Max(0, sponsorMoney + EpisodeState.Sum(episode.broadcastModifiers, EpisodeState.AirSponsorPay));
            int pay = empty ? 0 : Mathf.Max(0, Progression.Payout(result.score, GameSession.State.castLevel, wishDone) + sponsorMoney + bonus);
            GameSession.State.money += pay;
            GameSession.State.ratingSum += result.score;
            GameSession.State.rated++;
            string line = empty ? "0 ЕБ   ·   эфир пустой — платить не за что" : PayLine(pay, hadTasks, wishDone);
            if (sponsorMoney > 0)
                line += "   ·   спонсор +" + sponsorMoney + " ЕБ, отзывы −" + hit;
            else if (playedAd)
                line += "   ·   реклама не в эфире, выплаты нет";
            if (bonus != 0)
                line += "   ·   события выпуска " + (bonus > 0 ? "+" : "−") + Mathf.Abs(bonus) + " ЕБ";
            episode.settled = true;
            episode.settledPay = pay;
            episode.settledSponsor = sponsorMoney;
            episode.settledLine = line;
            GameSession.Save();
        }

        // Модификатор эфира «оценка» из событий выпуска (в десятых балла). Пустой эфир не двигает: смотреть нечего.
        static FeedbackResult AirRating(FeedbackResult result, EpisodeState episode, int clips)
        {
            int tenths = episode != null ? EpisodeState.Sum(episode.broadcastModifiers, EpisodeState.AirRating) : 0;
            if (tenths == 0 || clips <= 0 || result.score <= 0f)
                return result;
            result.score = Mathf.Clamp(Mathf.Round((result.score + tenths / 10f) * 10f) / 10f, 1f, 10f);
            return result;
        }

        // Бренд спонсора в эфире (для комментариев): из названия карты контракта «Hell Cola — Холодильник».
        string Brand(EpisodeState episode)
        {
            if (episode == null || episode.contracts == null)
                return null;
            foreach (var c in episode.contracts)
            {
                if (c == null || string.IsNullOrEmpty(c.grantedCardId))
                    continue;
                string name = CardName(c.grantedCardId);
                int dash = name.IndexOf(" — ", System.StringComparison.Ordinal);
                return dash > 0 ? name.Substring(0, dash) : name.Replace("Постер ", "").Trim('«', '»');
            }

            return null;
        }

        // Итог монтажа: спонсор, задачи зрителей, купленные подсказки — строки под прогнозом аудитории.
        List<string> MontageExtra(List<FootageClip> chosen)
        {
            var lines = new List<string>();
            var ep = _episode != null && _episode.Active ? _episode.Current : null;
            var state = GameSession.State;
            if (ep == null || state == null || chosen == null)
                return lines;
            var moments = Shells(chosen);
            bool ad = chosen.Exists(c => c != null && c.tags != null && c.tags.Contains(MomentTags.Sponsor));
            foreach (var contract in ep.contracts)
            {
                if (contract == null || contract.status != ContractStatus.Active)
                    continue;
                bool inCut = chosen.Exists(c => c != null && contract.matchingFootageIds.Contains(c.id));
                lines.Add("Спонсор «" + CardName(contract.grantedCardId) + "»: " + (inCut ? "<color=#7FE08A>выполнен ✓</color>" : ad ? "<color=#F2C35C>реклама есть, но не его кадр</color>" : "<color=#FF7A5C>кадра нет ✗</color>"));
            }

            foreach (var task in state.tasks)
            {
                if (task == null)
                    continue;
                bool met = Progression.WishMet(task.id, moments, GameSession.Tone);
                lines.Add("Задача зрителей «" + task.label + "»: " + (met ? "<color=#7FE08A>выполнено ✓</color>" : "<color=#A89F96>нет</color>"));
            }

            // Купленные в маркетинге подсказки.
            if (ep.HasFlag("MontageHint"))
                lines.Add(BestPair());
            if (ep.HasFlag("ToneForecast") && chosen.Count > 0)
            {
                var report = CutAnalysis.Analyze(chosen);
                var forecast = AirRating(FeedbackGenerator.BuildCut(moments, GameSession.Tone, report.coherence, false, report, null), ep, chosen.Count);
                lines.Add("<color=#8FE3FF>Мониторинг HellTube: прогноз оценки ≈ " + forecast.score.ToString("0.0") + "</color>");
            }

            return lines;
        }

        // «Срочный монтажный совет»: лучшая связь среди всего отснятого.
        string BestPair()
        {
            var library = Library(_episode.Current);
            int best = int.MinValue;
            string text = null;
            for (int i = 0; i < library.Count; i++)
            {
                for (int j = 0; j < library.Count; j++)
                {
                    if (i == j)
                        continue;
                    var r = CutAnalysis.Analyze(new List<FootageClip> { library[i], library[j] });
                    if (r.links.Count == 1 && r.links[0].score > best)
                    {
                        best = r.links[0].score;
                        text = "«" + r.clips[0].what + "» → «" + r.clips[1].what + "»";
                    }
                }
            }

            return text == null ? "<color=#8FE3FF>Совет монтажёра: снимите хотя бы два кадра</color>" : "<color=#8FE3FF>Совет монтажёра: лучшая связь — " + text + "</color>";
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
                return "+" + pay + " ЕБ   ·   задач не было";
            if (wishDone)
                return "+" + pay + " ЕБ   ·   задача закрыта ×1.3";
            return "+" + pay + " ЕБ   ·   задачи открыты, мимо";
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

        // Меню — MainMenu. Хаб, карта, монтаж, эфир — Intro. Съёмка — SceneMainTheme. Смена трека кроссфейдом.
        void SyncBed()
        {
            SilenceMenuSource();
            bool menuUp = menu != null && menu.gameObject.activeSelf
                || settings != null && settings.gameObject.activeSelf && _back == menu.gameObject;
            MusicBed.Play(menuUp ? MusicBed.Menu : MusicBed.Theme);
        }

        static void SilenceMenuSource()
        {
            var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include);
            for (int i = 0; i < sources.Length; i++)
            {
                var src = sources[i];
                if (src == null || src.gameObject.name != "MainMenu")
                    continue;
                if (src.GetComponent<MainMenuView>() != null)
                    continue;
                src.playOnAwake = false;
                src.Stop();
                src.enabled = false;
            }
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
