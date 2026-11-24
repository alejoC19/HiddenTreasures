using System;

namespace HuesoOxido.Core
{
    /// <summary>Canal de avisos para el jugador (hallazgos, errores, ayudas). La UI lo escucha.</summary>
    public sealed class Notifier
    {
        public event Action<string> Posted;
        public void Post(string message) => Posted?.Invoke(message);
    }
}
