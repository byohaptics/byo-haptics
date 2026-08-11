#r "FParsecCS.dll"
#r "FParsec.dll"
#r "FluxSDK.Common.dll"
#r "FluxSDK.Build.dll"
#r "FluxSDK.Packages.dll"
#r "FluxSDK.State.dll"
#r "FluxSDK.FrooxBridge.dll"
#r "FluxSDK.ResoniteLink.dll"

open System
open System.IO
open FluxSDK.Common
open FluxSDK.Build
open FluxSDK.Build.Pipeline
open FluxSDK.Build.Logging
open FluxSDK.Packages.Types
open FluxSDK.ResoniteLink

let modulePath =
    match Environment.GetEnvironmentVariable "PROTOGRAPH_MODULE" with
    | null | "" -> failwith "Set PROTOGRAPH_MODULE, e.g. flux/BYOHapticsLifecycle."
    | value -> value
let resoniteDllDir =
    match Environment.GetEnvironmentVariable "RESONITE_DIR" with
    | null | "" -> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Resonite")
    | value -> value
if not (Directory.Exists resoniteDllDir) then failwithf "Resonite directory was not found. Set RESONITE_DIR: %s" resoniteDllDir
let struct (projectStore, moduleStore) = Build.initializeStores resoniteDllDir
let manifest: ProjectManifest = Build.loadManifest Environment.CurrentDirectory
let config = { Build.defaultConfig manifest with NoDefaultAssets = true; SkipUnmodified = false }
match Build.compileModule (struct (projectStore, moduleStore)) config modulePath with
| Stage.Success(_, logs) ->
    LogMessage.printLogs (manifest.RootDirectory, config.CompactErrorMessages, Log.rev logs)
    printfn "Compiled %s" modulePath
| Stage.EarlyExit(message, logs)
| Stage.Failure(message, logs) ->
    LogMessage.printLogs (manifest.RootDirectory, config.CompactErrorMessages, Log.rev logs)
    failwith message
