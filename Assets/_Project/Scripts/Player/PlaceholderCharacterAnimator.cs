using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Player
{
    /// <summary>Animacion procedural del cuerpo placeholder: caminar, correr, agacharse, barrer y excavar.</summary>
    public sealed class PlaceholderCharacterAnimator : MonoBehaviour
    {
        PlaceholderCharacter _body;
        PlayerMotor _motor;
        IDigActivity _dig;
        float _phase, _digPhase, _lean;

        public void Initialize(PlaceholderCharacter body, PlayerMotor motor, IDigActivity dig)
        {
            _body = body; _motor = motor; _dig = dig;
        }

        void LateUpdate()
        {
            if (_body == null || _motor == null || _body.Torso == null) return;
            float dt = Time.deltaTime;
            bool digging = _dig != null && _dig.IsDigging;

            _phase += dt * Mathf.Lerp(5f, 11f, _motor.Speed01) * (_motor.IsMoving ? 1f : 0f);
            float swing = Mathf.Sin(_phase) * Mathf.Lerp(0f, 38f, Mathf.Clamp01(_motor.Speed01 * 1.6f));

            _body.LeftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            _body.RightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            _body.LeftArm.localRotation = Quaternion.Euler(-swing * 0.7f, 0f, 0f);

            float targetLean = digging ? 28f : (_motor.IsCrouched ? 18f : 0f);
            _lean = Mathf.Lerp(_lean, targetLean, 1f - Mathf.Exp(-10f * dt));
            _body.Torso.localRotation = Quaternion.Euler(_lean, 0f, 0f);
            float drop = _motor.IsCrouched ? 0.32f : 0f;
            _body.Torso.localPosition = new Vector3(0f, 0.9f - drop, 0f);

            if (digging)
            {
                _digPhase += dt * 5.5f;
                float stroke = Mathf.Sin(_digPhase);
                _body.RightArm.localRotation = Quaternion.Euler(-70f + stroke * 35f, 0f, 0f);
                _body.Shovel.gameObject.SetActive(true);
            }
            else
            {
                // Brazo extendido sosteniendo el detector, con un barrido lento.
                float sweep = Mathf.Sin(Time.time * 1.3f) * 14f;
                _body.RightArm.localRotation = Quaternion.Euler(-62f, sweep, 0f);
                if (_body.Shovel.gameObject.activeSelf) _body.Shovel.gameObject.SetActive(false);
            }
        }
    }
}
