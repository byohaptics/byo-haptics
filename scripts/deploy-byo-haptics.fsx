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
      "flux/BYOHapticsSourceBinding"
      "flux/BYOHapticsSampler"
      "flux/BYOHapticsPluginDiscovery"
      "flux/BYOHapticsPluginPackageManager"
      "flux/BYOHapticsOutputBus"
      "flux/BYOHapticsDiagnostics" ]

let requestedModuleNames =
    match Environment.GetEnvironmentVariable "BYO_HAPTICS_MODULES" with
    | null | "" -> None
    | value ->
        value.Split(',', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries)
        |> Seq.map (fun name ->
            if name.StartsWith("flux/", StringComparison.OrdinalIgnoreCase) then
                name.Substring("flux/".Length)
            else name)
        |> Set.ofSeq
        |> Some

let modulePaths =
    match requestedModuleNames with
    | None -> allModulePaths
    | Some names ->
        allModulePaths
        |> List.filter (fun path -> names.Contains(Path.GetFileName path))

if modulePaths.IsEmpty then
    failwith "BYO_HAPTICS_MODULES did not match any deployable BYO Haptics sheet."

let selectedModuleNames = modulePaths |> Seq.map Path.GetFileName |> Set.ofSeq
let patchOnly = Environment.GetEnvironmentVariable "BYO_HAPTICS_PATCH_ONLY" = "1"
let skipLiveDiscovery = Environment.GetEnvironmentVariable "BYO_HAPTICS_SKIP_LIVE_DISCOVERY" = "1"

let deployedModuleNames =
    set
        [ "BYOHapticsLifecycle"
          "BYOHapticsPositioning"
          "BYOHapticsSourceBinding"
          "BYOHapticsSampler"
          "BYOHapticsPluginDiscovery"
          "BYOHapticsPluginPackageManager"
          "BYOHapticsOutputBus"
          "BYOHapticsDiagnostics" ]
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
    let fluxSlotId = root.GetProperty("fluxSlotId").GetString()
    let inputs = Dictionary<string, string>()
    for inputProp in root.GetProperty("fluxInputs").EnumerateObject() do
        inputs[inputProp.Name] <- inputProp.Value.GetString()
    if not (inputs.ContainsKey "CloseButton") then
        let mutable components = Unchecked.defaultof<JsonElement>
        let mutable closeButton = Unchecked.defaultof<JsonElement>
        if root.TryGetProperty("components", &components)
           && components.TryGetProperty("ui.close.button", &closeButton) then
            inputs["CloseButton"] <- closeButton.GetString()
    fluxSlotId, inputs :> IDictionary<string, string>

let initialTargetFluxSlotId, inputs =
    if not (File.Exists generatedIdsPath) then
        failwithf
            "Generated deployment IDs were not found: %s. Build the SlotSpec in the current Resonite session before deploying ProtoFlux."
            generatedIdsPath
    loadGeneratedIds generatedIdsPath

let mutable targetFluxSlotId = initialTargetFluxSlotId
let mutable usingDiscoveredLiveIds = false
let mutable discoveredToolRootSlotId: string option = None
let requestedToolRootSlotId =
    match Environment.GetEnvironmentVariable "BYO_HAPTICS_TOOL_ID" with
    | null | "" -> None
    | value -> Some value

let generatedRuntimeIds =
    if File.Exists generatedIdsPath then
        use stream = File.OpenRead generatedIdsPath
        use doc = JsonDocument.Parse stream
        let root = doc.RootElement
        Some(
            root.GetProperty("toolRootSlotId").GetString(),
            root.GetProperty("avatarRootSlotId").GetString(),
            root.GetProperty("components").GetProperty("config.installed").GetString()
        )
    else
        None

let outputs = Dictionary<string, string>()

