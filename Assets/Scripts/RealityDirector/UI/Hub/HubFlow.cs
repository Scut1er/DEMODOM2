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
            _cut.Edited += OnCutEdited;
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
                hub.ApplyTutorial(teach, talking, true);
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
                MusicBed.Play(MusicBed.Scene);
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
            var state = GameSession.State;
            if (state != null && state.wantsTutorial && state.tutorialBeat == 0)
                BossCoach.Ensure().Order("Жми «Начать съёмку». Каст первого выпуска я уже собрал.", hub.StartFocus());
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
                CastMember[] roster = _cast;
                List<string> prefill = state.castPick;
                if (firstLesson)
                {
                    roster = TutorialCast(_cast);
                    prefill = new List<string>();
                    for (int i = 0; i < roster.Length; i++)
                        prefill.Add(roster[i].id);
                    min = roster.Length;
                    seats = roster.Length;
                }
                else if (state.castLevel < 3)
                    TipOnce("secret", "Скрытая черта закрыта, пока кастинг ниже третьего. Апгрейд кастинга — и на карточке будет видно, кто клептоман, а кто пранкер.");

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
            var state = GameSession.State;
            if (state != null && state.wantsTutorial && state.tutorialBeat == 2)
                BossCoach.Ensure().Order("Жми «Снимать». Светящаяся комната — единственная, куда можно. Назад по карте нельзя.", map.EnterFocus != null ? map.EnterFocus : map.NodeFocus(RoomType.Situation));
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
            MusicBed.Play(MusicBed.Scene);
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

        // Пул маркетинга — OWM. Старый список комнаты больше не режет витрину.
        List<MarketingOffer> Deals()
        {
            int reputation = GameSession.State.sponsorReputation;
            return OwmOffers.All(reputation);
        }

        void RefreshDeals()
        {
            if (!_episode.Active)
                return;
            string head = _meta.ReputationLine();
            if (!string.IsNullOrEmpty(_meta.Reject))
                head += "\n" + _meta.Reject;
            TipOnce("market", "Маркетинг. Касса — деньги этого выпуска. Кр копится на сезон и тратится в хабе. Контракт не платит сразу: сыграй карту спонсора и оставь этот ролик в монтаже.");
            _screens.ShowDeals(head, Deals(), offer =>
            {
                bool ok = _meta.TryBuyOffer(offer);
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
                "Событие. Это не клип: в библиотеку футажа ничего не падает.",
                () => BossCoach.Ensure().Order(BossMood.Mad, "Варианты снизу. Если на кнопке процент — это шанс. Выбери одну. Назад выбор не переигрывается.", _eventView.Focus),
                _eventView.BodyFocus);
        }

        void OpenEvent(EventRoomDefinition def, string nodeId, RuleContext ctx, System.Action<int> applied, Action done)
        {
            var roles = EventResolver.CastRoles(def, ctx.episode, nodeId);
            Func nameOf = ActorName;
            var choices = EventResolver.Choices(def, ctx, roles, nameOf);
            Sfx.PlayHellCall();
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
                    if (map != null && map.gameObject.activeInHierarchy)
                        RefreshMap();
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
            int slots = Progression.AirSlots;
            _cut.ShowMontage(Library(_episode.Current), slots);
            var state = GameSession.State;
            bool lesson = state != null && state.wantsTutorial && state.tutorialBeat == 4 && _cut.LibraryCount >= 2;
            _cut.HoldAir(lesson);
            if (!lesson)
                return;
            _montageCoach = 1;
            BossCoach.Ensure().Order("Сверху снятое. Кликни ролик — он ляжет в слот снизу. Положи оба: мест три, РАНЬШЕ и ПОЗЖЕ меняют порядок.", _cut.LibraryFocus);
        }

        void OnCutEdited()
        {
            if (_montageCoach != 1 || _cut == null || _cut.CutCount < 2)
                return;
            var state = GameSession.State;
            if (state == null || !state.wantsTutorial || state.tutorialBeat != 4)
                return;
            _montageCoach = 2;
            _cut.HoldAir(false);
            BossCoach.Ensure().Freeze(
                "Оба в кате. Соседние про одно поднимают связность — цифра сверху справа. Про разное это нарезка.",
                () => BossCoach.Ensure().Order("Жми «В ЭФИР». Чего нет в кате — для зрителя не было.", _cut.AirFocus),
                _cut.CoherenceFocus);
        }

        void ConfirmCut(List<string> ids)
        {
            if (!_episode.Active)
                return;
            _episode.Current.finalCut = ids != null ? new List<string>(ids) : new List<string>();
            var state = GameSession.State;
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
            var state = GameSession.State;
            if (state == null || !state.wantsTutorial || state.tutorialBeat != 5)
                return;
            BossCoach.Ensure().Freeze(
                "Эфир. Зрители видят только кат. «Нравится» — та же оценка в процентах, не отдельные деньги. Рейтинг кормит чек.",
                () => BossCoach.Ensure().Freeze(
                    "Обучение окончено. Дальше сам.",
                    () =>
                    {
                        state.tutorialBeat = 6;
                        state.wantsTutorial = false;
                        GameSession.Save();
                    }),
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
