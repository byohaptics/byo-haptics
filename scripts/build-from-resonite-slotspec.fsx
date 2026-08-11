#r "nuget: YellowDogMan.ResoniteLink, 0.13.1"

open System
open System.Collections.Generic
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
open ResoniteLink

let projectDir = Environment.CurrentDirectory

let envOr name fallback =
    let value = Environment.GetEnvironmentVariable name
    if String.IsNullOrWhiteSpace value then fallback else value

let validateOnly = Environment.GetEnvironmentVariable "RESONITE_SLOTSPEC_VALIDATE_ONLY" = "1"
let resoniteLinkUrl =
    if validateOnly then ""
    else
        match Environment.GetEnvironmentVariable "RESONITELINK_URL" with
        | null | "" -> failwith "RESONITELINK_URL is required. Discover the current session endpoint before building."
        | value -> value
let specPath = envOr "RESONITE_SLOTSPEC_PATH" "specs/byo-haptics.resoslots.json"
let outputPath = envOr "RESONITE_SLOTSPEC_OUTPUT" "build/byo-haptics.generated-ids.json"
let recreate = Environment.GetEnvironmentVariable "RESONITE_SLOTSPEC_RECREATE" = "1"

let fullSpecPath = Path.GetFullPath(Path.Combine(projectDir, specPath))
if not (File.Exists fullSpecPath) then
    failwithf "SlotSpec not found: %s" fullSpecPath

let document =
    use stream = File.OpenRead fullSpecPath
    JsonDocument.Parse(stream)

let root = document.RootElement

let prop (name: string) (e: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if e.ValueKind = JsonValueKind.Object && e.TryGetProperty(name, &value) then Some value else None

let reqProp name e =
    match prop name e with
    | Some value -> value
    | None -> failwithf "Missing required property '%s'" name

let strProp name e = (reqProp name e).GetString()
let optStrProp name e = prop name e |> Option.map _.GetString()
let boolProp name e = (reqProp name e).GetBoolean()
let intProp name e = (reqProp name e).GetInt32()
let floatProp name e = (reqProp name e).GetSingle()

let sanitizeIdPart (value: string) =
    value
    |> Seq.map (fun c -> if Char.IsLetterOrDigit c then c else '_')
    |> Seq.toArray
    |> String
    |> fun s -> s.Trim('_')
    |> fun s -> if String.IsNullOrWhiteSpace s then "node" else s

let runPrefix = "SlotSpec_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() + "_"

let splitPath (path: string) =
    path.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries) |> Array.toList

let normalizeName (value: string) =
    if isNull value then "" else value.Trim()

let makeFloat2 (x: single) (y: single) =
    let mutable v = Unchecked.defaultof<float2>
    v.x <- x
    v.y <- y
    v

let makeFloat3 (x: single) (y: single) (z: single) =
    let mutable v = Unchecked.defaultof<float3>
    v.x <- x
    v.y <- y
    v.z <- z
    v

let makeFloat4 (x: single) (y: single) (z: single) (w: single) =
    let mutable v = Unchecked.defaultof<float4>
    v.x <- x
    v.y <- y
    v.z <- z
    v.w <- w
    v

let makeColorX (r: single) (g: single) (b: single) (a: single) (profile: string) =
    let mutable v = Unchecked.defaultof<colorX>
    v.r <- r
    v.g <- g
    v.b <- b
    v.a <- a
    v.Profile <- profile
    v

let makeFloatQ (x: single) (y: single) (z: single) (w: single) =
    let mutable v = Unchecked.defaultof<floatQ>
    v.x <- x
    v.y <- y
    v.z <- z
    v.w <- w
    v

let fieldBool value =
    let f = Field_bool()
    f.Value <- value
    f :> Member

let fieldInt value =
    let f = Field_int()
    f.Value <- value
    f :> Member

let fieldFloat value =
    let f = Field_float()
    f.Value <- value
    f :> Member

let fieldString value =
    let f = Field_string()
    f.Value <- value
    f :> Member

let fieldUri value =
    let f = Field_Uri()
    f.Value <- Uri value
    f :> Member

let fieldUriValue (value: Uri) =
    let f = Field_Uri()
    f.Value <- value
    f :> Member

let fieldFloat2 x y =
    let f = Field_float2()
    f.Value <- makeFloat2 x y
    f :> Member

let fieldFloat3 x y z =
    let f = Field_float3()
    f.Value <- makeFloat3 x y z
    f :> Member

