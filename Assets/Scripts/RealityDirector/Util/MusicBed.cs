using UnityEngine;

namespace RealityDirector.Util
{
    // Два источника, DontDestroyOnLoad. Смена трека — кроссфейд, сцена хаба его не убивает.
    public class MusicBed : MonoBehaviour
    {
        public const string Menu = "Music/MainMenu";
        public const string Theme = "Music/Intro";
        public const string Scene = "Music/SceneMainTheme";
        const float Fade = 2f;

        static MusicBed _live;
        AudioSource _out;
        AudioSource _in;
        string _path;
        float _fade;
        bool _fading;
        // Громкость ведущего трека (кроссфейд) и приглушение под озвучку.
        float _level = 1f;
        float _duck = 1f;
        static bool _ducked;
        const float DuckLevel = 0.35f;

        public static void Play(string path)
        {
            Ensure().StartFade(path);
        }

        // Пока говорит персонаж — музыка тише, потом плавно возвращается.
        public static void Duck(bool on)
        {
            _ducked = on;
        }

        static MusicBed Ensure()
        {
            if (_live != null)
                return _live;
            var go = new GameObject("MusicBed");
            _live = go.AddComponent<MusicBed>();
            return _live;
        }

        void Awake()
        {
            if (_live != null && _live != this)
            {
                Destroy(gameObject);
                return;
            }

            _live = this;
            DontDestroyOnLoad(gameObject);
            _out = Voice();
            _in = Voice();
        }

        static AudioSource Voice()
        {
            var src = _live.gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.ignoreListenerPause = true;
            src.volume = 0f;
            return src;
        }

        void StartFade(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            if (_path == path && _in != null && _in.isPlaying)
                return;
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
                return;
            if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();

            var next = _out;
            var prev = _in;
            _out = prev;
            _in = next;
            _path = path;
            _in.clip = clip;
            _in.volume = 0f;
            _in.loop = true;
            _in.Play();
            _fade = 0f;
            _level = 0f;
            _fading = true;
        }

        void Update()
        {
            _duck = Mathf.MoveTowards(_duck, _ducked ? DuckLevel : 1f, Time.unscaledDeltaTime * 2.5f);
            if (_fading && _in != null)
            {
                _fade += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(_fade / Fade);
                k = k * k * (3f - 2f * k);
                _level = k;
                if (_out != null)
                    _out.volume = (1f - k) * _duck;
                if (_fade >= Fade)
                {
                    _fading = false;
                    _level = 1f;
                    if (_out != null)
                    {
                        _out.volume = 0f;
                        _out.Stop();
                    }
                }
            }

            if (_in != null)
                _in.volume = _level * _duck;
        }
    }
}