let diagnosticOutputSpecs =
    [ "PluginPresent", "Diagnostics/Plugin/Present", "[FrooxEngine]FrooxEngine.ValueField<bool>", "bool"
      "PluginId", "Diagnostics/Plugin/Id", "[FrooxEngine]FrooxEngine.ValueField<string>", "string"
      "PluginActive", "Diagnostics/Plugin/Active", "[FrooxEngine]FrooxEngine.ValueField<bool>", "bool"
      "PluginConnected", "Diagnostics/Plugin/Connected", "[FrooxEngine]FrooxEngine.ValueField<bool>", "bool"
      "PluginConnectionStatusAvailable", "Diagnostics/Plugin/ConnectionStatusAvailable", "[FrooxEngine]FrooxEngine.ValueField<bool>", "bool"
      "PluginDisconnected", "Diagnostics/Plugin/Disconnected", "[FrooxEngine]FrooxEngine.ValueField<bool>", "bool"
      "InstalledState", "Diagnostics/Lifecycle/Installed", "[FrooxEngine]FrooxEngine.ValueField<bool>", "bool"
      "PanelVisibleState", "Diagnostics/Lifecycle/PanelVisible", "[FrooxEngine]FrooxEngine.ValueField<bool>", "bool"
      "Row0RawForce", "Diagnostics/Samplers/Row_000/RawForce", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row0RawVibration", "Diagnostics/Samplers/Row_000/RawVibration", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row0RawPain", "Diagnostics/Samplers/Row_000/RawPain", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row0RawTemperature", "Diagnostics/Samplers/Row_000/RawTemperature", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row1RawForce", "Diagnostics/Samplers/Row_001/RawForce", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row1RawVibration", "Diagnostics/Samplers/Row_001/RawVibration", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row1RawPain", "Diagnostics/Samplers/Row_001/RawPain", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row1RawTemperature", "Diagnostics/Samplers/Row_001/RawTemperature", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row2RawForce", "Diagnostics/Samplers/Row_002/RawForce", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row2RawVibration", "Diagnostics/Samplers/Row_002/RawVibration", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row2RawPain", "Diagnostics/Samplers/Row_002/RawPain", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row2RawTemperature", "Diagnostics/Samplers/Row_002/RawTemperature", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row3RawForce", "Diagnostics/Samplers/Row_003/RawForce", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row3RawVibration", "Diagnostics/Samplers/Row_003/RawVibration", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row3RawPain", "Diagnostics/Samplers/Row_003/RawPain", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "Row3RawTemperature", "Diagnostics/Samplers/Row_003/RawTemperature", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow0Force", "Diagnostics/Output/Row_000/Force", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow0Vibration", "Diagnostics/Output/Row_000/Vibration", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow0Pain", "Diagnostics/Output/Row_000/Pain", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow0Temperature", "Diagnostics/Output/Row_000/Temperature", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow1Force", "Diagnostics/Output/Row_001/Force", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow1Vibration", "Diagnostics/Output/Row_001/Vibration", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow1Pain", "Diagnostics/Output/Row_001/Pain", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow1Temperature", "Diagnostics/Output/Row_001/Temperature", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow2Force", "Diagnostics/Output/Row_002/Force", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow2Vibration", "Diagnostics/Output/Row_002/Vibration", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow2Pain", "Diagnostics/Output/Row_002/Pain", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow2Temperature", "Diagnostics/Output/Row_002/Temperature", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow3Force", "Diagnostics/Output/Row_003/Force", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow3Vibration", "Diagnostics/Output/Row_003/Vibration", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow3Pain", "Diagnostics/Output/Row_003/Pain", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow3Temperature", "Diagnostics/Output/Row_003/Temperature", "[FrooxEngine]FrooxEngine.ValueField<float>", "float"
      "OutputRow0Target", "Diagnostics/Output/Row_000/Target", "[FrooxEngine]FrooxEngine.ValueField<string>", "string"
      "OutputRow1Target", "Diagnostics/Output/Row_001/Target", "[FrooxEngine]FrooxEngine.ValueField<string>", "string"
      "OutputRow2Target", "Diagnostics/Output/Row_002/Target", "[FrooxEngine]FrooxEngine.ValueField<string>", "string"
      "OutputRow3Target", "Diagnostics/Output/Row_003/Target", "[FrooxEngine]FrooxEngine.ValueField<string>", "string" ]

let diagnosticAlias outputName =
    match outputName with
    | "PluginPresent" -> Some "diagnostics.plugin.present"
    | "PluginId" -> Some "diagnostics.plugin.id"
    | "PluginActive" -> Some "diagnostics.plugin.active"
    | "PluginConnected" -> Some "diagnostics.plugin.connected"
    | "PluginConnectionStatusAvailable" -> Some "diagnostics.plugin.connectionStatusAvailable"
    | "PluginDisconnected" -> Some "diagnostics.plugin.disconnected"
    | "InstalledState" -> Some "diagnostics.lifecycle.installed"
    | "PanelVisibleState" -> Some "diagnostics.lifecycle.panelVisible"
    | name when name.StartsWith("Row") && name.Contains("Raw") ->
        let row = name.Substring(3, 1)
        let sensation = name.Substring(name.IndexOf("Raw") + 3)
        Some(sprintf "diagnostics.samplers.row%s.raw%s" row sensation)
    | name when name.StartsWith("OutputRow") && not (name.EndsWith("Target")) ->
        let row = name.Substring(9, 1)
        let sensation = name.Substring(10)
        let aliasSensation = sensation.Substring(0, 1).ToLowerInvariant() + sensation.Substring(1)
        Some(sprintf "diagnostics.output.row%s.%s" row aliasSensation)
    | "OutputRow0Target" -> Some "diagnostics.output.row0.target"
    | "OutputRow1Target" -> Some "diagnostics.output.row1.target"
    | "OutputRow2Target" -> Some "diagnostics.output.row2.target"
    | "OutputRow3Target" -> Some "diagnostics.output.row3.target"
    | _ -> None

