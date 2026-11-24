using UnityEngine;

namespace HuesoOxido.Detection
{
    /// <summary>Pitidos procedurales: mas rapidos y agudos cuanto mas cerca. Sin archivos de audio.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class DetectorAudio : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float volume = 0.35f;

        AudioSource _src;
        AudioClip _beep;
        MetalDetector _detector;
        float _timer;

        public void Initialize(MetalDetector detector)
        {
            _detector = detector;
            _src = GetComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
            _beep = BuildBeep();
        }

        static AudioClip BuildBeep()
        {
            const int rate = 22050;
            int n = (int)(rate * 0.12f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Clamp01(i / 120f) * Mathf.Clamp01((n - i) / 600f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * env * 0.8f;
            }
            var clip = AudioClip.Create("detector_beep", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Update()
        {
            if (_detector == null || !_detector.DetectorEnabled) return;
            float s = _detector.Signal;
            if (s < 0.04f) { _timer = 0f; return; }

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _src.pitch = Mathf.Lerp(0.7f, 1.9f, s);
            _src.PlayOneShot(_beep, volume * Mathf.Lerp(0.5f, 1f, s));
            _timer = Mathf.Lerp(1.1f, 0.07f, Mathf.Pow(s, 0.8f));
        }
    }
}
