using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
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

        // Personaje real: Idle/Walk/Run mezclados con Playables (no hace falta un AnimatorController).
        PlayableGraph _graph;
        AnimationMixerPlayable _mixer;
        bool _graphReady;
        float _wIdle = 1f, _wWalk, _wRun, _swayPhase, _crouchDrop;

        public void Initialize(PlaceholderCharacter body, PlayerMotor motor, IDigActivity dig)
        {
            _body = body; _motor = motor; _dig = dig;
        }

        void LateUpdate()
        {
            if (_body == null || _motor == null || _body.Torso == null) return;
            float dt = Time.deltaTime;
            bool digging = _dig != null && _dig.IsDigging;
            if (_body.IsModel) { UpdateModel(dt, digging); return; }

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

        // ---------------------------------------------------------------- personaje real

        void EnsureGraph()
        {
            if (_graphReady || _body.ModelAnimator == null) return;
            var clips = Resources.LoadAll<AnimationClip>(PlaceholderCharacter.ModelPath);
            AnimationClip idle = null, walk = null, run = null;
            foreach (var c in clips)
            {
                string n = c.name.ToLowerInvariant();
                if (n == "idle") idle = c; else if (n == "walk") walk = c; else if (n == "run") run = c;
            }
            if (idle == null) { Debug.LogWarning("[Personaje] no se encontraron clips de animacion en " + PlaceholderCharacter.ModelPath + " (" + clips.Length + " clips)"); return; }
            _graph = PlayableGraph.Create("CharacterAnim");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationMixerPlayable.Create(_graph, 3);
            Connect(0, idle); Connect(1, walk != null ? walk : idle); Connect(2, run != null ? run : (walk != null ? walk : idle));
            var output = AnimationPlayableOutput.Create(_graph, "out", _body.ModelAnimator);
            output.SetSourcePlayable(_mixer);
            _graph.Play();
            _graphReady = true;
        }

        void Connect(int port, AnimationClip clip)
        {
            var p = AnimationClipPlayable.Create(_graph, clip);
            _graph.Connect(p, 0, _mixer, port);
        }

        void UpdateModel(float dt, bool digging)
        {
            EnsureGraph();
            bool moving = _motor.IsMoving && !digging;
            float run = moving ? Mathf.InverseLerp(0.55f, 0.9f, _motor.Speed01) : 0f;
            float tIdle = moving ? 0f : 1f, tWalk = moving ? 1f - run : 0f, tRun = moving ? run : 0f;
            float k = 1f - Mathf.Exp(-9f * dt);
            _wIdle = Mathf.Lerp(_wIdle, tIdle, k); _wWalk = Mathf.Lerp(_wWalk, tWalk, k); _wRun = Mathf.Lerp(_wRun, tRun, k);
            if (_graphReady)
            {
                _mixer.SetInputWeight(0, _wIdle); _mixer.SetInputWeight(1, _wWalk); _mixer.SetInputWeight(2, _wRun);
            }
            ApplyModelPose(dt, digging);
        }

        /// <summary>Evalua una pose fija (vista previa en modo edicion). t en segundos dentro del clip de caminar.</summary>
        public void PreviewPose(float walkWeight, float time, bool digging)
        {
            EnsureGraph();
            Debug.Log("[PREVIEW] animacion del personaje lista=" + _graphReady);
            if (_graphReady)
            {
                _mixer.SetInputWeight(0, 1f - walkWeight); _mixer.SetInputWeight(1, walkWeight); _mixer.SetInputWeight(2, 0f);
                _graph.Evaluate(time);
            }
            ApplyModelPose(0.02f, digging);
        }

        void ApplyModelPose(float dt, bool digging)
        {
            var body = _body;
            float targetLean = digging ? 26f : (_motor != null && _motor.IsCrouched ? 16f : 0f);
            _lean = Mathf.Lerp(_lean, targetLean, 1f - Mathf.Exp(-10f * dt));
            float targetDrop = (_motor != null && _motor.IsCrouched) ? 0.25f : 0f;
            _crouchDrop = Mathf.Lerp(_crouchDrop, targetDrop, 1f - Mathf.Exp(-10f * dt));
            body.ModelRoot.localPosition = new Vector3(0f, -_crouchDrop, 0f);
            if (body.SpineBone != null && _lean > 0.1f)
                body.SpineBone.rotation = Quaternion.AngleAxis(_lean, body.transform.right) * body.SpineBone.rotation;

            Vector3 fwd = body.transform.forward, right = body.transform.right;
            Transform hand = body.RightHand;
            if (digging)
            {
                _digPhase += dt * 5.5f;
                float stroke = Mathf.Sin(_digPhase);
                float pitch = 38f + stroke * 24f;
                Vector3 dir = Quaternion.AngleAxis(pitch, right) * fwd;
                AimArm(body.ArmRBone, body.ForeArmRBone, hand, dir, 1f);
                AimArm(body.ArmLBone, body.ForeArmLBone, null, dir, 1f);
                body.Shovel.gameObject.SetActive(true);
                body.Shovel.rotation = Quaternion.LookRotation(dir, Vector3.up);
                body.Shovel.position = hand.position;
            }
            else
            {
                // Brazo derecho extendido con el detector, barrido lento.
                float sweep = Mathf.Sin(Time.time * 1.3f) * 14f;
                Vector3 dir = Quaternion.AngleAxis(sweep, Vector3.up) * (Quaternion.AngleAxis(24f, right) * fwd);
                AimArm(body.ArmRBone, body.ForeArmRBone, hand, dir, 1f);
                if (body.Shovel.gameObject.activeSelf) body.Shovel.gameObject.SetActive(false);
            }
        }

        /// <summary>Apunta el brazo (hombro y antebrazo) hacia una direccion de mundo, sin depender de los ejes locales de los huesos.</summary>
        static void AimArm(Transform arm, Transform fore, Transform hand, Vector3 dir, float weight)
        {
            if (arm == null || fore == null) return;
            Vector3 cur = (fore.position - arm.position).normalized;
            Quaternion q = Quaternion.FromToRotation(cur, dir.normalized);
            arm.rotation = Quaternion.Slerp(Quaternion.identity, q, weight) * arm.rotation;
            Transform end = hand != null ? hand : (fore.childCount > 0 ? fore.GetChild(0) : null);
            if (end == null) return;
            Vector3 cur2 = (end.position - fore.position).normalized;
            fore.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(cur2, dir.normalized), weight) * fore.rotation;
        }

        void OnDestroy() { if (_graphReady && _graph.IsValid()) _graph.Destroy(); }
    }
}
