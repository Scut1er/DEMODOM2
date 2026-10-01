using UnityEngine;

namespace RealityDirector.Util
{
    public class FadeBit : MonoBehaviour
    {
        Vector3 _velocity;
        float _life;
        float _max;
        SpriteRenderer _renderer;

        public static void Burst(Vector3 origin, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                if (dir.sqrMagnitude < 0.01f)
                    dir = Vector2.up;
                Spawn(origin, dir * Random.Range(1.6f, 3.8f), color, Random.Range(0.35f, 0.6f), Random.Range(0.08f, 0.2f));
            }
        }

        public static void Spawn(Vector3 pos, Vector3 velocity, Color color, float life, float size)
        {
            var go = new GameObject("fx");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteUtil.White;
            renderer.sharedMaterial = SpriteUtil.Unlit;
            renderer.color = color;
            renderer.sortingOrder = 18;
            var bit = go.AddComponent<FadeBit>();
            bit._renderer = renderer;
            bit._velocity = velocity;
            bit._life = life;
            bit._max = life;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;
            _velocity *= 0.9f;
            var color = _renderer.color;
            color.a = Mathf.Clamp01(_life / _max);
            _renderer.color = color;
            if (_life <= 0f)
                Destroy(gameObject);
        }
    }
}
