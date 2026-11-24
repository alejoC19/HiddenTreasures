using UnityEngine;

namespace HuesoOxido.Interaction
{
    public interface IInteractable
    {
        /// <summary>Texto del indicador, por ejemplo "Entrar al vehiculo". Vacio = no mostrar.</summary>
        string Prompt { get; }
        bool CanInteract { get; }
        void Interact(GameObject actor);
    }
}