let loadGeneratedDiagnosticOutputs () =
    if File.Exists generatedIdsPath then
        use stream = File.OpenRead generatedIdsPath
        use doc = JsonDocument.Parse stream
        let root = doc.RootElement
        let mutable members = Unchecked.defaultof<JsonElement>
        if root.TryGetProperty("members", &members) then
            for outputName, _, _, _ in diagnosticOutputSpecs do
                match diagnosticAlias outputName with
                | None -> ()
                | Some alias ->
                    let mutable componentMembers = Unchecked.defaultof<JsonElement>
                    let mutable valueMember = Unchecked.defaultof<JsonElement>
                    if members.TryGetProperty(alias, &componentMembers)
                       && componentMembers.TryGetProperty("Value", &valueMember) then
                        let fieldId = valueMember.GetString()
                        if not (String.IsNullOrWhiteSpace fieldId) then
                            outputs[outputName] <- fieldId

loadGeneratedDiagnosticOutputs ()

let rec flattenDiscoverySlots (slot: Slot) =
    seq {
        yield slot
        if not (isNull slot.Children) then
            for child in slot.Children do
                yield! flattenDiscoverySlots child
    }

let discoveryInputName (name: string) =
    if not (name.StartsWith("Input:", StringComparison.Ordinal)) then None
    else
        let suffix = name.Substring("Input:".Length)
        if suffix.StartsWith("[", StringComparison.Ordinal) then
            let modifierEnd = suffix.IndexOf(']')
            if modifierEnd >= 0 && modifierEnd + 1 < suffix.Length then
                Some(suffix.Substring(modifierEnd + 1))
            else None
        else Some suffix

