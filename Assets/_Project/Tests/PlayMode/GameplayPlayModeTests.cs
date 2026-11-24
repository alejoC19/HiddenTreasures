using System.Collections;
using System.Linq;
using HuesoOxido.Core;
using HuesoOxido.Detection;
using HuesoOxido.Survival;
using HuesoOxido.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace HuesoOxido.Tests.PlayMode
{
    /// <summary>
    /// Ejecucion real en el Editor (Play Mode): arranque del bootstrap, integracion de sistemas y loop central.
    /// Los tests dependen entre si (comparten la misma partida) y por eso corren en orden.
    /// Limitacion: con -nographics no se valida el renderizado, solo la logica y la fisica.
    /// </summary>
    [TestFixture]
    public class GameplayPlayModeTests
    {
        static GameBootstrap _boot;
        static GameContext C => _boot.Context;
        static GameDirector _director;
        static Keyboard _kb;
        static Mouse _mouse;

        static IEnumerator WaitFrames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest, Order(1)]
        public IEnumerator T01_Bootstrap_BuildsAllSystems()
        {
            for (int i = 0; i < 600 && _boot == null; i++)
            {
                _boot = Object.FindFirstObjectByType<GameBootstrap>();
                if (_boot == null && i == 5) _boot = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
                yield return null;
            }
            Assert.NotNull(_boot, "no se creo el GameBootstrap");
            for (int i = 0; i < 600 && _boot.Context == null; i++) yield return null;
            Assert.NotNull(_boot.Context, "el bootstrap no termino de construir el juego");
            yield return WaitFrames(5);

            _director = Object.FindFirstObjectByType<GameDirector>();
            Assert.NotNull(_director);
            Assert.NotNull(C.World.Terrain, "terreno");
            Assert.GreaterOrEqual(C.World.Sites.Sites.Count, 25, "sitios enterrados");
            Assert.NotNull(C.Player); Assert.NotNull(C.Motor); Assert.NotNull(C.Rig); Assert.NotNull(C.Look);
            Assert.NotNull(C.Detector); Assert.NotNull(C.Dig); Assert.NotNull(C.Sensor); Assert.NotNull(C.Vehicle);
            Assert.NotNull(C.Cam); Assert.NotNull(C.Cam.GetComponent<AudioListener>(), "AudioListener");
            Assert.AreEqual(MenuView.Mode.Main, C.Ui.MenuMode, "debe abrir en el menu principal");
            Assert.AreEqual(0f, Time.timeScale, "el menu pausa el juego");
            Assert.NotNull(UnityEngine.EventSystems.EventSystem.current, "EventSystem para la UI");
            Assert.AreEqual(1, Object.FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None).Length, "bootstrap duplicado");
        }

        [UnityTest, Order(2)]
        public IEnumerator T02_NewGame_StartsAndRuns()
        {
            _director.Menu.StartNewGame();
            yield return WaitFrames(3);
            Assert.AreEqual(MenuView.Mode.None, C.Ui.MenuMode);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(GameServices.StartMoney, C.Services.Wallet.Money);
            Assert.IsFalse(C.Input.UiMode);

            float t0 = Time.time;
            yield return WaitFrames(90); // 90 frames de juego real sin errores en consola
            Assert.Greater(Time.time, t0);
            Assert.IsTrue(C.Motor.IsGrounded || C.Player.transform.position.y > -5f, "el jugador cayo del mundo");
            Assert.Greater(C.Player.transform.position.y, C.World.CampPosition.y - 3f, "el jugador atraveso el terreno");
        }

        [UnityTest, Order(3)]
        public IEnumerator T03_RealInput_MovesPlayer_AndTogglesView()
        {
            // Dispositivos virtuales propios: el test no depende del teclado/mouse fisico ni de que la vista Game tenga el foco.
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _kb = InputSystem.AddDevice<Keyboard>("HT_TestKeyboard");
            _mouse = InputSystem.AddDevice<Mouse>("HT_TestMouse");
            Assert.NotNull(_kb, "no hay teclado virtual");
            Cursor.lockState = CursorLockMode.Locked;

            var start = C.Player.transform.position;
            InputSystem.QueueStateEvent(_kb, new KeyboardState(Key.W));
            yield return new WaitForSeconds(1.5f);
            InputSystem.QueueStateEvent(_kb, new KeyboardState());
            yield return WaitFrames(3);
            float moved = Vector3.Distance(Flat(start), Flat(C.Player.transform.position));
            Assert.Greater(moved, 1.5f, "W no movio al personaje (input del Input System)");

            var mode = C.Rig.Mode;
            InputSystem.QueueStateEvent(_kb, new KeyboardState(Key.V));
            yield return null;
            InputSystem.QueueStateEvent(_kb, new KeyboardState());
            yield return WaitFrames(30);
            Assert.AreNotEqual(mode, C.Rig.Mode, "V no cambio la camara");
            InputSystem.QueueStateEvent(_kb, new KeyboardState(Key.V)); yield return null;
            InputSystem.QueueStateEvent(_kb, new KeyboardState()); yield return WaitFrames(30);
            Assert.AreEqual(mode, C.Rig.Mode);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        [UnityTest, Order(4)]
        public IEnumerator T04_DetectorAndDig_RealInput_FindsItem()
        {
            var s = C.Services;
            Assert.IsTrue(s.Permits.TryBuy("permit_forest_ruins", s.Wallet), "no se pudo comprar el permiso");
            var site = C.World.Sites.Sites.OrderBy(x => Vector3.Distance(x.transform.position, C.World.CampPosition)).First(x => !x.Excavated);
            // Se ubica al jugador de modo que la cabeza del detector quede sobre el sitio.
            var p = C.Player.transform;
            C.Motor.Teleport(site.transform.position - new Vector3(0f, 0f, 1.1f) + Vector3.up * 0.3f, 0f);
            yield return WaitFrames(20);
            Assert.AreSame(site, C.Detector.Target, "el detector no apunto al sitio mas cercano");
            Assert.Greater(C.Detector.Signal, 0.3f, "senal debil sobre el sitio");

            int before = s.Backpack.Items.Count + (CountPickups());
            int found = 0;
            C.Dig.FindUnearthed += (_, __) => found++;
            InputSystem.QueueDeltaStateEvent(_mouse.leftButton, 1f);
            float deadline = Time.time + site.DigSeconds * 3f + 3f;
            while (found == 0 && Time.time < deadline) yield return null;
            InputSystem.QueueDeltaStateEvent(_mouse.leftButton, 0f);
            yield return WaitFrames(3);
            Assert.AreEqual(1, found, "la excavacion con clic mantenido no termino");
            Assert.IsTrue(site.Excavated);
            Assert.AreEqual(before + 1, s.Backpack.Items.Count + CountPickups(), "el hallazgo no llego ni a la mochila ni al suelo");
            Assert.AreEqual(1, s.Collection.DiscoveredCount);
        }

        static int CountPickups() => Object.FindObjectsByType<HuesoOxido.Inventory.WorldPickup>(FindObjectsSortMode.None).Length;

        [UnityTest, Order(5)]
        public IEnumerator T05_Vehicle_EnterDriveExit()
        {
            var v = C.Vehicle;
            C.Motor.Teleport(v.transform.position + v.transform.right * -2.5f + Vector3.up * 0.3f, 0f);
            yield return WaitFrames(10);
            float groundY = v.transform.position.y;
            v.Interact(C.Player);
            yield return WaitFrames(3);
            Assert.IsTrue(v.HasDriver); Assert.IsFalse(C.Player.activeSelf);

            var start = v.transform.position;
            InputSystem.QueueStateEvent(_kb, new KeyboardState(Key.W));
            yield return new WaitForSeconds(4f);
            InputSystem.QueueStateEvent(_kb, new KeyboardState());
            float dist = Vector3.Distance(Flat(start), Flat(v.transform.position));
            Assert.Greater(dist, 6f, "el vehiculo no avanzo con W (suspension/traccion)");
            Assert.Greater(v.transform.position.y, C.World.Terrain.SampleHeight(v.transform.position) - 1f, "el vehiculo atraveso el terreno");
            Assert.Less(Mathf.Abs(Vector3.Dot(v.transform.up, Vector3.up) - 1f), 0.4f, "el vehiculo volco al avanzar recto");
            Assert.Less(v.Fuel.Fraction, 1f, "no consumio combustible");

            // Frenar hasta casi detenerse (sin pasar a marcha atras) y bajar.
            InputSystem.QueueStateEvent(_kb, new KeyboardState(Key.S));
            float brakeDeadline = Time.time + 8f;
            while (v.SpeedKmh > 3f && Time.time < brakeDeadline) yield return null;
            InputSystem.QueueStateEvent(_kb, new KeyboardState());
            yield return WaitFrames(5);
            InputSystem.QueueStateEvent(_kb, new KeyboardState(Key.E)); yield return null;
            InputSystem.QueueStateEvent(_kb, new KeyboardState()); yield return WaitFrames(5);
            Assert.IsFalse(v.HasDriver, "E no bajo del vehiculo (velocidad: " + v.SpeedKmh + " km/h)");
            Assert.IsTrue(C.Player.activeSelf);
        }

        [UnityTest, Order(6)]
        public IEnumerator T06_SaveLoad_RoundTrip()
        {
            var s = C.Services;
            s.Wallet.Set(777);
            var item = new ItemInstance("test-uid", "flint_arrowhead", 0.9f);
            Assert.IsTrue(s.Backpack.TryAdd(item));
            var site = C.World.Sites.Sites.First(x => !x.Excavated);
            site.MarkExcavated(false);
            Assert.IsTrue(C.Save.Save(), "no se pudo guardar");
            Assert.IsTrue(C.Save.HasSave);

            s.Wallet.Set(1);
            s.Backpack.ReplaceAll(null);
            site.RestoreState(false, false);
            Assert.IsTrue(C.Save.Load(), "no se pudo cargar");
            yield return WaitFrames(3);
            Assert.AreEqual(777, s.Wallet.Money);
            Assert.IsNotNull(s.Backpack.Find("test-uid"));
            Assert.IsTrue(site.Excavated, "el sitio excavado no se restauro");
            Assert.IsTrue(s.Permits.CanDig("forest_ruins"), "el permiso no se restauro");
        }

        [UnityTest, Order(7)]
        public IEnumerator T07_TradeAndUpgrade_ThroughServices()
        {
            var s = C.Services;
            s.Backpack.ReplaceAll(null);
            s.Backpack.TryAdd(new ItemInstance("sell1", "roman_denarius", 1f));
            int before = s.Wallet.Money;
            Assert.Greater(s.Trade.SellAll(s.Backpack), 0);
            Assert.Greater(s.Wallet.Money, before);
            s.Wallet.Add(2000);
            float cap = s.Backpack.CapacityKg;
            Assert.IsTrue(s.Upgrades.TryPurchase("backpack", s.Wallet));
            yield return null;
            Assert.Greater(s.Backpack.CapacityKg, cap, "la mejora no cambio la capacidad de la mochila (cableado del director)");
        }

        [UnityTest, Order(8)]
        public IEnumerator T08_Exhaustion_FaintsToCamp_AndDropsItems()
        {
            var s = C.Services;
            s.Backpack.ReplaceAll(null);
            for (int i = 0; i < 5; i++) s.Backpack.TryAdd(new ItemInstance("f" + i, "pottery_shard", 1f));
            C.Motor.Teleport(C.World.CampPosition + new Vector3(60f, 5f, 0f), 0f);
            yield return WaitFrames(5);
            s.Energy.Drain(9999f);
            yield return WaitFrames(5);
            Assert.AreEqual(3, s.Backpack.Items.Count, "debia perder el 40% (2 de 5)");
            Assert.AreEqual(2, CountPickups() - 0, "los objetos perdidos deben quedar en el suelo");
            Assert.Less((C.Player.transform.position - C.World.CampPosition).magnitude, 20f, "no desperto en el campamento");
            Assert.Greater(s.Energy.Fraction, 0.3f);
        }

        [UnityTest, Order(9)]
        public IEnumerator T09_UiPanels_OpenAndRender()
        {
            C.Services.Backpack.TryAdd(new ItemInstance("ui1", "ammonite", 0.5f));
            foreach (var open in new System.Action[] { C.Ui.OpenInventory, C.Ui.OpenShop, C.Ui.OpenMap, C.Ui.OpenMuseum, C.Ui.OpenPause })
            {
                open();
                yield return WaitFrames(4);
                Assert.IsTrue(C.Ui.AnyOpen); Assert.IsTrue(C.Input.UiMode); Assert.AreEqual(0f, Time.timeScale);
                C.Ui.CloseAll();
                yield return WaitFrames(2);
                Assert.IsFalse(C.Ui.AnyOpen); Assert.AreEqual(1f, Time.timeScale);
            }
        }

        [UnityTest, Order(10)]
        public IEnumerator T10_DayNightAndWeather_AllStates()
        {
            var clock = C.Services.Clock; var weather = C.Services.Weather;
            foreach (WeatherKind k in System.Enum.GetValues(typeof(WeatherKind)))
                foreach (float h in new[] { 0f, 5.5f, 6.5f, 12f, 17f, 20.5f, 23f })
                {
                    weather.Set(k); clock.Set(h, clock.Day);
                    yield return null;
                    Assert.IsTrue(float.IsFinite(RenderSettings.fogDensity) && RenderSettings.fogDensity >= 0f);
                }
            Assert.IsTrue(true);
        }

        [UnityTest, Order(11)]
        public IEnumerator T11_MainMenuRoundTrip_ContinueRestores()
        {
            C.Services.Wallet.Set(4242);
            C.Save.Save();
            _director.Menu.ReturnToMainMenu();
            yield return WaitFrames(3);
            Assert.AreEqual(MenuView.Mode.Main, C.Ui.MenuMode);
            Assert.IsTrue(_director.Menu.HasSave);
            C.Services.Wallet.Set(5);
            _director.Menu.ContinueGame();
            yield return WaitFrames(3);
            Assert.AreEqual(MenuView.Mode.None, C.Ui.MenuMode);
            Assert.AreEqual(4242, C.Services.Wallet.Money, "Continuar no restauro la partida");
            C.Save.DeleteSave();
            if (_kb != null) InputSystem.RemoveDevice(_kb);
            if (_mouse != null) InputSystem.RemoveDevice(_mouse);
        }
    }
}
