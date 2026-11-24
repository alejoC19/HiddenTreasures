using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HuesoOxido.Core;
using HuesoOxido.Detection;
using HuesoOxido.Digging;
using HuesoOxido.Economy;
using HuesoOxido.Interaction;
using HuesoOxido.Inventory;
using HuesoOxido.Survival;
using HuesoOxido.Vehicle;

namespace HuesoOxido.UI
{
    /// <summary>Datos que el HUD solo lee. Cero logica de negocio en la vista.</summary>
    public sealed class HudContext
    {
        public EnergyModel Energy;
        public InventoryModel Backpack;
        public Wallet Wallet;
        public GameClock Clock;
        public WeatherModel Weather;
        public MetalDetector Detector;
        public DigController Dig;
        public InteractionSensor Sensor;
        public VehicleController Vehicle;
        public Notifier Notifier;
        public GameInput Input;
    }

    public sealed class HudView : MonoBehaviour
    {
        struct Toast { public string Text; public float Until; }

        HudContext _c;
        Text _energyText, _packText, _moneyText, _timeText, _prompt, _digMessage, _toastText, _speedText, _help;
        RectTransform _energyFill, _packFill, _signalFill, _digFill, _fuelFill;
        GameObject _digRoot, _vehicleRoot, _signalRoot;
        readonly List<Toast> _toasts = new List<Toast>();

        public void Initialize(Transform canvas, HudContext ctx)
        {
            _c = ctx;
            var root = UiFactory.Rect("HUD", canvas);
            UiFactory.Stretch(root);

            // Panel de estado (arriba a la izquierda)
            var status = UiFactory.Panel(root, "Status", new Color(0.04f, 0.04f, 0.03f, 0.55f));
            UiFactory.Place(status.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(430f, 196f));

            _energyText = Line(status.transform, 12f, "Energía");
            _energyFill = BarAt(status.transform, 38f, UiFactory.Good);
            _packText = Line(status.transform, 66f, "Mochila");
            _packFill = BarAt(status.transform, 92f, UiFactory.Accent);
            _moneyText = Line(status.transform, 122f, "");
            _timeText = Line(status.transform, 152f, "");

            // Mira
            var cross = UiFactory.Panel(root, "Crosshair", new Color(1f, 1f, 1f, 0.7f));
            UiFactory.Place(cross.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f));

            // Detector (señal)
            _signalRoot = UiFactory.Panel(root, "Signal", new Color(0.04f, 0.04f, 0.03f, 0.55f)).gameObject;
            UiFactory.Place((RectTransform)_signalRoot.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(54f, 260f));
            var sl = UiFactory.Label(_signalRoot.transform, "SEÑAL", 14, UiFactory.Muted, TextAnchor.UpperCenter);
            UiFactory.Region(sl.rectTransform, 0f, 0.92f, 1f, 1f);
            var sBack = UiFactory.Bar(_signalRoot.transform, "SignalBar", UiFactory.Good, out var sImg);
            _signalFill = sBack;
            UiFactory.Region(sImg.rectTransform, 0.3f, 0.04f, 0.7f, 0.9f);
            _signalFill.anchorMin = new Vector2(0f, 0f); _signalFill.anchorMax = new Vector2(1f, 0f);

            // Excavacion
            _digRoot = UiFactory.Rect("Dig", root).gameObject;
            var digRect = (RectTransform)_digRoot.transform;
            UiFactory.Place(digRect, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 26f));
            _digFill = UiFactory.Bar(_digRoot.transform, "DigBar", UiFactory.Accent, out var dImg);
            UiFactory.Stretch(dImg.rectTransform);
            _digRoot.SetActive(false);
            _digMessage = UiFactory.Label(root, "", 22, UiFactory.Accent, TextAnchor.MiddleCenter);
            UiFactory.Place(_digMessage.rectTransform, new Vector2(0.5f, 0.37f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 40f));

