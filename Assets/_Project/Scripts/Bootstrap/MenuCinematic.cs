using System;
using System.Collections.Generic;
using UnityEngine;
using HuesoOxido.UI;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Fondo cinematografico del menu principal: la camara orbita despacio el campamento (fogata, niebla, luciernagas) mientras el
    /// juego esta en pausa. Usa tiempo sin escala (Time.timeScale es 0 con la UI abierta) y pone las particulas en tiempo sin escala
    /// mientras dura el menu, para que el fuego y la bruma sigan vivos. Al salir del menu devuelve el control a CameraRig.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class MenuCinematic : MonoBehaviour
    {
        UnityEngine.Camera _cam;
        UiRoot _ui;
        Vector3 _focus;
        Func<float, float, float> _heightAt;
        readonly List<ParticleSystem> _unscaled = new List<ParticleSystem>();
        bool _active;
        float _startTime;

        public bool IsActive => _active;

        public void Initialize(UnityEngine.Camera cam, UiRoot ui, Vector3 campFire, Func<float, float, float> heightAt)
        {
            _cam = cam; _ui = ui; _focus = campFire; _heightAt = heightAt;
        }

        /// <summary>Posicion y mirada de la camara en el instante t (pura, para probar el recorrido).</summary>
        public static void Pose(Vector3 focus, Func<float, float, float> heightAt, float t, out Vector3 position, out Vector3 lookAt)
        {
            float angle = (200f + t * 4.5f) * Mathf.Deg2Rad;
            float radius = 10.5f + 1.0f * Mathf.Sin(t * 0.17f);      // dentro de la explanada despejada (sin pasto alto)
            float x = focus.x + Mathf.Sin(angle) * radius, z = focus.z + Mathf.Cos(angle) * radius;
            float y = heightAt(x, z) + 2.4f + 0.4f * Mathf.Sin(t * 0.23f);
            position = new Vector3(x, y, z);
            lookAt = focus + Vector3.up * (1.3f + 0.25f * Mathf.Sin(t * 0.31f)) + new Vector3(Mathf.Sin(t * 0.13f), 0f, Mathf.Cos(t * 0.11f)) * 1.2f;
        }

        void LateUpdate()
        {
            if (_cam == null || _ui == null) return;
            bool want = _ui.MenuMode == MenuView.Mode.Main;
            if (want && !_active) Begin();
            else if (!want && _active) End();
            if (!_active) return;

            Pose(_focus, _heightAt, Time.unscaledTime - _startTime, out var pos, out var look);
            _cam.transform.position = pos;
            _cam.transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up);
            _cam.fieldOfView = 52f;
        }

        void Begin()
        {
            _active = true;
            _startTime = Time.unscaledTime;
            _unscaled.Clear();
            foreach (var ps in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                var m = ps.main;
                if (m.useUnscaledTime) continue;
                m.useUnscaledTime = true;
                _unscaled.Add(ps);
            }
        }

        void End()
        {
            _active = false;
            foreach (var ps in _unscaled) if (ps != null) { var m = ps.main; m.useUnscaledTime = false; }
            _unscaled.Clear();
        }
    }
}
