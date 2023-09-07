Uses Doorstep to inject a C# dll into Inkbound and dump some information.
Inkbound directory:

Inkbound/
├── Miner/
│   ├── InkboundDataminer.dll 	(Put here by you)
├── doorstop_config.ini			(Put here by you)
├── winhttp.dll					(Put here by you)
├── UnityPlayer.dll
├── UnityCrashHandler64.exe
├── Inkbound.exe
├── MonoBleedingEdge/
├── Inkbound_Data/
└── Inkbound_BurstDebugInformation_DoNotShip/



doorstop_config.ini content:
```
# General options for Unity Doorstop
[General]

# Enable Doorstop?
enabled=true

# Path to the assembly to load and execute
# NOTE: The entrypoint must be of format `static void Doorstop.Entrypoint.Start()`
target_assembly=Miner/InkboundDataminer.dll

# If true, Unity's output log is redirected to <current folder>\output_log.txt
redirect_output_log=false

# If enabled, DOORSTOP_DISABLE env var value is ignored
# USE THIS ONLY WHEN ASKED TO OR YOU KNOW WHAT THIS MEANS
ignore_disable_switch=false


# Options specific to running under Unity Mono runtime
[UnityMono]

# Overrides default Mono DLL search path
# Sometimes it is needed to instruct Mono to seek its assemblies from a different path
# (e.g. mscorlib is stripped in original game)
# This option causes Mono to seek mscorlib and core libraries from a different folder before Managed
# Original Managed folder is added as a secondary folder in the search path
dll_search_path_override=

# If true, Mono debugger server will be enabled
debug_enabled=false

# When debug_enabled is true, specifies the address to use for the debugger server
debug_address=127.0.0.1:10000

# If true and debug_enabled is true, Mono debugger server will suspend the game execution until a debugger is attached
debug_suspend=false

# Options sepcific to running under Il2Cpp runtime
[Il2Cpp]

# Path to coreclr.dll that contains the CoreCLR runtime
coreclr_path=

# Path to the directory containing the managed core libraries for CoreCLR (mscorlib, System, etc.)
corlib_dir=
```