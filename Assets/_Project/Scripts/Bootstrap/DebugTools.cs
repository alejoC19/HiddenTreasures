using UnityEngine;
using UnityEngine.InputSystem;
using HuesoOxido.Core;
using HuesoOxido.Survival;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Herramientas para PROBAR el juego (solo Editor y builds de desarrollo). F1 muestra/oculta el panel:
    /// F2 teletransporta junto a la proxima reliquia sin desenterrar, F3 +$1000, F4 avanza 3 h, F6 cambia el clima,
    /// F7 vuelve al campamento, F8 llena la energia. Muestra FPS, estado, posicion, mision y vegetacion dibujada.
    /// </summary>
    public sealed class DebugTools : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        GameContext _c;
        InstancedVegetation _veg;
        bool _show;
        float _fps = 60f;
        string _flash = "";
        float _flashUntil;
        GUIStyle _style, _box;

        public void Initialize(GameContext c) { _c = c; }

        void Update()
        {
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.05f);
            var kb = Keyboard.current;
            if (kb == null || _c == null) return;
            if (kb.f1Key.wasPressedThisFrame) _show = !_show;
            if (!_show) return;

            var s = _c.Services;
            if (kb.f2Key.wasPressedThisFrame) GoToNextRelic();
            if (kb.f3Key.wasPressedThisFrame) { s.Wallet.Add(1000); Flash("+$1000"); }
            if (kb.f4Key.wasPressedThisFrame) { s.Clock.Set((s.Clock.Hour + 3f) % 24f, s.Clock.Day); Flash("Hora: " + s.Clock.Format()); }
            if (kb.f6Key.wasPressedThisFrame)
            {
                var next = (WeatherKind)(((int)s.Weather.Current + 1) % 4);
                s.Weather.Set(next); Flash("Clima: " + next);
            }
            if (kb.f7Key.wasPressedThisFrame) { _c.Motor.Teleport(_c.World.PlayerSpawn, 200f); Flash("Campamento"); }
            if (kb.f8Key.wasPressedThisFrame) { s.Energy.RestoreFull(); Flash("Energia llena"); }
        }

        void GoToNextRelic()
        {
            Detection.BuriedSite best = null; float bd = float.MaxValue;
            foreach (var site in _c.World.Sites.Sites)
            {
                if (site.Excavated || site.ItemId == null || !site.ItemId.StartsWith(QuestManager.RelicPrefix, System.StringComparison.Ordinal)) continue;
                float d = (site.transform.position - _c.Player.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = site; }
            }
            if (best == null) { Flash("No quedan reliquias sin desenterrar"); return; }
            Vector3 at = best.transform.position + new Vector3(3f, 0f, 3f);
            at.y = _c.World.HeightAt(at.x, at.z) + 0.5f;
            _c.Motor.Teleport(at, 225f);
            Flash("Junto a " + best.SiteId + " (" + best.ItemId + ")");
        }

        void Flash(string msg) { _flash = msg; _flashUntil = Time.unscaledTime + 3f; }

        void OnGUI()
        {
            if (!_show || _c == null) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
                _style.normal.textColor = Color.white;
                _box = new GUIStyle(GUI.skin.box);
            }
            if (_veg == null) _veg = FindFirstObjectByType<InstancedVegetation>();
            var s = _c.Services;
            var fsm = _c.PlayerState != null ? _c.PlayerState.Machine : null;
            var p = _c.Player.transform.position;
            int relics = 0, relicsFound = 0, sites = 0, sitesFound = 0;
            float nearest = float.MaxValue;
            foreach (var site in _c.World.Sites.Sites)
            {
                bool relic = site.ItemId != null && site.ItemId.StartsWith(QuestManager.RelicPrefix, System.StringComparison.Ordinal);
                if (relic) { relics++; if (site.Excavated) relicsFound++; } else { sites++; if (site.Excavated) sitesFound++; }
                if (!site.Excavated) nearest = Mathf.Min(nearest, Vector3.Distance(p, site.transform.position));
            }
            string text =
                "<b>DEPURACION (F1 oculta)</b>\n" +
                "FPS " + _fps.ToString("0") + "   (" + (1000f / Mathf.Max(1f, _fps)).ToString("0.0") + " ms)\n" +
                "Plantas dibujadas " + (_veg != null ? _veg.DrawnLastFrame + " / " + _veg.InstanceCount : "-") + "\n" +
                "Pos " + p.x.ToString("0") + ", " + p.y.ToString("0.0") + ", " + p.z.ToString("0") + "   Estado: " + (fsm != null ? fsm.State.ToString() : "-") + "\n" +
                "Hora " + s.Clock.Format() + "  Dia " + s.Clock.Day + "  Clima " + s.Weather.Current + "\n" +
                "Dinero $" + s.Wallet.Money + "  Mochila " + s.Backpack.TotalWeight.ToString("0.0") + "/" + s.Backpack.CapacityKg.ToString("0.0") + " kg\n" +
                "Mision: " + (s.Quests.HasQuest ? s.Quests.ProgressText : "-") + "   Reliquias " + relicsFound + "/" + relics + "   Sitios " + sitesFound + "/" + sites + "\n" +
                "Sitio sin excavar mas cercano: " + (nearest < float.MaxValue ? nearest.ToString("0") + " m" : "-") + "   Senal " + _c.Detector.Signal.ToString("0.00") + "\n" +
                "<b>F2</b> ir a reliquia  <b>F3</b> +$1000  <b>F4</b> +3 h  <b>F6</b> clima  <b>F7</b> campamento  <b>F8</b> energia";
            if (Time.unscaledTime < _flashUntil) text += "\n<color=#ffd24a>" + _flash + "</color>";
            GUI.Box(new Rect(Screen.width - 560, 10, 550, 190), GUIContent.none, _box);
            GUI.Label(new Rect(Screen.width - 550, 14, 535, 184), text, _style);
        }
#else
        public void Initialize(GameContext c) { }
#endif
    }
}
