using UnityEngine;
using UnityEngine.UI;

namespace HuesoOxido.UI
{
    /// <summary>Acciones que pide el menu (la logica vive en quien las implementa).</summary>
    public interface IMenuActions
    {
        bool HasSave { get; }
        float Sensitivity { get; set; }
        string QualityLabel { get; }
        void CycleQuality();
        void StartNewGame();
        void ContinueGame();
        void ContinueFromPause();
        void SaveGame();
        void LoadGame();
        void ReturnToMainMenu();
        void QuitGame();
    }

    /// <summary>Menu principal, pausa y pantalla de controles.</summary>
    public sealed class MenuView : MonoBehaviour
    {
        public enum Mode { None, Main, Pause, Controls, Credits }

        const string ControlsText =
            "<b>Moverse</b>   W A S D\n<b>Mirar</b>   mouse\n<b>Correr</b>   Shift\n<b>Agacharse</b>   C o Ctrl\n"
          + "<b>Cámara 1ª / 3ª persona</b>   V\n<b>Excavar</b>   mantener clic izquierdo sobre una señal fuerte\n"
          + "<b>Interactuar</b>   E (mostrador, carpa, surtidor, museo, vehículo, baúl, objetos del suelo)\n"
          + "<b>Inventario</b>   Tab        <b>Mapa</b>   M        <b>Libreta</b>   J        <b>Guardar</b>   F5        <b>Pausa</b>   Esc\n\n"
          + "<b>Vehículo:</b> W/S acelerar y frenar, A/D girar, E bajar (a baja velocidad).\n\n"
          + "<color=#F0A845>Tu decisión de siempre: ¿volvés a asegurar lo que encontraste, o arriesgás por una pieza más?</color>";

        IMenuActions _a;
        GameObject _root;
        RectTransform _column;
        Image _dim, _side;
        Mode _mode = Mode.None, _before = Mode.Main;

        public Mode Current => _mode;
        public bool IsOpen => _mode != Mode.None;

        public void Initialize(Transform canvas, IMenuActions actions)
        {
            _a = actions;
            _dim = UiFactory.Panel(canvas, "MenuView", new Color(0.02f, 0.02f, 0.015f, 0.8f));
            _root = _dim.gameObject;
            UiFactory.Stretch(_dim.rectTransform);
            // Franja oscura a la izquierda (solo menu principal): deja ver la escena cinematica a la derecha.
            _side = UiFactory.Panel(_dim.transform, "Side", new Color(0.02f, 0.02f, 0.015f, 0.62f));
            UiFactory.Region(_side.rectTransform, 0f, 0f, 0.44f, 1f);
            _column = UiFactory.Rect("Column", _dim.transform);
            UiFactory.Place(_column, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 760f));
            var v = _column.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12f; v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            _root.SetActive(false);
        }

        public void OpenMain() { _mode = Mode.Main; Show(); }
        public void OpenPause() { _mode = Mode.Pause; Show(); }
        public void Close() { _mode = Mode.None; _root.SetActive(false); }

        /// <summary>Esc: vuelve de Controles, o reanuda desde la pausa. En el menu principal no hace nada.</summary>
        public bool Back()
        {
            if (_mode == Mode.Controls || _mode == Mode.Credits) { _mode = _before; Show(); return false; }
            return _mode == Mode.Pause; // true => hay que cerrar y reanudar
        }

