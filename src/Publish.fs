module SemanticReleaseCargo.Publish

open SemanticReleaseCargo.Config
open SemanticReleaseCargo.Errors
open SemanticReleaseCargo.ExternalApi
open SemanticReleaseCargo.SemanticRelease


let publish (api: IExternalApi) (config: PluginConfig) (context: Context) =
    async {
        if not ((true, config.publish) ||> Option.defaultValue) then
            context.logger.warn "Publish set to 'false'. Skip publishing."
        else
            context.logger.info "Publish cargo crate."

            let args = ([||], config.publishArgs) ||> Option.defaultValue
            let allFeatures = (false, config.allFeatures) ||> Option.defaultValue

            let args =
                match (Array.contains "--all-features" args, allFeatures) with
                | false, true -> Array.append args [| "--all-features" |]
                | _ -> args

            let args =
                if Array.contains "--allow-dirty" args then
                    args
                else
                    Array.append args [| "--allow-dirty" |]

            let! _, err, exit = api.exec <| Array.append [| "publish" |] args

            if exit <> 0 then
                context.logger.error $"Cargo publish failed: {err}"
                raise (SemanticReleaseError("Cargo publish failed.", "ECARGOPUBLISH", Some(err)))

        ()
    }
