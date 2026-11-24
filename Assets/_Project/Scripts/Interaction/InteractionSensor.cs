using System;
using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Interaction
{
    /// <summary>Raycast desde el centro de la camara activa. Funciona igual en primera y tercera persona.</summary>
    public sealed class InteractionSensor : MonoBehaviour
    {
        [SerializeField] float rayLength = 9f;
        [SerializeField] float reach = 3.2f;

        readonly RaycastHit[] _hits = new RaycastHit[24];
        GameInput _input;
        Transform _cam;
        Transform _player;
        IInteractable _current;
        int _skipFrame = -1;

        public IInteractable Current => _current;
        public event Action<IInteractable> FocusChanged;

        public void Initialize(GameInput input, Transform camera, Transform player)
        {
            _input = input; _cam = camera; _player = player;
        }

        void OnEnable() { _skipFrame = Time.frameCount; }

        void OnDisable()
        {
            if (_current == null) return;
            _current = null;
            FocusChanged?.Invoke(null);
        }

        void Update()
        {
            if (_input == null || _cam == null || Time.frameCount == _skipFrame) return;
            var next = _input.GameplayBlocked ? null : Probe();
            if (!ReferenceEquals(next, _current))
            {
                _current = next;
                FocusChanged?.Invoke(_current);
            }
            if (_current != null && _input.InteractPressed && _current.CanInteract)
                _current.Interact(_player.gameObject);
        }

        IInteractable Probe()
        {
            var ray = new Ray(_cam.position, _cam.forward);
            int n = Physics.RaycastNonAlloc(ray, _hits, rayLength, ~0, QueryTriggerInteraction.Collide);
            IInteractable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var h = _hits[i];
                if (h.collider.transform.IsChildOf(_player)) continue;
                if (h.distance >= bestDist) continue;
                var it = h.collider.GetComponentInParent<IInteractable>();
                if (it == null || string.IsNullOrEmpty(it.Prompt)) continue;
                if (Vector3.Distance(_player.position + Vector3.up, h.point) > reach) continue;
                best = it;
                bestDist = h.distance;
            }
            return best;
        }
    }
}