let discoverLiveFluxAndInputs () =
    let discoveryLink = new ResoniteLink.LinkInterface()
    discoveryLink.Connect(resoniteLinkUrl, CancellationToken.None).GetAwaiter().GetResult()

    let get slotId depth includeComponents =
        let request = GetSlot()
        request.SlotID <- slotId
        request.Depth <- depth
        request.IncludeComponentData <- includeComponents
        let response = discoveryLink.GetSlotData(request).GetAwaiter().GetResult()
        if not response.Success then failwith response.ErrorInfo
        response.Data

    let slotName (slot: Slot) =
        if isNull slot.Name || isNull slot.Name.Value then "" else slot.Name.Value

    let parentId (slot: Slot) =
        if isNull slot.Parent then "" else slot.Parent.TargetID

    let makeFieldMember kind =
        match kind with
        | "float" -> Field_float(Value = 0.0f) :> Member
        | "bool" -> Field_bool(Value = false) :> Member
        | "string" -> Field_string(Value = "") :> Member
        | other -> failwithf "Unsupported diagnostic output field kind: %s" other

    let findDirectChild (children: seq<Slot>) expectedParentId name =
        children
        |> Seq.tryFind (fun slot -> parentId slot = expectedParentId && slotName slot = name)

    let makeSlot parentSlotId name =
        let slot = Slot()
        slot.ID <- "Deploy_" + Guid.NewGuid().ToString("N")
        slot.Name <- Field_string(Value = name)
        slot.Parent <- Reference(TargetID = parentSlotId, TargetType = "[FrooxEngine]FrooxEngine.Slot")
        slot.Position <- Field_float3(Value = float3())
        slot.Rotation <- Field_floatQ(Value = floatQ(w = 1.0f))
        slot.Scale <- Field_float3(Value = float3(x = 1.0f, y = 1.0f, z = 1.0f))
        slot.OrderOffset <- Field_long(Value = 0L)
        slot

    let makeValueField slotId componentType kind =
        let comp = Component()
        comp.ID <- "Deploy_" + Guid.NewGuid().ToString("N")
        comp.ComponentType <- componentType
        comp.Members <- Dictionary<string, Member>()
        comp.Members["Value"] <- makeFieldMember kind
        let op = AddComponent()
        op.ContainerSlotId <- slotId
        op.Data <- comp
        op :> DataModelOperation

    let addSlot parentSlotId name =
        let data = makeSlot parentSlotId name
        let op = AddSlot()
        op.Data <- data
        let ops = ResizeArray<DataModelOperation>()
        ops.Add(op :> DataModelOperation)
        let result = discoveryLink.RunDataModelOperationBatch(ops).GetAwaiter().GetResult()
        if not result.Success then failwith result.ErrorInfo
        for response in result.Responses do
            if not response.Success then failwith response.ErrorInfo
        data.ID

    let addComponent slotId componentType kind =
        let op = makeValueField slotId componentType kind
        let ops = ResizeArray<DataModelOperation>()
        ops.Add(op)
        let result = discoveryLink.RunDataModelOperationBatch(ops).GetAwaiter().GetResult()
        if not result.Success then failwith result.ErrorInfo
        for response in result.Responses do
            if not response.Success then failwith response.ErrorInfo

    let valueFieldMemberId slotId componentType =
        let slot = get slotId 1 true
        if isNull slot.Components then None
        else
            slot.Components
            |> Seq.tryPick (fun componentData ->
                if componentData.ComponentType = componentType
                   && not (isNull componentData.Members)
                   && componentData.Members.ContainsKey("Value") then
                    Some componentData.Members["Value"].ID
                else None)

    let ensureDiagnosticOutputFields toolRootSlotId =
        let mutable toolHierarchy = get toolRootSlotId -1 false |> flattenDiscoverySlots |> Seq.toArray
        let refresh () =
            toolHierarchy <- get toolRootSlotId -1 false |> flattenDiscoverySlots |> Seq.toArray

        let diagnosticsSlotId =
            match findDirectChild toolHierarchy toolRootSlotId "Diagnostics" with
            | Some slot -> slot.ID
            | None ->
                let id = addSlot toolRootSlotId "Diagnostics"
                refresh()
                id

        let ensurePath (relativePath: string) =
            let segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
            let mutable parentId = toolRootSlotId
            for segment in segments do
                match findDirectChild toolHierarchy parentId segment with
                | Some slot -> parentId <- slot.ID
                | None ->
                    parentId <- addSlot parentId segment
                    refresh()
            parentId

        for outputName, relativePath, componentType, kind in diagnosticOutputSpecs do
            let slotId = ensurePath relativePath

            match valueFieldMemberId slotId componentType with
            | Some fieldId -> outputs[outputName] <- fieldId
            | None ->
                addComponent slotId componentType kind
                match valueFieldMemberId slotId componentType with
                | Some fieldId -> outputs[outputName] <- fieldId
                | None -> failwithf "Failed to create diagnostic output field: %s" relativePath

    let world = get "Root" -1 false
    let hierarchy = flattenDiscoverySlots world |> Seq.toArray
    let slotsById = hierarchy |> Seq.map (fun slot -> slot.ID, slot) |> dict

    let existingGraphs =
        hierarchy
        |> Seq.filter (fun slot ->
            slotName slot <> ""
            && deployedModuleNames
               |> Seq.exists (fun name -> (slotName slot).Equals(name, StringComparison.OrdinalIgnoreCase))
            && not (isNull slot.Parent)
            && slotsById.ContainsKey(slot.Parent.TargetID)
            && slotName slotsById[slot.Parent.TargetID] = "Flux")
        |> Seq.filter (fun graph ->
            match requestedToolRootSlotId with
            | None -> true
            | Some toolRootSlotId ->
                let fluxSlot = slotsById[graph.Parent.TargetID]
                not (isNull fluxSlot.Parent) && fluxSlot.Parent.TargetID = toolRootSlotId)
        |> Seq.toArray

    if existingGraphs.Length = 0 then
        match generatedRuntimeIds with
        | Some(toolRootSlotId, _, _) when not (String.IsNullOrWhiteSpace toolRootSlotId) ->
            ensureDiagnosticOutputFields toolRootSlotId
        | _ -> ()
        0
    else
        let fluxIds =
            existingGraphs
            |> Seq.map (fun graph -> graph.Parent.TargetID)
            |> Seq.distinct
            |> Seq.toArray
        if fluxIds.Length <> 1 then
            failwithf "Expected one live BYO Haptics Flux slot, found %d." fluxIds.Length

        targetFluxSlotId <- fluxIds[0]
        usingDiscoveredLiveIds <- targetFluxSlotId <> initialTargetFluxSlotId
        discoveredToolRootSlotId <-
            if slotsById.ContainsKey(targetFluxSlotId)
               && not (isNull slotsById[targetFluxSlotId].Parent) then
                Some slotsById[targetFluxSlotId].Parent.TargetID
            else None

        match requestedToolRootSlotId, discoveredToolRootSlotId with
        | Some requested, Some discovered when requested <> discovered ->
            failwithf "Discovered BYO Haptics tool %s does not match BYO_HAPTICS_TOOL_ID=%s." discovered requested
        | Some requested, None ->
            failwithf "Could not resolve the BYO Haptics parent for BYO_HAPTICS_TOOL_ID=%s." requested
        | _ -> ()

        discoveredToolRootSlotId |> Option.iter ensureDiagnosticOutputFields
        let liveSamplerInputOverrides = Dictionary<string, string>()
        discoveredToolRootSlotId
        |> Option.iter (fun toolRootSlotId ->
            let toolHierarchy =
                get toolRootSlotId -1 false
                |> flattenDiscoverySlots
                |> Seq.filter (fun slot -> not (isNull slot.Name) && not (isNull slot.Name.Value))
                |> Seq.toArray

            let childNamed parentSlotId name =
                toolHierarchy
                |> Seq.tryFind (fun slot ->
                    not (isNull slot.Parent)
                    && slot.Parent.TargetID = parentSlotId
                    && slot.Name.Value = name)

            let rec slotByRelativePath parentSlotId (parts: string list) =
                match parts with
                | [] -> Some parentSlotId
                | name :: rest ->
                    childNamed parentSlotId name
                    |> Option.bind (fun slot -> slotByRelativePath slot.ID rest)

            let memberIdForComponent slotId componentType memberName =
                let slot = get slotId 0 true
                if isNull slot.Components then None
                else
                    slot.Components
                    |> Seq.tryPick (fun componentData ->
                        if componentData.ComponentType = componentType
                           && not (isNull componentData.Members)
                           && componentData.Members.ContainsKey(memberName) then
                            Some componentData.Members[memberName].ID
                        else None)

            let toolRoot = get toolRootSlotId 0 true
            let grabbable =
                if isNull toolRoot.Components then None
                else
                    toolRoot.Components
                    |> Seq.tryFind (fun componentData -> componentData.ComponentType = "[FrooxEngine]FrooxEngine.Grabbable")

            match grabbable with
            | None -> ()
            | Some componentData ->
                    inputs["ToolGrabbable"] <- componentData.ID
                    if not (isNull componentData.Members) && componentData.Members.ContainsKey("_lastParent") then
                        inputs["ToolGrabbableLastParent"] <- componentData.Members["_lastParent"].ID
                    if not (isNull componentData.Members) && componentData.Members.ContainsKey("_lastParentIsUserSpace") then
                        inputs["ToolGrabbableLastParentIsUserSpace"] <- componentData.Members["_lastParentIsUserSpace"].ID

            for row in 0 .. 3 do
                let rowName = sprintf "Row_%03d" row
                match slotByRelativePath toolRootSlotId [ "Samplers"; rowName; "Target" ] with
                | None -> ()
                | Some targetSlotId ->
                    match memberIdForComponent targetSlotId "[FrooxEngine]FrooxEngine.ValueField<string>" "Value" with
                    | None -> ()
                    | Some targetFieldId -> inputs[sprintf "Row%dTarget" row] <- targetFieldId

                match slotByRelativePath toolRootSlotId [ "Samplers"; rowName; "Sampler" ] with
                | None -> ()
                | Some samplerSlotId ->
                    let samplerSlot = get samplerSlotId 0 true
                    if not (isNull samplerSlot.Components) then
                        samplerSlot.Components
                        |> Seq.tryFind (fun componentData ->
                            componentData.ComponentType = "[FrooxEngine]FrooxEngine.Grabbable")
                        |> Option.iter (fun componentData ->
                            inputs[sprintf "Row%dSamplerGrabbable" row] <- componentData.ID)
                    for memberName in [ "Enabled"; "Force"; "Vibration"; "Pain"; "Temperature" ] do
                        match memberIdForComponent samplerSlotId "[FrooxEngine]FrooxEngine.VirtualHapticPointSampler" memberName with
                        | None -> ()
                        | Some fieldId ->
                            let inputName =
                                if memberName = "Enabled" then sprintf "Row%dSamplerEnabled" row
                                else sprintf "Row%d%s" row memberName
                            inputs[inputName] <- fieldId
                            liveSamplerInputOverrides[inputName] <- fieldId)

        let mutable recovered = 0
        let fluxHierarchy = get targetFluxSlotId -1 false |> flattenDiscoverySlots
        for slot in fluxHierarchy do
            if not (isNull slot.Name) then
                match discoveryInputName slot.Name.Value with
                | None -> ()
                | Some inputName ->
                    let inputSlot = get slot.ID 0 true
                    if not (isNull inputSlot.Components) then
                        for componentData in inputSlot.Components do
                            if componentData.ComponentType.Contains("ProtoFlux.GlobalReference")
                               && not (isNull componentData.Members)
                               && componentData.Members.ContainsKey("Reference") then
                                let reference = componentData.Members["Reference"] :?> ResoniteLink.Reference
                                if not (String.IsNullOrWhiteSpace reference.TargetID) then
                                    inputs[inputName] <- reference.TargetID
                                    recovered <- recovered + 1

        // A repaired/recreated sampler can leave stale GlobalReferences in the
        // currently deployed graphs. The live component is authoritative.
        for KeyValue(inputName, fieldId) in liveSamplerInputOverrides do
            inputs[inputName] <- fieldId
        recovered

