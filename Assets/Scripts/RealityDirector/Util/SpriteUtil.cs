using UnityEngine;

namespace RealityDirector.Util
{
    public static class SpriteUtil
    {
        static Sprite _white;
        static Material _unlit;

        public static Sprite White
        {
            get
            {
                if (_white != null)
                    return _white;

                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                _white = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                return _white;
            }
        }

        public static Material Unlit
        {
            get
            {
                if (_unlit != null)
                    return _unlit;

                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");
                _unlit = new Material(shader);
                return _unlit;
            }
        }

        public static SpriteRenderer Show(Transform parent, string name, Vector3 localPos, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = Unlit;
            renderer.sortingOrder = order;
            return renderer;
        }

        public static void Fit(SpriteRenderer renderer, Vector2 worldSize)
        {
            Vector2 size = renderer.sprite.bounds.size;
            float x = size.x < 0.0001f ? 1f : worldSize.x / size.x;
            float y = size.y < 0.0001f ? 1f : worldSize.y / size.y;
            renderer.transform.localScale = new Vector3(x, y, 1f);
        }

        public static SpriteRenderer Box(Transform parent, string name, Vector3 localPos, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = White;
            renderer.sharedMaterial = Unlit;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
