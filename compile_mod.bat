@echo off
:: Set paths to required assemblies
set RIMWORLD_PATH=C:\Program Files (x86)\Steam\steamapps\common\RimWorld
set ASSEMBLY_CSHARP=%RIMWORLD_PATH%\RimWorldWin64_Data\Managed\Assembly-CSharp.dll
set UNITYENGINE=%RIMWORLD_PATH%\RimWorldWin64_Data\Managed\UnityEngine.dll
set HARMONY="C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\2009463077\1.4\Assemblies\0Harmony.dll"
set UNITYCOREMODULE="C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll"
:: Set output path

set OUTPUT_PATH="C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\RickAndMortyBruh\Assemblies\RickAndMortyBruh.dll"

:: Compile the mod

"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" /target:library /out:%OUTPUT_PATH% /reference:"%ASSEMBLY_CSHARP%","%UNITYENGINE%",%HARMONY%,%UNITYCOREMODULE% "Source\RickAndMortyBruh\*.cs"

if %ERRORLEVEL% equ 0 (
    echo Compilation successful! DLL created at %OUTPUT_PATH%
) else (
    echo Compilation failed. Check the error messages above.
)
