#r "nuget: YellowDogMan.ResoniteLink, 0.13.1"
#load "slotspec-build-safety.fsx"

open System
open System.IO
open System.Text.Json
open System.Threading
open ResoniteLink
open SlotSpecBuildSafety

let env name =
    match Environment.GetEnvironmentVariable name with
    | null | "" -> failwithf "Set %s." name
    | value -> value

let outputPath = env "RESONITE_SLOTSPEC_OUTPUT"
let pendingPath = outputPath + ".pending.json"
let contents = File.ReadAllText pendingPath
let document = JsonDocument.Parse contents
let root = document.RootElement
let str (name: string) = root.GetProperty(name).GetString()
let url = env "RESONITELINK_URL"
if Uri(url) <> Uri(str "resoniteLinkUrl") then failwith "Pending IDs belong to a different endpoint."

let link = new LinkInterface()
link.Connect(Uri url, CancellationToken.None).GetAwaiter().GetResult()
let getRoot id =
    let response = link.GetSlotData(GetSlot(SlotID = id, Depth = 1, IncludeComponentData = false)).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo
    let slot = response.Data
    { Id = slot.ID; Parent = slot.Parent.TargetID; Name = slot.Name.Value
      Tag = slot.Tag.Value; Active = slot.IsActive.Value }
let updateRoot (value: Root) =
    let slot = Slot(ID = value.Id, Name = Field_string(Value = value.Name), IsActive = Field_bool(Value = value.Active))
    let response = link.UpdateSlot(UpdateSlot(Data = slot)).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo
let removeRoot id =
    let previous = getRoot id
    verifyTarget (str "toolParentSlotId") (str "toolName") (str "toolTag") (previous.Parent, previous.Name, previous.Tag)
    let response = link.RemoveSlot(RemoveSlot(SlotID = id)).GetAwaiter().GetResult()
    if not response.Success then failwith response.ErrorInfo

let parentResponse = link.GetSlotData(GetSlot(SlotID = str "toolParentSlotId", Depth = 1, IncludeComponentData = false)).GetAwaiter().GetResult()
if not parentResponse.Success then failwith parentResponse.ErrorInfo
let children =
    if isNull parentResponse.Data.Children then []
    else parentResponse.Data.Children |> Seq.map (fun slot -> slot.ID, slot.Name.Value, slot.Tag.Value) |> Seq.toList
let selected = selectExisting (str "toolName") (str "toolTag") children |> Option.defaultValue ""
if selected <> str "replacedToolRootSlotId" then failwith "Replacement candidates changed; rebuild before finalizing."

finalize (str "toolParentSlotId") (str "toolName") (str "toolTag")
    (str "toolRootSlotId") (str "replacedToolRootSlotId") (root.GetProperty("toolRootIsActive").GetBoolean())
    getRoot updateRoot removeRoot
File.Move(pendingPath, outputPath, true)
printfn "Finalized %s; generated IDs: %s" (str "toolName") outputPath
link.Dispose()
document.Dispose()
