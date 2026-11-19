#r "FParsecCS.dll"
#r "FParsec.dll"
#r "FluxSDK.Common.dll"
#r "FluxSDK.Build.dll"
#r "FluxSDK.Incremental.dll"
#r "FluxSDK.Packages.dll"
#r "FluxSDK.State.dll"
#r "FluxSDK.FrooxBridge.dll"
#r "FluxSDK.ResoniteLink.dll"

open System
open System.IO
open FluxSDK.Common
open FluxSDK.Build.Logging
open FluxSDK.Build.Incremental
open FluxSDK.Packages.Types
open FluxSDK.ResoniteLink

let modulePath =
    match Environment.GetEnvironmentVariable "PROTOGRAPH_MODULE" with
    | null | "" -> failwith "Set PROTOGRAPH_MODULE, e.g. Flux/BYOHapticsLifecycle."
    | value -> value
let resoniteDllDir =
    match Environment.GetEnvironmentVariable "RESONITE_DIR" with
    | null | "" -> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Resonite")
    | value -> value
if not (Directory.Exists resoniteDllDir) then failwithf "Resonite directory was not found. Set RESONITE_DIR: %s" resoniteDllDir
let store = Build.initializeStoreWith [ resoniteDllDir ]
let manifest: ProjectManifest = Build.loadManifest Environment.CurrentDirectory
let config = { Build.defaultConfig manifest with NoDefaultAssets = true; SkipUnmodified = false }
store.SetInput(BuildConfigKey(), config)
let nodeData = Build.moduleCompiler Environment.CurrentDirectory modulePath |> Step.runStep <| store
let diagnostics = nodeData.Diagnostics |> Seq.collect _.FlattenedMessages |> Seq.toList
printPretty diagnostics
if diagnostics |> List.exists (fun diagnostic -> diagnostic.Level = Severity.Error) then
    failwithf "Build failed for %s." modulePath
printfn "Compiled %s" modulePath