let fieldFloat4 x y z w =
    let f = Field_float4()
    f.Value <- makeFloat4 x y z w
    f :> Member

let fieldFloatQ x y z w =
    let f = Field_floatQ()
    f.Value <- makeFloatQ x y z w
    f :> Member

let fieldColorX r g b a profile =
    let f = Field_colorX()
    f.Value <- makeColorX r g b a profile
    f :> Member

let fieldEnum enumType value =
    let f = Field_Enum()
    f.EnumType <- enumType
    f.Value <- value
    f :> Member

let reference targetId targetType =
    let r = Reference()
    r.TargetID <- targetId
    r.TargetType <- targetType
    r :> Member

let referenceList targetIds targetType =
    let list = SyncList()
    list.Elements <-
        targetIds
        |> Seq.map (fun targetId -> reference targetId targetType)
        |> ResizeArray
    list :> Member

let fieldList (members: seq<Member>) =
    let list = SyncList()
    list.Elements <- members |> ResizeArray
    list :> Member

let link =
    if validateOnly then Unchecked.defaultof<LinkInterface>
    else
        let value = new LinkInterface()
        value.Connect(Uri resoniteLinkUrl, CancellationToken.None).GetAwaiter().GetResult()
        value

let texture2DRawDataUri (value: JsonElement) =
    let request = ImportTexture2DRawData()
    request.Width <- intProp "width" value
    request.Height <- intProp "height" value
    request.ColorProfile <- optStrProp "colorProfile" value |> Option.defaultValue "sRGB"
    request.RawBinaryPayload <-
        reqProp "rgba" value
        |> fun bytes -> bytes.EnumerateArray()
        |> Seq.map (fun item -> byte (item.GetInt32()))
        |> Seq.toArray
    let response = link.ImportTexture(request).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo
    response.AssetURL

let rec memberFromSpec (e: JsonElement) =
    let t = strProp "type" e
    let value = reqProp "value" e
    match t with
    | "bool" -> fieldBool (value.GetBoolean())
    | "int" -> fieldInt (value.GetInt32())
    | "float" -> fieldFloat (value.GetSingle())
    | "string" -> fieldString (value.GetString())
    | "uri" -> fieldUri (value.GetString())
    | "float2" ->
        let x = floatProp "x" value
        let y = floatProp "y" value
        fieldFloat2 x y
    | "float3" ->
        let x = floatProp "x" value
        let y = floatProp "y" value
        let z = floatProp "z" value
        fieldFloat3 x y z
    | "float4" ->
        let x = floatProp "x" value
        let y = floatProp "y" value
        let z = floatProp "z" value
        let w = floatProp "w" value
        fieldFloat4 x y z w
    | "floatQ" ->
        let x = floatProp "x" value
        let y = floatProp "y" value
        let z = floatProp "z" value
        let w = floatProp "w" value
        fieldFloatQ x y z w
    | "colorX" ->
        let r = floatProp "r" value
        let g = floatProp "g" value
        let b = floatProp "b" value
        let a = floatProp "a" value
        let profile = optStrProp "profile" value |> Option.defaultValue "sRGB"
        fieldColorX r g b a profile
    | "enum" ->
        let enumType = strProp "enumType" e
        fieldEnum enumType (value.GetString())
    | "stringList" ->
        value.EnumerateArray()
        |> Seq.map (fun item -> fieldString (item.GetString()))
        |> fieldList
    | "float3List" ->
        value.EnumerateArray()
        |> Seq.map (fun item ->
            fieldFloat3 (floatProp "x" item) (floatProp "y" item) (floatProp "z" item))
        |> fieldList
    | "syncObject" ->
        let syncObject = SyncObject()
        syncObject.Members <- Dictionary<string, Member>()
        for memberProp in value.EnumerateObject() do
            syncObject.Members[memberProp.Name] <- memberFromSpec memberProp.Value
        syncObject :> Member
    | "texture2DRawData" ->
        texture2DRawDataUri value |> fieldUriValue
    | other -> failwithf "Unsupported member type '%s'" other

if validateOnly then
    printfn "SlotSpec builder compiled and loaded: %s" fullSpecPath
    Environment.Exit 0

let fieldRefParts (fieldRef: string) =
    let index = fieldRef.LastIndexOf('.')
    if index <= 0 || index = fieldRef.Length - 1 then
        failwithf "Invalid fieldRef '%s'. Expected componentAlias.MemberName." fieldRef
    fieldRef.Substring(0, index), fieldRef.Substring(index + 1)

