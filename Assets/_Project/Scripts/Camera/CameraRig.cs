using System;
using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Camera
{
    /// <summary>
    /// Una sola camara con dos perspectivas intercambiables (tecla V) y transicion suave.
    /// Tercera persona: cámara al hombro con colision. En vehiculo se fuerza tercera persona.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour, IViewState
    {
        [Header("Tercera persona")]
        [SerializeField] float thirdDistance = 3.1f;
        [SerializeField] float shoulderOffset = 0.55f;
        [SerializeField] float thirdHeightBoost = 0.18f;
        [SerializeField] float collisionRadius = 0.25f;
        [SerializeField] float followSharpness = 16f;
        [Header("Primera persona")]
        [SerializeField] float firstForward = 0.12f;
        [Header("Transicion y FOV")]
        [SerializeField] float blendTime = 0.22f;
        [SerializeField] float thirdFov = 62f;
        [SerializeField] float firstFov = 72f;

        UnityEngine.Camera _cam;
        GameInput _input;
        LookController _look;
        Transform _target;
        Func<float> _pivotHeight;
        IBodyVisibility _body;
        Transform _playerRoot;
        Func<float> _playerEye;
        float _fpWeight, _fpVelocity, _tpDistance;
        bool _vehicleMode;
        float _vehicleDistance = 6.5f;
        ViewMode _wanted = ViewMode.ThirdPerson;

        public ViewMode Mode => _wanted;
        public float FirstPersonWeight => _fpWeight;
        public UnityEngine.Camera Camera => _cam;
        public event Action<ViewMode> ModeChanged;

        public void Initialize(UnityEngine.Camera cam, GameInput input, LookController look,
                               Transform player, Func<float> playerEyeHeight, IBodyVisibility body)
        {
            _cam = cam; _input = input; _look = look; _body = body;
            _playerRoot = player; _playerEye = playerEyeHeight;
            _target = player; _pivotHeight = playerEyeHeight;
            _tpDistance = thirdDistance;
            _cam.nearClipPlane = 0.05f;
            _fpWeight = _wanted == ViewMode.FirstPerson ? 1f : 0f;
        }

        public void SetMode(ViewMode mode)
        {
            if (_vehicleMode && mode == ViewMode.FirstPerson) return;
            if (_wanted == mode) return;
            _wanted = mode;
            ModeChanged?.Invoke(mode);
        }

        public void EnterVehicle(Transform vehicle, float distance)
        {
            _vehicleMode = true;
            _vehicleDistance = distance;
            _target = vehicle;
            _pivotHeight = () => 1.7f;
            if (_wanted != ViewMode.ThirdPerson) { _wanted = ViewMode.ThirdPerson; ModeChanged?.Invoke(_wanted); }
        }

        public void ExitVehicle()
        {
            _vehicleMode = false;
            _target = _playerRoot;
            _pivotHeight = _playerEye;
        }

        void LateUpdate()
        {
            if (_cam == null || _target == null || _look == null) return;
            float dt = Time.deltaTime;

            if (_input != null && _input.ToggleViewPressed)
                SetMode(_wanted == ViewMode.ThirdPerson ? ViewMode.FirstPerson : ViewMode.ThirdPerson);

            float goal = _wanted == ViewMode.FirstPerson ? 1f : 0f;
            _fpWeight = Mathf.SmoothDamp(_fpWeight, goal, ref _fpVelocity, blendTime);
            if (Mathf.Abs(_fpWeight - goal) < 0.002f) _fpWeight = goal;

            var rot = Quaternion.Euler(_look.Pitch, _look.Yaw, 0f);
            Vector3 pivot = _target.position + Vector3.up * _pivotHeight();

            float dist = _vehicleMode ? _vehicleDistance : thirdDistance;
            float shoulder = _vehicleMode ? 0f : shoulderOffset;
            Vector3 offset = rot * new Vector3(shoulder, thirdHeightBoost, -dist);
            float desired = offset.magnitude;
            Vector3 dir = offset / Mathf.Max(0.0001f, desired);

            float allowed = desired;
            if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit, desired, ~0, QueryTriggerInteraction.Ignore))
                allowed = Mathf.Max(0.35f, hit.distance - 0.05f);

            // Se acerca de golpe (para no atravesar paredes) y se aleja suavemente.
            _tpDistance = allowed < _tpDistance ? allowed : Mathf.Lerp(_tpDistance, allowed, 1f - Mathf.Exp(-followSharpness * dt * 0.5f));

            Vector3 thirdPos = pivot + dir * _tpDistance;
            Vector3 firstPos = pivot + rot * new Vector3(0f, 0f, firstForward);
            transform.position = Vector3.Lerp(thirdPos, firstPos, Mathf.SmoothStep(0f, 1f, _fpWeight));
            transform.rotation = rot;
            _cam.fieldOfView = Mathf.Lerp(thirdFov, firstFov, _fpWeight);

            _body?.SetVisible(_vehicleMode ? true : _fpWeight < 0.7f);
        }
    }
}
