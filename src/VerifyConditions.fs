module SemanticReleaseCargo.VerifyConditions

open SemanticReleaseCargo.Config
open SemanticReleaseCargo.Errors
open SemanticReleaseCargo.ExternalApi
open SemanticReleaseCargo.SemanticRelease
open SemanticReleaseCargo.Utils
open SemanticReleaseCargo.Workspaces

let verifyConditions (api: IExternalApi) (config: PluginConfig) (context: VerifyReleaseContext) =
    async {
        context.logger.debug "Check cargo executable and cargo version."

        try
            let! out, _, _ = api.exec [| "--version" |]
            context.logger.info $"Cargo version: {out}"
        with ex ->
            context.logger.error $"Failed to check cargo version: {ex.Message}"

            raise (
                SemanticReleaseError(
                    $"Cargo executable ({api.cargoExecutable}) not valid.",
                    "ECARGOEXECUTABLE",
                    Some ex.Message
                )
            )

        // Skip token verification if not publishing and verification explicitly disabled
        if not ((true, config.publish) ||> Option.defaultValue) &&
           not ((true, config.alwaysVerifyToken) ||> Option.defaultValue) then
            context.logger.warn "Publish and alwaysVerifyToken set to 'false'. Skip CARGO_REGISTRY_TOKEN verification."
        else
            if not (mapContainsKey context.env "CARGO_REGISTRY_TOKEN") then
                context.logger.error "CARGO_REGISTRY_TOKEN is not set"
                raise (SemanticReleaseError("CARGO_REGISTRY_TOKEN is not set.", "ENOREGISTRYTOKEN", None))

            context.logger.info "Login into registry."
            let! _, err, exit = api.exec [| "login"; mapGetKey context.env "CARGO_REGISTRY_TOKEN" |]

            if exit <> 0 then
                context.logger.error $"Failed to login into registry: {err}"
                raise (SemanticReleaseError("Failed to login into registry.", "ELOGIN", Some err))

        let checkManifest crate = async {
            let path = composeManifestPath crate
            try
                do! api.isReadable path
                do! api.isWritable path
            with ex ->
                context.logger.error $"Could not access {path} file."
                raise (SemanticReleaseError($"Could not access {path} file.", "EACCESS", Some ex.Message))
        }

        runAsyncs <|
            match config.crates with
            | Some crates  ->
                match crates with
                | [||] ->
                    context.logger.error "Empty 'crates' array specified."
                    raise (
                        SemanticReleaseError(
                            "'crates' array should be non-empty, add at least one crate or remove this configuratoin option.",
                            "EACCESS",
                            None
                        )
                    )
                | _ -> Array.map checkManifest crates
            | None -> [| checkManifest "" |]

        ()
    }
