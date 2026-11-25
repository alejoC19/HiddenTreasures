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
        readonly AudioClip[] _toneBeeps = new AudioClip[4];
        IItemLookup _lookup;
        const float ToneRevealSignal = 0.55f;   // el timbre solo se distingue cerca de la señal
        MetalDetector _detector;
        float _timer;

        public void Initialize(MetalDetector detector, IItemLookup lookup = null)
        {
            _detector = detector; _lookup = lookup;
            _src = GetComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
            _beep = BuildBeep();
            foreach (DetectorTone t in System.Enum.GetValues(typeof(DetectorTone)))
            {
                var data = ProceduralSounds.Beep(BaseHz, t, 0.12f);
                var clip = AudioClip.Create("detector_beep_" + t, data.Length, 1, ProceduralSounds.Rate, false);
                clip.SetData(data, 0);
                _toneBeeps[(int)t] = clip;
            }
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
            var clip = _beep;
            var target = _detector.Target;
            if (_lookup != null && target != null && s >= ToneRevealSignal && _lookup.TryGet(target.ItemId, out var info))
                clip = _toneBeeps[(int)FindPresentation.ToneFor(info)];
            _src.PlayOneShot(clip, volume * Mathf.Lerp(0.5f, 1f, s));
            _timer = ProceduralSounds.DetectorInterval(s);
        }
    }
}