let hasTodo =
    seq {
        yield targetFluxSlotId
        yield! inputs.Values
    }
    |> Seq.exists (fun value -> value.StartsWith("TODO_", StringComparison.Ordinal))

if hasTodo then
    failwith
        "Set targetFluxSlotId and all input IDs before running this deploy script. Use RESONITELINK_URL to override the ResoniteLink endpoint."

if Environment.GetEnvironmentVariable "BYO_HAPTICS_DEPLOY" <> "1" then
    failwith
        "Refusing to deploy until BYO_HAPTICS_DEPLOY=1 is set. Set row TargetSlot references and initial values in Resonite first."

let recoveredLiveInputs =
    if skipLiveDiscovery then
        match generatedRuntimeIds with
        | Some(toolRootSlotId, _, _) -> discoveredToolRootSlotId <- Some toolRootSlotId
        | None -> ()
        0
    else
        discoverLiveFluxAndInputs ()

ElementID.setEpoch (DateTime.UtcNow.Ticks |> uint64) |> ignore

let struct (projectStore, moduleStore) = Build.initializeStores resoniteDllDir
let manifest: ProjectManifest = Build.loadManifest projectDir

let config =
    { Build.defaultConfig manifest with
        NoDefaultAssets = true
        SkipUnmodified = false }

