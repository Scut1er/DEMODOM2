using System.Collections;
using System.Collections.Generic;
using RealityDirector.Capture;
using RealityDirector.Core;
using RealityDirector.Events;
using RealityDirector.Meta;
using RealityDirector.NPC;
using RealityDirector.UI;
using RealityDirector.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace RealityDirector
{
    public class PitchFlow : MonoBehaviour
    {
        static readonly Vector2 ZloiHome = new Vector2(-6.5f, 1.65f);
        static readonly Vector2 DobryakHome = new Vector2(-4.1f, 1.65f);
        static readonly Vector2 FleePoint = new Vector2(6.2f, 1.65f);

        PitchPhase _phase = PitchPhase.Intro;
        PitchContent _content;
        PitchUi _ui;
        EpisodeContext _context;
        EventExecutor _executor;
        CaptureSystem _capture;
        CameraShake _shake;
        Interactable _fridge;
        Interactable _bathDoor;
        GameObject _boards;
        GameObject _bathroom;
        bool _bathOpen;
        NPCController _zloi;
        NPCController _dobryak;
        readonly List<NPCController> _cast = new List<NPCController>();
        EventDefinition _armed;
        bool _endQueued;
        bool _inputLock;
        bool _suppressHoldCam;
        float _rumble;
        readonly SeasonTone _tone = new SeasonTone();
        readonly SeasonState _state = new SeasonState();
        EventDefinition[] _hand = new EventDefinition[0];
        string _prepReject;
        ViewerWishId _offerId;
        string _offerLabel;

        void Awake()
        {
            Interactable.ResetGlobal();
            _content = PitchContent.Create();
            _context = new EpisodeContext();
            _context.Bind();
            SetupCamera();
            SetupInput();
            _ui = PitchUi.Build();
            _state.Reset(_content.StarterIds());
            _ui.BindFlow(OpenPrep, EndEpisode, ToggleCamera, ResetSeason);
            _ui.BindMeta(UpgradeCrew, TogglePick, BuyCard, Embark);
            BuildApartment();
            WireTags();
            _executor = gameObject.AddComponent<EventExecutor>();
            _capture = gameObject.AddComponent<CaptureSystem>();
            _capture.Init(_context, _cast, () => _fridge != null && _fridge.IsOnFire);
            _capture.Captured += OnCaptured;
            _capture.Missed += () =>
            {
                _ui.Toast("В рамке никого.");
                Sfx.Play(Cue.Miss, 0.45f);
            };
            _ui.ShowIntro();
            Sfx.Bind(gameObject);

            NPCController.FightStarted -= OnFight;
            NPCController.FightStarted += OnFight;
        }

        void OnDestroy()
        {
            if (_context != null)
                _context.Unbind();
            NPCController.FightStarted -= OnFight;
            if (_content != null)
                _content.DestroyAssets();
        }

        void Update()
        {
            bool fighting = (_zloi != null && _zloi.IsFighting) || (_dobryak != null && _dobryak.IsFighting);
            if (fighting)
            {
                _rumble -= Time.deltaTime;
                if (_rumble <= 0f)
                {
                    _shake.Punch(0.045f, 0.1f);
                    _rumble = 0.18f;
                    if (_zloi != null && _dobryak != null)
                    {
                        Vector3 mid = (_zloi.transform.position + _dobryak.transform.position) * 0.5f;
                        FadeBit.Burst(mid, 3, new Color(1f, 0.82f, 0.55f, 1f));
                        Sfx.Play(Cue.Slap, 0.28f, Random.Range(0.86f, 1.2f));
                    }
                }
            }

            RefreshTargeting();
        }

        void LateUpdate()
        {
            if (_phase == PitchPhase.Intro)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                    OpenPrep();
                return;
            }

            if (_phase == PitchPhase.Feedback)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                    ContinueAfterFeedback();
                return;
            }

            if (_phase != PitchPhase.Play || _inputLock)
                return;

            ReadPlayInput();
            _ui.SetCaptureMode(_capture.Mode);
            _ui.SetHint(Coach());
        }

        void SetupCamera()
        {
            var cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 7.45f;
            cam.transform.position = new Vector3(0.2f, 3.35f, -10f);
            cam.backgroundColor = new Color(0.1f, 0.08f, 0.07f, 1f);
            _shake = cam.gameObject.AddComponent<CameraShake>();
            _shake.Base = cam.transform.position;
        }

        static void SetupInput()
        {
            var existing = FindAnyObjectByType<EventSystem>();
            if (existing != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            if (InputSystem.actions != null)
                module.actionsAsset = InputSystem.actions;
        }

        void BuildApartment()
        {
            var root = new GameObject("Apartment").transform;
            const float y = 2.6f;
            SpriteUtil.Box(root, "foundation", new Vector3(0.15f, y, 0f), new Vector2(16.6f, 5.35f), new Color(0.16f, 0.12f, 0.1f), -2);
            Place(root, "Living", IllustratedArt.Wood, new Vector3(-5.35f, y, 0f), new Vector2(4.55f, 4.55f), 0);
            Place(root, "Kitchen", IllustratedArt.Tile, new Vector3(0.05f, y, 0f), new Vector2(5.15f, 4.55f), 0);
            Place(root, "Bedroom", IllustratedArt.Wood, new Vector3(5.5f, y, 0f), new Vector2(4.7f, 4.55f), 0);
            WallV(root, -2.85f);
            WallV(root, 2.75f);
            WallH(root, -1.55f, 4.95f, 1.9f);
            WallH(root, 1.65f, 4.95f, 1.9f);

            Place(root, "Rug", IllustratedArt.Rug, new Vector3(-5.45f, 3.15f, 0f), new Vector2(2.1f, 1.25f), 1);
            Place(root, "Sofa", IllustratedArt.Sofa, new Vector3(-5.45f, 3.35f, 0f), new Vector2(2.15f, 1.15f), 4);
            Place(root, "Table", IllustratedArt.Table, new Vector3(-0.55f, 2.9f, 0f), new Vector2(1.45f, 0.95f), 4);
            Place(root, "Stove", IllustratedArt.Stove, new Vector3(2.05f, 3.55f, 0f), new Vector2(0.85f, 0.85f), 4);
            Place(root, "Bed", IllustratedArt.Bed, new Vector3(6.2f, 3.15f, 0f), new Vector2(1.85f, 2.35f), 4);
            Place(root, "Plant", IllustratedArt.Plant, new Vector3(-7.15f, 4.15f, 0f), new Vector2(0.7f, 0.9f), 5);
            Place(root, "WindowL", IllustratedArt.Window, new Vector3(-5.4f, 4.45f, 0f), new Vector2(1.35f, 0.7f), 5);
            Place(root, "WindowR", IllustratedArt.Window, new Vector3(6.3f, 4.45f, 0f), new Vector2(1.35f, 0.7f), 5);

            _fridge = BuildFridge(root);
            _boards = BuildDoor(root);
            _bathroom = BuildBathroom(root);
            _bathroom.SetActive(false);

            _zloi = BuildNpc("npc_zloi", "Злой", "Злому", _content.Aggressive, _content.AggressiveRules, ZloiHome, true);
            _dobryak = BuildNpc("npc_dobryak", "Добряк", "Добряку", _content.Sentimental, _content.SentimentalRules, DobryakHome, false);
            _zloi.Rival = _dobryak;
            _dobryak.Rival = _zloi;
            _zloi.FleePoint = FleePoint;
            _dobryak.FleePoint = FleePoint;
            _zloi.ChaseSpeed = 2.75f;
            _dobryak.PanicSpeed = 1.9f;
            _cast.Add(_zloi);
            _cast.Add(_dobryak);

            var zSpawn = new GameObject("Spawn_Zloi").transform;
            zSpawn.SetParent(root, false);
            zSpawn.position = ZloiHome;
            var dSpawn = new GameObject("Spawn_Dobryak").transform;
            dSpawn.SetParent(root, false);
            dSpawn.position = DobryakHome;

            RoomTag(root, new Vector3(-5.35f, 4.7f, 0f), "ГОСТИНАЯ");
            RoomTag(root, new Vector3(-1.7f, 4.15f, 0f), "КУХНЯ");
            RoomTag(root, new Vector3(5.5f, 4.7f, 0f), "СПАЛЬНЯ");
            var bathLabel = new GameObject("BathLabel").transform;
            bathLabel.SetParent(root, false);
            bathLabel.position = new Vector3(0.05f, 7.9f, 0f);
            _ui.AddTag(bathLabel, () => _bathOpen ? "ВАННАЯ" : "", new Color(0.25f, 0.32f, 0.34f), Vector2.zero, 16, false);
        }

        static void Place(Transform parent, string name, Sprite sprite, Vector3 pos, Vector2 size, int order)
        {
            var renderer = SpriteUtil.Show(parent, name, pos, sprite, order);
            SpriteUtil.Fit(renderer, size);
        }

        static void WallV(Transform root, float x)
        {
            Place(root, "wall", IllustratedArt.Wall, new Vector3(x, 3.75f, 0f), new Vector2(0.28f, 2.15f), 3);
            Place(root, "wall", IllustratedArt.Wall, new Vector3(x, 0.85f, 0f), new Vector2(0.24f, 1.15f), 3);
        }

        static void WallH(Transform root, float x, float y, float width)
        {
            Place(root, "wall", IllustratedArt.Wall, new Vector3(x, y, 0f), new Vector2(width, 0.32f), 3);
        }

        GameObject BuildBathroom(Transform root)
        {
            var go = new GameObject("Bathroom");
            go.transform.SetParent(root, false);
            var t = go.transform;
            Place(t, "floor", IllustratedArt.BathTile, new Vector3(0.05f, 6.4f, 0f), new Vector2(4.9f, 3.55f), 0);
            Place(t, "wallL", IllustratedArt.Wall, new Vector3(-2.3f, 6.55f, 0f), new Vector2(0.24f, 3.05f), 3);
            Place(t, "wallR", IllustratedArt.Wall, new Vector3(2.4f, 6.55f, 0f), new Vector2(0.24f, 3.05f), 3);
            Place(t, "wallT", IllustratedArt.Wall, new Vector3(0.05f, 8.05f, 0f), new Vector2(4.7f, 0.28f), 3);
            Place(t, "shower", IllustratedArt.Shower, new Vector3(-1.35f, 7.15f, 0f), new Vector2(1.2f, 1.2f), 4);
            Place(t, "toilet", IllustratedArt.Toilet, new Vector3(0.15f, 7.2f, 0f), new Vector2(0.6f, 0.85f), 4);
            Place(t, "sink", IllustratedArt.Sink, new Vector3(1.45f, 7.15f, 0f), new Vector2(1.05f, 0.7f), 4);
            return go;
        }

        GameObject BuildDoor(Transform root)
        {
            var go = new GameObject("BathDoor");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(0.05f, 5.15f, 0f);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.15f, 1.45f);
            var ring = SpriteUtil.Show(go.transform, "ring", Vector3.zero, IllustratedArt.Glow, 5);
            SpriteUtil.Fit(ring, new Vector2(1.5f, 1.8f));
            ring.color = new Color(1f, 0.86f, 0.25f, 0.9f);
            var body = SpriteUtil.Show(go.transform, "boards", Vector3.zero, IllustratedArt.Boards, 6);
            SpriteUtil.Fit(body, new Vector2(1.05f, 1.45f));
            var interactable = go.AddComponent<Interactable>();
            interactable.Id = "bath_door";
            interactable.DisplayName = "Дверь";
            interactable.Setup(body, ring, null, null, null);
            _bathDoor = interactable;
            return go;
        }

        Interactable BuildFridge(Transform root)
        {
            var go = new GameObject("Fridge");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(1.15f, 3.45f, 0f);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.95f, 1.55f);

            var glow = SpriteUtil.Show(go.transform, "glow", Vector3.zero, IllustratedArt.Glow, 4);
            SpriteUtil.Fit(glow, new Vector2(1.55f, 1.95f));
            var ring = SpriteUtil.Show(go.transform, "ring", Vector3.zero, IllustratedArt.Glow, 5);
            SpriteUtil.Fit(ring, new Vector2(1.25f, 1.8f));
            ring.color = new Color(1f, 0.86f, 0.25f, 0.9f);
            var body = SpriteUtil.Show(go.transform, "body", Vector3.zero, IllustratedArt.Fridge, 6);
            SpriteUtil.Fit(body, new Vector2(0.95f, 1.6f));

            var flames = new Transform[5];
            var renderers = new SpriteRenderer[5];
            var colors = new[]
            {
                new Color(1f, 0.45f, 0.05f, 1f),
                new Color(1f, 0.85f, 0.25f, 1f),
                new Color(1f, 0.25f, 0.05f, 1f),
                new Color(1f, 0.6f, 0.1f, 1f),
                new Color(1f, 0.4f, 0.08f, 1f)
            };
            for (int i = 0; i < flames.Length; i++)
            {
                float x = -0.28f + i * 0.14f;
                var flame = SpriteUtil.Show(go.transform, "flame" + i, new Vector3(x, 0.55f, 0f), IllustratedArt.Flame, 9);
                SpriteUtil.Fit(flame, new Vector2(0.22f, 0.36f));
                flame.color = colors[i];
                flames[i] = flame.transform;
                renderers[i] = flame;
            }

            var interactable = go.AddComponent<Interactable>();
            interactable.Id = "fridge";
            interactable.DisplayName = "Холодильник";
            interactable.Setup(body, ring, glow, flames, renderers);
            return interactable;
        }

        NPCController BuildNpc(string id, string displayName, string accusative, TraitDefinition trait, ReactionRuleSet rules, Vector2 home, bool angry)
        {
            var go = new GameObject(id);
            go.transform.position = home;
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.7f, 1.25f);

            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            var ring = SpriteUtil.Show(visual, "ring", new Vector3(0f, -0.15f, 0f), IllustratedArt.Glow, 8);
            SpriteUtil.Fit(ring, new Vector2(1.25f, 0.7f));
            ring.color = new Color(1f, 0.86f, 0.25f, 0.9f);
            var body = SpriteUtil.Show(visual, "body", Vector3.zero, angry ? IllustratedArt.PersonAngry : IllustratedArt.PersonKind, 10);
            SpriteUtil.Fit(body, new Vector2(0.95f, 1.52f));

            var npc = go.AddComponent<NPCController>();
            npc.Id = id;
            npc.DisplayName = displayName;
            npc.AccusativeName = accusative;
            npc.Trait = trait;
            npc.Rules = rules;
            npc.Home = home;
            npc.BindVisual(visual, ring);
            npc.ResetState();
            return npc;
        }

        void WireTags()
        {
            var paper = new Color(0.96f, 0.93f, 0.88f, 1f);
            var hot = new Color(1f, 0.78f, 0.35f, 1f);
            _ui.AddTag(_zloi.transform, () => _zloi.DisplayName + "\n" + _zloi.Trait.displayName, paper, new Vector2(0f, 78f), 18, true);
            _ui.AddTag(_dobryak.transform, () => _dobryak.DisplayName + "\n" + _dobryak.Trait.displayName, paper, new Vector2(0f, 78f), 18, true);
            _ui.AddTag(_zloi.transform, () => _zloi.Emote, hot, new Vector2(0f, 132f), 22, false);
            _ui.AddTag(_dobryak.transform, () => _dobryak.Emote, hot, new Vector2(0f, 132f), 22, false);
        }

        void RoomTag(Transform root, Vector3 position, string text)
        {
            var anchor = new GameObject(text).transform;
            anchor.SetParent(root, false);
            anchor.position = position;
            var muted = new Color(0.28f, 0.2f, 0.18f, 1f);
            _ui.AddTag(anchor, () => text, muted, Vector2.zero, 16, false);
        }

        void OpenPrep()
        {
            if (_phase != PitchPhase.Intro && _phase != PitchPhase.Feedback && _phase != PitchPhase.Prep)
                return;
            TrimPicked();
            _phase = PitchPhase.Prep;
            _ui.ShowPrep(BuildPrep());
            _ui.RefreshTone(_tone);
            RefreshTasks(true);
        }

        void Embark()
        {
            var model = BuildPrep();
            if (!model.canStart || _phase != PitchPhase.Prep)
            {
                Sfx.Play(Cue.Miss, 0.4f);
                return;
            }

            Sfx.Play(Cue.Card, 0.45f, 0.8f);
            ResetSet();
            _hand = SelectedHand();
            _state.picked.Clear();
            _prepReject = null;
            _ui.ClearHand();
            _ui.BindCards(_hand, Arm);
            _ui.ClearUsed();
            _ui.SetArmed(null);
            _capture.Capacity = Progression.CaptureSlots(_state.operatorLevel);
            _ui.SetCaptureCapacity(_capture.Capacity);
            _ui.SetEpisodeTitle("СЕРИЯ " + (_state.episodeIndex + 1) + " / " + Progression.SeasonLength + "\nты режиссёр, не участник");
            _phase = PitchPhase.Play;
            _ui.ShowPlay();
            _ui.RefreshTone(_tone);
            _ui.SetSlots(_capture.Moments, _capture.Capacity);
            RefreshTasks(true);
        }

        void ContinueAfterFeedback()
        {
            if (_phase != PitchPhase.Feedback)
                return;
            if (_ui.TaskTaken)
                _state.Accept(_offerId, _offerLabel);
            _state.episodeIndex++;
            if (_state.episodeIndex >= Progression.SeasonLength)
            {
                _phase = PitchPhase.SeasonEnd;
                _ui.ShowSeasonEnd(SeasonEndText(), ResetSeason);
                _ui.RefreshTone(_tone);
                return;
            }

            OpenPrep();
        }

        void ShowVision()
        {
            if (_phase != PitchPhase.Feedback)
                return;
            _phase = PitchPhase.Vision;
            _ui.ShowVision();
        }

        void EndEpisode()
        {
            if (_phase != PitchPhase.Play)
                return;
            _phase = PitchPhase.Feedback;
            Time.timeScale = 1f;
            _inputLock = false;
            _armed = null;
            _capture.SetSticky(false);
            _capture.SetHold(false);
            _ui.SetArmed(null);
            _ui.SetCaptureMode(false);
            var result = FeedbackGenerator.Build(_context, _capture.Moments, _cast, _tone);
            bool hadTasks = _state.tasks.Count > 0;
            bool wishDone = _state.Resolve(_capture.Moments, _tone);
            int pay = Progression.Payout(result.score, _state.castLevel, wishDone);
            _state.money += pay;
            result.payLine = PayLine(pay, hadTasks, wishDone);
            Sfx.Play(Cue.Coin, wishDone ? 0.7f : 0.5f, wishDone ? 1.12f : 1f);
            _offerId = result.nextWish;
            _offerLabel = result.wish;
            _ui.ShowFeedback(result, ContinueAfterFeedback);
            RefreshTasks(true);
        }

        void ResetSeason()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            _phase = PitchPhase.Intro;
            _endQueued = false;
            _inputLock = false;
            _suppressHoldCam = false;
            _armed = null;
            _prepReject = null;
            _hand = new EventDefinition[0];
            _tone.Reset();
            _state.Reset(_content.StarterIds());
            _ui.RefreshTone(_tone);
            _ui.ClearHand();
            ResetSet();
            _ui.ShowIntro();
        }

        void ResetSet()
        {
            _endQueued = false;
            _inputLock = false;
            _armed = null;
            _context.Reset();
            if (_fridge != null)
                _fridge.Extinguish();
            CloseBathroom();
            if (_zloi != null)
                _zloi.ResetState();
            if (_dobryak != null)
                _dobryak.ResetState();
            _capture.ResetCapture();
            _ui.ClearUsed();
            _ui.SetArmed(null);
            _ui.ClearSlots();
        }

        void ArmAt(int index)
        {
            if (_hand == null || index < 0 || index >= _hand.Length)
                return;
            Arm(_hand[index]);
        }

        int SlotsNow()
        {
            return Progression.EventSlots(_state.writerLevel, _state.episodeIndex);
        }

        void TrimPicked()
        {
            int slots = SlotsNow();
            for (int i = _state.picked.Count - 1; i >= 0; i--)
            {
                if (!_state.Owns(_state.picked[i]) || _state.IsPlayed(_state.picked[i]))
                    _state.picked.RemoveAt(i);
            }

            while (_state.picked.Count > slots)
                _state.picked.RemoveAt(_state.picked.Count - 1);
        }

        EventDefinition[] SelectedHand()
        {
            var list = new List<EventDefinition>();
            for (int i = 0; i < _state.picked.Count; i++)
            {
                var def = _content.Find(_state.picked[i]);
                if (def != null)
                    list.Add(def);
            }

            return list.ToArray();
        }

        PrepModel BuildPrep()
        {
            int slots = SlotsNow();
            int available = _state.UnplayedCount();
            int need = Mathf.Min(slots, available);
            int later = Progression.EventSlots(_state.writerLevel, 1);
            string writers = _state.episodeIndex <= 0
                ? "ур. " + _state.writerLevel + "\nсейчас 1 карта\nдальше " + later
                : "ур. " + _state.writerLevel + "\nкарт в серию: " + later;
            int hype = Mathf.RoundToInt(Progression.HypeBonus(_state.castLevel) * 100f);

            return new PrepModel
            {
                episodeNumber = _state.episodeIndex + 1,
                money = _state.money,
                slots = slots,
                picked = _state.picked.Count,
                available = available,
                canStart = need > 0 && _state.picked.Count == need,
                reject = _prepReject,
                slotsLabel = _state.episodeIndex <= 0
                    ? "Обучение: в серию берётся 1 карта. Со следующей серии слоты от сценаристов, минимум 2."
                    : "Карт в серию: " + slots + "  ·  несыгранных в колоде: " + available,
                crew = new[]
                {
                    CrewButtonOf("УЧАСТНИКИ", true, _state.castLevel, "ур. " + _state.castLevel + "\nчек +" + hype + "%\nв кадре пока 2"),
                    CrewButtonOf("ОПЕРАТОРЫ", false, _state.operatorLevel, "ур. " + _state.operatorLevel + "\nкадров: " + Progression.CaptureSlots(_state.operatorLevel)),
                    CrewButtonOf("СЦЕНАРИСТЫ", false, _state.writerLevel, writers)
                },
                deck = CollectCards(false),
                shop = CollectCards(true)
            };
        }

        CrewButton CrewButtonOf(string title, bool cast, int level, string detail)
        {
            bool maxed = level >= Progression.MaxLevel;
            int cost = Progression.UpgradeCost(cast, level);
            return new CrewButton
            {
                title = title,
                detail = detail,
                maxed = maxed,
                affordable = !maxed && _state.money >= cost,
                costLabel = maxed ? "МАКС" : "апгрейд " + cost + " кр"
            };
        }

        PrepCard[] CollectCards(bool shop)
        {
            var list = new List<PrepCard>();
            var all = _content.All;
            for (int i = 0; i < all.Length; i++)
            {
                var def = all[i];
                bool owned = _state.Owns(def.id);
                if (shop)
                {
                    if (owned || def.price <= 0)
                        continue;
                }
                else if (!owned || _state.IsPlayed(def.id))
                {
                    continue;
                }

                var moods = new ShowMood[def.moods != null ? def.moods.Count : 0];
                for (int m = 0; m < moods.Length; m++)
                    moods[m] = def.moods[m];
                list.Add(new PrepCard
                {
                    id = def.id,
                    title = def.displayName,
                    hint = def.hint,
                    price = def.price,
                    picked = _state.picked.Contains(def.id),
                    color = def.cardColor,
                    art = def.cardArt,
                    moods = moods
                });
            }

            return list.ToArray();
        }

        void UpgradeCrew(int index)
        {
            if (_phase != PitchPhase.Prep)
                return;
            bool cast = index == 0;
            int level = index == 0 ? _state.castLevel : index == 1 ? _state.operatorLevel : _state.writerLevel;
            int cost = Progression.UpgradeCost(cast, level);
            if (level >= Progression.MaxLevel || cost <= 0 || _state.money < cost)
            {
                Sfx.Play(Cue.Miss, 0.45f);
                return;
            }
            Sfx.Play(Cue.Coin, 0.45f, 0.9f);
            _state.money -= cost;
            if (index == 0)
                _state.castLevel++;
            else if (index == 1)
                _state.operatorLevel++;
            else
                _state.writerLevel++;
            _prepReject = null;
            OpenPrep();
        }

        void TogglePick(string id)
        {
            if (_phase != PitchPhase.Prep || _state.IsPlayed(id) || !_state.Owns(id))
                return;
            if (_state.picked.Contains(id))
            {
                _state.picked.Remove(id);
                _prepReject = null;
            }
            else if (_state.picked.Count >= SlotsNow())
            {
                _prepReject = "Слоты заняты — сними одну карту.";
                Sfx.Play(Cue.Miss, 0.4f);
            }
            else
            {
                _state.picked.Add(id);
                _prepReject = null;
            }

            OpenPrep();
        }

        void BuyCard(string id)
        {
            if (_phase != PitchPhase.Prep || _state.Owns(id))
                return;
            var def = _content.Find(id);
            if (def == null || def.price <= 0 || _state.money < def.price)
            {
                _prepReject = "Не хватает бюджета.";
                Sfx.Play(Cue.Miss, 0.5f);
                OpenPrep();
                return;
            }

            Sfx.Play(Cue.Coin, 0.5f);
            _state.money -= def.price;
            _state.owned.Add(id);
            _prepReject = null;
            OpenPrep();
        }

        void RefreshTasks(bool visible)
        {
            var lines = new List<string>(_state.tasks.Count);
            for (int i = 0; i < _state.tasks.Count; i++)
                lines.Add(_state.tasks[i].label);
            _ui.SetTasks(lines, visible);
        }

        string PayLine(int pay, bool hadTasks, bool wishDone)
        {
            if (!hadTasks)
                return "+" + pay + " кр   ·   задач не было";
            if (wishDone)
                return "+" + pay + " кр   ·   задача закрыта ×1.3";
            return "+" + pay + " кр   ·   задачи открыты, мимо";
        }

        string SeasonEndText()
        {
            string tone = _tone.TryLead(out ShowMood lead)
                ? MoodStyle.Paint(MoodStyle.Full(lead), lead)
                : "ничья — концовку не выбрать";
            return "Тон сезона: " + tone + "\nБюджет: " + _state.money + " кр\nСами концовки напишем следом.";
        }

        void ToggleCamera()
        {
            if (_phase != PitchPhase.Play)
                return;
            _armed = null;
            _ui.SetArmed(null);
            _capture.ToggleSticky();
        }

        void Arm(EventDefinition def)
        {
            if (_phase != PitchPhase.Play || def == null)
                return;
            if (_state.played.Contains(def.id))
            {
                _ui.Toast("Уже сыграно.");
                return;
            }

            _capture.SetSticky(false);
            if (def.id == "open_bathroom" && _bathOpen)
            {
                _armed = null;
                _ui.SetArmed(null);
                _ui.MarkUsed(def.id);
                _state.played.Add(def.id);
                _ui.Toast("Ванная уже открыта.");
                return;
            }

            if (def.targetType == TargetType.Global)
            {
                _armed = null;
                _ui.SetArmed(null);
                _executor.Play(def, null, null);
                _ui.MarkUsed(def.id);
                NoteCard(def);
                _ui.Toast(def.displayName);
                return;
            }

            _armed = def;
            _ui.SetArmed(def);
        }

        void ReadPlayInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null)
                return;

            if (keyboard.digit1Key.wasPressedThisFrame)
                ArmAt(0);
            else if (keyboard.digit2Key.wasPressedThisFrame)
                ArmAt(1);
            else if (keyboard.digit3Key.wasPressedThisFrame)
                ArmAt(2);
            else if (keyboard.digit4Key.wasPressedThisFrame)
                ArmAt(3);
            else if (keyboard.digit5Key.wasPressedThisFrame)
                ArmAt(4);

            if (keyboard.cKey.wasPressedThisFrame)
                ToggleCamera();

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _armed = null;
                _ui.SetArmed(null);
                _capture.SetSticky(false);
            }

            if (mouse.rightButton.wasPressedThisFrame && _armed != null)
            {
                _armed = null;
                _ui.SetArmed(null);
                _suppressHoldCam = true;
            }

            if (mouse.rightButton.wasReleasedThisFrame)
                _suppressHoldCam = false;

            bool hold = mouse.rightButton.isPressed && !_suppressHoldCam && _armed == null;
            _capture.SetHold(hold);

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                if (!_capture.Mode)
                    _capture.SetSticky(true);
                else
                    _capture.TryCapture();
            }

            if (mouse.leftButton.wasPressedThisFrame && !OverUi())
            {
                if (_capture.Mode)
                    _capture.TryCapture();
                else if (_armed != null)
                    TryCommitTarget();
            }
        }

        void TryCommitTarget()
        {
            Vector2 world = MouseWorld();
            var hits = Physics2D.OverlapPointAll(world);
            if (_armed.targetType == TargetType.Actor)
            {
                NPCController npc = null;
                for (int i = 0; i < hits.Length; i++)
                {
                    npc = hits[i].GetComponent<NPCController>();
                    if (npc != null)
                        break;
                }

                if (npc == null)
                {
                    _ui.Toast("Кликни по человеку.");
                    Sfx.Play(Cue.Miss, 0.4f);
                    return;
                }

                if (_armed.limitTrait && (npc.Trait == null || npc.Trait.traitId != _armed.targetTrait))
                {
                    _ui.Toast("Это " + npc.DisplayName + ". " + _armed.hint);
                    Sfx.Play(Cue.Miss, 0.4f);
                    return;
                }

                JuiceCard(_armed, npc.transform.position);
                _executor.Play(_armed, null, npc);
                _ui.MarkUsed(_armed.id);
                NoteCard(_armed);
                _armed = null;
                _ui.SetArmed(null);
                return;
            }

            Interactable obj = null;
            for (int i = 0; i < hits.Length; i++)
            {
                obj = hits[i].GetComponent<Interactable>();
                if (obj != null)
                    break;
            }

            if (obj == null || (!string.IsNullOrEmpty(_armed.requiredObjectId) && obj.Id != _armed.requiredObjectId))
            {
                _ui.Toast(_armed.hint);
                Sfx.Play(Cue.Miss, 0.4f);
                return;
            }

            bool openBath = _armed.id == "open_bathroom";
            EventDefinition played = _armed;
            JuiceCard(played, obj.transform.position);
            _executor.Play(played, obj, null);
            _ui.MarkUsed(played.id);
            NoteCard(played);
            _armed = null;
            _ui.SetArmed(null);
            if (openBath)
                StartCoroutine(RevealBathroom());
        }

        IEnumerator RevealBathroom()
        {
            if (_bathOpen || _bathroom == null)
                yield break;
            _bathOpen = true;
            if (_boards != null)
                _boards.SetActive(false);
            _bathroom.SetActive(true);
            var renderers = _bathroom.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var c = renderers[i].color;
                c.a = 0f;
                renderers[i].color = c;
            }

            _shake.Punch(0.1f, 0.25f);
            Sfx.Play(Cue.Splash, 0.7f);
            FadeBit.Burst(new Vector3(0.05f, 5.15f, 0f), 12, new Color(0.55f, 0.36f, 0.18f, 1f));
            _ui.Toast("Дверь в ванную открыта.");
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / 0.45f);
                for (int i = 0; i < renderers.Length; i++)
                {
                    var c = renderers[i].color;
                    c.a = a;
                    renderers[i].color = c;
                }

                yield return null;
            }
        }

        void CloseBathroom()
        {
            _bathOpen = false;
            if (_boards != null)
                _boards.SetActive(true);
            if (_bathroom == null)
                return;
            var renderers = _bathroom.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var c = renderers[i].color;
                c.a = 1f;
                renderers[i].color = c;
            }

            _bathroom.SetActive(false);
        }

        void RefreshTargeting()
        {
            if (_zloi == null)
                return;
            bool actors = _phase == PitchPhase.Play && _armed != null && _armed.targetType == TargetType.Actor;
            bool objects = _phase == PitchPhase.Play && _armed != null && _armed.targetType == TargetType.Object;
            bool zloi = actors && (!_armed.limitTrait || _armed.targetTrait == TraitId.Aggressive);
            bool dobryak = actors && (!_armed.limitTrait || _armed.targetTrait == TraitId.Sentimental);
            _zloi.SetTargeted(zloi);
            _dobryak.SetTargeted(dobryak);
            _fridge.SetTargeted(objects && _armed.requiredObjectId == _fridge.Id);
            if (_bathDoor != null)
                _bathDoor.SetTargeted(objects && _armed.requiredObjectId == _bathDoor.Id);
        }

        string Coach()
        {
            if (_capture.Mode)
            {
                if (_capture.Moments.Count == 0)
                    return "Наведи рамку на них и жми Space.";
                if (_capture.IsFull)
                    return "Кадры сняты. Можно закрыть серию.";
                return "Кадр есть. Сними ещё или жми «Конец серии».";
            }

            if (_armed != null)
                return "«" + _armed.displayName + "» — " + _armed.hint + ". ПКМ отмена.";

            bool fighting = _zloi.IsFighting || _dobryak.IsFighting;
            if (fighting)
                return "Драка. Отношения −20. C — камера, Space — снять.";
            if (_fridge.IsOnFire && _zloi.HasRage)
                return "Добряк бежит, Злой догоняет. Дождись драки и жми C.";
            if (_zloi.HasRage)
                return "Злой на взводе. Теперь «Поджог» на холодильник.";
            if (_fridge.IsOnFire)
                return "Добряк в панике. Разозли Злого — он догонит.";
            if (_capture.Moments.Count > 0)
                return "Можно снять ещё или закрыть серию.";
            return "Карты внизу. C — камера, Space — кадр.";
        }

        void JuiceCard(EventDefinition def, Vector3 at)
        {
            if (def == null)
                return;
            if (def.id == "meditation_bell")
                Sfx.Play(Cue.Bell, 0.75f);
            else if (!def.ignite)
                Sfx.Play(Cue.Card, 0.6f);
            if (_shake != null)
                _shake.Punch(def.ignite ? 0.16f : 0.1f, def.ignite ? 0.18f : 0.13f);
            var fx = def.cardColor;
            fx.a = 1f;
            FadeBit.Burst(at + Vector3.up * 0.4f, 8, fx);
        }

        void NoteCard(EventDefinition def)
        {
            if (def == null)
                return;
            _state.played.Add(def.id);
            if (def.moods == null)
                return;

            int count = def.moods.Count > 2 ? 2 : def.moods.Count;
            for (int i = 0; i < count; i++)
            {
                int gained = _tone.Add(def.moods[i], SeasonTone.CardGain);
                if (gained > 0)
                    _ui.FlashTone(def.moods[i], gained);
            }

            _ui.RefreshTone(_tone);
        }

        void OnCaptured(CapturedMoment moment)
        {
            int gained = _tone.Add(moment.mood, SeasonTone.MomentGain);
            _ui.RefreshTone(_tone);
            if (gained > 0)
                _ui.FlashTone(moment.mood, gained);

            int index = _capture.Moments.Count - 1;
            string caption = "<color=" + MoodStyle.Hex(moment.mood) + ">" + moment.Title + "</color>";
            _ui.FlyPhoto(moment.photo, index, moment.screenPoint, caption);
            _ui.Pulse(new Color(1f, 1f, 1f, 0.72f));
            _shake.Punch(0.05f, 0.08f);
            Sfx.Play(Cue.Shutter, 0.8f);
            StartCoroutine(HitStop());
            if (_capture.IsFull && !_endQueued)
                StartCoroutine(AutoEnd());
        }

        void OnFight(NPCController a, NPCController b)
        {
            Vector3 mid = (a.transform.position + b.transform.position) * 0.5f;
            _shake.Punch(0.18f, 0.45f);
            _rumble = 0.12f;
            Sfx.Play(Cue.Slap, 0.85f, 0.8f);
            FadeBit.Burst(mid, 16, new Color(1f, 0.28f, 0.12f, 1f));
            _ui.Pulse(new Color(1f, 0.18f, 0.12f, 0.32f));
        }

        IEnumerator HitStop()
        {
            if (_inputLock)
                yield break;
            _inputLock = true;
            Time.timeScale = 0.02f;
            yield return new WaitForSecondsRealtime(0.16f);
            if (_phase == PitchPhase.Play)
                Time.timeScale = 1f;
            _inputLock = false;
        }

        IEnumerator AutoEnd()
        {
            _endQueued = true;
            yield return new WaitForSecondsRealtime(1.15f);
            EndEpisode();
        }

        static bool OverUi()
        {
            if (EventSystem.current == null || Mouse.current == null)
                return false;
            if (EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId))
                return true;
            return EventSystem.current.IsPointerOverGameObject();
        }

        static Vector2 MouseWorld()
        {
            var cam = Camera.main;
            Vector2 screen = Mouse.current.position.ReadValue();
            float dist = Mathf.Abs(cam.transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, dist));
            return world;
        }
    }
}