let schema = strProp "schema" root
if schema <> "resonite-slotspec/v1" then
    failwithf "Unsupported schema '%s'" schema

let toolName = strProp "toolName" root
let toolParentKind =
    prop "toolParent" root
    |> Option.bind (optStrProp "kind")
    |> Option.defaultValue "avatarRoot"
let avatarRootName =
    match prop "avatarRoot" root with
    | Some avatarRootSpec -> strProp "name" avatarRootSpec |> normalizeName
    | None when toolParentKind = "worldRoot" -> ""
    | None -> failwith "Missing required property 'avatarRoot' for avatarRoot tool parent."
let slotsArray = reqProp "slots" root

let getSlot slotId depth includeComponents =
    let req = GetSlot()
    req.SlotID <- slotId
    req.Depth <- depth
    req.IncludeComponentData <- includeComponents
    let response = link.GetSlotData(req).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo
    response.Data

let childByName (parent: Slot) (name: string) =
    let parentWithChildren = getSlot parent.ID 1 false
    if isNull parentWithChildren.Children then None
    else parentWithChildren.Children |> Seq.tryFind (fun c -> not (isNull c.Name) && normalizeName c.Name.Value = name)

let resolvePathUnder (parent: Slot) (path: string) =
    let parts = splitPath path |> List.map normalizeName
    ((Some parent), parts)
    ||> List.fold (fun current part ->
        current |> Option.bind (fun p -> childByName p part))

let tryGeneratedSlot propertyName =
    try
        let generatedPath = Path.GetFullPath(Path.Combine(projectDir, outputPath))
        if not (File.Exists generatedPath) then None
        else
            use generatedDoc = JsonDocument.Parse(File.ReadAllText generatedPath)
            match prop propertyName generatedDoc.RootElement with
            | Some value ->
                let slotId = value.GetString()
                if String.IsNullOrWhiteSpace slotId then None else Some (getSlot slotId 1 false)
            | None -> None
    with _ ->
        None

let rec findSlotByName expectedName parentId remainingDepth =
    if remainingDepth < 0 then None
    else
        let parent = getSlot parentId 1 false
        if not (isNull parent.Name) && normalizeName parent.Name.Value = expectedName then Some parent
        elif isNull parent.Children then None
        else
            parent.Children
            |> Seq.tryPick (fun child ->
                if not (isNull child.Name) && normalizeName child.Name.Value = expectedName then Some (getSlot child.ID 1 false)
                else findSlotByName expectedName child.ID (remainingDepth - 1))

let worldRoot = getSlot "Root" 1 false
let resolveAvatarRoot () =
    tryGeneratedSlot "avatarRootSlotId"
    |> Option.orElseWith (fun () -> findSlotByName avatarRootName "Root" 8)
    |> Option.defaultWith (fun () -> failwithf "Avatar root named '%s' was not found." avatarRootName)

let toolParent =
    match toolParentKind with
    | "avatarRoot" -> resolveAvatarRoot ()
    | "worldRoot" -> worldRoot
    | other -> failwithf "Unsupported toolParent.kind '%s'. Expected avatarRoot or worldRoot." other

let existingTool =
    tryGeneratedSlot "toolRootSlotId"
    |> Option.orElseWith (fun () -> resolvePathUnder toolParent toolName)
    |> Option.orElseWith (fun () -> findSlotByName toolName "Root" 8)
match existingTool with
| Some slot when recreate ->
    let remove = RemoveSlot()
    remove.SlotID <- slot.ID
    let response = link.RemoveSlot(remove).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo
| Some slot ->
    failwithf "Tool slot '%s' already exists at %s. Set RESONITE_SLOTSPEC_RECREATE=1 to remove it first." toolName slot.ID
| None -> ()

let explicitSlots =
    slotsArray.EnumerateArray()
    |> Seq.map (fun e -> strProp "path" e, e)
    |> dict

let allPaths =
    seq {
        yield ""
        for entry in slotsArray.EnumerateArray() do
            let path = strProp "path" entry
            let parts = splitPath path
            for i in 1 .. parts.Length do
                yield String.Join("/", parts |> List.take i)
    }
    |> Seq.distinct
    |> Seq.sortBy (fun p -> (splitPath p).Length, p)
    |> Seq.toArray

let slotIds = Dictionary<string, string>()
let aliasToComponentId = Dictionary<string, string>()
let componentAliasToType = Dictionary<string, string>()

