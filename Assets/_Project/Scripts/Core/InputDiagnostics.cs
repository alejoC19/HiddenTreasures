using UnityEngine;
using UnityEngine.InputSystem;

namespace HuesoOxido.Core
{
    /// <summary>Si el Input System no esta activo (sin dispositivos), muestra un aviso en pantalla en vez de un juego "muerto".</summary>
    public sealed class InputDiagnostics : MonoBehaviour
    {
        bool _noDevices;
        float _checkAt = 0.5f;

        void Update()
        {
            if (Time.unscaledTime < _checkAt) return;
            _checkAt = Time.unscaledTime + 1f;
            _noDevices = InputSystem.devices.Count == 0;
        }

        void OnGUI()
        {
            if (!_noDevices) return;
            var style = new GUIStyle(GUI.skin.box) { fontSize = 20, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            var r = new Rect(Screen.width * 0.2f, Screen.height * 0.35f, Screen.width * 0.6f, Screen.height * 0.3f);
            GUI.Box(r, "El Input System no detecta teclado ni mouse.\n\nActivá Edit > Project Settings > Player > Other Settings > "
                       + "Active Input Handling = \"Input System Package (New)\" y reiniciá el Editor\n(o usá el menú Hidden Treasures > Configurar proyecto).", style);
        }
    }
}
