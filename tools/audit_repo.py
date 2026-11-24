#!/usr/bin/env python3
"""Auditoria estatica del repositorio Unity. Sale con codigo 1 si encuentra problemas.
Comprueba: .meta completos y sin huerfanos, GUIDs unicos, asmdef (referencias y ciclos), clases de Unity con
nombre distinto al archivo, namespaces, escena -> script, EditorBuildSettings y manifest."""
import glob, json, os, re, sys

root = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else ".")
errors = []
def err(m): errors.append(m)
def rel(p): return os.path.relpath(p, root).replace(os.sep, "/")

# ---- .meta ----
guids = {}
for dp, dns, fns in os.walk(os.path.join(root, "Assets")):
    dns[:] = [d for d in dns if not d.startswith(".")]
    for n in dns + [f for f in fns if not f.startswith(".") and not f.endswith(".meta")]:
        if not os.path.exists(os.path.join(dp, n) + ".meta"): err("falta .meta: " + rel(os.path.join(dp, n)))
    for f in fns:
        if f.endswith(".meta"):
            target = os.path.join(dp, f[:-5])
            if not os.path.exists(target): err("meta huerfano: " + rel(os.path.join(dp, f)))
            m = re.search(r"guid: ([0-9a-f]{32})", open(os.path.join(dp, f)).read())
            if not m: err("meta sin guid: " + rel(os.path.join(dp, f)))
            else:
                if m.group(1) in guids: err("guid duplicado: " + rel(os.path.join(dp, f)))
                guids[m.group(1)] = target

# ---- asmdef ----
EXTERNAL = {"Unity.InputSystem", "UnityEngine.UI", "UnityEngine.TestRunner", "UnityEditor.TestRunner"}
asm = {}
for f in glob.glob(os.path.join(root, "Assets", "**", "*.asmdef"), recursive=True):
    d = json.load(open(f, encoding="utf-8")); asm[d["name"]] = (f, d)
for name, (f, d) in asm.items():
    for r in d.get("references", []):
        if r not in asm and r not in EXTERNAL: err(f"{rel(f)}: referencia inexistente {r}")
def has_cycle():
    state = {}
    def visit(n, path):
        if state.get(n) == 1: return path + [n]
        if state.get(n) == 2: return None
        state[n] = 1
        for r in asm[n][1].get("references", []):
            if r in asm:
                c = visit(r, path + [n])
                if c: return c
        state[n] = 2
        return None
    for n in asm:
        c = visit(n, [])
        if c: return c
    return None
cyc = has_cycle()
if cyc: err("ciclo de asmdef: " + " -> ".join(cyc))

# ---- scripts ----
script_class = {}
for f in glob.glob(os.path.join(root, "Assets", "**", "*.cs"), recursive=True):
    s = open(f, encoding="utf-8").read()
    base = os.path.basename(f)[:-3]
    ns = re.search(r"^namespace\s+([\w\.]+)", s, re.M)
    if not ns or not ns.group(1).startswith("HuesoOxido."): err(f"{rel(f)}: namespace invalido ({ns.group(1) if ns else 'ninguno'})")
    for m in re.finditer(r"class\s+(\w+)\s*(?:<[^>]*>)?\s*:\s*([\w\., ]+)", s):
        if re.search(r"\b(MonoBehaviour|ScriptableObject)\b", m.group(2)) and m.group(1) != base:
            err(f"{rel(f)}: la clase {m.group(1)} deberia estar en {m.group(1)}.cs")
    script_class[f] = base

# ---- escena ----
scene = os.path.join(root, "Assets/_Project/Scenes/Main.unity")
if not os.path.exists(scene): err("falta Assets/_Project/Scenes/Main.unity")
else:
    txt = open(scene).read()
    for g in re.findall(r"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32})", txt):
        target = guids.get(g)
        if not target or not target.endswith(".cs"): err("la escena referencia un script inexistente: " + g)
        elif not os.path.basename(target).startswith("GameBootstrap"): err("script inesperado en la escena: " + target)
    sm = open(scene + ".meta").read() if os.path.exists(scene + ".meta") else ""
    sg = re.search(r"guid: ([0-9a-f]{32})", sm)
    ebs = os.path.join(root, "ProjectSettings/EditorBuildSettings.asset")
    if not os.path.exists(ebs): err("falta ProjectSettings/EditorBuildSettings.asset")
    elif sg and sg.group(1) not in open(ebs).read(): err("EditorBuildSettings no apunta a la escena Main (guid)")

# ---- proyecto ----
pv = os.path.join(root, "ProjectSettings/ProjectVersion.txt")
if not os.path.exists(pv) or "m_EditorVersion: 6000." not in open(pv).read(): err("ProjectVersion.txt ausente o no es Unity 6")
try:
    deps = json.load(open(os.path.join(root, "Packages/manifest.json")))["dependencies"]
    for need in ["com.unity.inputsystem", "com.unity.render-pipelines.universal", "com.unity.ugui", "com.unity.test-framework"]:
        if need not in deps: err("manifest sin " + need)
    if "com.unity.cinemachine" in deps: err("manifest incluye Cinemachine pero el proyecto no lo usa")
except Exception as e:
    err("manifest invalido: " + str(e))

if errors:
    print("AUDITORIA: %d problema(s)" % len(errors))
    for e in errors: print(" -", e)
    sys.exit(1)
print("AUDITORIA OK: %d archivos con meta, %d asmdef, escena y ajustes consistentes" % (len(guids), len(asm)))