for slotEntry in slotsArray.EnumerateArray() do
    match prop "components" slotEntry with
    | None -> ()
    | Some components ->
        for componentEntry in components.EnumerateArray() do
            let alias = strProp "alias" componentEntry
            if aliasToComponentId.ContainsKey alias then failwithf "Duplicate component alias: %s" alias
            aliasToComponentId[alias] <- runPrefix + sanitizeIdPart alias
            componentAliasToType[alias] <- strProp "type" componentEntry

let idForPath path =
    match slotIds.TryGetValue path with
    | true, id -> id
    | false, _ ->
        let id = runPrefix + if path = "" then "tool" else sanitizeIdPart path
        slotIds[path] <- id
        id

let makeSlot path =
    let slot = Slot()
    slot.ID <- idForPath path
    slot.Name <- Field_string(Value = if path = "" then toolName else (splitPath path |> List.last))
    let tag =
        if path = "" then
            optStrProp "tag" root |> Option.defaultValue ""
        else
            match explicitSlots.TryGetValue path with
            | true, entry -> optStrProp "tag" entry |> Option.defaultValue ""
            | false, _ -> ""
    slot.Tag <- Field_string(Value = tag)
    let isActive =
        match explicitSlots.TryGetValue path with
        | true, entry ->
            match prop "isActive" entry with
            | Some value -> value.GetBoolean()
            | None -> true
        | false, _ -> true
    slot.IsActive <- Field_bool(Value = isActive)
    slot.IsPersistent <- Field_bool(Value = true)
    let orderOffset =
        match explicitSlots.TryGetValue path with
        | true, entry ->
            match prop "orderOffset" entry with
            | Some o -> int64 (o.GetInt32())
            | None -> 0L
        | false, _ -> 0L
    slot.OrderOffset <- Field_long(Value = orderOffset)
    let scale =
        match explicitSlots.TryGetValue path with
        | true, entry ->
            match prop "scale" entry with
            | Some s -> makeFloat3 (floatProp "x" s) (floatProp "y" s) (floatProp "z" s)
            | None -> makeFloat3 1.0f 1.0f 1.0f
        | false, _ -> makeFloat3 1.0f 1.0f 1.0f
    slot.Scale <- Field_float3(Value = scale)
    let rotation =
        match explicitSlots.TryGetValue path with
        | true, entry ->
            match prop "rotation" entry with
            | Some r -> makeFloatQ (floatProp "x" r) (floatProp "y" r) (floatProp "z" r) (floatProp "w" r)
            | None -> makeFloatQ 0.0f 0.0f 0.0f 1.0f
        | false, _ -> makeFloatQ 0.0f 0.0f 0.0f 1.0f
    slot.Rotation <- Field_floatQ(Value = rotation)
    let position =
        match explicitSlots.TryGetValue path with
        | true, entry ->
            match prop "position" entry with
            | Some p -> makeFloat3 (floatProp "x" p) (floatProp "y" p) (floatProp "z" p)
            | None -> makeFloat3 0.0f 0.0f 0.0f
        | false, _ -> makeFloat3 0.0f 0.0f 0.0f
    slot.Position <- Field_float3(Value = position)
    let parentId =
        if path = "" then toolParent.ID
        else
            let parts = splitPath path
            let parentPath = String.Join("/", parts |> List.take (parts.Length - 1))
            idForPath parentPath
    slot.Parent <- Reference(TargetID = parentId, TargetType = "[FrooxEngine]FrooxEngine.Slot")
    slot

let runBatch (operations: ResizeArray<DataModelOperation>) =
    if operations.Count > 0 then
        let result = link.RunDataModelOperationBatch(operations).GetAwaiter().GetResult()
        if not result.Success then failwith result.ErrorInfo
        for response in result.Responses do
            if not response.Success then failwith response.ErrorInfo

let slotOps = ResizeArray<DataModelOperation>()
for path in allPaths do
    let op = AddSlot()
    op.Data <- makeSlot path
    slotOps.Add(op :> DataModelOperation)

runBatch slotOps

let validateComponentType componentType =
    let response = link.GetComponentDefinition(componentType, true).GetAwaiter().GetResult()
    if not response.Success then
        failwithf "Component type did not resolve: %s\n%s" componentType response.ErrorInfo

let componentTypes =
    slotsArray.EnumerateArray()
    |> Seq.collect (fun slot ->
        match prop "components" slot with
        | Some comps -> comps.EnumerateArray() |> Seq.map (strProp "type")
        | None -> Seq.empty)
    |> Seq.distinct

for componentType in componentTypes do
    validateComponentType componentType

