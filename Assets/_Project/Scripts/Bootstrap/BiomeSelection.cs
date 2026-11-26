using UnityEngine;
using UnityEngine.SceneManagement;

namespace HuesoOxido.Core
{
    /// <summary>Que hacer al arrancar la escena tras cambiar de ecosistema.</summary>
    public enum StartMode { Menu, Continue, NewGame }

    /// <summary>
    /// Ecosistema que se construye al iniciar la escena. Viajar guarda la partida, cambia esta seleccion y recarga la escena:
    /// el mundo se genera de nuevo con el perfil elegido y la partida se retoma sola.
    /// </summary>
    public static class BiomeSelection
    {
        public static BiomeProfile Current { get; private set; } = Biomes.Forest;
        public static StartMode Mode { get; private set; } = StartMode.Menu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() { Current = Biomes.Forest; Mode = StartMode.Menu; }

        /// <summary>Cambia de ecosistema y recarga la escena activa.</summary>
        public static void SwitchTo(string biomeId, StartMode mode)
        {
            Current = Biomes.ById(biomeId);
            Mode = mode;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public static StartMode ConsumeMode()
        {
            var m = Mode;
            Mode = StartMode.Menu;
            return m;
        }
    }
}
