#r "FParsecCS.dll"
#r "FParsec.dll"
#r "FluxSDK.Common.dll"
#r "FluxSDK.Build.dll"
#r "FluxSDK.Packages.dll"
#r "FluxSDK.State.dll"
#r "FluxSDK.FrooxBridge.dll"
#r "FluxSDK.ResoniteLink.dll"
#r "nuget: FSharp.Data.Json.Core, 6.6.0.0"
#r "nuget: YellowDogMan.ResoniteLink, 0.13.1"

open System
open System.Collections.Generic
open System.IO
open System.Text.Json
open System.Threading
open ResoniteLink
open FluxSDK.Common
open FluxSDK.Packages.Types
open FluxSDK.Build.Pipeline
open FluxSDK.Build.Logging
open FluxSDK.ResoniteLink

let env name =
    match Environment.GetEnvironmentVariable name with
    | null | "" -> failwithf "Set %s." name
    | value -> value

let projectDir = Environment.CurrentDirectory
let modulePath = env "OUTPUT_PLUGIN_MODULE"
let idsPath = Path.GetFullPath(Path.Combine(projectDir, env "OUTPUT_PLUGIN_IDS"))
let resoniteLinkUrl = Uri(env "RESONITELINK_URL")
if Environment.GetEnvironmentVariable "OUTPUT_PLUGIN_DEPLOY" <> "1" then failwith "Set OUTPUT_PLUGIN_DEPLOY=1."

let stream = File.OpenRead idsPath
let document = JsonDocument.Parse stream
let root = document.RootElement
let fluxSlotId = root.GetProperty("fluxSlotId").GetString()
let pluginRootSlotId = root.GetProperty("toolRootSlotId").GetString()
let inputs = Dictionary<string, string>()
for property in root.GetProperty("fluxInputs").EnumerateObject() do inputs[property.Name] <- property.Value.GetString()
inputs["PluginRoot"] <- pluginRootSlotId
let outputs = Dictionary<string, string>() :> IDictionary<string, string>
let moduleName = Path.GetFileNameWithoutExtension(modulePath)
let patchLink = new ResoniteLink.LinkInterface()
patchLink.Connect(resoniteLinkUrl, CancellationToken.None).GetAwaiter().GetResult()
let getSlot slotId depth includeComponents =
    let request = GetSlot()
    request.SlotID <- slotId
    request.Depth <- depth
    request.IncludeComponentData <- includeComponents
    let response = patchLink.GetSlotData(request).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo
    response.Data
let rec flattenSlots (slot: Slot) =
    seq {
        yield slot
        if not (isNull slot.Children) then
            for child in slot.Children do
                yield! flattenSlots child
    }
let inputNameFromSlotName (name: string) =
    if not (name.StartsWith("Input:", StringComparison.Ordinal)) then None
    else
        let suffix = name.Substring("Input:".Length)
        if suffix.StartsWith("[", StringComparison.Ordinal) then
            let modifierEnd = suffix.IndexOf(']')
            if modifierEnd >= 0 && modifierEnd + 1 < suffix.Length then
                Some(suffix.Substring(modifierEnd + 1))
            else None
        else Some suffix
let updateReference componentId (current: ResoniteLink.Reference) targetId =
    let componentData = Component()
    componentData.ID <- componentId
    componentData.Members <- Dictionary<string, Member>()
    componentData.Members["Reference"] <-
        ResoniteLink.Reference(TargetID = targetId, TargetType = current.TargetType)
    let request = UpdateComponent()
    request.Data <- componentData
    let response = patchLink.UpdateComponent(request).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo
let patchFluxInputReferences () =
    let mutable patched = 0
    let flux = getSlot fluxSlotId -1 false
    for slot in flattenSlots flux do
        if not (isNull slot.Name) && not (isNull slot.Name.Value) then
            match inputNameFromSlotName slot.Name.Value with
            | None -> ()
            | Some inputName ->
                match inputs.TryGetValue inputName with
                | false, _ -> ()
                | true, targetId ->
                    let inputSlot = getSlot slot.ID 0 true
                    if not (isNull inputSlot.Components) then
                        for componentData in inputSlot.Components do
                            if componentData.ComponentType.Contains("ProtoFlux.GlobalReference")
                               && not (isNull componentData.Members)
                               && componentData.Members.ContainsKey("Reference") then
                                let current = componentData.Members["Reference"] :?> ResoniteLink.Reference
                                if current.TargetID <> targetId then
                                    updateReference componentData.ID current targetId
                                    patched <- patched + 1
    patched
let previousModuleSlotIds =
    let request = GetSlot()
    request.SlotID <- fluxSlotId
    request.Depth <- 1
    request.IncludeComponentData <- false
    let response = patchLink.GetSlotData(request).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo
    if isNull response.Data.Children then [||]
    else
        response.Data.Children
        |> Seq.filter (fun slot -> not (isNull slot.Name) && slot.Name.Value = moduleName)
        |> Seq.map _.ID
        |> Seq.toArray

ElementID.setEpoch (DateTime.UtcNow.Ticks |> uint64) |> ignore
let resoniteDllDir =
    match Environment.GetEnvironmentVariable "RESONITE_DIR" with
    | null | "" -> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Resonite")
    | value -> value
if not (Directory.Exists resoniteDllDir) then failwithf "Resonite directory was not found. Set RESONITE_DIR: %s" resoniteDllDir
let struct (projectStore, moduleStore) = Build.initializeStores resoniteDllDir
let manifest: ProjectManifest = Build.loadManifest projectDir
let config = { Build.defaultConfig manifest with NoDefaultAssets = true; SkipUnmodified = false }
let nodeData =
    match Build.compileModule (struct (projectStore, moduleStore)) config modulePath with
    | Stage.Success(data, logs) ->
        LogMessage.printLogs (manifest.RootDirectory, config.CompactErrorMessages, Log.rev logs)
        data
    | Stage.EarlyExit(message, logs)
    | Stage.Failure(message, logs) ->
        LogMessage.printLogs (manifest.RootDirectory, config.CompactErrorMessages, Log.rev logs)
        failwith message

let moduleLink: LinkInterface = Link.initialize (resoniteLinkUrl, CancellationToken.None)
let deploy : Step<int> =
    step {
        let! result = Link.batchAddProtoGraphNodes(config, fluxSlotId, inputs, outputs, nodeData)
        if not result.Success then return failwith result.ErrorInfo
        return result.Responses.Count
    }
let responseCount = deploy |> Link.runStep moduleLink
let patchedInputReferences = patchFluxInputReferences ()
for slotId in previousModuleSlotIds do
    let request = RemoveSlot()
    request.SlotID <- slotId
    let response = patchLink.RemoveSlot(request).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo
printfn "Deployed %s to %s (%d responses)" modulePath fluxSlotId responseCount
printfn "Patched Flux input GlobalReferences: %d" patchedInputReferences
printfn "Removed previous plugin graphs: %d" previousModuleSlotIds.Length

