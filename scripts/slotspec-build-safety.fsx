module SlotSpecBuildSafety

// Candidate lists must contain only direct children of the declared parent.
let selectExisting name tag (children: (string * string * string) list) =
    match children |> List.filter (fun (_, childName, _) -> childName = name) with
    | [] -> None
    | [ (id, _, childTag) ] when childTag = tag -> Some id
    | [ _ ] -> failwithf "Refusing to replace '%s': tag does not match the specification." name
    | _ -> failwithf "Ambiguous replacement: multiple direct children named '%s'." name

let verifyTarget expectedParent expectedName expectedTag (parent, name, tag) =
    if parent <> expectedParent || name <> expectedName || tag <> expectedTag then
        failwith "Replacement target moved or changed; rebuild before finalizing."

let stagingName name = name + " (building)"

type Root = { Id: string; Parent: string; Name: string; Tag: string; Active: bool }

let finalize parent name tag newId oldId active getRoot updateRoot removeRoot =
    let candidate: Root = getRoot newId
    verifyTarget parent (stagingName name) tag (candidate.Parent, candidate.Name, candidate.Tag)
    if candidate.Active then failwith "Staging root must remain inactive until finalization."
    let previous = if oldId = "" then None else Some (getRoot oldId)
    previous |> Option.iter (fun old -> verifyTarget parent name tag (old.Parent, old.Name, old.Tag))
    // Do not remove the previous root until activation of the replacement succeeds.
    try
        previous |> Option.iter (fun old -> updateRoot { old with Active = false })
        updateRoot { candidate with Name = name; Active = active }
    with _ ->
        try updateRoot candidate
        finally previous |> Option.iter updateRoot
        reraise ()
    // If deletion fails, leave the new item usable and the previous item disabled.
    previous |> Option.iter (fun old -> removeRoot old.Id)