let findExistingModuleSlots () =
    let discoveryLink = new ResoniteLink.LinkInterface()
    discoveryLink.Connect(resoniteLinkUrl, CancellationToken.None).GetAwaiter().GetResult()
    let request = GetSlot()
    request.SlotID <- targetFluxSlotId
    request.Depth <- 1
    request.IncludeComponentData <- false
    let response = discoveryLink.GetSlotData(request).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo
    if isNull response.Data.Children then [||]
    else
        response.Data.Children
        |> Seq.filter (fun slot ->
            not (isNull slot.Name)
            && not (isNull slot.Name.Value)
            && selectedModuleNames
               |> Seq.exists (fun name -> slot.Name.Value.Equals(name, StringComparison.OrdinalIgnoreCase)))
        |> Seq.map _.ID
        |> Seq.toArray

let previousModuleSlotIds = if patchOnly then [||] else findExistingModuleSlots ()

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
let compiledModules = if patchOnly then [] else modulePaths |> List.map compileModule

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

let updateReferenceMemberWithType componentId memberName targetId targetType =
    let compData = Component()
    compData.ID <- componentId
    compData.Members <- Dictionary<string, Member>()

    let refMember = ResoniteLink.Reference()
    refMember.TargetID <- targetId
    refMember.TargetType <- targetType
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

let fieldTypeForDiagnosticComponent (componentType: string) =
    if componentType.Contains("<float>") then "[FrooxEngine]FrooxEngine.IField<float>"
    elif componentType.Contains("<bool>") then "[FrooxEngine]FrooxEngine.IField<bool>"
    elif componentType.Contains("<string>") then "[FrooxEngine]FrooxEngine.IField<string>"
    else failwithf "Unsupported diagnostic output component type: %s" componentType

let patchFluxOutputDrives () =
    let fluxSlot = getSlot targetFluxSlotId -1 true
    let graphSlots =
        flattenSlots fluxSlot
        |> Seq.filter (fun slot -> not (isNull slot.Name) && not (isNull slot.Name.Value))

    let mutable patched = 0
    for outputName, _, componentType, _ in diagnosticOutputSpecs do
        match outputs.TryGetValue outputName with
        | false, _ -> ()
        | true, targetFieldId ->
            let targetType = fieldTypeForDiagnosticComponent componentType
            for slot in graphSlots do
                if slot.Name.Value = "Output:" + outputName && not (isNull slot.Components) then
                    for compData in slot.Components do
                        if compData.ComponentType.Contains("FieldDriveBase")
                           && not (isNull compData.Members)
                           && compData.Members.ContainsKey("Drive") then
                            let current = compData.Members["Drive"] :?> ResoniteLink.Reference
                            if current.TargetID <> targetFieldId then
                                updateReferenceMemberWithType compData.ID "Drive" targetFieldId targetType
                                patched <- patched + 1
    patched