let componentEntries =
    [| for slotEntry in slotsArray.EnumerateArray() do
           let path = strProp "path" slotEntry
           match prop "components" slotEntry with
           | None -> ()
           | Some components ->
               for componentEntry in components.EnumerateArray() do yield path, componentEntry |]

let hasAtomicComponentRef (_, componentEntry: JsonElement) =
    match prop "members" componentEntry with
    | None -> false
    | Some members ->
        members.EnumerateObject()
        |> Seq.exists (fun memberProp ->
            let memberType = strProp "type" memberProp.Value
            memberType = "componentRef" || memberType = "componentRefList")

let deferredAliases =
    componentEntries
    |> Seq.filter hasAtomicComponentRef
    |> Seq.map (fun (_, entry) -> strProp "alias" entry)
    |> Set.ofSeq

let componentOperation (path, componentEntry: JsonElement) =
            let alias = strProp "alias" componentEntry
            let componentType = strProp "type" componentEntry
            let componentId = aliasToComponentId[alias]
            let compData = Component()
            compData.ID <- componentId
            compData.ComponentType <- componentType
            compData.Members <- Dictionary<string, Member>()
            match prop "members" componentEntry with
            | Some members ->
                for memberProp in members.EnumerateObject() do
                    let memberType = strProp "type" memberProp.Value
                    if memberType = "componentRef" then
                        let targetAlias = strProp "component" memberProp.Value
                        if not (aliasToComponentId.ContainsKey targetAlias) then
                            failwithf "Unknown component alias in atomic componentRef: %s" targetAlias
                        if deferredAliases.Contains targetAlias then
                            failwithf "Atomic componentRef target must not itself be deferred: %s" targetAlias
                        let targetType = strProp "targetType" memberProp.Value
                        compData.Members[memberProp.Name] <- reference aliasToComponentId[targetAlias] targetType
                    elif memberType = "componentRefList" then
                        let targetAliases =
                            reqProp "components" memberProp.Value
                            |> fun components -> components.EnumerateArray()
                            |> Seq.map _.GetString()
                            |> Seq.toArray
                        for targetAlias in targetAliases do
                            if not (aliasToComponentId.ContainsKey targetAlias) then
                                failwithf "Unknown component alias in atomic componentRefList: %s" targetAlias
                            if deferredAliases.Contains targetAlias then
                                failwithf "Atomic componentRefList target must not itself be deferred: %s" targetAlias
                        let targetType = strProp "targetType" memberProp.Value
                        compData.Members[memberProp.Name] <-
                            referenceList (targetAliases |> Seq.map (fun alias -> aliasToComponentId[alias])) targetType
                    else
                        compData.Members[memberProp.Name] <- memberFromSpec memberProp.Value
            | None -> ()
            let op = AddComponent()
            op.ContainerSlotId <- idForPath path
            op.Data <- compData
            op :> DataModelOperation

componentEntries
|> Seq.filter (hasAtomicComponentRef >> not)
|> Seq.map componentOperation
|> ResizeArray
|> runBatch

componentEntries
|> Seq.filter hasAtomicComponentRef
|> Seq.map componentOperation
|> ResizeArray
|> runBatch

let getComponent componentId =
    let rec attempt remaining =
        let req = GetComponent()
        req.ComponentID <- componentId
        let response = link.GetComponentData(req).GetAwaiter().GetResult()
        if isNull response then
            if remaining <= 0 then failwithf "GetComponent returned null: %s" componentId
            Thread.Sleep 100
            attempt (remaining - 1)
        elif not response.Success then
            if remaining <= 0 then failwith response.ErrorInfo
            Thread.Sleep 100
            attempt (remaining - 1)
        elif isNull response.Data then
            if remaining <= 0 then failwithf "GetComponent returned null data: %s" componentId
            Thread.Sleep 100
            attempt (remaining - 1)
        else response.Data
    attempt 20

let componentSnapshots = Dictionary<string, Component>()
for KeyValue(alias, componentId) in aliasToComponentId do
    componentSnapshots[alias] <- getComponent componentId

let memberDictionary (owner: obj) =
    let property = owner.GetType().GetProperty("Members")
    if isNull property then
        failwithf "Member '%s' has no nested Members collection." (owner.GetType().FullName)
    match property.GetValue(owner) with
    | :? IDictionary<string, Member> as members -> members
    | _ -> failwithf "Member '%s' has an unsupported Members collection." (owner.GetType().FullName)

