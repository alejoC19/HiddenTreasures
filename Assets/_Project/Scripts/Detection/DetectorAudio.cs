using UnityEngine;
using HuesoOxido.Core;

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

        const float BaseHz = 660f;

        static AudioClip BuildBeep()
        {
            var data = ProceduralSounds.Beep(BaseHz, 0.12f);
            var clip = AudioClip.Create("detector_beep", data.Length, 1, ProceduralSounds.Rate, false);
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
            // Mas cerca: bip mas agudo (520 -> 1500 Hz) y mas rapido (1,1 s -> 0,07 s entre bips).
            _src.pitch = ProceduralSounds.DetectorFrequency(s) / BaseHz;
            _src.PlayOneShot(_beep, volume * Mathf.Lerp(0.5f, 1f, s));
            _timer = ProceduralSounds.DetectorInterval(s);
        }
    }
}
