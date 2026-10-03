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
        static readonly Vector2 BedroomFlee = new Vector2(6.2f, 1.65f);
        static readonly Vector2 ClosedFlee = new Vector2(-7.05f, 1.7f);
        const float BedroomGate = 2.55f;

        enum Lesson
        {
            None,
            Take,
            Throw,
            Holding,
            Camera,
            TurnIn
        }

        Lesson _lesson;
        bool _frozen;
        bool _paused;
        readonly List<string> _frameNames = new List<string>();
        PitchUi.CastFace[] _faces;
        PitchPhase _phase = PitchPhase.Prep;
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
        Interactable _bedDoor;
        GameObject _bedBoards;
        GameObject _bedroom;
        bool _bedOpen;
        NPCController _zloi;
        NPCController _dobryak;
        readonly List<NPCController> _cast = new List<NPCController>();
        EventDefinition _armed;
        bool _inputLock;
        float _rumble;
        bool _panning;
        bool _rmbLatched;
        Vector2 _panLast;
        float _handUntil;
        SeasonTone _tone => GameSession.Tone;
        SeasonState _state => GameSession.State;
        EventDefinition[] _hand = new EventDefinition[0];

        void Awake()
        {
            Interactable.ResetGlobal();
            _content = PitchContent.Create();
            _context = new EpisodeContext();
            _context.Bind();
            SetupCamera();
            SetupInput();
            _ui = PitchUi.Build();
            EnsureSession();
            _ui.BindFlow(BackToHub, EndEpisode, ToggleCamera, BackToHub);
            _ui.BindExit(ExitToHub);
            _ui.BindPause(TogglePause);
            BuildApartment();
            WireTags();
            _executor = gameObject.AddComponent<EventExecutor>();
            _capture = gameObject.AddComponent<CaptureSystem>();
            _capture.Init(_context, _cast, () => _fridge != null && _fridge.IsOnFire, _fridge.transform, _bathroom.transform, () => _bathOpen);
            _capture.Captured += OnCaptured;
            _capture.Missed += () =>
            {
                _ui.Toast("Кадр не вышел.");
                Sfx.Play(Cue.Miss, 0.45f);
            };
            Sfx.Bind(gameObject);

            NPCController.FightStarted -= OnFight;
            NPCController.FightStarted += OnFight;
            StartEpisode();
        }

        // Квартиру можно открыть прямо из редактора: тогда продолжаем сейв или стартуем новый сезон.
        void EnsureSession()
        {
            if (!GameSession.Active && !GameSession.Continue())
                GameSession.NewSeason(_content.StarterIds());
            if (GameSession.Embarked)
                return;
            var meta = new MetaService(_state, _tone, _content.All);
            meta.AutoPick();
            meta.TryEmbark(GameSession.Hand);
        }

        void OnDestroy()
        {
            if (_context != null)
                _context.Unbind();
            NPCController.FightStarted -= OnFight;
            if (_content != null)
                _content.DestroyAssets();
            _frozen = false;
            Time.timeScale = 1f;
            BossCoach.Dismiss();
        }

        void Update()
        {
            TickCamera();
            if (_handUntil > 0f && Time.unscaledTime >= _handUntil)
            {
                _handUntil = 0f;
                if (_ui != null)
                    _ui.SetHandLocked(false);
            }
            bool fire = _fridge != null && _fridge.IsOnFire;
            bool fighting = false;
            NPCController hitA = null;
            NPCController hitB = null;
            for (int i = 0; i < _cast.Count; i++)
            {
                var npc = _cast[i];
                if (npc == null)
                    continue;
                npc.Heat = fire && _fridge != null && Vector2.Distance(npc.transform.position, _fridge.transform.position) < 2.2f;
                if (!npc.IsFighting)
                    continue;
                fighting = true;
                if (hitA == null)
                    hitA = npc;
                else if (hitB == null)
                    hitB = npc;
            }

            if (fighting)
            {
                _rumble -= Time.deltaTime;
                if (_rumble <= 0f)
                {
                    _shake.Punch(0.045f, 0.1f);
                    _rumble = 0.18f;
                    if (hitA != null && hitB != null)
                    {
                        Vector3 mid = (hitA.transform.position + hitB.transform.position) * 0.5f;
                        FadeBit.Burst(mid, 3, new Color(1f, 0.82f, 0.55f, 1f));
                        Sfx.Play(Cue.Slap, 0.28f, Random.Range(0.86f, 1.2f));
                    }
                }
            }

            RefreshTargeting();
        }

        void LateUpdate()
        {
            if (_lesson != Lesson.None && !_state.wantsTutorial)
            {
                _lesson = Lesson.None;
                _frozen = false;
                Time.timeScale = 1f;
            }

            TrackAim();
            SyncGates();
            if (_phase != PitchPhase.Play)
                return;
            PushBoard();
            if (_paused)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                    TogglePause();
                _ui.SetHint(Coach());
                return;
            }

            if (_frozen || Time.timeScale < 0.05f)
            {
                _ui.SetHint(Coach());
                return;
            }

            if (_inputLock)
                return;

            ReadPlayInput();
            _ui.SetCaptureMode(_capture.Mode);
            _ui.SetRecord(_capture.Recorded, _capture.Recording);
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

        void TickCamera()
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null || _shake == null)
                return;

            bool right = mouse.rightButton.isPressed;
            if (!right)
                _rmbLatched = false;
            else if (_armed != null)
                _rmbLatched = true;
            bool drag = mouse.middleButton.isPressed || (right && !_rmbLatched);
            float raw = mouse.scroll.ReadValue().y;
            if (!OverUi() && !mouse.middleButton.isPressed && Mathf.Abs(raw) > 0.01f)
            {
                float notches = Mathf.Abs(raw) > 8f ? raw / 120f : raw;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - notches * 0.9f, 3.15f, 11.5f);
            }

            Vector2 now = mouse.position.ReadValue();
            if (!drag)
            {
                _panning = false;
                return;
            }

            if (!_panning)
            {
                if (OverUi())
                    return;
                _panning = true;
                _panLast = now;
                return;
            }

            Vector2 delta = now - _panLast;
            _panLast = now;
            if (delta.sqrMagnitude < 0.01f)
                return;
            float pixels = cam.pixelHeight > 1 ? cam.pixelHeight : Screen.height;
            float worldPerPixel = cam.orthographicSize * 2f / pixels;
            var pos = _shake.Base;
            pos.x -= delta.x * worldPerPixel;
            pos.y -= delta.y * worldPerPixel;
            pos.x = Mathf.Clamp(pos.x, -7.2f, 7.4f);
            pos.y = Mathf.Clamp(pos.y, 0.5f, 7.4f);
            pos.z = -10f;
            _shake.Base = pos;
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
            Floor(root, "Living", GameArt.FloorParquetLight, IllustratedArt.Wood, new Vector3(-5.35f, y, 0f), new Vector2(4.55f, 4.55f));
            Floor(root, "Kitchen", GameArt.FloorTiles, IllustratedArt.Tile, new Vector3(0.05f, y, 0f), new Vector2(5.15f, 4.55f));
            BackWall(root, GameArt.WallStripes, -5.35f, 4.5f, 4.55f);
            BackWall(root, GameArt.WallStripes, 0.05f, 4.5f, 5.15f);
            WallV(root, -2.85f);
            WallV(root, 2.75f);
            WallH(root, -1.55f, 4.95f, 1.9f);
            WallH(root, 1.65f, 4.95f, 1.9f);

            Place(root, "Rug", IllustratedArt.Rug, new Vector3(-5.45f, 3.15f, 0f), new Vector2(2.1f, 1.25f), 1);
            PlaceArt(root, "Sofa", GameArt.Sofa, IllustratedArt.Sofa, new Vector3(-5.45f, 3.35f, 0f), new Vector2(2.2f, 1.5f), new Vector2(2.15f, 1.15f), 4);
            Place(root, "Table", IllustratedArt.Table, new Vector3(-0.55f, 2.9f, 0f), new Vector2(1.45f, 0.95f), 4);
            Place(root, "Stove", IllustratedArt.Stove, new Vector3(2.05f, 3.55f, 0f), new Vector2(0.85f, 0.85f), 4);
            PlaceArt(root, "Plant", GameArt.Plant, IllustratedArt.Plant, new Vector3(-7.15f, 4.15f, 0f), new Vector2(0.7f, 1.1f), new Vector2(0.7f, 0.9f), 5);
            Place(root, "WindowL", IllustratedArt.Window, new Vector3(-5.4f, 4.45f, 0f), new Vector2(1.35f, 0.7f), 5);

            _fridge = BuildFridge(root);
            _boards = BuildDoor(root);
            _bathroom = BuildBathroom(root);
            _bathroom.SetActive(false);
            _bedBoards = BuildBedDoor(root);
            _bedroom = BuildBedroom(root);
            _bedroom.SetActive(false);

            SpawnCast();
            RegisterHangouts();

            var zSpawn = new GameObject("Spawn_Zloi").transform;
            zSpawn.SetParent(root, false);
            zSpawn.position = ZloiHome;
            var dSpawn = new GameObject("Spawn_Dobryak").transform;
            dSpawn.SetParent(root, false);
            dSpawn.position = DobryakHome;

            RoomTag(root, new Vector3(-5.35f, 4.7f, 0f), "ГОСТИНАЯ");
            RoomTag(root, new Vector3(-1.7f, 4.15f, 0f), "КУХНЯ");
            var bedLabel = new GameObject("BedLabel").transform;
            bedLabel.SetParent(root, false);
            bedLabel.position = new Vector3(5.5f, 4.7f, 0f);
            _ui.AddTag(bedLabel, () => _bedOpen ? "СПАЛЬНЯ" : "", new Color(0.42f, 0.28f, 0.22f), Vector2.zero, 16, false);
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

        // Арт художника вписывается без искажений; нет арта — старая рисованная заглушка.
        static void PlaceArt(Transform parent, string name, Sprite art, Sprite fallback, Vector3 pos, Vector2 artBox, Vector2 fallbackSize, int order)
        {
            if (art == null)
            {
                Place(parent, name, fallback, pos, fallbackSize, order);
                return;
            }

            GameArt.FitInside(SpriteUtil.Show(parent, name, pos, art, order), artBox);
        }

        static void Floor(Transform parent, string name, Sprite art, Sprite fallback, Vector3 pos, Vector2 size)
        {
            if (art != null)
                GameArt.Tiled(parent, name, art, pos, size, 0);
            else
                Place(parent, name, fallback, pos, size, 0);
        }

        // Полоса стены вдоль верхнего края комнаты (на ней висят окна).
        static void BackWall(Transform parent, Sprite art, float x, float y, float width)
        {
            if (art != null)
                GameArt.Tiled(parent, "backWall", art, new Vector3(x, y, 0f), new Vector2(width, 0.75f), 1);
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
            Floor(t, "floor", GameArt.FloorTiles, IllustratedArt.BathTile, new Vector3(0.05f, 6.4f, 0f), new Vector2(4.9f, 3.55f));
            BackWall(t, GameArt.WallBathTiles, 0.05f, 7.8f, 4.9f);
            Place(t, "wallL", IllustratedArt.Wall, new Vector3(-2.3f, 6.55f, 0f), new Vector2(0.24f, 3.05f), 3);
            Place(t, "wallR", IllustratedArt.Wall, new Vector3(2.4f, 6.55f, 0f), new Vector2(0.24f, 3.05f), 3);
            Place(t, "wallT", IllustratedArt.Wall, new Vector3(0.05f, 8.05f, 0f), new Vector2(4.7f, 0.28f), 3);
            PlaceArt(t, "shower", GameArt.Bath, IllustratedArt.Shower, new Vector3(-1.25f, 7.0f, 0f), new Vector2(1.7f, 1.0f), new Vector2(1.2f, 1.2f), 4);
            Place(t, "toilet", IllustratedArt.Toilet, new Vector3(0.15f, 7.2f, 0f), new Vector2(0.6f, 0.85f), 4);
            Place(t, "sink", IllustratedArt.Sink, new Vector3(1.45f, 7.15f, 0f), new Vector2(1.05f, 0.7f), 4);
            return go;
        }

        GameObject BuildDoor(Transform root)
        {
            var go = new GameObject("BathDoor");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(0.05f, 5.15f, 0f);
            if (GameArt.DoorWhite != null)
                GameArt.FitInside(SpriteUtil.Show(root, "BathDoorArt", go.transform.position, GameArt.DoorWhite, 4), new Vector2(1.0f, 1.45f));
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

        void RegisterHangouts()
        {
            Hangout.Clear();
            Hangout.Add(new Vector2(-5.45f, 2.85f), SpotKind.Seat);
            Hangout.Add(new Vector2(-0.55f, 2.35f), SpotKind.Prop);
            Hangout.Add(new Vector2(1.15f, 2.55f), SpotKind.Prop);
            Hangout.Add(new Vector2(2.05f, 2.7f), SpotKind.Prop);
            Hangout.Add(new Vector2(-4.4f, 2.15f), SpotKind.Floor);
            Hangout.Add(new Vector2(-6.6f, 2.05f), SpotKind.Floor);
            Hangout.Add(new Vector2(0.2f, 1.9f), SpotKind.Floor);
            if (_bedOpen)
            {
                Hangout.Add(new Vector2(6.05f, 2.55f), SpotKind.Seat);
                Hangout.Add(new Vector2(4.6f, 2.1f), SpotKind.Floor);
            }
        }

        static readonly Vector2[] Homes =
        {
            new Vector2(-6.5f, 1.65f),
            new Vector2(-4.1f, 1.65f),
            new Vector2(-5.2f, 3.2f),
            new Vector2(0.4f, 1.7f),
            new Vector2(1.8f, 3.1f)
        };

        void SpawnCast()
        {
            var ids = new List<string>();
            if (_state.episode != null && _state.episode.cast != null && _state.episode.cast.Count > 0)
                ids.AddRange(_state.episode.cast);
            if (ids.Count == 0)
            {
                ids.Add("npc_zloi");
                ids.Add("npc_dobryak");
            }

            var roster = CastRoster.All();
            int n = Mathf.Min(ids.Count, Homes.Length);
            for (int i = 0; i < n; i++)
            {
                string id = ids[i];
                CastMember member = null;
                for (int r = 0; r < roster.Length; r++)
                {
                    if (roster[r].id == id)
                        member = roster[r];
                }

                string name = member != null ? member.name : id;
                var traitId = TraitOf(id, member);
                bool angry = traitId == TraitId.Aggressive || traitId == TraitId.Jealous || traitId == TraitId.Chaotic;
                var npc = BuildNpc(id, name, name, _content.TraitOf(traitId), _content.RulesFor(traitId), Homes[i], angry, HiddenOf(id));
                _cast.Add(npc);
                if (id == "npc_zloi")
                    _zloi = npc;
                if (id == "npc_dobryak")
                    _dobryak = npc;
            }

            if (_zloi == null && _cast.Count > 0)
                _zloi = _cast[0];
            if (_dobryak == null && _cast.Count > 1)
                _dobryak = _cast[1];
            else if (_dobryak == null)
                _dobryak = _zloi;
            for (int i = 0; i < _cast.Count; i++)
                _cast[i].Rival = _cast[(i + 1) % _cast.Count];
            if (_zloi != null)
                _zloi.ChaseSpeed = 2.75f;
            if (_dobryak != null)
                _dobryak.PanicSpeed = 1.9f;
            ApplyBedroomGate();
        }

        static TraitId TraitOf(string id, CastMember member)
        {
            var defs = Resources.LoadAll<ActorDefinition>("Content/Characters");
            for (int i = 0; i < defs.Length; i++)
            {
                if (defs[i] != null && defs[i].Id == id)
                    return defs[i].mainTrait;
            }

            string label = member != null && member.traits != null && member.traits.Length > 0 ? member.traits[0] : "";
            if (label.Contains("агресс"))
                return TraitId.Aggressive;
            if (label.Contains("паник"))
                return TraitId.Panicker;
            if (label.Contains("сентим"))
                return TraitId.Sentimental;
            if (label.Contains("ревн"))
                return TraitId.Jealous;
            if (label.Contains("тщесл"))
                return TraitId.Vain;
            if (label.Contains("застен") || label.Contains("робк"))
                return TraitId.Shy;
            if (label.Contains("трус"))
                return TraitId.Cowardly;
            if (label.Contains("хаот"))
                return TraitId.Chaotic;
            if (id == "npc_zloi")
                return TraitId.Aggressive;
            if (id == "npc_dobryak")
                return TraitId.Panicker;
            return TraitId.Sentimental;
        }

        static HiddenTrait HiddenOf(string id)
        {
            if (id == "npc_zloi")
                return HiddenTrait.Prankster;
            if (id == "npc_dobryak")
                return HiddenTrait.Kleptomaniac;
            if (id == "npc_max")
                return HiddenTrait.Singer;
            var defs = Resources.LoadAll<ActorDefinition>("Content/Characters");
            for (int i = 0; i < defs.Length; i++)
            {
                if (defs[i] != null && defs[i].Id == id)
                    return defs[i].hiddenTrait;
            }

            return HiddenTrait.None;
        }

        void ApplyBedroomGate()
        {
            Vector2 flee = _bedOpen ? BedroomFlee : ClosedFlee;
            for (int i = 0; i < _cast.Count; i++)
            {
                var npc = _cast[i];
                if (npc == null)
                    continue;
                npc.FleePoint = flee;
                npc.BlockEast = !_bedOpen;
                npc.EastLimit = BedroomGate;
            }
        }

        GameObject BuildBedroom(Transform root)
        {
            var go = new GameObject("Bedroom");
            go.transform.SetParent(root, false);
            var t = go.transform;
            Floor(t, "floor", GameArt.FloorParquetDark, IllustratedArt.Wood, new Vector3(5.5f, 2.6f, 0f), new Vector2(4.7f, 4.55f));
            BackWall(t, GameArt.WallStripes, 5.5f, 4.5f, 4.7f);
            Place(t, "Bed", IllustratedArt.Bed, new Vector3(6.2f, 3.15f, 0f), new Vector2(1.85f, 2.35f), 4);
            Place(t, "WindowR", IllustratedArt.Window, new Vector3(6.3f, 4.45f, 0f), new Vector2(1.35f, 0.7f), 5);
            return go;
        }

        GameObject BuildBedDoor(Transform root)
        {
            var go = new GameObject("BedDoor");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(2.75f, 2.05f, 0f);
            if (GameArt.DoorWood != null)
                GameArt.FitInside(SpriteUtil.Show(root, "BedDoorArt", go.transform.position, GameArt.DoorWood, 4), new Vector2(0.75f, 1.35f));
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.7f, 1.35f);
            var ring = SpriteUtil.Show(go.transform, "ring", Vector3.zero, IllustratedArt.Glow, 5);
            SpriteUtil.Fit(ring, new Vector2(1.15f, 1.7f));
            ring.color = new Color(1f, 0.86f, 0.25f, 0.9f);
            var body = SpriteUtil.Show(go.transform, "boards", Vector3.zero, IllustratedArt.Boards, 6);
            SpriteUtil.Fit(body, new Vector2(0.72f, 1.35f));
            var interactable = go.AddComponent<Interactable>();
            interactable.Id = "bed_door";
            interactable.DisplayName = "Дверь";
            interactable.Setup(body, ring, null, null, null);
            _bedDoor = interactable;
            return go;
        }

        NPCController BuildNpc(string id, string displayName, string accusative, TraitDefinition trait, ReactionRuleSet rules, Vector2 home, bool angry, HiddenTrait hidden)
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
            npc.Hidden = hidden;
            npc.Rules = rules;
            npc.Home = home;
            npc.BindVisual(visual, ring);
            NpcLook.Attach(npc, visual, body);
            npc.ResetState();
            return npc;
        }

        void WireTags()
        {
            var paper = new Color(0.96f, 0.93f, 0.88f, 1f);
            for (int i = 0; i < _cast.Count; i++)
            {
                var npc = _cast[i];
                if (npc == null)
                    continue;
                float side = i % 2 == 0 ? -18f : 18f;
                _ui.AddTag(npc.transform, npc.Plate, paper, new Vector2(0f, 78f), 16, true);
                _ui.AddBubble(npc.transform, () => npc.Emote, () => npc.Thought, new Vector2(side, 148f));
            }
        }

        void RoomTag(Transform root, Vector3 position, string text)
        {
            var anchor = new GameObject(text).transform;
            anchor.SetParent(root, false);
            anchor.position = position;
            var muted = new Color(0.28f, 0.2f, 0.18f, 1f);
            _ui.AddTag(anchor, () => text, muted, Vector2.zero, 16, false);
        }

        void StartEpisode()
        {
            if (_phase != PitchPhase.Prep)
                return;

            Sfx.Play(Cue.Card, 0.45f, 0.8f);
            ResetSet();
            RestoreMood();
            if (ShootLesson())
            {
                GameSession.Hand.Remove("fridge_fire");
                GameSession.Hand.Insert(0, "fridge_fire");
            }

            if (_state.episode != null)
                _state.episode.OpenHell(GameSession.RoomNodeId);
            _hand = SelectedHand();
            _ui.ClearHand();
            _ui.BindCards(_hand, Arm);
            _ui.ClearUsed();
            _ui.SetArmed(null);
            _capture.Capacity = Progression.CaptureSlots(_state.operatorLevel);
            _ui.SetCaptureCapacity(_capture.Capacity);
            string scene = string.IsNullOrEmpty(GameSession.SceneTitle) ? "" : "  ·  " + GameSession.SceneTitle;
            string roles = _state.episode != null && !string.IsNullOrEmpty(_state.episode.roleBrief) ? "\n" + _state.episode.roleBrief : "";
            _ui.SetEpisodeTitle("СЕРИЯ " + (_state.episodeIndex + 1) + scene + roles + "\nты режиссёр, не участник");
            _phase = PitchPhase.Play;
            _ui.ShowPlay();
            if (ShootLesson())
            {
                _state.tutorialBeat = 3;
                BossCoach.Ensure().Hide();
                _lesson = Lesson.Take;
                StartCoroutine(OpenLesson());
            }
            _ui.RefreshTone(_tone);
            _ui.SetSlots(_capture.Moments, _capture.Capacity);
            RefreshTasks(true);
        }

        // Съёмка — комната карты. Клипы в библиотеку выпуска, эфир и деньги после монтажа.
        void EndEpisode()
        {
            if (_phase != PitchPhase.Play)
                return;
            if (_lesson != Lesson.None && _lesson != Lesson.TurnIn)
            {
                _ui.Toast("Сначала доделай, что я сказал.");
                return;
            }

            Time.timeScale = 1f;
            _paused = false;
            _ui.SetPaused(false);
            _frozen = false;
            if (_state.wantsTutorial && _state.tutorialBeat < 4)
                _state.tutorialBeat = 4;
            _lesson = Lesson.None;
            BossCoach.Ensure().Hide();
            _inputLock = false;
            _armed = null;
            PersistMood();
            DepositFootage();
            _capture.SetSticky(false);
            _capture.SetHold(false);
            _ui.SetArmed(null);
            _ui.SetCaptureMode(false);
            GameSession.ReturnToMap = true;
            _phase = PitchPhase.Feedback;
            int scene = _state.episodeIndex + 1;
            _ui.PlaySlate(scene, BackToHub);
        }

        void DepositFootage()
        {
            var episode = _state.episode;
            var taken = _capture.Detach();
            if (episode != null)
                episode.EnsureLists();
            if (episode == null)
            {
                for (int i = 0; i < taken.Count; i++)
                    taken[i].Release();
                return;
            }

            int cap = episode.footageLimit > 0 ? episode.footageLimit : 5;
            for (int i = 0; i < taken.Count; i++)
            {
                if (episode.footage.Count >= cap)
                {
                    taken[i].Release();
                    continue;
                }

                var clip = FootageReel.Adopt(taken[i], GameSession.RoomNodeId);
                StampSponsor(episode, clip);
                episode.footage.Add(FootageReel.Entry(clip));
            }
        }

        static void StampSponsor(EpisodeState episode, FootageClip clip)
        {
            if (episode.pendingSponsors == null || episode.pendingSponsors.Count == 0)
                return;
            string cardId = episode.pendingSponsors[0];
            episode.pendingSponsors.RemoveAt(0);
            if (!clip.tags.Contains(MomentTags.Sponsor))
                clip.tags.Add(MomentTags.Sponsor);
            clip.sponsorCardId = cardId;
            for (int i = 0; i < episode.contracts.Count; i++)
            {
                var contract = episode.contracts[i];
                if (contract.grantedCardId != cardId || contract.matchingFootageIds.Count > 0)
                    continue;
                contract.matchingFootageIds.Add(clip.id);
                return;
            }
        }

        void BackToHub()
        {
            StopAllCoroutines();
            GameSession.Hand.Clear();
            GameSession.Embarked = false;
            GameSession.Save();
            SceneFlow.ToHub();
        }

        // Съёмка брошена. Комната карты остаётся текущей, хаб не засчитывает её.
        void ExitToHub()
        {
            if (_phase != PitchPhase.Play || _lesson != Lesson.None)
                return;
            StopAllCoroutines();
            GameSession.Hand.Clear();
            GameSession.Embarked = false;
            PersistMood();
            GameSession.RoomNodeId = null;
            GameSession.ExitToHub = true;
            GameSession.ReturnToMap = false;
            GameSession.Save();
            SceneFlow.ToHub();
        }

        void ResetSet()
        {
            _inputLock = false;
            _armed = null;
            _context.Reset();
            if (_fridge != null)
                _fridge.Extinguish();
            CloseBathroom();
            for (int i = 0; i < _cast.Count; i++)
            {
                if (_cast[i] != null)
                    _cast[i].ResetState();
            }
            _capture.ResetCapture();
            _ui.ClearUsed();
            _ui.SetArmed(null);
            _ui.ClearSlots();
            _handUntil = 0f;
            _ui.SetHandLocked(false);
            _ui.SetWrapReady(false);
        }

        void ArmAt(int index)
        {
            if (_hand == null || index < 0 || index >= _hand.Length)
                return;
            Arm(_hand[index]);
        }

        EventDefinition[] SelectedHand()
        {
            var list = new List<EventDefinition>();
            for (int i = 0; i < GameSession.Hand.Count; i++)
            {
                var def = _content.Find(GameSession.Hand[i]);
                if (def != null)
                    list.Add(def);
            }

            return list.ToArray();
        }
        void RefreshTasks(bool visible)
        {
            var lines = new List<string>(_state.tasks.Count);
            for (int i = 0; i < _state.tasks.Count; i++)
                lines.Add(_state.tasks[i].label);
            _ui.SetTasks(lines, visible);
        }

        void ToggleCamera()
        {
            if (_phase != PitchPhase.Play)
                return;
            if (_lesson != Lesson.None && _lesson != Lesson.Camera)
                return;
            _armed = null;
            _ui.SetArmed(null);
            _capture.ToggleSticky();
        }

        void Arm(EventDefinition def)
        {
            if (_phase != PitchPhase.Play || def == null)
                return;
            if (_lesson == Lesson.Take && def.id != "fridge_fire")
            {
                _ui.Toast("Сейчас только «Поджог».");
                return;
            }

            if (_lesson == Lesson.Throw && def.id != "fridge_fire")
                return;
            if (_lesson == Lesson.Holding || _lesson == Lesson.Camera || _lesson == Lesson.TurnIn)
                return;
            if (Time.unscaledTime < _handUntil)
            {
                _ui.Toast("Подожди, пусть сцена доиграет.");
                Sfx.Play(Cue.Miss, 0.35f);
                return;
            }
            if (_state.played.Contains(def.id) && !TempCard(def.id))
            {
                _ui.Toast("Уже сыграно.");
                return;
            }

            if (!CanPay(def))
                return;

            _capture.SetSticky(false);
            if (def.id == "open_bedroom" && _bedOpen)
            {
                _armed = null;
                _ui.SetArmed(null);
                _ui.MarkUsed(def.id);
                _state.played.Add(def.id);
                _ui.Toast("Спальня уже открыта.");
                return;
            }

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
                if (!Pay(def))
                    return;
                _armed = null;
                _ui.SetArmed(null);
                _executor.Play(def, null, null);
                _ui.MarkUsed(def.id);
                NoteCard(def);
                _ui.Toast(def.sponsor ? "Сними кадр. В эфир реклама попадёт только из монтажа." : def.displayName);
                return;
            }

            _armed = def;
            _ui.SetArmed(def);
            if (_lesson == Lesson.Take && def.id == "fridge_fire")
            {
                _lesson = Lesson.Throw;
                BossCoach.Ensure().Order("Кинь её на холодильник. Прямо на дверцу.", _ui.AimRect);
            }
        }

        void ReadPlayInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null)
                return;

            if (_lesson == Lesson.None || _lesson == Lesson.Take)
            {
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
            }

            bool cameraOpen = _lesson == Lesson.None || _lesson == Lesson.Camera;
            if (cameraOpen && keyboard.cKey.wasPressedThisFrame)
                ToggleCamera();

            if (_lesson == Lesson.None && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (_armed != null || _capture.Recording || _capture.Mode)
                {
                    _armed = null;
                    _ui.SetArmed(null);
                    if (_capture.Recording)
                        _capture.CancelRecord();
                    _capture.SetSticky(false);
                }
                else
                    TogglePause();
            }

            if (_lesson == Lesson.None && mouse.rightButton.wasPressedThisFrame && _armed != null)
            {
                _armed = null;
                _ui.SetArmed(null);
            }

            if (cameraOpen && keyboard.spaceKey.wasPressedThisFrame)
            {
                if (_capture.Recording)
                    _capture.EndRecord();
                _capture.SetSticky(!_capture.Mode);
            }

            if (cameraOpen && mouse.leftButton.wasReleasedThisFrame && _capture.Recording)
                _capture.EndRecord();

            if (mouse.leftButton.wasPressedThisFrame && !OverUi())
            {
                if (cameraOpen && _capture.Mode)
                    _capture.BeginRecord();
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

                if (!Pay(_armed))
                    return;
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
            bool openBed = _armed.id == "open_bedroom";
            if (!Pay(_armed))
                return;
            EventDefinition played = _armed;
            JuiceCard(played, obj.transform.position);
            _executor.Play(played, obj, null);
            _ui.MarkUsed(played.id);
            NoteCard(played);
            _armed = null;
            _ui.SetArmed(null);
            if (openBath)
                StartCoroutine(RevealBathroom());
            if (openBed)
                StartCoroutine(RevealBedroom());
            if (_lesson == Lesson.Throw && played.id == "fridge_fire")
            {
                _lesson = Lesson.Holding;
                StartCoroutine(FreezeSoon(1.15f, () => BossCoach.Ensure().Freeze(
                    "Вот так. Карта не ставит оценку. Она портит им день. Смотри, кто уже идёт к огню.",
                    () =>
                    {
                        _frozen = false;
                        Time.timeScale = 1f;
                        _lesson = Lesson.Camera;
                        BossCoach.Ensure().Order("Жми C. Зажми левую и веди рамку по людям. Три секунды — и отпусти.", _ui.CameraRect);
                    },
                    _ui.AimRect)));
            }
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

        IEnumerator RevealBedroom()
        {
            if (_bedOpen || _bedroom == null)
                yield break;
            _bedOpen = true;
            if (_bedBoards != null)
                _bedBoards.SetActive(false);
            _bedroom.SetActive(true);
            ApplyBedroomGate();
            RegisterHangouts();
            var renderers = _bedroom.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var c = renderers[i].color;
                c.a = 0f;
                renderers[i].color = c;
            }

            _shake.Punch(0.12f, 0.28f);
            Sfx.Play(Cue.Card, 0.55f, 0.7f);
            FadeBit.Burst(new Vector3(2.75f, 2.05f, 0f), 14, new Color(0.55f, 0.36f, 0.18f, 1f));
            _ui.Toast("Дверь в спальню открыта.");
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

        void CloseBedroom()
        {
            _bedOpen = false;
            if (_bedBoards != null)
                _bedBoards.SetActive(true);
            ApplyBedroomGate();
            RegisterHangouts();
            if (_bedroom == null)
                return;
            var renderers = _bedroom.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var c = renderers[i].color;
                c.a = 1f;
                renderers[i].color = c;
            }

            _bedroom.SetActive(false);
        }

        void RefreshTargeting()
        {
            bool actors = _phase == PitchPhase.Play && _armed != null && _armed.targetType == TargetType.Actor;
            bool objects = _phase == PitchPhase.Play && _armed != null && _armed.targetType == TargetType.Object;
            for (int i = 0; i < _cast.Count; i++)
            {
                var npc = _cast[i];
                if (npc == null || npc.Trait == null)
                    continue;
                bool traitOk = _armed != null && (!_armed.limitTrait || npc.Trait.traitId == _armed.targetTrait
                    || (_armed.targetTrait == TraitId.Panicker && npc.Trait.traitId == TraitId.Sentimental)
                    || (_armed.targetTrait == TraitId.Sentimental && npc.Trait.traitId == TraitId.Panicker));
                npc.SetTargeted(actors && traitOk);
            }
            _fridge.SetTargeted(objects && _armed.requiredObjectId == _fridge.Id);
            if (_bathDoor != null)
                _bathDoor.SetTargeted(objects && _armed.requiredObjectId == _bathDoor.Id);
            if (_bedDoor != null)
                _bedDoor.SetTargeted(objects && _armed.requiredObjectId == _bedDoor.Id);
        }

        string Coach()
        {
            if (_capture.Mode)
            {
                if (_capture.Recording)
                    return "Ролик. Веди рамку. Отпусти ЛКМ — или само встанет на 3 секундах.";
                if (_capture.Moments.Count == 0)
                    return "Зажми ЛКМ и веди рамку. Пустой угол съест слот.";
                if (_capture.IsFull)
                    return "Слоты полные. Карты ещё можно кидать. СДАНО — на карту эпизода.";
                return "Кадр есть. Сними ещё или жми СДАНО — вернёшься на карту эпизода.";
            }

            if (Time.unscaledTime < _handUntil)
                return "Колода на паузе. Сними реакцию. Следующая карта перебьёт то, что ещё не кончилось.";

            if (_armed != null)
                return "«" + _armed.displayName + "» — " + _armed.hint + ". " + Forecast() + " ПКМ отмена.";

            bool fighting = false;
            for (int i = 0; i < _cast.Count; i++)
            {
                if (_cast[i] != null && _cast[i].IsFighting)
                    fighting = true;
            }
            if (fighting)
                return "Драка. Отношения −20. C — камера, зажми ЛКМ — ролик.";
            if (_fridge.IsOnFire && Approaching())
                return "Кто ближе к холодильнику — тот подойдёт первым. Сними реакцию, пока она держится.";
            if ((_dobryak != null && (_dobryak.IsSeekingComfort || _dobryak.IsHugging)) || (_zloi != null && _zloi.IsHugging))
                return "Добряк идёт обниматься. Разозли Злого сейчас — и объятие сорвётся в драку.";
            if (_fridge.IsOnFire && _zloi != null && _zloi.HasRage)
                return "Добряк бежит, Злой догоняет. Дождись драки и жми C.";
            if (_zloi != null && _zloi.HasRage)
                return "Злой на взводе. Теперь «Поджог» на холодильник.";
            if (_fridge.IsOnFire)
                return "Ждут, кто заметит огонь. Ближний подойдёт первым.";
            if (_capture.IsFull)
                return "Слоты полные. Карты ещё можно кидать. СДАНО — на карту эпизода.";
            if (_capture.Moments.Count > 0)
                return "Можно снять ещё или сдать комнату. Эфир будет после монтажа.";
            return "Карты внизу. C — камера, зажми ЛКМ — ролик до 3 секунд.";
        }

        bool Approaching()
        {
            for (int i = 0; i < _cast.Count; i++)
            {
                if (_cast[i] != null && _cast[i].IsApproaching)
                    return true;
            }

            return false;
        }

        string Forecast()
        {
            int hot = 0;
            for (int i = 0; i < _cast.Count; i++)
            {
                var npc = _cast[i];
                if (npc == null)
                    continue;
                if (npc.Anger >= 55 || npc.Stress >= 55 || (npc.Heat && npc.SelfControl <= 40))
                    hot++;
            }

            if (hot >= 2)
                return "Сорвётся почти наверняка.";
            if (hot == 1)
                return "Кто-то сорвётся.";
            return "Скорее проглотят.";
        }

        void RestoreMood()
        {
            var episode = _state.episode;
            if (episode == null)
                return;
            episode.EnsureLists();
            int stress = EpisodeState.Sum(episode.nextRoomModifiers, "stress");
            int anger = EpisodeState.Sum(episode.nextRoomModifiers, "anger");
            int sadness = EpisodeState.Sum(episode.nextRoomModifiers, "sadness");
            int hostility = EpisodeState.Sum(episode.nextRoomModifiers, "hostility");
            PullModifier(episode, "stress");
            PullModifier(episode, "anger");
            PullModifier(episode, "sadness");
            PullModifier(episode, "hostility");
            for (int i = 0; i < _cast.Count; i++)
            {
                var npc = _cast[i];
                if (npc == null)
                    continue;
                var runtime = episode.Actor(npc.Id);
                if (!runtime.seeded)
                {
                    Seed(runtime, npc);
                    runtime.seeded = true;
                }

                runtime.stress = Mathf.Clamp(runtime.stress + stress, 0, 100);
                runtime.anger = Mathf.Clamp(runtime.anger + anger, 0, 100);
                runtime.sadness = Mathf.Clamp(runtime.sadness + sadness, 0, 100);
                runtime.hostility = Mathf.Clamp(runtime.hostility + hostility, 0, 100);
                npc.LoadMood(runtime);
            }
        }

        static void Seed(ActorRuntime runtime, NPCController npc)
        {
            var id = npc.Trait != null ? npc.Trait.traitId : TraitId.Sentimental;
            if (id == TraitId.Aggressive || id == TraitId.Jealous || id == TraitId.Chaotic)
            {
                runtime.anger = 22;
                runtime.selfControl = 35;
            }
            else if (id == TraitId.Panicker || id == TraitId.Cowardly || id == TraitId.Shy || id == TraitId.Timid)
            {
                runtime.stress = 18;
                runtime.selfControl = 28;
            }
            else if (id == TraitId.Sentimental)
            {
                runtime.sadness = 12;
                runtime.selfControl = 55;
            }
            else if (id == TraitId.Vain)
                runtime.confidence = 70;
        }

        static void PullModifier(EpisodeState episode, string key)
        {
            for (int i = episode.nextRoomModifiers.Count - 1; i >= 0; i--)
            {
                if (episode.nextRoomModifiers[i] != null && episode.nextRoomModifiers[i].key == key)
                    episode.nextRoomModifiers.RemoveAt(i);
            }
        }

        void PersistMood()
        {
            var episode = _state.episode;
            if (episode == null)
                return;
            episode.EnsureLists();
            episode.actors.Clear();
            for (int i = 0; i < _cast.Count; i++)
            {
                if (_cast[i] != null)
                    episode.actors.Add(_cast[i].SaveMood());
            }
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

        bool TempCard(string id)
        {
            return _state.episode != null && _state.episode.tempCards.Contains(id);
        }

        void NoteCard(EventDefinition def)
        {
            if (def == null)
                return;
            bool temp = _state.episode != null && _state.episode.tempCards.Remove(def.id);
            if (!temp)
                _state.played.Add(def.id);
            if (def.sponsor && _state.episode != null)
            {
                var episode = _state.episode;
                episode.EnsureLists();
                SponsorContractState deal = null;
                for (int i = 0; i < episode.contracts.Count; i++)
                {
                    if (episode.contracts[i].grantedCardId == def.id && !episode.contracts[i].cardWasPlayed)
                    {
                        deal = episode.contracts[i];
                        break;
                    }
                }

                if (deal == null)
                {
                    deal = new SponsorContractState
                    {
                        offerId = def.id,
                        grantedCardId = def.id,
                        status = ContractStatus.Active,
                        payout = def.sponsorPay,
                        scoreHit = def.sponsorScoreHit
                    };
                    episode.contracts.Add(deal);
                }

                deal.cardWasPlayed = true;
                episode.pendingSponsors.Add(def.id);
            }
            _handUntil = Time.unscaledTime + 3.4f;
            _armed = null;
            _ui.SetArmed(null);
            _ui.SetHandLocked(true);
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
            if (moment.grade == CaptureGrade.Blank)
            {
                _tone.Tax(SeasonTone.BlankTax, out int drama, out int trash, out int family);
                _ui.RefreshTone(_tone);
                _ui.FlashTone(ShowMood.Drama, -drama);
                _ui.FlashTone(ShowMood.Trash, -trash);
                _ui.FlashTone(ShowMood.Family, -family);
            }
            else
            {
                int amount = moment.grade == CaptureGrade.Cast ? SeasonTone.MomentGain : SeasonTone.PropGain;
                int gained = _tone.Add(moment.mood, amount);
                _ui.RefreshTone(_tone);
                if (gained > 0)
                    _ui.FlashTone(moment.mood, gained);
            }

            int index = _capture.Moments.Count - 1;
            _ui.FlyPhoto(moment.photo, index, moment.screenPoint, moment.Framed);
            _ui.Pulse(new Color(1f, 1f, 1f, 0.72f));
            _shake.Punch(0.05f, 0.08f);
            Sfx.Play(Cue.Shutter, 0.8f);
            StartCoroutine(HitStop());
            if (_lesson == Lesson.Camera)
            {
                _lesson = Lesson.Holding;
                StartCoroutine(FreezeSoon(0.7f, () => BossCoach.Ensure().Freeze(
                    "Ролик в слоте. Один клип — один кусок футажа. Пустой угол сожрал бы его так же.",
                    () =>
                    {
                        _frozen = false;
                        Time.timeScale = 1f;
                        _lesson = Lesson.TurnIn;
                        BossCoach.Ensure().Order("СДАНО. Это не эфир. Клипы лягут в библиотеку, и ты вернёшься на карту.", _ui.DoneRect);
                    },
                    _ui.SlotRects())));
            }

            if (_capture.IsFull)
                _ui.SetWrapReady(true);
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
            if (_inputLock || _frozen || _paused)
                yield break;
            _inputLock = true;
            Time.timeScale = 0.02f;
            yield return new WaitForSecondsRealtime(0.16f);
            if (_phase == PitchPhase.Play && !_frozen && !_paused)
                Time.timeScale = 1f;
            _inputLock = false;
        }

        bool ShootLesson()
        {
            return _state != null && _state.wantsTutorial && _state.tutorialBeat >= 2 && _state.tutorialBeat < 4;
        }

        IEnumerator OpenLesson()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (_lesson != Lesson.Take)
                yield break;
            BossCoach.Ensure().Order("Возьми «Поджог». Вот эта карта.", _ui.CardRect("fridge_fire"));
        }

        IEnumerator FreezeSoon(float wait, System.Action show)
        {
            yield return new WaitForSecondsRealtime(wait);
            if (_lesson == Lesson.None)
                yield break;
            _frozen = true;
            Time.timeScale = 0f;
            show?.Invoke();
        }

        bool CanPay(EventDefinition def)
        {
            if (def == null || def.cost <= 0)
                return true;
            var ep = _state != null ? _state.episode : null;
            if (ep == null || ep.hell >= def.cost)
                return true;
            _ui.Toast("Мало Hell Token. Нужно " + def.cost + ".");
            Sfx.Play(Cue.Miss, 0.4f);
            return false;
        }

        bool Pay(EventDefinition def)
        {
            if (def == null || def.cost <= 0)
                return true;
            var ep = _state != null ? _state.episode : null;
            if (ep == null)
                return true;
            if (ep.SpendHell(def.cost))
                return true;
            _ui.Toast("Мало Hell Token. Нужно " + def.cost + ".");
            Sfx.Play(Cue.Miss, 0.4f);
            return false;
        }

        void TogglePause()
        {
            if (_lesson != Lesson.None || _frozen || _phase != PitchPhase.Play)
                return;
            _paused = !_paused;
            Time.timeScale = _paused ? 0f : 1f;
            _ui.SetPaused(_paused);
        }

        void PushBoard()
        {
            if (_ui == null)
                return;
            int n = _cast.Count;
            if (_faces == null || _faces.Length != n)
                _faces = new PitchUi.CastFace[n];
            for (int i = 0; i < n; i++)
            {
                var npc = _cast[i];
                if (npc == null)
                    continue;
                _faces[i] = new PitchUi.CastFace
                {
                    name = npc.DisplayName,
                    trait = npc.Trait != null ? npc.Trait.displayName : "",
                    mood = MoodLine(npc),
                    stress = npc.Stress,
                    anger = npc.Anger
                };
            }

            _ui.SetCast(_faces);
            _capture.Peek(_frameNames);
            _ui.SetFrame(string.Join("\n", _frameNames), _capture.Mode);
            int library = 0;
            if (_state != null)
            {
                for (int i = 0; i < _state.owned.Count; i++)
                {
                    if (!_state.IsPlayed(_state.owned[i]))
                        library++;
                }
            }

            int hand = _hand != null ? _hand.Length : 0;
            int used = _ui.UsedCount;
            _ui.SetHandMeta(Mathf.Max(0, hand - used), hand, library, used);
            var ep = _state != null ? _state.episode : null;
            if (ep == null)
                _ui.SetHell(EpisodeState.HellCap, EpisodeState.HellCap);
            else
                _ui.SetHell(ep.hell, ep.hellMax > 0 ? ep.hellMax : EpisodeState.HellCap);
            _ui.SetFootage(_capture.Moments.Count, _capture.Capacity);
        }

        static string MoodLine(NPCController npc)
        {
            if (npc.Anger >= npc.Stress && npc.Anger >= npc.Sadness && npc.Anger >= 40)
                return npc.Anger >= 70 ? "злость высокая" : "злость";
            if (npc.Stress >= npc.Sadness && npc.Stress >= 40)
                return npc.Stress >= 70 ? "стресс высокий" : "стресс";
            if (npc.Sadness >= 40)
                return npc.Sadness >= 70 ? "грусть высокая" : "грусть";
            if (npc.Attraction >= 40)
                return "тянет";
            return "спокоен";
        }

        void SyncGates()
        {
            if (_ui == null)
                return;
            _ui.SetPauseEnabled(_lesson == Lesson.None);
            if (_lesson == Lesson.None)
            {
                _ui.ClearActionGates();
                return;
            }

            switch (_lesson)
            {
                case Lesson.Take:
                    _ui.SetActionGates(true, "fridge_fire", false, false, false);
                    break;
                case Lesson.Camera:
                    _ui.SetActionGates(false, null, true, false, false);
                    break;
                case Lesson.TurnIn:
                    _ui.SetActionGates(false, null, false, true, false);
                    break;
                default:
                    _ui.SetActionGates(false, null, false, false, false);
                    break;
            }
        }

        void TrackAim()
        {
            if ((_lesson != Lesson.Throw && _lesson != Lesson.Holding) || _ui == null || _ui.AimRect == null || _fridge == null || Camera.main == null)
                return;
            Vector2 screen = Camera.main.WorldToScreenPoint(_fridge.transform.position);
            var canvas = _ui.AimRect.parent as RectTransform;
            if (canvas == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out var local))
                return;
            _ui.AimRect.anchoredPosition = local;
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
