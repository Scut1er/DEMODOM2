using RealityDirector.Util;
using UnityEngine;

namespace RealityDirector.Events
{
    public class Interactable : MonoBehaviour
    {
        public static bool AnyOnFire { get; private set; }

        public static void ResetGlobal()
        {
            AnyOnFire = false;
        }

        public string Id;
        public string DisplayName;
        public bool IsOnFire { get; private set; }

        Transform[] _flames;
        float _ember;
        SpriteRenderer[] _flameRenderers;
        SpriteRenderer _glow;
        SpriteRenderer _ring;
        SpriteRenderer _body;
        Color _bodyColor;

        public void Setup(SpriteRenderer body, SpriteRenderer ring, SpriteRenderer glow, Transform[] flames, SpriteRenderer[] flameRenderers)
        {
            _body = body;
            _bodyColor = body.color;
            _ring = ring;
            _glow = glow;
            _flames = flames;
            _flameRenderers = flameRenderers;
            if (_ring != null)
                _ring.enabled = false;
            if (_glow != null)
                _glow.enabled = false;
            SetFlamesActive(false);
        }

        public void SetTargeted(bool on)
        {
            if (_ring == null || IsOnFire)
                return;
            _ring.enabled = on;
        }

        public void Ignite()
        {
            if (IsOnFire)
                return;
            IsOnFire = true;
            AnyOnFire = true;
            if (_glow != null)
                _glow.enabled = true;
            if (_ring != null)
                _ring.enabled = true;
            if (_body != null)
                _body.color = new Color(0.25f, 0.18f, 0.16f, 1f);
            SetFlamesActive(true);
            Sfx.Play(Cue.Ignite, 0.85f);
            Sfx.Crackle(true);
            FadeBit.Burst(transform.position + Vector3.up * 0.35f, 14, new Color(1f, 0.42f, 0.08f, 1f));
        }

        public void Extinguish()
        {
            IsOnFire = false;
            AnyOnFire = false;
            if (_glow != null)
                _glow.enabled = false;
            if (_ring != null)
                _ring.enabled = false;
            if (_body != null)
                _body.color = _bodyColor;
            SetFlamesActive(false);
            Sfx.Crackle(false);
        }

        void SetFlamesActive(bool on)
        {
            if (_flames == null)
                return;
            for (int i = 0; i < _flames.Length; i++)
                _flames[i].gameObject.SetActive(on);
        }

        void Update()
        {
            if (!IsOnFire || _flames == null)
            {
                if (_ring != null && _ring.enabled && !IsOnFire)
                {
                    var pulse = 0.55f + Mathf.Sin(Time.time * 6f) * 0.35f;
                    var color = _ring.color;
                    color.a = pulse;
                    _ring.color = color;
                }

                return;
            }

            _ember -= Time.deltaTime;
            if (_ember <= 0f)
            {
                _ember = 0.07f;
                var tint = Random.value > 0.45f
                    ? new Color(1f, 0.48f, 0.08f, 1f)
                    : new Color(1f, 0.88f, 0.35f, 1f);
                Vector3 origin = transform.position + new Vector3(Random.Range(-0.28f, 0.28f), 0.35f, 0f);
                FadeBit.Spawn(origin, new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(1.4f, 2.6f), 0f),
                    tint, Random.Range(0.28f, 0.5f), Random.Range(0.05f, 0.12f));
            }

            for (int i = 0; i < _flames.Length; i++)
            {
                var pos = _flames[i].localPosition;
                pos.y += Time.deltaTime * (0.9f + i * 0.18f);
                if (pos.y > 1.35f)
                    pos.y = 0.15f;
                pos.x = Mathf.Sin(Time.time * 7f + i * 1.3f) * 0.22f;
                _flames[i].localPosition = pos;
                var color = _flameRenderers[i].color;
                color.a = 1f - pos.y / 1.45f;
                _flameRenderers[i].color = color;
            }
        }
    }
}