let patchConnectionLedReference () =
    match outputs.TryGetValue "PluginConnected" with
    | false, _ -> 0
    | true, connectedFieldId ->
        let toolRootId =
            match discoveredToolRootSlotId with
            | Some value -> value
            | None ->
                let fluxSlot = getSlot targetFluxSlotId 0 false
                if isNull fluxSlot.Parent then failwith "Flux slot has no parent tool root."
                fluxSlot.Parent.TargetID
        let toolHierarchy = getSlot toolRootId -1 false
        let slots = flattenSlots toolHierarchy |> Seq.toArray
        let slotsById = slots |> Seq.map (fun slot -> slot.ID, slot) |> dict
        let ledSlot =
            slots
            |> Seq.tryFind (fun slot ->
                not (isNull slot.Name)
                && slot.Name.Value = "LED"
                && not (isNull slot.Parent)
                && slotsById.ContainsKey(slot.Parent.TargetID)
                && not (isNull slotsById[slot.Parent.TargetID].Name)
                && slotsById[slot.Parent.TargetID].Name.Value = "StatusArea")
        match ledSlot with
        | None -> 0
        | Some slot ->
            let ledData = getSlot slot.ID 0 true
            let valueCopy =
                ledData.Components
                |> Seq.tryFind (fun componentData ->
                    componentData.ComponentType.Contains("ValueCopy<bool>", StringComparison.Ordinal))
            match valueCopy with
            | None -> 0
            | Some componentData ->
                updateReferenceMemberWithType
                    componentData.ID
                    "Source"
                    connectedFieldId
                    "[FrooxEngine]FrooxEngine.IField<bool>"
                1

let patchPluginPresentLedReference () =
    match outputs.TryGetValue "PluginPresent" with
    | false, _ -> 0
    | true, presentFieldId ->
        let toolRootId =
            match discoveredToolRootSlotId with
            | Some value -> value
            | None ->
                let fluxSlot = getSlot targetFluxSlotId 0 false
                if isNull fluxSlot.Parent then failwith "Flux slot has no parent tool root."
                fluxSlot.Parent.TargetID
        let toolHierarchy = getSlot toolRootId -1 false
        let ledSlot =
            flattenSlots toolHierarchy
            |> Seq.tryFind (fun slot ->
                not (isNull slot.Name)
                && slot.Name.Value = "Plugin Status LED")
        match ledSlot with
        | None -> 0
        | Some slot ->
            let ledData = getSlot slot.ID 0 true
            let valueCopy =
                ledData.Components
                |> Seq.tryFind (fun componentData ->
                    componentData.ComponentType.Contains("ValueCopy<bool>", StringComparison.Ordinal))
            match valueCopy with
            | None -> 0
            | Some componentData ->
                updateReferenceMemberWithType
                    componentData.ID
                    "Source"
                    presentFieldId
                    "[FrooxEngine]FrooxEngine.IField<bool>"
                1

let removePreviousModules () =
    if Environment.GetEnvironmentVariable "BYO_HAPTICS_KEEP_PREVIOUS" = "1" then
        0
    else
        for slotId in previousModuleSlotIds do
            let request = RemoveSlot()
            request.SlotID <- slotId
            let response = patchLink.RemoveSlot(request).GetAwaiter().GetResult()
            if not response.Success then failwith response.ErrorInfo
        previousModuleSlotIds.Length

let removedPreviousModules = removePreviousModules ()
let patchedOutputDrives = patchFluxOutputDrives ()
let patchedConnectionLed = patchConnectionLedReference ()
let patchedPluginPresentLed = patchPluginPresentLedReference ()

let sheetLayout =
    [ "BYOHapticsLifecycle", (0.0f, 0.0f, 0.0f), 0L
      "BYOHapticsPositioning", (7.5f, 0.0f, 0.0f), 1L
      "BYOHapticsSourceBinding", (15.0f, 0.0f, 0.0f), 2L
      "BYOHapticsSampler", (22.5f, 0.0f, 0.0f), 3L
      "BYOHapticsPluginDiscovery", (30.0f, 0.0f, 0.0f), 4L
      "BYOHapticsPluginPackageManager", (37.5f, 0.0f, 0.0f), 5L
      "BYOHapticsOutputBus", (45.0f, 0.0f, 0.0f), 6L
      "BYOHapticsDiagnostics", (22.5f, -6.0f, 0.0f), 7L ]

