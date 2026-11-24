using System;
using System.Collections.Generic;
using UnityEngine;
using HuesoOxido.Collection;
using HuesoOxido.Detection;
using HuesoOxido.Digging;
using HuesoOxido.Economy;
using HuesoOxido.Interaction;
using HuesoOxido.Inventory;
using HuesoOxido.SaveSystem;
using HuesoOxido.UI;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Reglas de la partida que cruzan sistemas: hallazgos, desmayo, dormir, combustible, mejoras, guardado.
    /// Conecta eventos de los sistemas entre si; ningun sistema conoce a otro.
    /// </summary>
    public sealed class GameDirector : MonoBehaviour
    {
        GameContext _c;
        GameServices _s;
        readonly List<WorldPickup> _pickups = new List<WorldPickup>();
        readonly Dictionary<string, bool> _initialHints = new Dictionary<string, bool>();
        readonly System.Random _rng = new System.Random(Environment.TickCount);
        Material _dirt;
        Vector3 _faintSpot;
        bool _faintMarkerActive;
        bool _wasInCamp = true;
        float _lastAutosave;

        public IInventoryActions InventoryActions { get; private set; }
        public ISessionActions Session { get; private set; }
        public IMenuActions Menu { get; private set; }

        // ---------- Montaje ----------
        public void Prepare(GameContext ctx)
        {
            _c = ctx; _s = ctx.Services;
            InventoryActions = new InventoryActionsImpl(this);
            Session = new SessionActionsImpl(this);
            Menu = new MenuActionsImpl(this);
        }

        public void Begin()
        {
            _dirt = MaterialUtil.Lit(new Color(0.3f, 0.21f, 0.12f), 0.05f);
            foreach (var site in _c.World.Sites.Sites) _initialHints[site.SiteId] = site.HasHint;

            _c.Dig.FindUnearthed += OnFound;
            _c.Dig.FindUnearthed += (_, __) => _c.PlayerState.Machine.NotifyFind();
            AddRelicMarkers();
            _s.Quests.Completed += OnQuestCompleted;
            if (!_s.Quests.HasQuest) _s.Quests.Start(QuestManager.RelicHunt());
            _c.Dig.Stroke += pos => DirtBit.Burst(pos + Vector3.up * 0.1f, 5, _dirt);
            _c.Services.Energy.Exhausted += OnExhausted;
            _c.EnergyComp.HardLanding += loss => _s.Notifier.Post("Caída fuerte: perdiste " + Mathf.RoundToInt(loss) + " de energía");
            _s.Upgrades.Changed += ApplyUpgrades;
            _c.Vehicle.DriverChanged += driving =>
            {
                _c.Sensor.enabled = !driving;
                _c.Detector.DetectorEnabled = !driving;
            };

            WireInteractables();
            _c.Map.AddMarker(new MapMarker
            {
                Label = "Objetos caídos", Color = new Color(1f, 0.4f, 0.3f), Position = () => _faintSpot,
                Visible = () => _faintMarkerActive && _pickups.Exists(p => p != null),
            });

            _c.Save.Register(new SaveParticipant(this));
            _c.Save.Message += _s.Notifier.Post;
            ApplyUpgrades();

            _c.Ui.OpenMainMenu();
            _lastAutosave = Time.time;
        }

        void WireInteractables()
        {
            var w = _c.World;
            Add(w.ShopPoint, () => "Mostrador: vender, restaurar, mejoras y permisos", _ => _c.Ui.OpenShop());
            Add(w.MuseumPoint, () => "Museo del campamento", _ => _c.Ui.OpenMuseum());
            Add(w.TentPoint, () => "Dormir hasta la mañana (guarda la partida)", _ => Sleep());
            Add(w.FuelPoint, () => "Cargar combustible ($" + FuelPurchase.CostToFill(_s.Fuel) + " para llenar)", _ => BuyFuel());

            var display = new GameObject("Display").transform;
            display.SetParent(w.MuseumPoint.parent, false);
            display.localPosition = new Vector3(0f, 0f, 0.8f);
            var items = new List<IItemInfo>();
            foreach (var d in _s.Catalog.Items) items.Add(d);
            display.gameObject.AddComponent<MuseumDisplay>().Initialize(_s.Collection, items);

            var trunk = new GameObject("TrunkTrigger");
            trunk.transform.SetParent(_c.Vehicle.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.0f, -2.3f);
            var box = trunk.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.2f, 1.6f, 1.2f);
            Add(trunk.transform, () => "Baúl: guardar la mochila (" + _s.Backpack.Items.Count + " objetos)", _ => DepositAll());
        }

        void Add(Transform t, Func<string> prompt, Action<GameObject> action)
            => t.gameObject.AddComponent<ActionInteractable>().ConfigureDynamic(prompt, action);

        // ---------- Reglas ----------
        void OnFound(BuriedSite site, ItemInstance item)
        {
            var res = _s.Collection.RegisterFind(item.DefinitionId);
            _s.Catalog.TryGet(item.DefinitionId, out var info);
            string name = info != null ? info.DisplayName : item.DefinitionId;
            string color = info != null ? UiFactory.Hex(RarityStyle.ColorOf(info.Rarity)) : "ffffff";
            string rarity = info != null ? RarityStyle.NameOf(info.Rarity) : "";
            string tail = res.IsDuplicate ? "duplicado" : "¡nueva en tu colección!";
            bool isRelic = item.DefinitionId != null && item.DefinitionId.StartsWith(QuestManager.RelicPrefix, StringComparison.Ordinal);
            FindReveal.Spawn(site.transform.position, info != null ? info.Rarity : Rarity.Common, isRelic);
            string msg = "¡Hallazgo! <color=#" + color + ">" + name + "</color> (" + rarity + ") · " + tail;

            if (_s.Quests.ReportFind(item.DefinitionId) && !_s.Quests.IsCompleted)
                _s.Notifier.Post("Misión: " + _s.Quests.ProgressText);

            if (_s.Backpack.TryAdd(item)) _s.Notifier.Post(msg);
            else
            {
                SpawnPickup(item, GroundNear(_c.Player.transform.position + _c.Player.transform.forward * 1.1f));
                _s.Notifier.Post(msg);
                _s.Notifier.Post("Mochila llena: la pieza quedó en el suelo.");
            }
        }

        /// <summary>Marca en el mapa cada reliquia ya desenterrada (las que faltan no se revelan).</summary>
        void AddRelicMarkers()
        {
            foreach (var site in _c.World.Sites.Sites)
            {
                if (site.ItemId == null || !site.ItemId.StartsWith(QuestManager.RelicPrefix, StringComparison.Ordinal)) continue;
                var captured = site;
                _c.Map.AddMarker(new MapMarker
                {
                    Label = "Reliquia hallada", Color = new Color(1f, 0.82f, 0.25f),
                    Position = () => captured.transform.position,
                    Visible = () => captured.Excavated,
                });
            }
        }

        void OnQuestCompleted(QuestDefinition quest)
        {
            _s.Wallet.Add(quest.RewardMoney);
            _s.Notifier.Post("<color=#" + UiFactory.Hex(UiFactory.Good) + ">¡Misión cumplida!</color> " + quest.Title + " · recompensa $" + quest.RewardMoney);
        }

        void OnExhausted()
        {
            if (_c.Vehicle.HasDriver) _c.Vehicle.ExitVehicle();
            _c.Ui.CloseAll();
            _c.Ui.Blink();
            _faintSpot = _c.Player.transform.position;
            var lost = _s.Backpack.RemoveRandomFraction(0.4f, _rng);
            foreach (var it in lost)
            {
                var offset = new Vector3((float)(_rng.NextDouble() - 0.5) * 4f, 0f, (float)(_rng.NextDouble() - 0.5) * 4f);
                SpawnPickup(it, GroundNear(_faintSpot + offset));
            }
            _faintMarkerActive = lost.Count > 0;
            _c.Motor.Teleport(_c.World.PlayerSpawn, 200f);
            _s.Clock.SkipTo(7f);
            _s.Energy.Set(_s.Energy.Max * 0.5f);
            _s.Notifier.Post("Te desmayaste de agotamiento y despertaste en el campamento.");
            if (lost.Count > 0) _s.Notifier.Post("Se te cayeron " + lost.Count + " objetos: quedaron donde caíste (marcados en el mapa).");
        }

        void Sleep()
        {
            _c.Ui.Blink();
            _s.Clock.SkipTo(6.5f);
            _s.Energy.RestoreFull();
            _s.Weather.Set(WeatherKind.Cloudy);
            _c.Save.Save();
            _lastAutosave = Time.time;
            _s.Notifier.Post("Dormiste hasta la mañana. Energía restaurada.");
        }

        void BuyFuel()
        {
            var (liters, cost) = FuelPurchase.Buy(_s.Fuel, _s.Wallet);
            if (liters > 0f) _s.Notifier.Post("Cargaste " + Mathf.RoundToInt(liters) + " L de combustible por $" + cost);
            else _s.Notifier.Post(_s.Fuel.Fraction > 0.99f ? "El tanque ya está lleno." : "No te alcanza el dinero para combustible.");
        }

        void DepositAll()
        {
            int moved = _s.Backpack.TransferAllTo(_s.Trunk);
            if (moved > 0) _s.Notifier.Post("Guardaste " + moved + " objeto(s) en el baúl.");
            else _s.Notifier.Post(_s.Backpack.Items.Count == 0 ? "La mochila está vacía." : "No hay lugar en el baúl.");
        }

        void ApplyUpgrades()
        {
            var u = _s.Upgrades;
            _s.Backpack.SetCapacity(GameServices.BaseBackpackKg + u.Bonus(UpgradeKind.BackpackCapacity));
            _s.Trunk.SetCapacity(GameServices.BaseTrunkKg + u.Bonus(UpgradeKind.TrunkCapacity));
            _c.Detector.SetRangeBonus(u.Bonus(UpgradeKind.DetectorRange));
            _s.Energy.SetMax(GameServices.BaseEnergy + u.Bonus(UpgradeKind.EnergyMax));
            _s.Fuel.SetCapacity(GameServices.BaseFuel + u.Bonus(UpgradeKind.FuelTank));
        }

        // ---------- Suelo y pickups ----------
        Vector3 GroundNear(Vector3 p)
        {
            if (Physics.Raycast(p + Vector3.up * 4f, Vector3.down, out var hit, 12f, ~0, QueryTriggerInteraction.Ignore)) return hit.point;
            return p;
        }

        void SpawnPickup(ItemInstance item, Vector3 position)
        {
            var p = WorldPickup.Spawn(item, position + Vector3.up * 0.25f, _s.Catalog, _s.Backpack, null, _s.Notifier.Post);
            _pickups.Add(p);
        }

        void ClearPickups()
        {
            foreach (var p in _pickups) if (p != null) Destroy(p.gameObject);
            _pickups.Clear();
        }

        // ---------- Actualizacion ----------
        void Update()
        {
            if (_c == null) return;
            if (_c.Ui.MenuMode == MenuView.Mode.Main)
            {
                // Camara lenta alrededor del campamento detras del menu principal.
                _c.Look.SetRotation(_c.Look.Yaw + 7f * Time.unscaledDeltaTime, 14f);
                _c.Rig.SetMode(ViewMode.ThirdPerson);
                return;
            }
            if (_c.Input.SavePressed && !_c.Input.UiMode) _c.Save.Save();

            bool inCamp = (_c.Player.transform.position - _c.World.CampPosition).sqrMagnitude < 14f * 14f && !_c.Vehicle.HasDriver;
            if (inCamp && !_wasInCamp && Time.time - _lastAutosave > 60f)
            {
                _lastAutosave = Time.time;
                _c.Save.Save();
            }
            _wasInCamp = inCamp;
            if (_faintMarkerActive && !_pickups.Exists(p => p != null)) _faintMarkerActive = false;
        }

        // ---------- Sesion ----------
        void NewGame()
        {
            if (_c.Vehicle.HasDriver) _c.Vehicle.ExitVehicle();
            _c.Ui.CloseAll();
            _c.Save.DeleteSave();
            _s.Wallet.Set(GameServices.StartMoney);
            _s.Backpack.ReplaceAll(null);
            _s.Trunk.ReplaceAll(null);
            _s.Upgrades.Restore(null);
            _s.Permits.Restore(null);
            _s.Collection.Restore(null);
            ApplyUpgrades();
            _s.Energy.RestoreFull();
            _s.Clock.Set(17f, 1);
            _s.Weather.Set(WeatherKind.Clear);
            _s.Fuel.SetLevel(_s.Fuel.CapacityLiters);
            foreach (var site in _c.World.Sites.Sites) site.RestoreState(false, _initialHints[site.SiteId]);
            _s.Quests.Start(QuestManager.RelicHunt());
            ClearPickups();
            _faintMarkerActive = false;
            _c.Motor.Teleport(_c.World.PlayerSpawn, 200f);
            _c.Look.SetRotation(200f, 10f);
            _c.Vehicle.Teleport(_c.World.VehicleSpawn, 140f);
            _s.Notifier.Post("Nueva partida. Comprá el permiso de excavación en el mostrador.");
        }

        // ---------- Guardado ----------
        sealed class SaveParticipant : ISaveParticipant
        {
            readonly GameDirector _d;
            public SaveParticipant(GameDirector d) { _d = d; }

            static ItemDto ToDto(ItemInstance i) => new ItemDto { Uid = i.Uid, DefinitionId = i.DefinitionId, Condition = i.Condition };
            static ItemInstance FromDto(ItemDto d) => new ItemInstance(d.Uid, d.DefinitionId, d.Condition);

            public void Capture(SaveData data)
            {
                var s = _d._s; var c = _d._c;
                data.Money = s.Wallet.Money;
                foreach (var i in s.Backpack.Items) data.Backpack.Add(ToDto(i));
                foreach (var i in s.Trunk.Items) data.Trunk.Add(ToDto(i));
                foreach (var kv in s.Upgrades.Snapshot()) data.Upgrades.Add(new UpgradeLevelDto { Id = kv.Key, Level = kv.Value });
                data.Permits.AddRange(s.Permits.Snapshot());
                foreach (var e in s.Collection.Entries) data.Collection.Add(new CollectionEntryDto { Id = e.Id, TimesFound = e.TimesFound, Displayed = e.Displayed });
                data.Energy = s.Energy.Current;
                data.Hour = s.Clock.Hour; data.Day = s.Clock.Day;
                data.Weather = (int)s.Weather.Current;
                data.Fuel = s.Fuel.Level;
                data.ExcavatedSites.AddRange(c.World.Sites.ExcavatedIds());
                if (s.Quests.HasQuest) { data.QuestId = s.Quests.Current.Id; data.QuestProgress = s.Quests.Progress; data.QuestCompleted = s.Quests.IsCompleted; }

                data.HasPositions = true;
                var vp = c.Vehicle.transform;
                data.Vehicle = new Vec3Dto { X = vp.position.x, Y = vp.position.y, Z = vp.position.z, Yaw = vp.eulerAngles.y };
                if (c.Vehicle.HasDriver)
                {
                    Vector3 side = vp.TransformPoint(-2.2f, 0.5f, 0f);
                    data.Player = new Vec3Dto { X = side.x, Y = side.y, Z = side.z, Yaw = vp.eulerAngles.y };
                }
                else
                {
                    var pp = c.Player.transform;
                    data.Player = new Vec3Dto { X = pp.position.x, Y = pp.position.y, Z = pp.position.z, Yaw = pp.eulerAngles.y };
                }
                foreach (var p in _d._pickups)
                {
                    if (p == null) continue;
                    var pos = p.transform.position;
                    data.GroundItems.Add(new GroundItemDto { Item = ToDto(p.Item), X = pos.x, Y = pos.y, Z = pos.z });
                }
            }

            public void Restore(SaveData data)
            {
                var s = _d._s; var c = _d._c;
                if (c.Vehicle.HasDriver) c.Vehicle.ExitVehicle();
                c.Ui.CloseAll();

                var levels = new Dictionary<string, int>();
                foreach (var u in data.Upgrades) levels[u.Id] = u.Level;
                s.Upgrades.Restore(levels);
                _d.ApplyUpgrades();

                s.Wallet.Set(data.Money);
                var pack = new List<ItemInstance>(); foreach (var i in data.Backpack) pack.Add(FromDto(i));
                var trunk = new List<ItemInstance>(); foreach (var i in data.Trunk) trunk.Add(FromDto(i));
                s.Backpack.ReplaceAll(pack);
                s.Trunk.ReplaceAll(trunk);
                s.Permits.Restore(data.Permits);
                var col = new List<CollectionEntry>();
                foreach (var e in data.Collection) col.Add(new CollectionEntry { Id = e.Id, TimesFound = e.TimesFound, Displayed = e.Displayed });
                s.Collection.Restore(col);

                if (data.Energy >= 0f) s.Energy.Set(data.Energy); else s.Energy.RestoreFull();
                s.Clock.Set(data.Hour, data.Day);
                s.Weather.Set((WeatherKind)Mathf.Clamp(data.Weather, 0, 3));
                if (data.Fuel >= 0f) s.Fuel.SetLevel(data.Fuel);

                if (!string.IsNullOrEmpty(data.QuestId)) s.Quests.Restore(data.QuestId, data.QuestProgress, data.QuestCompleted);
                else s.Quests.Start(QuestManager.RelicHunt());

                var done = new HashSet<string>(data.ExcavatedSites);
                foreach (var site in c.World.Sites.Sites) site.RestoreState(done.Contains(site.SiteId), _d._initialHints[site.SiteId]);

                _d.ClearPickups();
                foreach (var g in data.GroundItems)
                    _d.SpawnPickup(FromDto(g.Item), new Vector3(g.X, g.Y - 0.25f, g.Z));
                _d._faintMarkerActive = data.GroundItems.Count > 0;
                if (data.GroundItems.Count > 0) _d._faintSpot = new Vector3(data.GroundItems[0].X, data.GroundItems[0].Y, data.GroundItems[0].Z);

                if (data.HasPositions)
                {
                    c.Motor.Teleport(new Vector3(data.Player.X, data.Player.Y + 0.3f, data.Player.Z), data.Player.Yaw);
                    c.Look.SetRotation(data.Player.Yaw, 10f);
                    c.Vehicle.Teleport(new Vector3(data.Vehicle.X, data.Vehicle.Y + 0.5f, data.Vehicle.Z), data.Vehicle.Yaw);
                }
            }
        }

        sealed class InventoryActionsImpl : IInventoryActions
        {
            readonly GameDirector _d;
            public InventoryActionsImpl(GameDirector d) { _d = d; }

            public InventoryModel Trunk => _d._s.Trunk;
            public bool TrunkReachable =>
                _d._c != null && !_d._c.Vehicle.HasDriver
                && (_d._c.Player.transform.position - _d._c.Vehicle.transform.position).sqrMagnitude < 7f * 7f;

            public void Drop(string uid)
            {
                if (!_d._s.Backpack.Remove(uid, out var item)) return;
                var p = _d._c.Player.transform;
                _d.SpawnPickup(item, _d.GroundNear(p.position + p.forward * 1.3f));
                _d._s.Notifier.Post("Tiraste el objeto al suelo.");
            }

            public void MoveToTrunk(string uid)
            {
                if (!_d._s.Backpack.TransferTo(_d._s.Trunk, uid)) _d._s.Notifier.Post("No entra en el baúl.");
            }

            public void TakeFromTrunk(string uid)
            {
                if (!_d._s.Trunk.TransferTo(_d._s.Backpack, uid)) _d._s.Notifier.Post("Tu mochila no soporta ese peso.");
            }

            public void DepositAll() => _d.DepositAll();
        }

        sealed class MenuActionsImpl : IMenuActions
        {
            readonly GameDirector _d;
            public MenuActionsImpl(GameDirector d) { _d = d; }
            public bool HasSave => _d._c.Save.HasSave;
            public float Sensitivity
            {
                get => _d._c.Look.Sensitivity;
                set { _d._c.Look.Sensitivity = value; _d._c.Look.SaveSensitivity(); }
            }
            public void StartNewGame() { _d.NewGame(); _d._c.Ui.CloseAll(); _d._lastAutosave = Time.unscaledTime; }
            public void ContinueGame()
            {
                if (_d._c.Save.Load()) { _d._c.Ui.CloseAll(); _d._lastAutosave = Time.unscaledTime; }
            }
            public void ContinueFromPause() => _d._c.Ui.CloseAll();
            public void SaveGame() => _d._c.Save.Save();
            public void LoadGame() { if (_d._c.Save.Load()) _d._c.Ui.CloseAll(); }
            public void ReturnToMainMenu()
            {
                _d._c.Save.Save();
                _d._c.Ui.OpenMainMenu();
            }
            public void QuitGame()
            {
                if (Application.isEditor) _d._s.Notifier.Post("Salir cierra la aplicación compilada. En el Editor, detené el modo Play.");
                else Application.Quit();
            }
        }

        sealed class SessionActionsImpl : ISessionActions
        {
            readonly GameDirector _d;
            public SessionActionsImpl(GameDirector d) { _d = d; }
            public bool HasSave => _d._c.Save.HasSave;
            public void SaveGame() => _d._c.Save.Save();
            public void LoadGame() => _d._c.Save.Load();
            public void NewGame() => _d.NewGame();
        }
    }
}
