using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Комнаты, доступные карте: пул конфига (или весь Resources/Content) + старт и монтаж.
    // Если комнат какого-то типа нет вообще — подставляется заглушка, чтобы выпуск не ломался.
    public class RoomCatalog
    {
        readonly List<RoomDefinition> _rooms = new List<RoomDefinition>();
        readonly List<RoomDefinition> _runtime = new List<RoomDefinition>();
        readonly Dictionary<RoomType, RoomDefinition> _placeholders = new Dictionary<RoomType, RoomDefinition>();

        public EpisodeMapConfig Config { get; }

        public RoomCatalog(EpisodeMapConfig config)
        {
            Config = config;
            if (config.pool != null && config.pool.Count > 0)
            {
                for (int i = 0; i < config.pool.Count; i++)
                    Add(config.pool[i]);
            }
            else
            {
                var all = ContentLibrary.All<RoomDefinition>();
                for (int i = 0; i < all.Count; i++)
                    Add(all[i]);
            }

            Add(config.opening);
            Add(config.montage);
            JamContent.Fill(_rooms, _runtime);
        }

        void Add(RoomDefinition room)
        {
            if (room != null && !_rooms.Contains(room))
                _rooms.Add(room);
        }

        public IReadOnlyList<RoomDefinition> All => _rooms;

        // Комнаты типа, подходящие под условия прямо сейчас.
        public List<RoomDefinition> Available(RoomType type, RuleContext ctx)
        {
            var list = new List<RoomDefinition>();
            for (int i = 0; i < _rooms.Count; i++)
            {
                var r = _rooms[i];
                if (r.Type == type && r.weight > 0f && Rules.Check(r.conditions, ctx))
                    list.Add(r);
            }

            return list;
        }

        public RoomDefinition Find(string id, RoomType type)
        {
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < _rooms.Count; i++)
                {
                    if (_rooms[i].Id == id)
                        return _rooms[i];
                }

                var any = ContentLibrary.Find<RoomDefinition>(id);
                if (any != null)
                    return any;
            }

            return Placeholder(type);
        }

        public RoomDefinition Placeholder(RoomType type)
        {
            if (_placeholders.TryGetValue(type, out var room))
                return room;

            switch (type)
            {
                case RoomType.Event: room = ScriptableObject.CreateInstance<EventRoomDefinition>(); break;
                case RoomType.Marketing: room = ScriptableObject.CreateInstance<MarketingRoomDefinition>(); break;
                case RoomType.Montage: room = ScriptableObject.CreateInstance<MontageRoomDefinition>(); break;
                default: room = ScriptableObject.CreateInstance<SituationRoomDefinition>(); break;
            }

            room.name = "Placeholder_" + type;
            room.SetId("placeholder_" + type.ToString().ToLowerInvariant());
            room.hideFlags = HideFlags.DontSave;
            room.title = TypeTitle(type);
            room.subtitle = type == RoomType.Montage ? "финальная склейка" : "комната";
            room.description = type == RoomType.Montage
                ? "Собери до трёх роликов и отправь выпуск в эфир."
                : "Запасная комната этого типа.";
            room.icon = type == RoomType.Montage ? MapNodeKind.Climax : type == RoomType.Marketing ? MapNodeKind.Shop
                : type == RoomType.Event ? MapNodeKind.Mystery : MapNodeKind.Scene;
            _placeholders[type] = room;
            return room;
        }

        public void DestroyPlaceholders()
        {
            for (int i = 0; i < _runtime.Count; i++)
            {
                if (_runtime[i] == null)
                    continue;
                if (Application.isPlaying)
                    Object.Destroy(_runtime[i]);
                else
                    Object.DestroyImmediate(_runtime[i]);
            }

            _runtime.Clear();
            foreach (var room in _placeholders.Values)
            {
                if (Application.isPlaying)
                    Object.Destroy(room);
                else
                    Object.DestroyImmediate(room);
            }

            _placeholders.Clear();
        }

        public static string TypeTitle(RoomType type)
        {
            switch (type)
            {
                case RoomType.Event: return "СОБЫТИЕ";
                case RoomType.Marketing: return "МАРКЕТИНГ";
                case RoomType.Montage: return "МОНТАЖ";
                default: return "СЪЁМКА";
            }
        }
    }
}
