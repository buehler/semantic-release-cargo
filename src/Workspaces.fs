module SemanticReleaseCargo.Workspaces

open SemanticReleaseCargo.Errors
open SemanticReleaseCargo.SemanticRelease
open System.Text.RegularExpressions

let composeManifestPath (crate: string): string =
    "." + (if crate = "" then "" else $"/{crate}") + "/Cargo.toml"

let private prohibitedFlags =
    let comp p = Regex($"^{p}$", RegexOptions.Compiled)
    [
        "-p|--package", @"[\w-]+"
        "-Z", "package-workspace"
    ] |> List.map (fun (l, r) -> comp l, comp r)

let addPackageFlags
    (crates: array<string> option)
    (context: Context)
    (args: array<string>)
    (code: string): array<string> =

    let findMatch (l: string,r: string) =
        prohibitedFlags
        |> List.tryFind (fun (lp, rp) -> lp.IsMatch l && rp.IsMatch r)
        |> Option.isSome

    match crates with
    | Some crates ->
        match args |> Array.pairwise |> Array.tryFind findMatch with
        | Some(f, v) ->
            context.logger.error $"Prohibited flag {f} found."
            raise (
                SemanticReleaseError(
                    $"'{f} {v}' flag is invalid to use with 'crates' configuration option.",
                    code,
                    None
                )
            )
        | None ->
            Array.concat [|
                args
                [|"-Z"; "package-workspace"|]
                Seq.ofArray crates
                |> Seq.zip (Seq.initInfinite (fun _ -> "--package"))
                |> List.ofSeq
                |> fun prefixed -> List.foldBack (fun (p, c) acc -> p :: c :: acc) prefixed []
                |> Array.ofList
            |]
    | None -> args
