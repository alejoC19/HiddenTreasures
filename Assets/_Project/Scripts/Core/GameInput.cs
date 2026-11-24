using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Unica puerta de entrada de input. Crea las acciones por codigo (sin assets).
    /// Con la UI abierta, el input de juego queda en cero y el cursor se libera.
    /// </summary>
    public sealed class GameInput : MonoBehaviour, IInputBlocker
    {
        const float MouseDegreesPerCount = 0.09f;
        const float StickDegreesPerSecond = 150f;

        InputAction _move, _lookMouse, _lookStick, _sprint, _crouch, _toggleView,
                    _interact, _dig, _inventory, _map, _save, _cancel, _journal;
        bool _uiMode;

        public event Action<bool> UiModeChanged;

        public bool UiMode => _uiMode;
        public bool GameplayBlocked => _uiMode;

        public Vector2 Move => _uiMode ? Vector2.zero : Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);

        /// <summary>Delta de mirada en grados para este frame.</summary>
        public Vector2 LookDegrees
        {
            get
            {
                if (_uiMode || Cursor.lockState != CursorLockMode.Locked) return Vector2.zero;
                return _lookMouse.ReadValue<Vector2>() * MouseDegreesPerCount
                     + _lookStick.ReadValue<Vector2>() * (StickDegreesPerSecond * Time.deltaTime);
            }
        }

        public bool SprintHeld => !_uiMode && _sprint.IsPressed();
        public bool CrouchPressed => !_uiMode && _crouch.WasPressedThisFrame();
        public bool ToggleViewPressed => !_uiMode && _toggleView.WasPressedThisFrame();
        public bool InteractPressed => !_uiMode && _interact.WasPressedThisFrame();
        public bool DigHeld => !_uiMode && Cursor.lockState == CursorLockMode.Locked && _dig.IsPressed();
        public bool SavePressed => _save.WasPressedThisFrame();
        // Estas se leen incluso con la UI abierta para poder cerrarla.
        public bool InventoryPressed => _inventory.WasPressedThisFrame();
        public bool MapPressed => _map.WasPressedThisFrame();
        public bool JournalPressed => _journal.WasPressedThisFrame();
        public bool CancelPressed => _cancel.WasPressedThisFrame();

        void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");

            _lookMouse = new InputAction("LookMouse", InputActionType.Value, "<Mouse>/delta");
            _lookStick = new InputAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");

            _sprint = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            _crouch = Button("Crouch", "<Keyboard>/c", "<Keyboard>/leftCtrl", "<Gamepad>/buttonEast");
            _toggleView = Button("ToggleView", "<Keyboard>/v", "<Gamepad>/rightStickPress");
            _interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonSouth");
            _dig = Button("Dig", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            _inventory = Button("Inventory", "<Keyboard>/tab", "<Gamepad>/buttonNorth");
            _map = Button("Map", "<Keyboard>/m", "<Gamepad>/select");
            _journal = Button("Journal", "<Keyboard>/j", "<Gamepad>/dpad/up");
            _save = Button("Save", "<Keyboard>/f5");
            _cancel = Button("Cancel", "<Keyboard>/escape");
        }

        static InputAction Button(string name, params string[] paths)
        {
            var a = new InputAction(name, InputActionType.Button);
            foreach (var p in paths) a.AddBinding(p);
            return a;
        }

        void OnEnable()
        {
            _move.Enable(); _lookMouse.Enable(); _lookStick.Enable(); _sprint.Enable(); _crouch.Enable();
            _toggleView.Enable(); _interact.Enable(); _dig.Enable(); _inventory.Enable(); _map.Enable(); _journal.Enable();
            _save.Enable(); _cancel.Enable();
            ApplyCursor();
            if (InputSystem.devices.Count == 0)
                Debug.LogError("[Hidden Treasures] El Input System no detecta dispositivos. Activá 'Input System Package' en Project Settings > Player > Active Input Handling y reiniciá el Editor (menú Hidden Treasures > Configurar proyecto lo hace).");
        }

        void OnDisable()
        {
            _move.Disable(); _lookMouse.Disable(); _lookStick.Disable(); _sprint.Disable(); _crouch.Disable();
            _toggleView.Disable(); _interact.Disable(); _dig.Disable(); _inventory.Disable(); _map.Disable(); _journal.Disable();
            _save.Disable(); _cancel.Disable();
        }

        void OnDestroy()
        {
            _move?.Dispose(); _lookMouse?.Dispose(); _lookStick?.Dispose(); _sprint?.Dispose(); _crouch?.Dispose();
            _toggleView?.Dispose(); _interact?.Dispose(); _dig?.Dispose(); _inventory?.Dispose(); _map?.Dispose(); _journal?.Dispose();
            _save?.Dispose(); _cancel?.Dispose();
        }

        void Update()
        {
            // Si el usuario libero el cursor con Esc en el Editor, un clic lo recupera.
            if (!_uiMode && Cursor.lockState != CursorLockMode.Locked
                && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                ApplyCursor();
            }
        }

        public void SetUiMode(bool open)
        {
            if (_uiMode == open) return;
            _uiMode = open;
            ApplyCursor();
            UiModeChanged?.Invoke(open);
        }

        void ApplyCursor()
        {
            Cursor.lockState = _uiMode ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _uiMode;
        }
    }
}