        void Show()
        {
            _root.SetActive(true);
            bool main = _mode == Mode.Main;
            _dim.color = main ? new Color(0.02f, 0.02f, 0.015f, 0.12f) : new Color(0.02f, 0.02f, 0.015f, 0.82f);
            _side.gameObject.SetActive(main);
            if (main) UiFactory.Place(_column, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(520f, 760f));
            else UiFactory.Place(_column, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 760f));
            UiFactory.Clear(_column);
            switch (_mode)
            {
                case Mode.Main: BuildMain(); break;
                case Mode.Pause: BuildPause(); break;
                case Mode.Controls: BuildControls(); break;
                case Mode.Credits: BuildCredits(); break;
            }
        }

        Text Title(string text, int size, Color c, float height)
        {
            var t = UiFactory.Label(_column, text, size, c, TextAnchor.MiddleCenter, FontStyle.Bold);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            return t;
        }

        Button Btn(string text, System.Action onClick, bool enabled = true)
        {
            var b = UiFactory.MakeButton(_column, text, onClick, 420f, 56f, 24);
            b.interactable = enabled;
            return b;
        }

        void SensitivityRow()
        {
            var row = UiFactory.Row(_column, 56f);
            var label = UiFactory.RowText(row, "Sensibilidad de cámara: <b>" + _a.Sensitivity.ToString("0.0") + "</b>", 22);
            UiFactory.MakeButton(row, "−", () => { _a.Sensitivity -= 0.1f; Show(); }, 64f, 40f, 26);
            UiFactory.MakeButton(row, "+", () => { _a.Sensitivity += 0.1f; Show(); }, 64f, 40f, 26);
        }

        void BuildMain()
        {
            Title("HIDDEN TREASURES", 58, UiFactory.Accent, 90f).alignment = TextAnchor.MiddleLeft;
            Title("Expedición al Bosque de las Ruinas", 24, UiFactory.Muted, 40f).alignment = TextAnchor.MiddleLeft;
            Spacer(40f);
            var cont = Btn("Continuar", () => _a.ContinueGame(), _a.HasSave);
            var fresh = Btn("Nueva partida", () => _a.StartNewGame());
            Btn("Controles", () => { _before = Mode.Main; _mode = Mode.Controls; Show(); });
            Btn("Créditos", () => { _before = Mode.Main; _mode = Mode.Credits; Show(); });
            SensitivityRow();
            Btn("Calidad gráfica: " + _a.QualityLabel, () => { _a.CycleQuality(); Show(); });
            Btn("Salir", () => _a.QuitGame());
            // Con partida guardada, "Continuar" queda seleccionado; si no, "Nueva partida".
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null) es.SetSelectedGameObject((_a.HasSave ? cont : fresh).gameObject);
        }

        void BuildPause()
        {
            Title("PAUSA", 48, UiFactory.Accent, 70f);
            Spacer(10f);
            Btn("Reanudar", () => _a.ContinueFromPause());
            Btn("Guardar partida", () => _a.SaveGame());
            Btn("Cargar última partida", () => _a.LoadGame(), _a.HasSave);
            Btn("Controles", () => { _before = Mode.Pause; _mode = Mode.Controls; Show(); });
            Btn("Créditos", () => { _before = Mode.Pause; _mode = Mode.Credits; Show(); });
            SensitivityRow();
            Btn("Calidad gráfica: " + _a.QualityLabel, () => { _a.CycleQuality(); Show(); });
            Btn("Menú principal", () => _a.ReturnToMainMenu());
            Btn("Salir", () => _a.QuitGame());
        }

        void BuildControls()
        {
            Title("CONTROLES", 44, UiFactory.Accent, 64f);
            var t = UiFactory.Label(_column, ControlsText, 22, UiFactory.TextColor);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 420f;
            Btn("Volver", () => { _mode = _before; Show(); });
        }

        const string CreditsText =
            "<b>Modelos 3D (Sketchfab, CC BY 4.0; modificados: optimizados y escalados)</b>\n"
          + "Pine Tree [Game-ready] - Wenedi (^-^)/\nBeech tree - evolveduk\nDead Tree - MaX3Dd\nMid Poly Dead Pine Tree - sujirour\nStone Arch Pillars - Ruins - Unreal Designer\nOld Ruined Temple - Muhammet Gündel\nModular Ancient Stone Archway - PBR Game Ready - Hardframe\nBig Rock 1B - Andrei.D\nRockcliff - DJMaesen\nCliff - DJMaesen\nDesert Cliff 6 - WireframeArt\nPine Tree Fallen Trunk Scan 02 - Pers Scans\nLog Pile (Game Ready / 2K PBR) - Meerschaum Digital\nAncient Ruined Sandstone Archway & Gate - mana5677\nRealistic HD Saguaro cactus (27/30) - PlantCatalog\nMetal Detector - Tr0n\nGrass Bushes (game ready asset) - Aparicio Silva 3D\nVintage Gas Pump - Rescue3D Assets\nMilitary Tent - Khata\nMedieval Market Stall - AspectStudios\nGame Ready Jerry Cans - Liam3D" + "\n\n<b>Poly Haven y ambientCG (CC0)</b>: texturas, rocas, tocones, plantas y props.\n"
          + "<b>Camioneta</b>: Chevrolet Silverado (Sketchfab, CC BY 4.0). Detalle completo en CREDITS_ASSETS.md.";

        void BuildCredits()
        {
            Title("CRÉDITOS", 44, UiFactory.Accent, 64f);
            var t = UiFactory.Label(_column, CreditsText, 16, UiFactory.TextColor);
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = 520f;
            Btn("Volver", () => { _mode = _before; Show(); });
        }

        void Spacer(float h)
        {
            var r = UiFactory.Rect("Spacer", _column);
            r.gameObject.AddComponent<LayoutElement>().preferredHeight = h;
        }
    }
}
