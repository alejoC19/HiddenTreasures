using System;
using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Player
{
    /// <summary>
    /// Controlador unico del personaje (primera y tercera persona comparten este componente).
    /// Mueve relativo a la camara; caminar, correr y agacharse; carga y energia lo modulan.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("Velocidades (m/s)")]
        [SerializeField] float walkSpeed = 3.4f;
        [SerializeField] float runSpeed = 5.8f;
        [SerializeField] float crouchSpeed = 1.7f;
        [SerializeField] float acceleration = 14f;
        [Header("Fisica")]
        [SerializeField] float gravity = -22f;
        [SerializeField] float turnSpeed = 14f;
        [Header("Altura")]
        [SerializeField] float standHeight = 1.8f;
        [SerializeField] float crouchHeight = 1.1f;
        [SerializeField] float standEye = 1.65f;
        [SerializeField] float crouchEye = 1.0f;

        CharacterController _cc;
        GameInput _input;
        IViewDirection _view;
        ISprintPermission _sprintPermission;
        ILoadSource _load;
        Vector3 _horizontal;
        float _vertical;
        bool _wasGrounded = true;
        float _heightNow, _eyeNow;

        public bool ControlEnabled { get; set; } = true;
        public bool IsCrouched { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsGrounded => _cc != null && _cc.isGrounded;
        /// <summary>Velocidad horizontal normalizada: 0 quieto, 1 corriendo.</summary>
        public float Speed01 { get; private set; }
        public float EyeHeight => _eyeNow;
        public float CurrentHeight => _heightNow;

        /// <summary>Se dispara al aterrizar con la velocidad vertical de impacto (m/s).</summary>
        public event Action<float> Landed;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _cc.radius = 0.3f;
            _cc.stepOffset = 0.38f;
            _cc.slopeLimit = 50f;
            _cc.skinWidth = 0.03f;
            _heightNow = standHeight;
            _eyeNow = standEye;
            ApplyHeight();
        }

        public void Initialize(GameInput input, IViewDirection view, ISprintPermission sprintPermission, ILoadSource load)
        {
            _input = input;
            _view = view;
            _sprintPermission = sprintPermission;
            _load = load;
        }

        public void Teleport(Vector3 position, float yaw)
        {
            _cc.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            _cc.enabled = true;
            _horizontal = Vector3.zero;
            _vertical = 0f;
        }

        void Update()
        {
            if (_input == null || _view == null) return;
            float dt = Time.deltaTime;

            Vector2 move = ControlEnabled ? _input.Move : Vector2.zero;
            if (ControlEnabled && _input.CrouchPressed) ToggleCrouch();

            bool wantsSprint = ControlEnabled && _input.SprintHeld && move.y > 0.1f && !IsCrouched
                               && (_sprintPermission == null || _sprintPermission.CanSprint);
            IsSprinting = wantsSprint;

            float baseSpeed = IsCrouched ? crouchSpeed : (IsSprinting ? runSpeed : walkSpeed);
            float load = _load != null ? _load.LoadFraction : 0f;
            float speed = baseSpeed * (1f - 0.35f * load * load);

            var yaw = Quaternion.Euler(0f, _view.Yaw, 0f);
            Vector3 wish = yaw * new Vector3(move.x, 0f, move.y) * speed;
            _horizontal = Vector3.MoveTowards(_horizontal, wish, acceleration * dt);

            if (_cc.isGrounded && _vertical < 0f) _vertical = -2f;
            _vertical += gravity * dt;

            if (ControlEnabled)
            {
                // El cuerpo mira hacia donde mira la camara (estilo "shoulder"), el movimiento es relativo a ella.
                var target = Quaternion.Euler(0f, _view.Yaw, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSpeed * dt));
            }

            float preVertical = _vertical;
            _cc.Move((_horizontal + Vector3.up * _vertical) * dt);

            bool grounded = _cc.isGrounded;
            if (grounded && !_wasGrounded && preVertical < -6f) Landed?.Invoke(-preVertical);
            _wasGrounded = grounded;

            float flat = new Vector3(_horizontal.x, 0f, _horizontal.z).magnitude;
            IsMoving = flat > 0.2f;
            Speed01 = Mathf.Clamp01(flat / runSpeed);

            UpdateHeight(dt);
        }

        void ToggleCrouch()
        {
            if (!IsCrouched) { IsCrouched = true; return; }
            // Para levantarse tiene que haber espacio libre arriba.
            Vector3 start = transform.position + Vector3.up * (crouchHeight + 0.02f);
            if (!Physics.Raycast(start, Vector3.up, standHeight - crouchHeight, ~0, QueryTriggerInteraction.Ignore))
                IsCrouched = false;
        }

        void UpdateHeight(float dt)
        {
            float targetH = IsCrouched ? crouchHeight : standHeight;
            float targetEye = IsCrouched ? crouchEye : standEye;
            float k = 1f - Mathf.Exp(-12f * dt);
            _heightNow = Mathf.Lerp(_heightNow, targetH, k);
            _eyeNow = Mathf.Lerp(_eyeNow, targetEye, k);
            ApplyHeight();
        }

        void ApplyHeight()
        {
            _cc.height = _heightNow;
            _cc.center = new Vector3(0f, _heightNow * 0.5f, 0f);
        }
    }
}
