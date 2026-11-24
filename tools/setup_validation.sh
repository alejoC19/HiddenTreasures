#!/usr/bin/env bash
# Prepara lo necesario para tools/validate.sh en una maquina sin Unity (Linux/macOS):
#  - .NET SDK 8 (si no hay `dotnet`) y
#  - Unity.InputSystem.dll compilado desde el codigo fuente oficial del paquete.
# Uso: tools/setup_validation.sh [work_dir]   (por defecto /home/user/tools/val)
set -euo pipefail
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
WORK=${1:-/home/user/tools/val}
TOOLS=$(dirname "$WORK")
INPUT_SYSTEM_VERSION=1.11.2
mkdir -p "$WORK" "$TOOLS/dl"

if ! command -v dotnet >/dev/null 2>&1 && [ ! -x "$TOOLS/dotnet/dotnet" ]; then
  curl -sSL https://dot.net/v1/dotnet-install.sh -o "$TOOLS/dotnet-install.sh"
  bash "$TOOLS/dotnet-install.sh" --channel 8.0 --install-dir "$TOOLS/dotnet"
fi
export PATH="$TOOLS/dotnet:$PATH"

if [ ! -d "$TOOLS/dl/is/package" ]; then
  mkdir -p "$TOOLS/dl/is"
  curl -sSL -o "$TOOLS/dl/is.tgz" "https://download.packages.unity.com/com.unity.inputsystem/-/com.unity.inputsystem-$INPUT_SYSTEM_VERSION.tgz"
  tar xzf "$TOOLS/dl/is.tgz" -C "$TOOLS/dl/is"
fi

mkdir -p "$WORK/InputSystemLib"
cat > "$WORK/InputSystemLib/InputSystemLib.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <AssemblyName>Unity.InputSystem</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <NoWarn>CS0618;CS0649;CS0169;CS0414;CS0067;CS1591;CS8632</NoWarn>
    <DefineConstants>\$(DefineConstants);UNITY_INPUT_SYSTEM_ENABLE_UI;UNITY_INPUT_SYSTEM_ENABLE_UNITY_INPUT_SYSTEM_UI</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$TOOLS/dl/is/package/InputSystem/**/*.cs" Exclude="$TOOLS/dl/is/package/InputSystem/Editor/**/*.cs" />
    <PackageReference Include="UnityEngine.Modules" Version="2021.3.33" />
    <PackageReference Include="Unity3D.UnityEngine.UI" Version="2018.3.5.1" />
  </ItemGroup>
</Project>
XML
dotnet build "$WORK/InputSystemLib" -c Release -v q
echo "Listo. Ahora: tools/validate.sh $WORK"
