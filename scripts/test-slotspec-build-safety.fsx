#load "slotspec-build-safety.fsx"

open System.Collections.Generic
open SlotSpecBuildSafety

let check condition = if not condition then failwith "Safety regression"
let rejects action =
    let failed = try action (); false with _ -> true
    check failed

check (selectExisting "Tool" "tag" [] = None)
check (selectExisting "Tool" "tag" [("old", "Tool", "tag")] = Some "old")
rejects (fun () -> selectExisting "Tool" "tag" [("old", "Tool", "other")] |> ignore)
rejects (fun () -> selectExisting "Tool" "tag" ["a", "Tool", "tag"; "b", "Tool", "tag"] |> ignore)

let old = { Id = "old"; Parent = "parent"; Name = "Tool"; Tag = "tag"; Active = true }
let candidate = { old with Id = "new"; Name = stagingName "Tool"; Active = false }
let fresh = Dictionary<string, Root>()
fresh["new"] <- candidate
finalize "parent" "Tool" "tag" "new" "" true (fun id -> fresh[id]) (fun value -> fresh[value.Id] <- value) (fun _ -> failwith "Fresh build must not delete")
check (fresh["new"].Active && fresh["new"].Name = "Tool")
rejects (fun () -> finalize "parent" "Tool" "tag" "new" "" true (fun id -> fresh[id]) (fun _ -> failwith "Must reject before writing") ignore)
for changed in [{ old with Parent = "avatar" }; { old with Name = "Renamed" }; { old with Tag = "other" }] do
    rejects (fun () -> verifyTarget "parent" "Tool" "tag" (changed.Parent, changed.Name, changed.Tag))

for scenario in ["success"; "moved"; "activation failure"; "delete failure"] do
    let roots = Dictionary<string, Root>()
    roots["old"] <- if scenario = "moved" then { old with Parent = "avatar" } else old
    roots["new"] <- candidate
    let mutable deletes = 0
    let update (root: Root) =
        if scenario = "activation failure" && root.Id = "new" && root.Active then failwith "Injected activation failure"
        roots[root.Id] <- root
    let remove id =
        check roots["new"].Active
        check (not roots["old"].Active)
        if scenario = "delete failure" then failwith "Injected deletion failure"
        deletes <- deletes + 1
        roots.Remove id |> ignore
    let run () = finalize "parent" "Tool" "tag" "new" "old" true (fun id -> roots[id]) update remove
    if scenario = "success" then
        run ()
        check (deletes = 1 && roots["new"].Name = "Tool")
    else
        rejects run
        check (deletes = 0 && roots.ContainsKey "old")
        if scenario = "delete failure" then check roots["new"].Active
        else check (roots["old"].Active && not roots["new"].Active)

printfn "SlotSpec safety: identity, ambiguity, activation rollback and deletion ordering passed."
