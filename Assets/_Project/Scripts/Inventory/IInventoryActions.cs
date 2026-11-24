namespace HuesoOxido.Inventory
{
    /// <summary>Acciones sobre el inventario que la UI puede pedir (la logica vive en quien lo implementa).</summary>
    public interface IInventoryActions
    {
        /// <summary>True si el jugador esta lo bastante cerca del baul.</summary>
        bool TrunkReachable { get; }
        InventoryModel Trunk { get; }
        void Drop(string uid);
        void MoveToTrunk(string uid);
        void TakeFromTrunk(string uid);
        void DepositAll();
    }
}
