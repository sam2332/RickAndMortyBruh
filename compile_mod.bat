@echo off
:: Set paths to required assemblies
set RIMWORLD_PATH=C:\Program Files (x86)\Steam\steamapps\common\RimWorld
set ASSEMBLY_CSHARP="%RIMWORLD_PATH%\RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
set UNITYENGINE="%RIMWORLD_PATH%\RimWorldWin64_Data\Managed\UnityEngine.dll"
set HARMONY="C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\2009463077\1.5\Assemblies\0Harmony.dll"
set UNITYCOREMODULE="%RIMWORLD_PATH%\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll"
set UNITYUI="%RIMWORLD_PATH%\RimWorldWin64_Data\Managed\UnityEngine.UI.dll"
:: Set output path
set OUTPUT_PATH="%~dp0Assemblies\RickAndMortyBruh.dll"

:: Create assemblies directory if it doesn't exist
if not exist "%~dp0Assemblies" mkdir "%~dp0Assemblies"

:: Compile the mod using .NET 4.7.2 compatible compiler
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe" (
    echo Using Visual Studio 2019 C# compiler...
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe" /target:library /out:%OUTPUT_PATH% /langversion:5 /reference:%ASSEMBLY_CSHARP%,%UNITYENGINE%,%HARMONY%,%UNITYCOREMODULE%,%UNITYUI% /recurse:"%~dp0Source\RickAndMortyBruh\*.cs"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe" (
    echo Using Visual Studio 2022 C# compiler...
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe" /target:library /out:%OUTPUT_PATH% /langversion:5 /reference:%ASSEMBLY_CSHARP%,%UNITYENGINE%,%HARMONY%,%UNITYCOREMODULE%,%UNITYUI% /recurse:"%~dp0Source\RickAndMortyBruh\*.cs"
) else (
    echo Using .NET Framework 4.7.2 compiler...
    "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools\csc.exe" /target:library /out:%OUTPUT_PATH% /langversion:5 /reference:%ASSEMBLY_CSHARP%,%UNITYENGINE%,%HARMONY%,%UNITYCOREMODULE%,%UNITYUI% /recurse:"%~dp0Source\RickAndMortyBruh\*.cs"
)

if %ERRORLEVEL% equ 0 (
    echo Compilation successful! DLL created at %OUTPUT_PATH%
) else (
    echo Compilation failed. Check the error messages above.
)
