using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Player
{
    /// <summary>
    /// Cuerpo placeholder armado con primitivas (hasta integrar el personaje de Mixamo, ver ASSETS_TODO.md).
    /// Ofrece anclajes (mano) y oculta/muestra el cuerpo segun la perspectiva.
    /// </summary>
    public sealed class PlaceholderCharacter : MonoBehaviour, IBodyVisibility
    {
        Transform _torso, _legL, _legR, _armL, _armR, _hand, _shovel;
        Renderer[] _renderers;
        bool _built;

        public const string ModelPath = "Art/Models/Soldier";
        /// <summary>True si se uso el modelo 3D real (Resources/Art/Models/Soldier.glb) en lugar del cuerpo de primitivas.</summary>
        public bool IsModel { get; private set; }
        public Animator ModelAnimator { get; private set; }
        public Transform ModelRoot { get; private set; }
        public Transform ArmRBone, ForeArmRBone, ArmLBone, ForeArmLBone, SpineBone;

        public Transform RightHand => _hand;
        public Transform Shovel => _shovel;
        public Transform Torso => _torso;
        public Transform LeftLeg => _legL;
        public Transform RightLeg => _legR;
        public Transform LeftArm => _armL;
        public Transform RightArm => _armR;

        public void Build()
        {
            if (_built) return;
            _built = true;
            if (BuildFromModel()) return;
            var skin = MaterialUtil.Lit(new Color(0.80f, 0.62f, 0.48f), 0.2f);
            var shirt = MaterialUtil.Lit(new Color(0.30f, 0.38f, 0.26f), 0.1f);
            var pants = MaterialUtil.Lit(new Color(0.24f, 0.22f, 0.20f), 0.1f);
            var pack = MaterialUtil.Lit(new Color(0.55f, 0.34f, 0.16f), 0.1f);
            var hat = MaterialUtil.Lit(new Color(0.42f, 0.30f, 0.16f), 0.05f);

            var root = new GameObject("Body").transform;
            root.SetParent(transform, false);

            _legL = Limb(root, "LegL", new Vector3(-0.11f, 0.9f, 0f), 0.85f, 0.2f, pants);
            _legR = Limb(root, "LegR", new Vector3(0.11f, 0.9f, 0f), 0.85f, 0.2f, pants);

            _torso = new GameObject("Torso").transform;
            _torso.SetParent(root, false);
            _torso.localPosition = new Vector3(0f, 0.9f, 0f);
            var body = MaterialUtil.Primitive(PrimitiveType.Capsule, "Chest", _torso, shirt);
            body.transform.localPosition = new Vector3(0f, 0.34f, 0f);
            body.transform.localScale = new Vector3(0.44f, 0.36f, 0.28f);
            var head = MaterialUtil.Primitive(PrimitiveType.Sphere, "Head", _torso, skin);
            head.transform.localPosition = new Vector3(0f, 0.86f, 0f);
            head.transform.localScale = Vector3.one * 0.24f;
            var brim = MaterialUtil.Primitive(PrimitiveType.Cylinder, "HatBrim", _torso, hat);
            brim.transform.localPosition = new Vector3(0f, 0.98f, 0f);
            brim.transform.localScale = new Vector3(0.42f, 0.015f, 0.42f);
            var crown = MaterialUtil.Primitive(PrimitiveType.Cylinder, "HatCrown", _torso, hat);
            crown.transform.localPosition = new Vector3(0f, 1.03f, 0f);
            crown.transform.localScale = new Vector3(0.24f, 0.06f, 0.24f);
            var bag = MaterialUtil.Primitive(PrimitiveType.Cube, "Backpack", _torso, pack);
            bag.transform.localPosition = new Vector3(0f, 0.38f, -0.25f);
            bag.transform.localScale = new Vector3(0.36f, 0.5f, 0.22f);

            _armL = Limb(_torso, "ArmL", new Vector3(-0.3f, 0.64f, 0f), 0.62f, 0.15f, shirt);
            _armR = Limb(_torso, "ArmR", new Vector3(0.3f, 0.64f, 0f), 0.62f, 0.15f, shirt);

            _hand = new GameObject("RightHand").transform;
            _hand.SetParent(_armR, false);
            _hand.localPosition = new Vector3(0f, -0.62f, 0.04f);

            _shovel = new GameObject("Shovel").transform;
            _shovel.SetParent(_hand, false);
            var steel = MaterialUtil.Lit(new Color(0.5f, 0.52f, 0.55f), 0.5f, 0.6f);
            var wood = MaterialUtil.Lit(new Color(0.45f, 0.3f, 0.15f), 0.1f);
            var handle = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Handle", _shovel, wood);
            handle.transform.localPosition = new Vector3(0f, -0.1f, 0.2f);
            handle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            handle.transform.localScale = new Vector3(0.04f, 0.55f, 0.04f);
            var blade = MaterialUtil.Primitive(PrimitiveType.Cube, "Blade", _shovel, steel);
            blade.transform.localPosition = new Vector3(0f, -0.1f, 0.78f);
            blade.transform.localScale = new Vector3(0.2f, 0.02f, 0.28f);
            _shovel.gameObject.SetActive(false);

            _renderers = root.GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>Instancia el personaje real y deja listos los anclajes (mano, pala). Devuelve false si no hay modelo.</summary>
        bool BuildFromModel()
        {
            var prefab = Resources.Load<GameObject>(ModelPath);
            if (prefab == null) return false;
            var root = new GameObject("Body").transform;
            root.SetParent(transform, false);
            var model = Object.Instantiate(prefab, root);
            model.name = "Model";
            GltfMaterials.ConvertToLit(model, 0.25f);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // el glTF mira hacia -Z en Unity
            ModelRoot = model.transform;
            ModelAnimator = model.GetComponentInChildren<Animator>();
            if (ModelAnimator != null) { ModelAnimator.applyRootMotion = false; ModelAnimator.runtimeAnimatorController = null; }

            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n.EndsWith(":RightArm")) ArmRBone = t;
                else if (n.EndsWith(":RightForeArm")) ForeArmRBone = t;
                else if (n.EndsWith(":LeftArm")) ArmLBone = t;
                else if (n.EndsWith(":LeftForeArm")) ForeArmLBone = t;
                else if (n.EndsWith(":RightHand")) _hand = t;
                else if (n.EndsWith(":Spine1")) SpineBone = t;
            }
            if (_hand == null || ArmRBone == null) { MaterialUtil.Dispose(root.gameObject); ModelRoot = null; ModelAnimator = null; return false; }
            _torso = SpineBone != null ? SpineBone : root;
            _armR = ArmRBone; _armL = ArmLBone;

            _shovel = BuildShovel(_hand);
            _shovel.gameObject.SetActive(false);
            _renderers = root.GetComponentsInChildren<Renderer>(true);
            IsModel = true;
            return true;
        }

        static Transform BuildShovel(Transform hand)
        {
            var shovel = new GameObject("Shovel").transform;
            shovel.SetParent(hand, false);
            var steel = ProceduralSurfaces.Steel("shovel", new Color(0.55f, 0.56f, 0.58f), 0.45f, 0.8f);
            var wood = ProceduralSurfaces.Leather("shovel_wood", new Color(0.5f, 0.34f, 0.18f));
            var handle = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Handle", shovel, wood);
            handle.transform.localPosition = new Vector3(0f, 0f, 0.45f);
            handle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            handle.transform.localScale = new Vector3(0.04f, 0.55f, 0.04f);
            var grip = MaterialUtil.Primitive(PrimitiveType.Cube, "DGrip", shovel, wood);
            grip.transform.localPosition = new Vector3(0f, 0f, -0.08f);
            grip.transform.localScale = new Vector3(0.16f, 0.035f, 0.035f);
            var blade = MaterialUtil.Primitive(PrimitiveType.Cube, "Blade", shovel, steel);
            blade.transform.localPosition = new Vector3(0f, -0.02f, 1.12f);
            blade.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            blade.transform.localScale = new Vector3(0.22f, 0.015f, 0.3f);
            var socket = MaterialUtil.Primitive(PrimitiveType.Cube, "Socket", shovel, steel);
            socket.transform.localPosition = new Vector3(0f, -0.01f, 0.98f);
            socket.transform.localScale = new Vector3(0.05f, 0.04f, 0.1f);
            return shovel;
        }

        static Transform Limb(Transform parent, string name, Vector3 pivotPos, float length, float thickness, Material mat)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = pivotPos;
            var mesh = MaterialUtil.Primitive(PrimitiveType.Capsule, "Mesh", pivot, mat);
            mesh.transform.localPosition = new Vector3(0f, -length * 0.5f, 0f);
            mesh.transform.localScale = new Vector3(thickness, length * 0.5f, thickness);
            return pivot;
        }

        public void SetVisible(bool visible)
        {
            if (_renderers == null) return;
            var mode = visible ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            for (int i = 0; i < _renderers.Length; i++) if (_renderers[i] != null) _renderers[i].shadowCastingMode = mode;
        }
    }
}
