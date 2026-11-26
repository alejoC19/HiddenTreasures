using System.Collections.Generic;
using UnityEngine;
using HuesoOxido.Camera;
using HuesoOxido.Collection;
using HuesoOxido.Detection;
using HuesoOxido.Digging;
using HuesoOxido.Interaction;
using HuesoOxido.Inventory;
using HuesoOxido.Player;
using HuesoOxido.SaveSystem;
using HuesoOxido.Survival;
using HuesoOxido.UI;
using HuesoOxido.Vehicle;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Raiz de composicion. Al dar Play en cualquier escena crea el mundo, el jugador, los sistemas y la UI.
    /// Es el unico lugar que conoce todas las piezas; el resto de los sistemas solo recibe dependencias explicitas.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] int worldSeed = 1337;

        /// <summary>Contexto ya construido (null hasta que termina Awake). Lo usan los tests de PlayMode.</summary>
        public GameContext Context { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FindFirstObjectByType<GameBootstrap>() != null) return;
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            QualitySettings.shadowDistance = 110f;
            var ctx = new GameContext { Services = new GameServices(worldSeed) };
            var s = ctx.Services;

            // Input y guardado
            var systems = new GameObject("Systems");
            ctx.Input = systems.AddComponent<GameInput>();
            ctx.Save = systems.AddComponent<SaveManager>();
            systems.AddComponent<InputDiagnostics>();

            // Mundo
            var worldRoot = new GameObject("World").transform;
            var allItems = new List<IItemInfo>();
            foreach (var d in s.Catalog.Items) allItems.Add(d);
            ctx.World = new WorldBuilder(worldSeed).Build(allItems, worldRoot, s.Clock);

            // Jugador
            ctx.Player = new GameObject("Player");
            ctx.Motor = ctx.Player.AddComponent<PlayerMotor>();
            ctx.Character = ctx.Player.AddComponent<PlaceholderCharacter>();
            ctx.Character.Build();
            var animator = ctx.Player.AddComponent<PlaceholderCharacterAnimator>();
            ctx.Motor.Teleport(ctx.World.PlayerSpawn, 200f);

            // Camara
            ctx.Cam = PrepareCamera();
            PostProcessSetup.Apply(ctx.Cam);
            ctx.Look = ctx.Cam.gameObject.AddComponent<LookController>();
            ctx.Rig = ctx.Cam.gameObject.AddComponent<CameraRig>();
            ctx.Look.Initialize(ctx.Input);
            ctx.Look.SetRotation(200f, 10f);
            ctx.Rig.Initialize(ctx.Cam, ctx.Input, ctx.Look, ctx.Player.transform, () => ctx.Motor.EyeHeight, ctx.Character);

            // Supervivencia
            var load = new InventoryLoadSource(s.Backpack);
            ctx.EnergyComp = systems.AddComponent<EnergyComponent>();
            ctx.Motor.Initialize(ctx.Input, ctx.Look, ctx.EnergyComp, load);
            var clockComp = systems.AddComponent<WorldClockComponent>();
            clockComp.Initialize(s.Clock, s.Weather, () => ctx.Input.UiMode);
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;   // texturas del suelo nitidas de lejos
            systems.AddComponent<AmbientMotes>().Initialize(ctx.Cam.transform, s.Clock, s.Weather);
            var rain = new GameObject("Rain").AddComponent<RainEffect>();
            rain.Initialize(ctx.Cam.transform, s.Weather);
            systems.AddComponent<AtmosphereFx>().Initialize(ctx.Cam.transform, ctx.World.Sun, s.Clock, s.Weather, ctx.World.HeightAt);
            systems.AddComponent<DayNightCycle>().Initialize(ctx.World.Sun, ctx.World.Moon, s.Clock, s.Weather, ctx.World.Skybox, ctx.Cam);

            // Vehiculo
            var vehicleGo = new GameObject("Vehicle");
            vehicleGo.transform.position = ctx.World.VehicleSpawn;
            vehicleGo.transform.rotation = Quaternion.Euler(0f, 140f, 0f);
            ctx.Vehicle = vehicleGo.AddComponent<VehicleController>();
            ctx.Vehicle.Initialize(ctx.Input, s.Fuel, ctx.Rig);

            // Deteccion y excavacion
            ctx.Detector = systems.AddComponent<MetalDetector>();
            ctx.Detector.Initialize(ctx.World.Sites, ctx.Player.transform);
            systems.AddComponent<DetectorAudio>().Initialize(ctx.Detector, s.Catalog);
            ctx.Dig = systems.AddComponent<DigController>();
            ctx.Dig.Initialize(ctx.Input, ctx.Detector, s.Permits, ctx.EnergyComp,
                () => !ctx.Vehicle.HasDriver && ctx.Motor.IsGrounded && !ctx.Motor.IsSprinting,
                () => 1f + s.Upgrades.Bonus(Economy.UpgradeKind.DigSpeed), worldSeed + 99);
            systems.AddComponent<FootprintSystem>().Initialize(ctx.Motor, ctx.Player.transform, ctx.World);
            ctx.PlayerState = ctx.Player.AddComponent<PlayerStateDriver>();
            systems.AddComponent<GameAudio>().Initialize(ctx.Player.transform, ctx.Motor, ctx.World, s.Clock, s.Weather, ctx.Dig, ctx.PlayerState.Machine, s.Trade);
            ctx.PlayerState.Initialize(ctx.Motor, ctx.Dig, ctx.Character);
            animator.Initialize(ctx.Character, ctx.Motor, ctx.Dig);
            animator.Machine = ctx.PlayerState.Machine;
            systems.AddComponent<DetectorVisual>().Initialize(ctx.Rig, ctx.Cam.transform, ctx.Player.transform, ctx.Detector, ctx.PlayerState, ctx.Character.IsModel ? ctx.Character.RightHand : null);

            // Interaccion
            ctx.Sensor = systems.AddComponent<InteractionSensor>();
            ctx.Sensor.Initialize(ctx.Input, ctx.Cam.transform, ctx.Player.transform);

            var campPos = ctx.World.CampPosition;
            ctx.EnergyComp.Initialize(s.Energy, ctx.Motor, load, s.Weather, s.Clock,
                () => (ctx.Player.transform.position - campPos).sqrMagnitude < 14f * 14f, () => !ctx.Vehicle.HasDriver);

            // UI
            var uiGo = new GameObject("UI");
            ctx.Ui = uiGo.AddComponent<UiRoot>();
            ctx.Ui.Initialize(ctx.Input);
            var director = uiGo.AddComponent<GameDirector>();
            director.Prepare(ctx);
            director.SetWorldSeed(worldSeed);
            BuildUi(ctx, uiGo, director, allItems);
            var reflections = systems.AddComponent<ReflectionBaker>();
            reflections.Initialize(ctx.Player.transform, s.Clock, s.Weather);
            reflections.BakeAll(ctx.World.CampPosition + Vector3.up * 2f);
            ctx.Quality = systems.AddComponent<GraphicsQuality>();
            ctx.Quality.Initialize(ctx.World.Vegetation, () => !ctx.Input.UiMode, s.Notifier.Post);
            var cinematic = systems.AddComponent<MenuCinematic>();
            cinematic.Initialize(ctx.Cam, ctx.Ui, ctx.World.CampPosition + new Vector3(0f, 0f, -1.5f), ctx.World.HeightAt);
            systems.AddComponent<DebugTools>().Initialize(ctx);
            systems.AddComponent<PerfRecorder>().Initialize(ctx);
            director.Begin();
            Context = ctx;
        }

        UnityEngine.Camera PrepareCamera()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<UnityEngine.Camera>();
            }
            cam.transform.SetParent(null);
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            cam.farClipPlane = 700f;
            cam.allowHDR = true;
            cam.clearFlags = CameraClearFlags.Skybox;
            return cam;
        }

        static IEnumerable<JournalSite> JournalSites(GameContext ctx)
        {
            foreach (var site in ctx.World.Sites.Sites)
                yield return new JournalSite { SiteId = site.SiteId, ItemId = site.ItemId, Excavated = site.Excavated };
        }

        void BuildUi(GameContext ctx, GameObject uiGo, GameDirector director, List<IItemInfo> allItems)
        {
            var s = ctx.Services;
            var canvas = ctx.Ui.CanvasTransform;

            var hud = uiGo.AddComponent<HudView>();
            hud.Initialize(canvas, new HudContext
            {
                Energy = s.Energy, Backpack = s.Backpack, Wallet = s.Wallet, Clock = s.Clock, Weather = s.Weather,
                Detector = ctx.Detector, Dig = ctx.Dig, Sensor = ctx.Sensor, Vehicle = ctx.Vehicle, Notifier = s.Notifier, Input = ctx.Input, Quests = s.Quests, PlayerFsm = ctx.PlayerState.Machine, Hidden = () => ctx.Ui.MenuMode == MenuView.Mode.Main,
            });

            ctx.FindCard = uiGo.AddComponent<FindCardView>();
            ctx.FindCard.Initialize(canvas);

            var inventory = uiGo.AddComponent<InventoryView>();
            inventory.Initialize(canvas, s.Backpack, director.InventoryActions, s.Catalog, s.Collection, s.Trade);

            var shop = uiGo.AddComponent<ShopView>();
            shop.Initialize(canvas, new ShopView.Deps
            {
                Trade = s.Trade, Upgrades = s.Upgrades, Permits = s.Permits, Collection = s.Collection, Wallet = s.Wallet,
                Lookup = s.Catalog, AllItems = allItems, Backpack = s.Backpack, Trunk = s.Trunk, Session = director.Session,
                GetSensitivity = () => ctx.Look.Sensitivity,
                SetSensitivity = v => { ctx.Look.Sensitivity = v; ctx.Look.SaveSensitivity(); },
            });

            ctx.Map = uiGo.AddComponent<MapView>();
            var camp = ctx.World.CampPosition;
            var ruins = ctx.World.RuinsCenter;
            ctx.Map.Initialize(canvas, ctx.World.MapTexture, ctx.World.MapBounds, ctx.Player.transform, new[]
            {
                new MapMarker { Label = "Campamento", Color = new Color(0.4f, 0.7f, 1f), Position = () => camp },
                new MapMarker { Label = "Ruinas", Color = new Color(0.8f, 0.8f, 0.75f), Position = () => ruins },
                new MapMarker { Label = "Vehículo", Color = new Color(0.5f, 0.9f, 0.4f), Position = () => ctx.Vehicle.transform.position },
            });
            var menu = uiGo.AddComponent<MenuView>();
            menu.Initialize(canvas, director.Menu);
            var journal = uiGo.AddComponent<JournalView>();
            journal.Initialize(canvas, () => JournalSites(ctx), ctx.Services.Catalog, ctx.Services.Quests);
            ctx.Ui.Bind(inventory, shop, ctx.Map, menu, journal);
        }
    }
}
