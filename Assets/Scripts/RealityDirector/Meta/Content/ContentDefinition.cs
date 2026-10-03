using UnityEngine;

namespace RealityDirector.Meta
{
    // База всего контента (комнаты, события, офферы, комментарии...).
    // Сейв хранит id, а не ссылку на ассет, поэтому id должен быть стабильным.
    public abstract class ContentDefinition : ScriptableObject
    {
        [Tooltip("Стабильный id. Не меняйте после того, как контент попал в игру — сейвы ссылаются на него. Пусто — возьмётся из имени ассета.")]
        [SerializeField] string id;

        public string Id => string.IsNullOrEmpty(id) ? MakeId(name) : id;

        // Для контента, созданного кодом (заглушки).
        public void SetId(string value)
        {
            id = value;
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = MakeId(name);
        }

        public static string MakeId(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";
            return raw.Trim().ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
        }
    }
}
