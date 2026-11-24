#!/usr/bin/env python3
"""Genera un csproj por asmdef para validar la compilacion con .NET (sin Unity Editor).
Uso: python3 tools/gen_validation.py <repo_root> <work_dir>
Requiere: dotnet SDK, y Unity.InputSystem.dll compilado en <work_dir>/InputSystemLib (ver tools/README.md)."""
import json, os, sys, glob

root, work = os.path.abspath(sys.argv[1]), os.path.abspath(sys.argv[2])
asmdefs = {}
for f in glob.glob(os.path.join(root, "Assets", "**", "*.asmdef"), recursive=True):
    d = json.load(open(f, encoding="utf-8"))
    asmdefs[d["name"]] = (os.path.dirname(f), d)

dirs = sorted(((p, n) for n, (p, _) in asmdefs.items()), key=lambda x: -len(x[0]))

def sources(name):
    base, _ = asmdefs[name]
    out = []
    for dp, _, fs in os.walk(base):
        owner = next(n for p, n in dirs if dp == p or dp.startswith(p + os.sep))
        if owner != name:
            continue
        out += [os.path.join(dp, x) for x in fs if x.endswith(".cs")]
    return out

EXT_REFS = {
    "Unity.InputSystem": '<Reference Include="Unity.InputSystem"><HintPath>%s</HintPath></Reference>' % os.path.join(work, "InputSystemLib/bin/Release/netstandard2.1/Unity.InputSystem.dll"),
    "UnityEngine.UI": '<PackageReference Include="Unity3D.UnityEngine.UI" Version="2018.3.5.1" />',
}
os.makedirs(os.path.join(work, "proj"), exist_ok=True)
sln = []
for name, (base, d) in asmdefs.items():
    editor = d.get("includePlatforms") == ["Editor"]
    test = name.startswith("HuesoOxido.Tests")
    if editor and not test:
        continue  # No hay UnityEditor.dll real para verificar el codigo de Editor: se revisa a mano.
    items = ['<PackageReference Include="UnityEngine.Modules" Version="2021.3.33" />']
    if editor and not test:
        items.append('<PackageReference Include="UnityEditor" Version="2019.3.2.1" />')
    for r in d.get("references", []):
        if r in asmdefs:
            items.append('<ProjectReference Include="../%s/%s.csproj" />' % (r, r))
        elif r in EXT_REFS:
            items.append(EXT_REFS[r])
    srcs = "\n    ".join('<Compile Include="%s" />' % s for s in sources(name))
    extra = ""
    if test:
        extra = '<PackageReference Include="NUnit" Version="3.14.0" /><PackageReference Include="NUnit3TestAdapter" Version="4.5.0" /><PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />'
    tfm = "net8.0" if test else "netstandard2.1"
    proj = f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>{tfm}</TargetFramework><LangVersion>9.0</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo><NoWarn>CS1591;CS8632;NU1701</NoWarn><AssemblyName>{name}</AssemblyName><IsPackable>false</IsPackable></PropertyGroup>
  <ItemGroup>{''.join(items)}{extra}</ItemGroup>
  <ItemGroup>
    {srcs}
  </ItemGroup>
</Project>"""
    pd = os.path.join(work, "proj", name)
    os.makedirs(pd, exist_ok=True)
    open(os.path.join(pd, name + ".csproj"), "w").write(proj)
    sln.append(os.path.join(pd, name + ".csproj"))
open(os.path.join(work, "proj", "projects.txt"), "w").write("\n".join(sln))
print("generated", len(sln), "projects")
