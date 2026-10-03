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

        public static void Play(string path)
        {
            Ensure().StartFade(path);
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
            _fading = true;
        }

        void Update()
        {
            if (!_fading || _in == null)
                return;
            _fade += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_fade / Fade);
            k = k * k * (3f - 2f * k);
            _in.volume = k;
            if (_out != null)
                _out.volume = 1f - k;
            if (_fade < Fade)
                return;
            _fading = false;
            _in.volume = 1f;
            if (_out == null)
                return;
            _out.volume = 0f;
            _out.Stop();
        }
    }
}
