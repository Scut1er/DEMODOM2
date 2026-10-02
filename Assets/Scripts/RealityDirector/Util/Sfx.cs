using UnityEngine;

namespace RealityDirector.Util
{
    public enum Cue
    {
        Click,
        Shutter,
        Card,
        Ignite,
        Slap,
        Cry,
        Coin,
        Miss,
        Splash,
        Bell,
        Tick,
        Blip,
        Crackle
    }

    public static class Sfx
    {
        const int Rate = 22050;

        static AudioSource[] _voices;
        static int _voice;
        static AudioSource _loop;
        static AudioClip[] _clips;

        public static void Bind(GameObject host)
        {
            if (_voices != null && _voices.Length > 0 && _voices[0] != null)
                return;

            _voices = new AudioSource[4];
            for (int i = 0; i < _voices.Length; i++)
            {
                var src = host.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.ignoreListenerPause = true;
                _voices[i] = src;
            }

            _loop = host.AddComponent<AudioSource>();
            _loop.playOnAwake = false;
            _loop.spatialBlend = 0f;
            _loop.loop = true;
            _loop.volume = 0.22f;
            _loop.ignoreListenerPause = true;

            _clips = new AudioClip[13];
            _clips[(int)Cue.Click] = Noise("click", 0.045f, 11u, 0.9f);
            _clips[(int)Cue.Shutter] = Shutter();
            _clips[(int)Cue.Card] = Sweep("card", 0.16f, 220f, 90f, 3u);
            _clips[(int)Cue.Ignite] = Sweep("ignite", 0.32f, 70f, 420f, 9u);
            _clips[(int)Cue.Slap] = Slap();
            _clips[(int)Cue.Cry] = Cry();
            _clips[(int)Cue.Coin] = Coin();
            _clips[(int)Cue.Miss] = Sweep("miss", 0.14f, 240f, 90f, 5u);
            _clips[(int)Cue.Splash] = Noise("splash", 0.28f, 17u, 0.35f);
            _clips[(int)Cue.Bell] = Bell();
            _clips[(int)Cue.Tick] = Tone("tick", 0.06f, 1400f, 1);
            _clips[(int)Cue.Blip] = Tone("blip", 0.09f, 520f, 3);
            _clips[(int)Cue.Crackle] = Noise("crackle", 1.1f, 23u, 0.15f);
        }

        public static void Play(Cue cue, float volume = 1f, float pitch = 1f)
        {
            if (_voices == null || _clips == null)
                return;
            var clip = _clips[(int)cue];
            if (clip == null)
                return;
            var src = _voices[_voice];
            _voice = (_voice + 1) % _voices.Length;
            if (src == null)
                return;
            src.pitch = pitch <= 0f ? 1f : pitch;
            src.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public static void Crackle(bool on)
        {
            if (_loop == null || _clips == null)
                return;
            if (!on)
            {
                _loop.Stop();
                return;
            }

            if (_loop.isPlaying)
                return;
            _loop.clip = _clips[(int)Cue.Crackle];
            _loop.Play();
        }

        static AudioClip Shutter()
        {
            int n = Samples(0.07f);
            var data = new float[n];
            uint s = 41u;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * 48f);
                float noise = Hash(ref s);
                float tick = Mathf.Sin(2f * Mathf.PI * 1800f * t);
                data[i] = (noise * 0.65f + tick * 0.35f) * env;
            }

            return Bake("shutter", data);
        }

        static AudioClip Slap()
        {
            int n = Samples(0.08f);
            var data = new float[n];
            uint s = 7u;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * 36f);
                float thump = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 95f * t));
                data[i] = (thump * 0.55f + Hash(ref s) * 0.45f) * env;
            }

            return Bake("slap", data);
        }

        static AudioClip Cry()
        {
            int n = Samples(0.42f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float k = t / 0.42f;
                float freq = Mathf.Lerp(860f, 380f, k);
                float trem = 1f + Mathf.Sin(2f * Mathf.PI * 6.5f * t) * 0.08f;
                float env = Mathf.Sin(k * Mathf.PI) * 0.55f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * trem * t) * env;
            }

            return Bake("cry", data);
        }

        static AudioClip Coin()
        {
            int n = Samples(0.22f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float freq = t < 0.08f ? 880f : 1320f;
                float local = t < 0.08f ? t : t - 0.08f;
                float env = Mathf.Exp(-local * 18f) * 0.7f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }

            return Bake("coin", data);
        }

        static AudioClip Bell()
        {
            int n = Samples(0.55f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * 4.5f);
                float wave = Mathf.Sin(2f * Mathf.PI * 660f * t)
                    + 0.35f * Mathf.Sin(2f * Mathf.PI * 1320f * t)
                    + 0.15f * Mathf.Sin(2f * Mathf.PI * 1980f * t);
                data[i] = wave * env * 0.45f;
            }

            return Bake("bell", data);
        }

        static AudioClip Tone(string name, float dur, float freq, int harm)
        {
            int n = Samples(dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * (28f / dur));
                float wave = 0f;
                for (int h = 1; h <= harm; h++)
                    wave += Mathf.Sin(2f * Mathf.PI * freq * h * t) / h;
                data[i] = wave * env * 0.6f;
            }

            return Bake(name, data);
        }

        static AudioClip Sweep(string name, float dur, float from, float to, uint seed)
        {
            int n = Samples(dur);
            var data = new float[n];
            uint s = seed;
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float k = t / dur;
                float freq = Mathf.Lerp(from, to, k);
                phase += 2f * Mathf.PI * freq / Rate;
                float env = Mathf.Sin(k * Mathf.PI);
                data[i] = (Mathf.Sin(phase) * 0.35f + Hash(ref s) * 0.65f) * env * 0.8f;
            }

            return Bake(name, data);
        }

        static AudioClip Noise(string name, float dur, uint seed, float gain)
        {
            int n = Samples(dur);
            var data = new float[n];
            uint s = seed;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * (6f / dur));
                float pop = Hash(ref s);
                if ((s & 31u) > 4u)
                    pop *= 0.15f;
                data[i] = pop * env * gain;
            }

            return Bake(name, data);
        }

        static int Samples(float dur)
        {
            return Mathf.Max(1, Mathf.CeilToInt(Rate * dur));
        }

        static float Hash(ref uint s)
        {
            s = s * 1664525u + 1013904223u;
            return ((s >> 16) & 65535u) / 32768f - 1f;
        }

        static AudioClip Bake(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            clip.hideFlags = HideFlags.HideAndDontSave;
            return clip;
        }
    }
}
