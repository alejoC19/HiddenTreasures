#!/usr/bin/env python3
"""Genera los .meta que falten bajo Assets/ con GUIDs deterministas (md5 de la ruta).
Unity genera los suyos al abrir el proyecto, pero versionarlos fija las referencias (por ejemplo, escena -> script)."""
import hashlib, os, sys

root = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else ".")
assets = os.path.join(root, "Assets")

def guid(rel):
    return hashlib.md5(rel.encode("utf-8")).hexdigest()

def meta_text(rel, is_dir):
    g = guid(rel)
    if is_dir:
        return f"fileFormatVersion: 2\nguid: {g}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    ext = os.path.splitext(rel)[1].lower()
    if ext == ".cs":
        body = "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif ext == ".asmdef":
        body = "AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    else:
        body = "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    return f"fileFormatVersion: 2\nguid: {g}\n{body}"

created = 0
for dp, dns, fns in os.walk(assets):
    dns[:] = [d for d in dns if not d.startswith(".")]
    entries = [(d, True) for d in dns] + [(f, False) for f in fns if not f.startswith(".") and not f.endswith(".meta")]
    for name, is_dir in entries:
        full = os.path.join(dp, name)
        rel = os.path.relpath(full, root).replace(os.sep, "/")
        m = full + ".meta"
        # Modelos/texturas importados por Unity (glTFast, FBX...): el meta correcto lo genera el Editor; uno generico los rompe.
        if not is_dir and os.path.splitext(name)[1].lower() in (".glb", ".gltf", ".fbx", ".png", ".jpg", ".jpeg", ".tga", ".exr", ".wav", ".ogg"):
            continue
        if not os.path.exists(m):
            open(m, "w", newline="\n").write(meta_text(rel, is_dir))
            created += 1
print("meta creados:", created)
