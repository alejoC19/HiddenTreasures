using System;
using UnityEngine;

namespace HuesoOxido.Survival
{
    /// <summary>Hace avanzar la hora y el clima. El tiempo se detiene mientras hay un menu abierto.</summary>
    public sealed class WorldClockComponent : MonoBehaviour
    {
        GameClock _clock;
        WeatherModel _weather;
        Func<bool> _paused;

        public GameClock Clock => _clock;
        public WeatherModel Weather => _weather;

        public void Initialize(GameClock clock, WeatherModel weather, Func<bool> paused)
        {
            _clock = clock; _weather = weather; _paused = paused;
        }

        void Update()
        {
            if (_clock == null || (_paused != null && _paused())) return;
            _clock.Advance(Time.deltaTime);
            _weather.Tick(Time.deltaTime);
        }
    }
}
