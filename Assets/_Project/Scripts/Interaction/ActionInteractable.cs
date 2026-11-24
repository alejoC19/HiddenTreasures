using System;
using UnityEngine;

namespace HuesoOxido.Interaction
{
    /// <summary>Interactuable generico: un texto y una accion. La logica vive en los servicios que la inyectan.</summary>
    public sealed class ActionInteractable : MonoBehaviour, IInteractable
    {
        string _prompt = "";
        Func<string> _promptFunc;
        Func<bool> _enabledFunc;
        Action<GameObject> _action;

        public string Prompt => _promptFunc != null ? _promptFunc() : _prompt;
        public bool CanInteract => _enabledFunc == null || _enabledFunc();

        public ActionInteractable Configure(string prompt, Action<GameObject> action, Func<bool> enabled = null)
        {
            _prompt = prompt; _action = action; _enabledFunc = enabled;
            return this;
        }

        public ActionInteractable ConfigureDynamic(Func<string> prompt, Action<GameObject> action, Func<bool> enabled = null)
        {
            _promptFunc = prompt; _action = action; _enabledFunc = enabled;
            return this;
        }

        public void Interact(GameObject actor) => _action?.Invoke(actor);
    }
}