let resolveFieldRef (fieldRef: string) =
    let alias =
        componentSnapshots.Keys
        |> Seq.filter (fun (candidate: string) -> fieldRef.StartsWith(candidate + ".", StringComparison.Ordinal))
        |> Seq.sortByDescending (fun (candidate: string) -> candidate.Length)
        |> Seq.tryHead
        |> Option.defaultWith (fun () -> failwithf "Unknown component alias in fieldRef: %s" fieldRef)
    let memberPath =
        fieldRef.Substring(alias.Length + 1).Split('.', StringSplitOptions.RemoveEmptyEntries)
        |> Array.toList
    if List.isEmpty memberPath then failwithf "Invalid fieldRef '%s'." fieldRef
    let rec resolve owner remaining =
        match remaining with
        | [] -> failwithf "Invalid fieldRef '%s'." fieldRef
        | memberName :: tail ->
            let members = memberDictionary owner
            if isNull members || not (members.ContainsKey memberName) then
                failwithf "Component alias '%s' does not contain member path '%s'." alias (String.Join(".", memberPath))
            let memberData = members[memberName]
            if List.isEmpty tail then memberData.ID else resolve (memberData :> obj) tail
    resolve (componentSnapshots[alias] :> obj) memberPath

let resolveSlotPath path =
    if not (slotIds.ContainsKey path) then failwithf "Unknown SlotSpec path: %s" path
    slotIds[path]

let resolveSlotFieldRef path memberName =
    let slotData = getSlot (resolveSlotPath path) 0 false
    let property = typeof<Slot>.GetProperty(memberName)
    if isNull property then
        failwithf "Slot does not contain field member '%s'." memberName
    match property.GetValue(slotData) with
    | :? Member as memberData -> memberData.ID
    | _ -> failwithf "Slot member '%s' is not a referenceable field." memberName

let updateReferenceMember componentAlias memberName targetId targetType =
    if not (aliasToComponentId.ContainsKey componentAlias) then failwithf "Unknown component alias: %s" componentAlias
    let compData = Component()
    compData.ID <- aliasToComponentId[componentAlias]
    compData.Members <- Dictionary<string, Member>()
    compData.Members[memberName] <- reference targetId targetType
    let req = UpdateComponent()
    req.Data <- compData
    let response = link.UpdateComponent(req).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo

match prop "slotReferences" root with
| Some refs when refs.GetArrayLength() > 0 ->
    let avatarRoot = resolveAvatarRoot ()
    for refSpec in refs.EnumerateArray() do
        let componentAlias = strProp "component" refSpec
        let memberName = strProp "member" refSpec
        let targetPath = strProp "avatarSlotPath" refSpec
        let targetSlot =
            resolvePathUnder avatarRoot targetPath
            |> Option.defaultWith (fun () -> failwithf "Avatar slot path was not found: %s" targetPath)
        updateReferenceMember componentAlias memberName targetSlot.ID "[FrooxEngine]FrooxEngine.Slot"
| _ -> ()

match prop "localSlotReferences" root with
| Some refs ->
    for refSpec in refs.EnumerateArray() do
        let componentAlias = strProp "component" refSpec
        let memberName = strProp "member" refSpec
        let targetPath = strProp "slotPath" refSpec
        let targetType =
            optStrProp "targetType" refSpec
            |> Option.defaultValue "[FrooxEngine]FrooxEngine.Slot"
        updateReferenceMember componentAlias memberName (resolveSlotPath targetPath) targetType
| None -> ()

match prop "fieldReferences" root with
| Some refs ->
    for refSpec in refs.EnumerateArray() do
        let componentAlias = strProp "component" refSpec
        let memberName = strProp "member" refSpec
        let targetFieldId = strProp "fieldRef" refSpec |> resolveFieldRef
        let targetType =
            optStrProp "targetType" refSpec
            |> Option.defaultValue "[FrooxEngine]FrooxEngine.IField`1[[System.Boolean, System.Private.CoreLib]]"
        try
            updateReferenceMember componentAlias memberName targetFieldId targetType
        with ex ->
            failwithf
                "Failed fieldReferences entry: component=%s member=%s fieldRef=%s targetType=%s. %s"
                componentAlias
                memberName
                (strProp "fieldRef" refSpec)
                targetType
                ex.Message
| None -> ()

