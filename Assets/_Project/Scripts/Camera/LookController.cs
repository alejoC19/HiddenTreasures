using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Camera
{
    /// <summary>Acumula yaw/pitch desde el input. Ambas perspectivas comparten esta orientacion.</summary>
    public sealed class LookController : MonoBehaviour, IViewDirection
    {
        [SerializeField, Range(0.2f, 3f)] float sensitivity = 1f;
        [SerializeField] float minPitch = -70f;
        [SerializeField] float maxPitch = 80f;
        [SerializeField] bool invertY;

        GameInput _input;

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        public float Sensitivity
        {
            get => sensitivity;
            set => sensitivity = Mathf.Clamp(value, 0.2f, 3f);
        }

        public void Initialize(GameInput input) { _input = input; }

        public void SetRotation(float yaw, float pitch) { Yaw = yaw; Pitch = Mathf.Clamp(pitch, minPitch, maxPitch); }

        void Update()
        {
            if (_input == null) return;
            Vector2 d = _input.LookDegrees * sensitivity;
            Yaw += d.x;
            Pitch = Mathf.Clamp(Pitch + (invertY ? d.y : -d.y), minPitch, maxPitch);
            if (Yaw > 360f) Yaw -= 360f; else if (Yaw < -360f) Yaw += 360f;
        }
    }
}
