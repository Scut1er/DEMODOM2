using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Жизнь выпуска без UI: начать (каст + карта), восстановить из сейва, закрыть комнату, выпустить в эфир.
    public class EpisodeService
    {
        readonly SeasonState _state;
        readonly SeasonTone _tone;
        readonly SeasonConfig _season;
        RoomCatalog _catalog;
        EpisodeMapConfig _fallbackMap;

        public MapService Map { get; private set; }
        public RuleContext Context { get; private set; }
        public EpisodeState Current => _state.episode;
        public bool Active => _state.episode != null;

        public EpisodeService(SeasonState state, SeasonTone tone, SeasonConfig season)
        {
            _state = state;
            _tone = tone;
            _season = season;
            Context = new RuleContext(state, null, tone);
            if (Active)
                Restore();
        }

        public void Begin(IList<string> cast)
        {
            ReleaseCatalog();
            int cash = _season != null ? _season.startingCash : 90;
            if (cash <= 0)
                cash = 90;
            var ep = new EpisodeState
            {
                index = _state.episodeIndex,
                budgetAtStart = _state.money,
                cash = cash,
                footageLimit = _season != null && _season.footageLimit > 0 ? _season.footageLimit : 5,
                hellMax = _season != null && _season.hellTokenBudget > 0f ? _season.hellTokenBudget : EpisodeState.HellCap,
                handSize = _season != null && _season.handSize > 0 ? _season.handSize : EpisodeState.DefaultHandSize,
                productionSlots = _season != null && _season.productionSlots > 0 ? _season.productionSlots : 3
            };
            if (cast != null)
                ep.cast.AddRange(cast);
            _state.episode = ep;
            Context = new RuleContext(_state, ep, _tone);

            var config = MapConfig(ep.index);
            _catalog = new RoomCatalog(config);
            ep.mapSeed = config.seed != 0 ? config.seed + ep.index : Random.Range(1, int.MaxValue);
            bool teach = _state.wantsTutorial && _state.tutorialBeat < 6;
            var graph = teach
                ? MapGenerator.Tutorial(_catalog, Context)
                : MapGenerator.Generate(_catalog, ep.mapSeed, Context);
            ep.nodes = graph.nodes;
            ep.mapLayers = graph.Layers;
            ep.mapLanes = graph.Lanes;
            Map = new MapService(Context, graph);
        }

        // Карта берётся из сейва как есть; комнаты находятся по id (пропавший контент — заглушка).
        void Restore()
        {
            var ep = _state.episode;
            // Квартира могла увеличить номер серии — номер выпуска главнее.
            _state.episodeIndex = ep.index;
            Context = new RuleContext(_state, ep, _tone);
            _catalog = new RoomCatalog(MapConfig(ep.index));
            for (int i = 0; i < ep.nodes.Count; i++)
                ep.nodes[i].room = _catalog.Find(ep.nodes[i].roomId, ep.nodes[i].type);
            Map = new MapService(Context, new MapGraph(ep.nodes) { Layers = ep.mapLayers, Lanes = ep.mapLanes });
        }

        // Съёмка вернулась на карту выпуска. Номер серии не двигается — эфир после монтажа.
        public void FinishSceneRoom()
        {
            if (!Active)
                return;
            _state.episodeIndex = Current.index;
            Map.CompleteStep();
        }

        public void CompleteRoom()
        {
            if (Active)
                Map.CompleteStep();
        }

        // Выпуск вышел в эфир: следующий номер, игрок снова в хабе.
        public void End()
        {
            if (!Active)
                return;
            _state.episodeIndex = Current.index + 1;
            _state.episode = null;
            ReleaseCatalog();
            Map = null;
            Context = new RuleContext(_state, null, _tone);
        }

        void ReleaseCatalog()
        {
            if (_catalog != null)
                _catalog.DestroyPlaceholders();
            _catalog = null;
        }

        public EpisodeMapConfig MapConfig(int index)
        {
            var map = _season != null ? _season.MapFor(index) : null;
            if (map != null)
                return map;
            if (_fallbackMap == null)
                _fallbackMap = EpisodeMapConfig.CreateDefault();
            return _fallbackMap;
        }

        public void Dispose()
        {
            ReleaseCatalog();
            if (_fallbackMap == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(_fallbackMap);
            else
                Object.DestroyImmediate(_fallbackMap);
        }
    }
}