match prop "slotFieldReferences" root with
| Some refs ->
    for refSpec in refs.EnumerateArray() do
        let componentAlias = strProp "component" refSpec
        let memberName = strProp "member" refSpec
        let targetPath = strProp "slotPath" refSpec
        let slotMember = strProp "slotMember" refSpec
        let targetType =
            optStrProp "targetType" refSpec
            |> Option.defaultValue "[FrooxEngine]FrooxEngine.IField`1[[System.Boolean, System.Private.CoreLib]]"
        updateReferenceMember componentAlias memberName (resolveSlotFieldRef targetPath slotMember) targetType
| None -> ()

match prop "componentReferences" root with
| Some refs ->
    for refSpec in refs.EnumerateArray() do
        let componentAlias = strProp "component" refSpec
        let memberName = strProp "member" refSpec
        let targetAlias = strProp "componentRef" refSpec
        let targetType = strProp "targetType" refSpec
        if not (aliasToComponentId.ContainsKey targetAlias) then failwithf "Unknown componentRef: %s" targetAlias
        updateReferenceMember componentAlias memberName aliasToComponentId[targetAlias] targetType
| None -> ()

match prop "builtinReferences" root with
| Some refs when refs.GetArrayLength() > 0 ->
    failwith "builtinReferences is no longer supported. Package assets in the SlotSpec and use componentReferences instead."
| None -> ()
| Some _ -> ()

let resolveScenePath (path: string) =
    let parts =
        splitPath path
        |> function
            | "Root" :: tail -> tail
            | other -> other
    let rootSlot = getSlot "Root" 1 false
    ((Some rootSlot), parts)
    ||> List.fold (fun current part ->
        current |> Option.bind (fun p -> childByName p part))

match prop "sceneSlotReferences" root with
| Some refs ->
    for refSpec in refs.EnumerateArray() do
        let componentAlias = strProp "component" refSpec
        let memberName = strProp "member" refSpec
        let sceneSlotPath = strProp "sceneSlotPath" refSpec
        let targetType =
            optStrProp "targetType" refSpec
            |> Option.defaultValue "[FrooxEngine]FrooxEngine.Slot"
        let sceneSlot =
            resolveScenePath sceneSlotPath
            |> Option.defaultWith (fun () -> failwithf "Scene slot path was not found: %s" sceneSlotPath)
        updateReferenceMember componentAlias memberName sceneSlot.ID targetType
| None -> ()

let rec sceneComponentsByTypeUnder slotId remainingDepth componentType slotName =
    seq {
        if remainingDepth >= 0 then
            let slot = getSlot slotId 1 true
            let slotMatches =
                match slotName with
                | Some expected ->
                    not (isNull slot.Name) && normalizeName slot.Name.Value = expected
                | None -> true
            if slotMatches && not (isNull slot.Components) then
                for c in slot.Components do
                    if c.ComponentType = componentType then yield c
            if remainingDepth > 0 && not (isNull slot.Children) then
                for child in slot.Children do
                    yield! sceneComponentsByTypeUnder child.ID (remainingDepth - 1) componentType slotName
    }

match prop "sceneComponentReferences" root with
| Some refs ->
    for refSpec in refs.EnumerateArray() do
        let componentAlias = strProp "component" refSpec
        let memberName = strProp "member" refSpec
        let sceneSlotPath = strProp "sceneSlotPath" refSpec
        let componentType = strProp "componentType" refSpec
        let targetType = strProp "targetType" refSpec
        let slotName = optStrProp "slotName" refSpec |> Option.map normalizeName
        let componentIndex =
            match prop "componentIndex" refSpec with
            | Some value -> value.GetInt32()
            | None -> 0
        match resolveScenePath sceneSlotPath with
        | None ->
            printfn "Warning: scene slot path was not found; skipped scene component reference: path=%s type=%s" sceneSlotPath componentType
        | Some sceneSlot ->
            let matchingComponents =
                match prop "recursiveDepth" refSpec with
                | Some depth ->
                    sceneComponentsByTypeUnder sceneSlot.ID (depth.GetInt32()) componentType slotName
                    |> Seq.toArray
                | None ->
                    // ResoniteLink may omit component data for Depth=0 even when
                    // IncludeComponentData is true. Depth=1 reliably includes the
                    // target slot's components without requiring recursion.
                    let sceneSlotWithComponents = getSlot sceneSlot.ID 1 true
                    if isNull sceneSlotWithComponents.Components then Array.empty
                    else
                        sceneSlotWithComponents.Components
                        |> Seq.filter (fun c -> c.ComponentType = componentType)
                        |> Seq.toArray
            if componentIndex < 0 || componentIndex >= matchingComponents.Length then
                printfn "Warning: scene component was not found; skipped scene component reference: path=%s type=%s index=%d" sceneSlotPath componentType componentIndex
            else
                updateReferenceMember componentAlias memberName matchingComponents[componentIndex].ID targetType
