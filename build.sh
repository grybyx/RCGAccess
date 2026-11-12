#!/bin/bash
# Build RCGAccess (plugin + speech server) with Roslyn (csc) from PowerShell 7.
# Paths can be overridden:
#   GAME_MANAGED  RiverCityGirls_Data/Managed of the game (Assembly-CSharp.dll and Unity DLLs)
#   BEPINEX_CORE  BepInEx/core of BepInEx 5.4.23.x (BepInEx.dll, 0Harmony.dll)
#   PWSH          PowerShell 7 executable
set -e
cd "$(dirname "$0")"
M=${GAME_MANAGED:-/home/claude/game/Managed}
B=${BEPINEX_CORE:-/home/claude/tools/bepinex/BepInEx/core}
PW=${PWSH:-pwsh}
CSC=scripts/csc.ps1
mkdir -p build
REFS=$(ls $M/*.dll | grep -v -E "Sony|XboxOne|DataPlatform|ConsoleUtils|Storage|TextSystems|UsersImport" | tr '\n' ',')
REFS="$REFS$B/BepInEx.dll,$B/0Harmony.dll"
$PW -NoProfile -File $CSC -out build/RCGAccess.dll -kind dll -platform anycpu -srcDir src -refs "$REFS"
$PW -NoProfile -File $CSC -out build/RCGAccessSpeech.exe -kind exe -platform x86 -srcDir speech -refs "$M/mscorlib.dll,$M/System.dll" | grep -v CS0649 || true
$PW -NoProfile -File $CSC -out build/RCGAccessSpeech64.exe -kind exe -platform x64 -srcDir speech -refs "$M/mscorlib.dll,$M/System.dll" | grep -v CS0649 || true
# The release also needs prism.dll (Windows x64, https://github.com/ethindp/prism/releases) next to RCGAccessSpeech64.exe.
