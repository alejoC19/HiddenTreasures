using System;
using UnityEngine;
using HuesoOxido.Core;
using HuesoOxido.Detection;

namespace HuesoOxido.Digging
{
    public enum DigStatus { Idle, NoTarget, NoPermit, Tired, NotReady, Ready, Digging }

    /// <summary>
    /// Excavacion: mantener el boton sobre una señal fuerte. Barra de progreso, golpes con feedback y aparicion del hallazgo.
    /// No conoce el inventario: avisa con eventos (FindUnearthed) y el compositor decide donde va la pieza.
    /// </summary>
    public sealed class DigController : MonoBehaviour, IDigActivity
    {
        [SerializeField] float strokeInterval = 0.55f;
        [SerializeField] float energyPerSecond = 2.2f;
        [SerializeField] float progressDecayPerSecond = 0.35f;

        GameInput _input;
        MetalDetector _detector;
        IDigPermission _permission;
        IEnergyConsumer _energy;
        Func<bool> _canAct;
        Func<float> _speedMultiplier;
        System.Random _rng;
        BuriedSite _current;
        float _progress;
        float _strokeTimer;
        AudioSource _audio;
        AudioClip _thud;

        public bool IsDigging { get; private set; }
        public float Progress => _progress;
        public DigStatus Status { get; private set; }
        public string StatusMessage { get; private set; } = "";

        public event Action<BuriedSite> DigStarted;
        public event Action<BuriedSite> DigCancelled;
        public event Action<Vector3> Stroke;
        public event Action<BuriedSite, ItemInstance> FindUnearthed;

        public void Initialize(GameInput input, MetalDetector detector, IDigPermission permission, IEnergyConsumer energy,
                               Func<bool> canAct, Func<float> speedMultiplier, int seed)
        {
            _input = input; _detector = detector; _permission = permission; _energy = energy;
            _canAct = canAct; _speedMultiplier = speedMultiplier;
            _rng = new System.Random(seed);
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _thud = BuildThud();
        }

        static AudioClip BuildThud()
        {
            const int rate = 22050;
            int n = (int)(rate * 0.18f);
            var data = new float[n];
            var rng = new System.Random(5);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float env = Mathf.Exp(-i / (rate * 0.045f));
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * 0.12f;
                data[i] = lp * env * 1.6f;
            }
            var clip = AudioClip.Create("dig_thud", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Update()
        {
            if (_input == null || _detector == null) return;
            float dt = Time.deltaTime;
            var target = _detector.Target;
            bool held = _input.DigHeld;
            bool actable = _canAct == null || _canAct();

            Status = DigStatus.Idle;
            StatusMessage = "";
            bool canDig = false;

            if (held && actable)
            {
                if (target == null || !_detector.InDigRange)
                {
                    Status = target == null ? DigStatus.NoTarget : DigStatus.NotReady;
                    StatusMessage = target == null ? "No hay nada para excavar acá" : "Ubicá la bobina sobre la señal más fuerte";
                }
                else if (_permission != null && !_permission.CanDig(target.ZoneId))
                {
                    Status = DigStatus.NoPermit;
                    StatusMessage = "Necesitás un permiso de excavación (campamento)";
                }
                else canDig = true;
            }
            else if (actable && target != null && _detector.InDigRange)
            {
                Status = (_permission == null || _permission.CanDig(target.ZoneId)) ? DigStatus.Ready : DigStatus.NoPermit;
                if (Status == DigStatus.NoPermit) StatusMessage = "Necesitás un permiso de excavación (campamento)";
            }

            if (canDig && _energy != null && !_energy.TryConsume(energyPerSecond * dt))
            {
                canDig = false;
                Status = DigStatus.Tired;
                StatusMessage = "Estás agotado";
            }

            if (_current != target && !canDig) { _progress = 0f; }

            if (canDig)
            {
                if (!IsDigging) { IsDigging = true; _strokeTimer = 0.2f; DigStarted?.Invoke(target); }
                _current = target;
                Status = DigStatus.Digging;
                float speed = _speedMultiplier != null ? Mathf.Max(0.2f, _speedMultiplier()) : 1f;
                _progress += dt * speed / Mathf.Max(0.5f, target.DigSeconds);

                _strokeTimer -= dt;
                if (_strokeTimer <= 0f)
                {
                    _strokeTimer = strokeInterval;
                    _audio.pitch = 0.85f + (float)_rng.NextDouble() * 0.3f;
                    _audio.PlayOneShot(_thud, 0.8f);
                    Stroke?.Invoke(target.transform.position);
                }

                if (_progress >= 1f) Complete(target);
            }
            else
            {
                if (IsDigging) { IsDigging = false; if (_current != null && !_current.Excavated) DigCancelled?.Invoke(_current); }
                _progress = Mathf.Max(0f, _progress - progressDecayPerSecond * dt);
                if (_progress <= 0f && !IsDigging) _current = null;
            }
        }

        void Complete(BuriedSite site) => Unearth(site);

        /// <summary>Saca la pieza del sitio y avisa por FindUnearthed. Lo usa la excavacion al completarse (y los tests).</summary>
        public void Unearth(BuriedSite site)
        {
            IsDigging = false;
            _progress = 0f;
            float condition = 0.35f + (float)_rng.NextDouble() * 0.4f;
            var item = ItemInstance.Create(site.ItemId, _rng, condition);
            site.MarkExcavated(true);
            _current = null;
            FindUnearthed?.Invoke(site, item);
        }
    }
}
