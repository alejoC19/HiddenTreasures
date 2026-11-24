using System;
using UnityEngine;
using HuesoOxido.Core;
using HuesoOxido.Interaction;

namespace HuesoOxido.Inventory
{
    /// <summary>Objeto suelto en el mundo (por ejemplo, un hallazgo que no entro en la mochila).</summary>
    public sealed class WorldPickup : MonoBehaviour
    {
        public ItemInstance Item { get; private set; }
        float _baseY;

        public static WorldPickup Spawn(ItemInstance item, Vector3 position, IItemLookup lookup, InventoryModel backpack,
                                        Action<ItemInstance> onPickedUp, Action<string> onMessage)
        {
            lookup.TryGet(item.DefinitionId, out var info);
            var go = new GameObject("Pickup_" + item.DefinitionId);
            go.transform.position = position;
            var pickup = go.AddComponent<WorldPickup>();
            pickup.Item = item;
            pickup._baseY = position.y;

            Color color = info != null ? RarityStyle.ColorOf(info.Rarity) : Color.white;
            var visual = MaterialUtil.Primitive(PrimitiveType.Cube, "Visual", go.transform, MaterialUtil.Emissive(color));
            visual.transform.localScale = new Vector3(0.22f, 0.12f, 0.16f);
            visual.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 15f);

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.6f;

            string name = info != null ? info.DisplayName : item.DefinitionId;
            string weight = info != null ? info.WeightKg.ToString("0.0#") + " kg" : "";
            go.AddComponent<ActionInteractable>().ConfigureDynamic(
                () => "Recoger: " + name + " (" + weight + ")",
                _ =>
                {
                    if (backpack.TryAdd(item))
                    {
                        onPickedUp?.Invoke(item);
                        Destroy(go);
                    }
                    else onMessage?.Invoke("Mochila llena: no podés cargar " + name);
                });
            return pickup;
        }

        void Update()
        {
            transform.position = new Vector3(transform.position.x, _baseY + 0.05f + Mathf.Sin(Time.time * 2f) * 0.04f, transform.position.z);
            transform.Rotate(0f, 40f * Time.deltaTime, 0f);
        }
    }

    /// <summary>Adaptador: el peso de la mochila frena al personaje.</summary>
    public sealed class InventoryLoadSource : ILoadSource
    {
        readonly InventoryModel _inv;
        public InventoryLoadSource(InventoryModel inv) { _inv = inv; }
        public float LoadFraction => _inv.LoadFraction;
    }
}