            // Indicador de interaccion
            _prompt = UiFactory.Label(root, "", 26, UiFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 44f));

            // Vehiculo
            _vehicleRoot = UiFactory.Panel(root, "Vehicle", new Color(0.04f, 0.04f, 0.03f, 0.55f)).gameObject;
            UiFactory.Place((RectTransform)_vehicleRoot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(300f, 110f));
            _speedText = UiFactory.Label(_vehicleRoot.transform, "0 km/h", 30, UiFactory.TextColor, TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Region(_speedText.rectTransform, 0.05f, 0.45f, 1f, 1f);
            var fBack = UiFactory.Bar(_vehicleRoot.transform, "FuelBar", UiFactory.Accent, out var fImg);
            _fuelFill = fBack;
            UiFactory.Region(fImg.rectTransform, 0.05f, 0.12f, 0.95f, 0.38f);
            _vehicleRoot.SetActive(false);

            // Avisos
            _toastText = UiFactory.Label(root, "", 24, UiFactory.Accent, TextAnchor.UpperCenter, FontStyle.Bold);
            UiFactory.Place(_toastText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1100f, 160f));

            // Ayuda
            _help = UiFactory.Label(root, "WASD mover · Shift correr · C agacharse · V cámara · Clic mantener: excavar · E interactuar · Tab inventario · M mapa · F5 guardar",
                14, new Color(1f, 1f, 1f, 0.45f), TextAnchor.LowerLeft);
            UiFactory.Place(_help.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 18f), new Vector2(1500f, 30f));

            _c.Notifier.Posted += OnMessage;
        }

        Text Line(Transform parent, float y, string text)
        {
            var t = UiFactory.Label(parent, text, 20, UiFactory.TextColor, TextAnchor.MiddleLeft);
            UiFactory.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -y), new Vector2(400f, 26f));
            return t;
        }

        RectTransform BarAt(Transform parent, float y, Color color)
        {
            var fill = UiFactory.Bar(parent, "Bar", color, out var back);
            UiFactory.Place(back.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -y), new Vector2(400f, 16f));
            return fill;
        }

        void OnDestroy() { if (_c != null && _c.Notifier != null) _c.Notifier.Posted -= OnMessage; }

        void OnMessage(string msg)
        {
            _toasts.Add(new Toast { Text = msg, Until = Time.unscaledTime + 4.5f });
            if (_toasts.Count > 4) _toasts.RemoveAt(0);
        }

        static string WeatherName(WeatherKind k)
        {
            switch (k)
            {
                case WeatherKind.Cloudy: return "Nublado";
                case WeatherKind.Rain: return "Lluvia";
                case WeatherKind.Fog: return "Niebla";
                default: return "Despejado";
            }
        }

        void Update()
        {
            if (_c == null) return;
            var e = _c.Energy;
            _energyText.text = "Energía  " + Mathf.CeilToInt(e.Current) + " / " + Mathf.CeilToInt(e.Max);
            UiFactory.SetBar(_energyFill, e.Fraction, e.Fraction < 0.25f ? UiFactory.Bad : UiFactory.Good);

            var b = _c.Backpack;
            _packText.text = "Mochila  " + b.TotalWeight.ToString("0.0") + " / " + b.CapacityKg.ToString("0.0") + " kg";
            UiFactory.SetBar(_packFill, b.LoadFraction, b.LoadFraction > 0.85f ? UiFactory.Bad : UiFactory.Accent);

            _moneyText.text = "<color=#" + UiFactory.Hex(UiFactory.Accent) + ">$ " + _c.Wallet.Money + "</color>";
            _timeText.text = "Día " + _c.Clock.Day + "  " + _c.Clock.Format() + "  ·  " + WeatherName(_c.Weather.Current)
                             + (_c.Clock.IsNight ? "  ·  Noche" : "");

            bool driving = _c.Vehicle != null && _c.Vehicle.HasDriver;
            _signalRoot.SetActive(!driving);
            if (!driving)
            {
                float s = _c.Detector.Signal;
                _signalFill.anchorMax = new Vector2(1f, Mathf.Clamp01(s));
                _signalFill.GetComponent<Image>().color = s > 0.66f ? UiFactory.Bad : (s > 0.33f ? UiFactory.Accent : UiFactory.Good);
            }

            var dig = _c.Dig;
            _digRoot.SetActive(dig.IsDigging);
            if (dig.IsDigging) UiFactory.SetBar(_digFill, dig.Progress);
            _digMessage.text = dig.StatusMessage;

            _vehicleRoot.SetActive(driving);
            if (driving)
            {
                _speedText.text = Mathf.RoundToInt(_c.Vehicle.SpeedKmh) + " km/h";
                UiFactory.SetBar(_fuelFill, _c.Vehicle.Fuel.Fraction, _c.Vehicle.Fuel.Fraction < 0.2f ? UiFactory.Bad : UiFactory.Accent);
            }

            string prompt = "";
            if (driving) prompt = "[E] Bajar del vehículo (a baja velocidad)";
            else if (!_c.Input.UiMode && _c.Sensor.Current != null && _c.Sensor.Current.CanInteract) prompt = "[E] " + _c.Sensor.Current.Prompt;
            _prompt.text = prompt;

            _toasts.RemoveAll(t => t.Until < Time.unscaledTime);
            var sb = new System.Text.StringBuilder();
            for (int i = _toasts.Count - 1; i >= 0; i--) sb.AppendLine(_toasts[i].Text);
            _toastText.text = sb.ToString();
        }
    }
}
