using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using HuesoOxido.Core;

namespace HuesoOxido.UI
{
    /// <summary>Raiz de la UI: crea el canvas y el EventSystem, y decide que panel esta abierto. Los paneles no se solapan.</summary>
    public sealed class UiRoot : MonoBehaviour
    {
        GameInput _input;
        InventoryView _inventory;
        ShopView _shop;
        MapView _map;
        MenuView _menu;
        JournalView _journal;
        Canvas _canvas;
        UnityEngine.UI.Image _fade;
        float _fadeT = -1f;
        const float FadeSeconds = 1.6f;

        public Transform CanvasTransform => _canvas.transform;
        public bool AnyOpen => (_inventory != null && _inventory.IsOpen) || (_shop != null && _shop.IsOpen) || (_map != null && _map.IsOpen) || (_menu != null && _menu.IsOpen) || (_journal != null && _journal.IsOpen);

        public void Initialize(GameInput input)
        {
            _input = input;
            // Con cualquier panel abierto el juego queda en pausa (tiempo y fisica).
            _input.UiModeChanged += open => Time.timeScale = open ? 0f : 1f;
            _canvas = UiFactory.CreateCanvas("GameUI", 10);
            _canvas.transform.SetParent(transform, false);

            _fade = UiFactory.Panel(_canvas.transform, "Fade", new Color(0f, 0f, 0f, 0f));
            UiFactory.Stretch(_fade.rectTransform);
            _fade.raycastTarget = false;

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                var module = es.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }
        }

        public MenuView.Mode MenuMode => _menu != null ? _menu.Current : MenuView.Mode.None;

        public void Bind(InventoryView inventory, ShopView shop, MapView map, MenuView menu, JournalView journal = null)
        {
            _inventory = inventory; _shop = shop; _map = map; _menu = menu; _journal = journal;
        }

        public void OpenInventory() { CloseAll(); _inventory.Open(); _input.SetUiMode(true); }
        public void OpenShop() { CloseAll(); _shop.Open(); _input.SetUiMode(true); }
        public void OpenMap() { CloseAll(); _map.Open(); _input.SetUiMode(true); }
        public void OpenJournal() { CloseAll(); if (_journal != null) _journal.Open(); _input.SetUiMode(true); }
        public void OpenMainMenu() { CloseAll(); _menu.OpenMain(); _input.SetUiMode(true); }
        public void OpenPause() { CloseAll(); _menu.OpenPause(); _input.SetUiMode(true); }
        public void OpenMuseum() { CloseAll(); _shop.OpenMuseum(); _input.SetUiMode(true); }

        /// <summary>Oscurece y vuelve a aclarar la pantalla (dormir, desmayo).</summary>
        public void Blink() { _fadeT = 0f; }

        public void CloseAll()
        {
            if (_inventory != null) _inventory.Close();
            if (_shop != null) _shop.Close();
            if (_map != null) _map.Close();
            if (_journal != null) _journal.Close();
            if (_menu != null) _menu.Close();
            _input.SetUiMode(false);
        }

        void OnDestroy() { Time.timeScale = 1f; }

        void Update()
        {
            if (_fadeT >= 0f)
            {
                _fadeT += Time.unscaledDeltaTime;
                float k = _fadeT / FadeSeconds;
                float a = k < 0.35f ? k / 0.35f : Mathf.Clamp01(1f - (k - 0.35f) / 0.65f);
                _fade.color = new Color(0f, 0f, 0f, a);
                if (k >= 1f) { _fadeT = -1f; _fade.color = new Color(0f, 0f, 0f, 0f); }
            }
            if (_input == null || _inventory == null) return;
            if (_menu.IsOpen)
            {
                if (_input.CancelPressed && _menu.Back()) CloseAll();
                return;
            }
            if (AnyOpen)
            {
                bool closeKey = _input.CancelPressed || _input.InventoryPressed || (_input.MapPressed && _map.IsOpen) || (_input.JournalPressed && _journal != null && _journal.IsOpen);
                if (closeKey) CloseAll();
            }
            else
            {
                if (_input.CancelPressed) OpenPause();
                else if (_input.InventoryPressed) OpenInventory();
                else if (_input.MapPressed) OpenMap();
                else if (_input.JournalPressed && _journal != null) OpenJournal();
            }
        }
    }
}
