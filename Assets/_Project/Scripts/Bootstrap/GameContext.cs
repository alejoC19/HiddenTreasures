using UnityEngine;
using HuesoOxido.Camera;
using HuesoOxido.Collection;
using HuesoOxido.Detection;
using HuesoOxido.Digging;
using HuesoOxido.Economy;
using HuesoOxido.Interaction;
using HuesoOxido.Inventory;
using HuesoOxido.Player;
using HuesoOxido.SaveSystem;
using HuesoOxido.Survival;
using HuesoOxido.UI;
using HuesoOxido.Vehicle;

namespace HuesoOxido.Core
{
    /// <summary>Modelos y servicios de la partida (logica pura, sin MonoBehaviours).</summary>
    public sealed class GameServices
    {
        public const float BaseBackpackKg = 8f, BaseTrunkKg = 40f, BaseEnergy = 100f, BaseFuel = 40f;
        public const int StartMoney = 120;

        public readonly Notifier Notifier = new Notifier();
        public readonly ItemCatalog Catalog = DefaultItems.CreateCatalog();
        public readonly Wallet Wallet = new Wallet(StartMoney);
        public readonly UpgradeService Upgrades = new UpgradeService(DefaultUpgrades.Create());
        public readonly PermitService Permits = new PermitService(new[] { PermitService.ForestRuins });
        public readonly CollectionLog Collection = new CollectionLog();
        public readonly QuestManager Quests = new QuestManager();
        public readonly InventoryModel Backpack;
        public readonly InventoryModel Trunk;
        public readonly EnergyModel Energy = new EnergyModel(BaseEnergy);
        public readonly GameClock Clock = new GameClock(17f, 1);
        public readonly WeatherModel Weather;
        public readonly FuelTank Fuel = new FuelTank(BaseFuel);
        public readonly TradingService Trade;

        /// <summary>Los modelos que forman la partida, para el guardado.</summary>
        public SaveBundle SaveState => new SaveBundle
        {
            Wallet = Wallet, Backpack = Backpack, Trunk = Trunk, Upgrades = Upgrades, Permits = Permits, Collection = Collection,
            Energy = Energy, Clock = Clock, Weather = Weather, Fuel = Fuel, Quests = Quests,
        };

        public GameServices(int seed)
        {
            Backpack = new InventoryModel("Mochila", BaseBackpackKg, Catalog);
            Trunk = new InventoryModel("Baúl", BaseTrunkKg, Catalog);
            Weather = new WeatherModel(seed);
            Trade = new TradingService(Catalog, Wallet, Collection);
        }
    }

    /// <summary>Referencias a los objetos de escena ya construidos.</summary>
    public sealed class GameContext
    {
        public GameServices Services;
        public WorldData World;
        public GameInput Input;
        public SaveManager Save;
        public GameObject Player;
        public PlayerMotor Motor;
        public PlaceholderCharacter Character;
        public PlayerStateDriver PlayerState;
        public LookController Look;
        public CameraRig Rig;
        public UnityEngine.Camera Cam;
        public MetalDetector Detector;
        public DigController Dig;
        public InteractionSensor Sensor;
        public VehicleController Vehicle;
        public EnergyComponent EnergyComp;
        public UiRoot Ui;
        public MapView Map;
    }
}