| None -> ()

// Refresh snapshots after deferred references have been applied.
componentSnapshots.Clear()
for KeyValue(alias, componentId) in aliasToComponentId do
    componentSnapshots[alias] <- getComponent componentId

let generated = JsonObject()
generated["schema"] <- JsonValue.Create("resonite-slotspec-generated-ids/v1")
generated["resoniteLinkUrl"] <- JsonValue.Create(resoniteLinkUrl)
generated["toolName"] <- JsonValue.Create(toolName)
generated["toolRootSlotId"] <- JsonValue.Create(resolveSlotPath "")
generated["avatarRootSlotId"] <-
    JsonValue.Create(if toolParentKind = "avatarRoot" then (resolveAvatarRoot ()).ID else "")
generated["toolParentKind"] <- JsonValue.Create(toolParentKind)
generated["fluxSlotId"] <- JsonValue.Create(strProp "fluxSlotPath" root |> resolveSlotPath)

let slotsJson = JsonObject()
for KeyValue(path, id) in slotIds do
    slotsJson[path] <- JsonValue.Create(id)
generated["slots"] <- slotsJson

let componentsJson = JsonObject()
for KeyValue(alias, id) in aliasToComponentId do
    componentsJson[alias] <- JsonValue.Create(id)
generated["components"] <- componentsJson

let membersJson = JsonObject()
for KeyValue(alias, compData) in componentSnapshots do
    let item = JsonObject()
    if not (isNull compData.Members) then
        for KeyValue(memberName, memberData) in compData.Members do
            item[memberName] <- JsonValue.Create(memberData.ID)
    membersJson[alias] <- item
generated["members"] <- membersJson

let fluxInputsJson = JsonObject()
match prop "fluxInputs" root with
| Some fluxInputs ->
    for inputProp in fluxInputs.EnumerateObject() do
        let input = inputProp.Value
        let id =
            match prop "slotField" input with
            | Some slotField ->
                resolveSlotFieldRef (strProp "slotPath" slotField) (strProp "slotMember" slotField)
            | None ->
                match prop "fieldRef" input, prop "slotPath" input, prop "componentRef" input, prop "sceneSlotPath" input, prop "avatarSlotPath" input, prop "avatarRoot" input with
                | Some fieldRef, _, _, _, _, _ -> resolveFieldRef (fieldRef.GetString())
                | _, Some slotPath, _, _, _, _ -> resolveSlotPath (slotPath.GetString())
                | _, _, Some componentRef, _, _, _ ->
                    let alias = componentRef.GetString()
                    if not (aliasToComponentId.ContainsKey alias) then failwithf "Unknown componentRef: %s" alias
                    aliasToComponentId[alias]
                | _, _, _, Some sceneSlotPath, _, _ ->
                    resolveScenePath (sceneSlotPath.GetString())
                    |> Option.map (fun slot -> slot.ID)
                    |> Option.defaultWith (fun () -> failwithf "Flux input scene slot path was not found: %s" (sceneSlotPath.GetString()))
                | _, _, _, _, Some avatarSlotPath, _ ->
                    resolvePathUnder (resolveAvatarRoot ()) (avatarSlotPath.GetString())
                    |> Option.map (fun slot -> slot.ID)
                    |> Option.defaultWith (fun () -> failwithf "Flux input avatar slot path was not found: %s" (avatarSlotPath.GetString()))
                | _, _, _, _, _, Some useAvatarRoot when useAvatarRoot.GetBoolean() -> (resolveAvatarRoot ()).ID
                | _ -> failwithf "fluxInputs.%s must specify fieldRef, slotPath, slotField, componentRef, sceneSlotPath, avatarSlotPath, or avatarRoot=true." inputProp.Name
        fluxInputsJson[inputProp.Name] <- JsonValue.Create(id)
| None -> ()
generated["fluxInputs"] <- fluxInputsJson

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))) |> ignore
File.WriteAllText(outputPath, generated.ToJsonString(JsonSerializerOptions(WriteIndented = true)))

printfn "Built %s from %s" toolName fullSpecPath
printfn "Tool root: %s" (resolveSlotPath "")
printfn "Flux slot: %s" (generated["fluxSlotId"].GetValue<string>())
printfn "Generated IDs: %s" (Path.GetFullPath outputPath)

link.Dispose()
