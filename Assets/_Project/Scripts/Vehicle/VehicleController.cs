using System;
using UnityEngine;
using HuesoOxido.Camera;
using HuesoOxido.Core;
using HuesoOxido.Interaction;
using HuesoOxido.Player;

namespace HuesoOxido.Vehicle
{
    /// <summary>
    /// Vehiculo conducible de suspension por raycast (4 resortes + agarre lateral). Un solo vehiculo en el MVP.
    /// Entrar/salir con E. Consume combustible y desactiva al personaje mientras se conduce.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour, IInteractable
    {
        [Header("Suspension")]
        [SerializeField] float rayLength = 0.85f;
        [SerializeField] float springK = 30000f;
        [SerializeField] float damper = 3800f;
        [Header("Conduccion")]
        [SerializeField] float mass = 1200f;
        [SerializeField] float driveForce = 7500f;
        [SerializeField] float brakeForce = 9000f;
        [SerializeField] float maxSpeed = 17f;
        [SerializeField] float reverseMaxSpeed = 6f;
        [SerializeField] float maxSteerDegrees = 30f;
        [SerializeField] float lateralGrip = 11f;
        [SerializeField] float tractionCoefficient = 1.15f;
        [SerializeField] float rollingResistance = 0.35f;

        static readonly Vector3[] Mounts =
        {
            new Vector3(-0.85f, 0.35f, 1.35f), new Vector3(0.85f, 0.35f, 1.35f),
            new Vector3(-0.85f, 0.35f, -1.35f), new Vector3(0.85f, 0.35f, -1.35f),
        };

        Rigidbody _rb;
        Transform[] _wheels;
        float[] _wheelDrop = new float[4];
        float _wheelSpin;
        GameInput _input;
        FuelTank _fuel;
        CameraRig _rig;
        GameObject _player;
        PlayerMotor _motor;
        float _steer;
        float _flipTimer;

        public bool HasDriver { get; private set; }
        public float SpeedKmh => _rb == null ? 0f : _rb.GetPointVelocity(_rb.worldCenterOfMass).magnitude * 3.6f;
        public FuelTank Fuel => _fuel;
        public string Prompt => HasDriver ? "" : "Entrar al vehículo";
        public bool CanInteract => !HasDriver;
        public event Action<bool> DriverChanged;

        public void Initialize(GameInput input, FuelTank fuel, CameraRig rig)
        {
            _input = input; _fuel = fuel; _rig = rig;
            _rb = GetComponent<Rigidbody>();
            _rb.mass = mass;
            _rb.centerOfMass = new Vector3(0f, 0.05f, 0f);
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _wheels = PlaceholderVehicle.Build(transform, Mounts);
        }

        public void Interact(GameObject actor)
        {
            var motor = actor.GetComponent<PlayerMotor>();
            if (motor == null || HasDriver) return;
            _player = actor; _motor = motor;
            HasDriver = true;
            _motor.ControlEnabled = false;
            _player.SetActive(false);
            _rig.EnterVehicle(transform, 7.2f);
            DriverChanged?.Invoke(true);
        }

        public void ExitVehicle()
        {
            if (!HasDriver) return;
            Vector3 side = transform.TransformPoint(-2.0f, 3f, 0f);
            Vector3 pos = Physics.Raycast(side, Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point + Vector3.up * 0.1f : transform.position + Vector3.up * 2f;
            HasDriver = false;
            _player.SetActive(true);
            _motor.Teleport(pos, transform.eulerAngles.y);
            _motor.ControlEnabled = true;
            _rig.ExitVehicle();
            DriverChanged?.Invoke(false);
        }

        public void Teleport(Vector3 position, float yaw)
        {
            _rb.position = position;
            _rb.rotation = Quaternion.Euler(0f, yaw, 0f);
            _rb.StopMotion();
        }

        void Update()
        {
            if (!HasDriver) return;
            if (_input.InteractPressed && SpeedKmh < 12f) ExitVehicle();

            // Autoenderezado si volco.
            if (Vector3.Dot(transform.up, Vector3.up) < 0.25f && SpeedKmh < 4f) _flipTimer += Time.deltaTime; else _flipTimer = 0f;
            if (_flipTimer > 2f)
            {
                _flipTimer = 0f;
                _rb.position += Vector3.up * 1.5f;
                _rb.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            }
        }

        void FixedUpdate()
        {
            if (_rb == null) return;
            float dt = Time.fixedDeltaTime;
            Vector2 move = HasDriver ? _input.Move : Vector2.zero;
            float throttle = move.y;
            bool fuelOk = _fuel != null && !_fuel.IsEmpty;
            if (!fuelOk) throttle = 0f;

            float speedRatio = Mathf.Clamp01(SpeedKmh / 3.6f / maxSpeed);
            float steerTarget = move.x * maxSteerDegrees * Mathf.Lerp(1f, 0.35f, speedRatio);
            _steer = Mathf.MoveTowards(_steer, steerTarget, 140f * dt);

            Vector3 up = transform.up;
            bool parked = !HasDriver;
            int grounded = 0;

            for (int i = 0; i < 4; i++)
            {
                Vector3 mount = transform.TransformPoint(Mounts[i]);
                if (!Physics.Raycast(mount, -up, out RaycastHit hit, rayLength, ~0, QueryTriggerInteraction.Ignore))
                {
                    _wheelDrop[i] = Mathf.Lerp(_wheelDrop[i], rayLength, 6f * dt);
                    continue;
                }
                grounded++;
                _wheelDrop[i] = hit.distance;

                Vector3 pointVel = _rb.GetPointVelocity(mount);
                float compression = rayLength - hit.distance;
                float spring = Mathf.Max(0f, compression * springK - Vector3.Dot(pointVel, up) * damper);
                _rb.AddForceAtPosition(up * spring, mount);

                Quaternion steerRot = i < 2 ? Quaternion.AngleAxis(_steer, up) : Quaternion.identity;
                Vector3 wf = Vector3.ProjectOnPlane(steerRot * transform.forward, hit.normal).normalized;
                Vector3 wr = Vector3.Cross(hit.normal, wf).normalized;
                float vLat = Vector3.Dot(pointVel, wr);
                float vFwd = Vector3.Dot(pointVel, wf);

                Vector3 force = -wr * vLat * (mass * 0.25f) * lateralGrip;
                if (throttle != 0f)
                {
                    float limit = throttle > 0f ? maxSpeed : reverseMaxSpeed;
                    if (throttle * vFwd >= -0.5f)
                        force += wf * throttle * driveForce * 0.25f * Mathf.Clamp01(1f - Mathf.Abs(vFwd) / limit);
                    else
                        force += wf * throttle * brakeForce * 0.25f;
                }
                else
                {
                    float resist = parked ? 6f : rollingResistance;
                    force += -wf * vFwd * (mass * 0.25f) * resist * 4f;
                }

                float maxTraction = spring * tractionCoefficient + 50f;
                if (force.sqrMagnitude > maxTraction * maxTraction) force = force.normalized * maxTraction;
                _rb.AddForceAtPosition(force, mount);
            }

            // Amortiguacion angular: fuerte en cabeceo/balanceo, suave en giro.
            Vector3 localAv = transform.InverseTransformDirection(_rb.angularVelocity);
            Vector3 damp = new Vector3(localAv.x * 2800f, localAv.y * 350f, localAv.z * 2800f);
            _rb.AddTorque(-transform.TransformDirection(damp));

            if (HasDriver && fuelOk) _fuel.Consume((0.012f + Mathf.Abs(throttle) * 0.2f) * dt);

            AnimateWheels(dt, move.x);
        }

        void AnimateWheels(float dt, float steerInput)
        {
            if (_wheels == null) return;
            float vFwd = Vector3.Dot(_rb.GetPointVelocity(_rb.worldCenterOfMass), transform.forward);
            _wheelSpin += vFwd / PlaceholderVehicle.WheelRadius * Mathf.Rad2Deg * dt;
            for (int i = 0; i < 4; i++)
            {
                var w = _wheels[i];
                w.localPosition = Mounts[i] + Vector3.down * Mathf.Max(0f, Mathf.Min(_wheelDrop[i], rayLength) - PlaceholderVehicle.WheelRadius);
                float steer = i < 2 ? _steer : 0f;
                w.localRotation = Quaternion.Euler(_wheelSpin, steer, 0f);
            }
        }
    }

    static class RigidbodyExtensions
    {
        public static void StopMotion(this Rigidbody rb)
        {
            // Evita la propiedad de velocidad (renombrada en Unity 6): se frena con un impulso opuesto.
            Vector3 v = rb.GetPointVelocity(rb.worldCenterOfMass);
            rb.AddForce(-v, ForceMode.VelocityChange);
            rb.angularVelocity = Vector3.zero;
        }
    }
}
