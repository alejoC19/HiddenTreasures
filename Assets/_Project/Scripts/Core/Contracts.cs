using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>Orientacion de la camara (solo yaw) para mover al personaje relativo a ella.</summary>
    public interface IViewDirection
    {
        float Yaw { get; }
        float Pitch { get; }
    }

    public interface IViewState
    {
        ViewMode Mode { get; }
        /// <summary>1 = primera persona completa, 0 = tercera persona completa.</summary>
        float FirstPersonWeight { get; }
    }

    public interface ISprintPermission { bool CanSprint { get; } }

    /// <summary>0 = mochila vacia, 1 = al limite de peso. Frena al personaje.</summary>
    public interface ILoadSource { float LoadFraction { get; } }

    public interface IEnergyConsumer { bool TryConsume(float amount); }

    public interface IDigActivity
    {
        bool IsDigging { get; }
        float Progress { get; }
    }

    public interface IDigPermission { bool CanDig(string zoneId); }

    public interface IInputBlocker { bool GameplayBlocked { get; } }
}
