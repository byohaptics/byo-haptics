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
#r "nuget: Papaltine.ResoniteLink.RPath, 0.4.0"

open System
open System.Collections.Generic
open System.IO
open System.Text.Json
open System.Threading

open ResoniteLink
open FluxSDK.ResoniteLink
open FluxSDK.Common
open FluxSDK.Packages.Types
open FluxSDK.Build.Pipeline
open FluxSDK.Build.Logging

// Deploys the compiled ProtoGraph to an existing Flux slot and wires input
// fields/components. Generated IDs from the current SlotSpec build are mandatory.

let projectDir = Environment.CurrentDirectory
let allModulePaths =
    [ "flux/BYOHapticsLifecycle"
      "flux/BYOHapticsPositioning"
      "flux/BYOHapticsSampler"
      "flux/BYOHapticsPluginDiscovery"
      "flux/BYOHapticsPluginPackageManager"
      "flux/BYOHapticsOutputBus" ]

let resoniteLinkUrl =
    match Environment.GetEnvironmentVariable "RESONITELINK_URL" with
    | null
    | "" -> Uri "ws://localhost:24319"
    | value -> Uri value

let resoniteDllDir =
    match Environment.GetEnvironmentVariable "RESONITE_DIR" with
    | null | "" -> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Resonite")
    | value -> value
if not (Directory.Exists resoniteDllDir) then failwithf "Resonite directory was not found. Set RESONITE_DIR: %s" resoniteDllDir

let generatedIdsPath =
    match Environment.GetEnvironmentVariable "BYO_HAPTICS_IDS" with
    | null
    | "" -> Path.Combine(projectDir, "build/byo-haptics.generated-ids.json")
    | value -> value

let loadGeneratedIds path =
    use stream = File.OpenRead path
    use doc = JsonDocument.Parse stream
    let root = doc.RootElement
    let inputs = Dictionary<string, string>()
    for inputProp in root.GetProperty("fluxInputs").EnumerateObject() do
        inputs[inputProp.Name] <- inputProp.Value.GetString()
    if not (inputs.ContainsKey "CloseButton") then
        let mutable components = Unchecked.defaultof<JsonElement>
        let mutable closeButton = Unchecked.defaultof<JsonElement>
        if root.TryGetProperty("components", &components)
           && components.TryGetProperty("ui.close.button", &closeButton) then
            inputs["CloseButton"] <- closeButton.GetString()
    ( root.GetProperty("fluxSlotId").GetString(),
      root.GetProperty("toolRootSlotId").GetString(),
      inputs :> IDictionary<string, string> )

let targetFluxSlotId, toolRootSlotId, inputs =
    if not (File.Exists generatedIdsPath) then
        failwithf
            "Generated deployment IDs were not found: %s. Build the SlotSpec in the current Resonite session before deploying ProtoFlux."
            generatedIdsPath
    loadGeneratedIds generatedIdsPath

let outputs = Dictionary<string, string>()

if Environment.GetEnvironmentVariable "BYO_HAPTICS_DEPLOY" <> "1" then
    failwith
        "Refusing to deploy until BYO_HAPTICS_DEPLOY=1 is set. Set row TargetSlot references and initial values in Resonite first."

ElementID.setEpoch (DateTime.UtcNow.Ticks |> uint64) |> ignore

let struct (projectStore, moduleStore) = Build.initializeStores resoniteDllDir
let manifest: ProjectManifest = Build.loadManifest projectDir

let config =
    { Build.defaultConfig manifest with
        NoDefaultAssets = true
        SkipUnmodified = false }

let compileModule modulePath =
    match Build.compileModule (struct (projectStore, moduleStore)) config modulePath with
    | Stage.EarlyExit(message, logs)
    | Stage.Failure(message, logs) ->
        LogMessage.printLogs (manifest.RootDirectory, config.CompactErrorMessages, Log.rev logs)
        failwithf "Build failed for %s: %s" modulePath message

    | Stage.Success(nodeData, logs) ->
        LogMessage.printLogs (manifest.RootDirectory, config.CompactErrorMessages, Log.rev logs)
        modulePath, nodeData

// Compile every sheet before mutating the live scene. A compile failure leaves
// the currently deployed graph untouched.
let compiledModules = allModulePaths |> List.map compileModule

let deployModule (modulePath, nodeData) =
    let moduleLink: LinkInterface = Link.initialize (resoniteLinkUrl, CancellationToken.None)
    let deploy : Step<int> =
        step {
            let! operationResult =
                Link.batchAddProtoGraphNodes (
                    config,
                    targetFluxSlotId,
                    inputs,
                    outputs,
                    nodeData
                )

            if not operationResult.Success then
                return failwithf "Deploy failed for %s: %s" modulePath operationResult.ErrorInfo

            return operationResult.Responses.Count
        }

    deploy |> Link.runStep moduleLink

let moduleResponseCounts =
    compiledModules
    |> List.map (fun compiled -> fst compiled, deployModule compiled)

// Large graph imports can close the ResoniteLink socket after the batch has
// completed. Use a fresh connection for verification and reference patching.
Thread.Sleep 500
let patchLink = new LinkInterface()
patchLink.Connect(resoniteLinkUrl, CancellationToken.None).GetAwaiter().GetResult()

let getSlot (slotId: string) depth includeComponents =
    let req = GetSlot()
    req.SlotID <- slotId
    req.Depth <- depth
    req.IncludeComponentData <- includeComponents
    let response = patchLink.GetSlotData(req).GetAwaiter().GetResult()
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

let updateReferenceMember componentId memberName (oldReference: ResoniteLink.Reference) targetId =
    let compData = Component()
    compData.ID <- componentId
    compData.Members <- Dictionary<string, Member>()

    let refMember = ResoniteLink.Reference()
    refMember.TargetID <- targetId
    refMember.TargetType <- oldReference.TargetType
    compData.Members[memberName] <- refMember

    let req = UpdateComponent()
    req.Data <- compData
    let response = patchLink.UpdateComponent(req).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo

let patchFluxInputReferences () =
    // Read hierarchy without component payloads. Some unrelated graph nodes
    // contain enum values that older ResoniteLink clients cannot deserialize.
    let fluxSlot = getSlot targetFluxSlotId -1 false
    let graphSlots =
        flattenSlots fluxSlot
        |> Seq.filter (fun slot -> not (isNull slot.Name))

    let mutable patched = 0
    for slot in graphSlots do
        match inputNameFromSlotName slot.Name.Value with
        | None -> ()
        | Some inputName ->
            match inputs.TryGetValue inputName with
            | false, _ -> ()
            | true, targetId ->
                let inputSlot = getSlot slot.ID 0 true
                if not (isNull inputSlot.Components) then
                    for compData in inputSlot.Components do
                        if compData.ComponentType.Contains("ProtoFlux.GlobalReference")
                           && not (isNull compData.Members)
                           && compData.Members.ContainsKey("Reference") then
                            let current = compData.Members["Reference"] :?> ResoniteLink.Reference
                            if current.TargetID <> targetId then
                                updateReferenceMember compData.ID "Reference" current targetId
                                patched <- patched + 1
    patched

let patchedInputReferences = patchFluxInputReferences ()
let deployedSlotCount = getSlot toolRootSlotId -1 false |> flattenSlots |> Seq.length
printfn "Deployed BYO Haptics sheets: %d" moduleResponseCounts.Length
for modulePath, responseCount in moduleResponseCounts do
    printfn "  %s response count: %d" modulePath responseCount
printfn "Patched Flux input GlobalReferences: %d" patchedInputReferences
printfn "BYO Haptics total slots: %d" deployedSlotCount

