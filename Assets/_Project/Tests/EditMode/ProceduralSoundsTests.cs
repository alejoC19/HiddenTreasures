using System;
using System.Linq;
using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public sealed class ProceduralSoundsTests
    {
        static float Peak(float[] d) => d.Max(Math.Abs);
        static int ZeroCrossings(float[] d) { int z = 0; for (int i = 1; i < d.Length; i++) if ((d[i - 1] < 0) != (d[i] < 0)) z++; return z; }
        static float Energy(float[] d) => d.Sum(x => x * x);

        static void AssertValid(float[] d, string name)
        {
            Assert.Greater(d.Length, 100, name + " longitud");
            Assert.IsTrue(d.All(x => !float.IsNaN(x) && !float.IsInfinity(x)), name + " finito");
            Assert.LessOrEqual(Peak(d), 1.0f, name + " sin saturar");
            Assert.Greater(Peak(d), 0.2f, name + " audible");
        }

        [Test]
        public void AllGenerators_ProduceValidAudio()
        {
            AssertValid(ProceduralSounds.Wind(4f, 1), "viento");
            AssertValid(ProceduralSounds.Bird(2), "pajaro");
            AssertValid(ProceduralSounds.Cricket(3), "grillo");
            foreach (SurfaceKind k in Enum.GetValues(typeof(SurfaceKind))) AssertValid(ProceduralSounds.Footstep(k, 4), "paso " + k);
            AssertValid(ProceduralSounds.Shovel(5), "pala");
            AssertValid(ProceduralSounds.Brush(6), "cepillo");
            AssertValid(ProceduralSounds.Clink(7), "clink");
            AssertValid(ProceduralSounds.Coin(8), "moneda");
            AssertValid(ProceduralSounds.Beep(880f), "bip");
        }

        [Test]
        public void Generators_AreDeterministicPerSeed_AndVaryAcrossSeeds()
        {
            CollectionAssert.AreEqual(ProceduralSounds.Footstep(SurfaceKind.Dirt, 11), ProceduralSounds.Footstep(SurfaceKind.Dirt, 11));
            CollectionAssert.AreNotEqual(ProceduralSounds.Footstep(SurfaceKind.Dirt, 11), ProceduralSounds.Footstep(SurfaceKind.Dirt, 12));
            CollectionAssert.AreNotEqual(ProceduralSounds.Bird(1), ProceduralSounds.Bird(2));
        }

        [Test]
        public void Wind_LoopsWithoutAJump()
        {
            var d = ProceduralSounds.Wind(4f, 9);
            Assert.AreEqual(4 * ProceduralSounds.Rate, d.Length);
            float step = Math.Abs(d[0] - d[d.Length - 1]);
            float typical = Enumerable.Range(1, 2000).Select(i => Math.Abs(d[i] - d[i - 1])).Average();
            Assert.Less(step, Math.Max(0.05f, typical * 8f), "el salto del empalme es comparable al cambio normal entre muestras");
        }

        [Test]
        public void Beep_HasTheRequestedFrequency()
        {
            foreach (float f in new[] { 520f, 880f, 1500f })
            {
                var d = ProceduralSounds.Beep(f, 0.3f);
                float measured = ZeroCrossings(d) / 2f / (d.Length / (float)ProceduralSounds.Rate);
                Assert.AreEqual(f, measured, f * 0.05f);
            }
        }

        [Test]
        public void Detector_GetsHigherAndFasterWhenCloser()
        {
            float lastF = 0f, lastI = 99f;
            for (float s = 0f; s <= 1.001f; s += 0.1f)
            {
                float f = ProceduralSounds.DetectorFrequency(s), i = ProceduralSounds.DetectorInterval(s);
                Assert.Greater(f, lastF); Assert.Less(i, lastI);
                lastF = f; lastI = i;
            }
            Assert.AreEqual(520f, ProceduralSounds.DetectorFrequency(-1f), 0.01f);
            Assert.AreEqual(1500f, ProceduralSounds.DetectorFrequency(5f), 0.01f);
            Assert.AreEqual(0.07f, ProceduralSounds.DetectorInterval(1f), 0.001f);
        }

        [Test]
        public void Footsteps_SoundDifferentPerSurface()
        {
            // "Brillo": energia de la diferencia entre muestras / energia total (mas alto = mas agudo).
            float Brightness(SurfaceKind k)
            {
                var d = ProceduralSounds.Footstep(k, 21);
                float diff = 0f; for (int i = 1; i < d.Length; i++) diff += (d[i] - d[i - 1]) * (d[i] - d[i - 1]);
                return diff / Energy(d);
            }
            Assert.Less(Brightness(SurfaceKind.Dirt), Brightness(SurfaceKind.Stone), "la tierra es mas sorda que la piedra");
            Assert.Less(Brightness(SurfaceKind.Dirt), Brightness(SurfaceKind.Grass), "la tierra es mas sorda que el roce del pasto");
            Assert.Less(ProceduralSounds.Footstep(SurfaceKind.Stone, 3).Length, ProceduralSounds.Footstep(SurfaceKind.Grass, 3).Length, "la piedra es mas seca y corta");
        }

        [Test]
        public void Clink_RingsLongerThanAStep()
        {
            var clink = ProceduralSounds.Clink(1);
            var step = ProceduralSounds.Footstep(SurfaceKind.Stone, 1);
            Assert.Greater(clink.Length, step.Length * 5);
            int half = clink.Length / 2;
            Assert.Greater(Energy(clink.Take(half).ToArray()), Energy(clink.Skip(half).ToArray()), "decae");
        }
    }
}
