using System.Collections.Generic;
using UnityEngine;
using HuesoOxido.Core;
using HuesoOxido.Digging;
using HuesoOxido.Economy;
using HuesoOxido.Player;
using HuesoOxido.Survival;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Todo el audio del juego, generado por codigo (ProceduralSounds): viento en los pinos, pajaros de dia, grillos de noche,
    /// pisadas segun la superficie (tierra, pasto, piedra), pala, cepillo, "clink" de reliquia y moneda al vender.
    /// El bip del detector lo maneja DetectorAudio. Sin archivos de audio: el juego nunca esta en silencio.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        const int Variants = 4;

        readonly Dictionary<SurfaceKind, AudioClip[]> _steps = new Dictionary<SurfaceKind, AudioClip[]>();
        AudioClip[] _birds, _crickets;
        AudioClip _shovel, _brush, _clink, _coin, _rustle;
        AudioSource _wind, _sfx, _ambient;

        PlayerMotor _motor;
        Transform _player;
        WorldData _world;
        GameClock _clock;
        WeatherModel _weather;
        Vector3 _last;
        float _stride, _birdTimer = 2f, _cricketTimer = 1f, _brushTimer = -1f;
        int _brushLeft;
        readonly System.Random _rng = new System.Random(77);

        public void Initialize(Transform player, PlayerMotor motor, WorldData world, GameClock clock, WeatherModel weather,
                               DigController dig, PlayerStateMachine fsm, TradingService trade)
        {
            _player = player; _motor = motor; _world = world; _clock = clock; _weather = weather;
            _last = player.position;

            foreach (SurfaceKind k in System.Enum.GetValues(typeof(SurfaceKind)))
            {
                var arr = new AudioClip[Variants];
                for (int i = 0; i < Variants; i++) arr[i] = Make("step_" + k + i, ProceduralSounds.Footstep(k, 100 + (int)k * 10 + i));
                _steps[k] = arr;
            }
            _birds = new AudioClip[5]; for (int i = 0; i < _birds.Length; i++) _birds[i] = Make("bird" + i, ProceduralSounds.Bird(300 + i));
            _crickets = new AudioClip[3]; for (int i = 0; i < _crickets.Length; i++) _crickets[i] = Make("cricket" + i, ProceduralSounds.Cricket(400 + i));
            _shovel = Make("shovel", ProceduralSounds.Shovel(500));
            _brush = Make("brush", ProceduralSounds.Brush(600));
            _clink = Make("clink", ProceduralSounds.Clink(700));
            _coin = Make("coin", ProceduralSounds.Coin(800));
            _rustle = _steps[SurfaceKind.Grass][0];

            _wind = NewSource("Wind", true);
            _wind.clip = Make("wind", ProceduralSounds.Wind(6f, 900));
            _wind.volume = 0.1f;
            _wind.Play();
            _sfx = NewSource("Sfx", false);
            _ambient = NewSource("AmbientOneShots", false);

            if (dig != null)
            {
                dig.Stroke += _ => PlaySfx(_shovel, 0.45f, 0.92f + (float)_rng.NextDouble() * 0.16f);
                dig.FindUnearthed += (site, item) =>
                {
                    bool relic = item != null && item.DefinitionId != null && item.DefinitionId.StartsWith(QuestManager.RelicPrefix, System.StringComparison.Ordinal);
                    PlaySfx(_clink, relic ? 0.95f : 0.45f, relic ? 1f : 1.25f);
                };
            }
            if (fsm != null) fsm.Changed += OnState;
            if (trade != null) trade.Sold += (total, count) => PlaySfx(_coin, 0.7f, 1f);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, ProceduralSounds.Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        AudioSource NewSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0f;
            return s;
        }

        void PlaySfx(AudioClip clip, float volume, float pitch)
        {
            if (clip == null) return;
            _sfx.pitch = pitch;
            _sfx.PlayOneShot(clip, volume);
        }

        void OnState(PlayerAction from, PlayerAction to)
        {
            if (to == PlayerAction.PickingUp) PlaySfx(_rustle, 0.5f, 0.8f);
            if (to == PlayerAction.Cleaning) { _brushLeft = 3; _brushTimer = 0f; }
        }

        void Update()
        {
            if (_player == null || _clock == null) return;
            float dt = Time.unscaledDeltaTime;
            Ambience(dt);
            Brush(dt);
            Footsteps();
        }

        void Ambience(float dt)
        {
            float night = NightLight.NightFactor(_clock.Hour);
            float day = 1f - night;
            var w = _weather != null ? _weather.Current : WeatherKind.Clear;
            float rain = w == WeatherKind.Rain ? 1f : 0f;
            float windTarget = 0.1f + (w == WeatherKind.Rain ? 0.1f : 0f) + (w == WeatherKind.Cloudy ? 0.04f : 0f) + night * 0.02f;
            _wind.volume = Mathf.Lerp(_wind.volume, windTarget, 1f - Mathf.Exp(-1.5f * dt));

            _birdTimer -= dt;
            if (_birdTimer <= 0f)
            {
                _birdTimer = 2.5f + (float)_rng.NextDouble() * 5f;
                if (day > 0.3f && rain < 0.5f) Ambient(_birds[_rng.Next(_birds.Length)], 0.3f * day, 0.9f + (float)_rng.NextDouble() * 0.3f);
            }
            _cricketTimer -= dt;
            if (_cricketTimer <= 0f)
            {
                _cricketTimer = 0.7f + (float)_rng.NextDouble() * 1.6f;
                if (night > 0.3f && rain < 0.5f) Ambient(_crickets[_rng.Next(_crickets.Length)], 0.22f * night, 0.95f + (float)_rng.NextDouble() * 0.1f);
            }
        }

        void Ambient(AudioClip clip, float volume, float pitch)
        {
            _ambient.panStereo = (float)(_rng.NextDouble() * 1.6 - 0.8);
            _ambient.pitch = pitch;
            _ambient.PlayOneShot(clip, volume);
        }

        void Brush(float dt)
        {
            if (_brushLeft <= 0) return;
            _brushTimer -= dt;
            if (_brushTimer > 0f) return;
            _brushLeft--;
            _brushTimer = 0.62f;
            PlaySfx(_brush, 0.5f, 0.95f + (float)_rng.NextDouble() * 0.1f);
        }

        void Footsteps()
        {
            Vector3 p = _player.position;
            Vector3 d = p - _last; d.y = 0f;
            _last = p;
            float moved = d.magnitude;
            if (_motor == null || !_motor.ControlEnabled || !_motor.IsGrounded || moved < 0.0005f || moved > 1.5f) return;
            _stride += moved;
            float step = _motor.IsCrouched ? 0.5f : (_motor.IsSprinting ? 1.05f : 0.78f);
            if (_stride < step) return;
            _stride = 0f;
            var kind = _world != null ? _world.SurfaceAt(p.x, p.z) : SurfaceKind.Grass;
            var set = _steps[kind];
            float vol = _motor.IsCrouched ? 0.16f : (_motor.IsSprinting ? 0.5f : 0.32f);
            PlaySfx(set[_rng.Next(set.Length)], vol, 0.93f + (float)_rng.NextDouble() * 0.14f);
        }
    }
}
