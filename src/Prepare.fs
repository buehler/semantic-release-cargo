module SemanticReleaseCargo.Prepare

open System.Text.RegularExpressions
open SemanticReleaseCargo.Config
open SemanticReleaseCargo.Errors
open SemanticReleaseCargo.ExternalApi
open SemanticReleaseCargo.SemanticRelease
open SemanticReleaseCargo.Utils
open SemanticReleaseCargo.Workspaces

let private versionRegex =
    Regex(@"^version\s*=\s*(.*)", RegexOptions.ECMAScript ||| RegexOptions.Multiline)

let prepare (api: IExternalApi) (config: PluginConfig) (context: PrepareContext) =
    async {
        let updateVersion crate = async {
            let path = composeManifestPath crate
            context.logger.info $"Write new release version ({context.nextRelease.version}) into {path}"
            let! tomlContent = api.readFile path

            let tomlContent =
                versionRegex.Replace(tomlContent, $"version = \"{context.nextRelease.version}\"")

            do! api.writeFile path tomlContent
        }

        runAsyncs <|
            match config.crates with
            | Some crates -> Array.map updateVersion crates
            | None ->  [| updateVersion "" |]

        if (true, config.check) ||> Option.defaultValue then
            context.logger.info "Run cargo check."

            let args = ([||], config.checkArgs) ||> Option.defaultValue
            let args = addPackageFlags config.crates context args "ECARGOCHECK"
            let allFeatures = (false, config.allFeatures) ||> Option.defaultValue

            let args =
                match (Array.contains "--all-features" args, allFeatures) with
                | false, true -> Array.append args [| "--all-features" |]
                | _ -> args

            let! _, err, exit = api.exec <| Array.append [| "check" |] args

            if exit <> 0 then
                context.logger.error $"Cargo check failed: {err}"
                raise (SemanticReleaseError("Cargo check failed.", "ECARGOCHECK", Some(err)))

        ()
    }
