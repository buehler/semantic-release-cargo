module SemanticReleaseCargo.Prepare

open System.Text.RegularExpressions
open SemanticReleaseCargo.Config
open SemanticReleaseCargo.Errors
open SemanticReleaseCargo.ExternalApi
open SemanticReleaseCargo.SemanticRelease

let private versionRegex = Regex(@"^version\s*=\s*(.*)", RegexOptions.ECMAScript)

let prepare (api: IExternalApi) (config: PluginConfig) (context: PrepareContext) =
    async {
        context.logger.info $"Write new release version ({context.nextRelease.version}) into Cargo.toml."
        let! tomlContent = api.readFile "./Cargo.toml"

        let tomlContent =
            versionRegex.Replace(tomlContent, $"version = \"{context.nextRelease.version}\"")

        do! api.writeFile "./Cargo.toml" tomlContent

        if (true, config.check) ||> Option.defaultValue then
            context.logger.info "Run cargo check."

            let args = ([||], config.checkArgs) ||> Option.defaultValue
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

        ()
    }