let arrangeDeployedSheets () =
    let fluxSlot = getSlot targetFluxSlotId 1 false
    if isNull fluxSlot.Children then 0
    else
        let childrenByName =
            fluxSlot.Children
            |> Seq.filter (fun slot -> not (isNull slot.Name) && not (isNull slot.Name.Value))
            |> Seq.map (fun slot -> slot.Name.Value, slot)
            |> dict

        let mutable arranged = 0
        for sheetName, (x, y, z), orderOffset in sheetLayout do
            if childrenByName.ContainsKey sheetName then
                let current = getSlot childrenByName[sheetName].ID 0 false
                let update = Slot()
                update.ID <- current.ID
                update.Position <- Field_float3(Value = float3(x = x, y = y, z = z))
                update.OrderOffset <- Field_long(Value = orderOffset)

                let request = UpdateSlot()
                request.Data <- update
                let response = patchLink.UpdateSlot(request).GetAwaiter().GetResult()
                if not response.Success then failwith response.ErrorInfo
                arranged <- arranged + 1
        arranged

let arrangedSheets = arrangeDeployedSheets ()

let reconcileInstalledState () =
    let updateInstalled toolRootSlotId installedComponentId installed =
        let toolRoot = getSlot toolRootSlotId 0 false

        let compData = Component()
        compData.ID <- installedComponentId
        compData.Members <- Dictionary<string, Member>()
        compData.Members["Value"] <- Field_bool(Value = installed)

        let req = UpdateComponent()
        req.Data <- compData
        let response = patchLink.UpdateComponent(req).GetAwaiter().GetResult()
        if not response.Success then failwith response.ErrorInfo
        installed

    match discoveredToolRootSlotId with
    | Some toolRootSlotId ->
        let toolRoot = getSlot toolRootSlotId 0 false
        if not (isNull toolRoot.Parent) && toolRoot.Parent.TargetID = "Root" then
            let toolHierarchy = getSlot toolRootSlotId -1 false
            let slots = flattenSlots toolHierarchy |> Seq.toArray
            let slotsById = slots |> Seq.map (fun slot -> slot.ID, slot) |> dict
            let installedSlot =
                slots
                |> Seq.find (fun slot ->
                    not (isNull slot.Name)
                    && slot.Name.Value = "Installed"
                    && not (isNull slot.Parent)
                    && slotsById.ContainsKey(slot.Parent.TargetID)
                    && slotsById[slot.Parent.TargetID].Name.Value = "Config")
                |> fun slot -> getSlot slot.ID 0 true
            let installedComponent =
                installedSlot.Components
                |> Seq.find (fun componentData ->
                    componentData.ComponentType.Contains("ValueField<bool>", StringComparison.Ordinal))
            Some(updateInstalled toolRootSlotId installedComponent.ID false)
        else
            None

    | None ->
        match if usingDiscoveredLiveIds then None else generatedRuntimeIds with
        | None -> None
        | Some(_, avatarRootSlotId, _) when String.IsNullOrWhiteSpace avatarRootSlotId ->
            None
        | Some(toolRootSlotId, avatarRootSlotId, installedComponentId) ->
            let toolRoot = getSlot toolRootSlotId 0 false
            let installed =
                not (isNull toolRoot.Parent)
                && toolRoot.Parent.TargetID = avatarRootSlotId
            Some(updateInstalled toolRootSlotId installedComponentId installed)

let reconciledInstalled = reconcileInstalledState ()
printfn "Deployed BYO Haptics sheets: %d" moduleResponseCounts.Length
for modulePath, responseCount in moduleResponseCounts do
    printfn "  %s response count: %d" modulePath responseCount
printfn "Patched Flux input GlobalReferences: %d" patchedInputReferences
printfn "Patched Flux output drives: %d" patchedOutputDrives
printfn "Patched connection LED references: %d" patchedConnectionLed
printfn "Patched plugin-present LED references: %d" patchedPluginPresentLed
printfn "Recovered live Flux input references: %d" recoveredLiveInputs
printfn "Removed previous BYO Haptics sheet graphs: %d" removedPreviousModules
printfn "Arranged BYO Haptics ModuPrint sheets: %d" arrangedSheets
match reconciledInstalled with
| Some value -> printfn "Reconciled Installed from parent: %b" value
| None -> ()

