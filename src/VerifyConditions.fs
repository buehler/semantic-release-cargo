module SemanticReleaseCargo.VerifyConditions

open Fable.Core
open SemanticReleaseCargo.Config
open SemanticReleaseCargo.Errors
open SemanticReleaseCargo.SemanticRelease

[<Import("access", "fs/promises")>]
let private access (_: string) (_: double) : Async<unit> = jsNative |> Async.AwaitPromise

let verifyConditions (_: PluginConfig) (context: VerifyReleaseContext) =
    async {
        context.logger.debug "Check cargo executable and cargo version."

        try
            let! out, _, _ = Cargo.exec [| "--version" |]
            context.logger.info $"Cargo version: {out}"
        with ex ->
            context.logger.error $"Failed to check cargo version: {ex.Message}"

            raise (
                SemanticReleaseError(
                    $"Cargo executable ({Cargo.cargoExecutable}) not valid.",
                    "ECARGOEXECUTABLE",
                    Some(ex.Message)
                )
            )

        if not (context.env.ContainsKey "CARGO_REGISTRY_TOKEN") then
            context.logger.error "CARGO_REGISTRY_TOKEN is not set"
            raise (SemanticReleaseError("CARGO_REGISTRY_TOKEN is not set.", "ENOREGISTRYTOKEN", None))

        context.logger.info "Login into registry."
        let! _, err, exit = Cargo.exec [| "login"; context.env["CARGO_REGISTRY_TOKEN"] |]

        if exit <> 0 then
            context.logger.error $"Failed to login into registry: {err}"
            raise (SemanticReleaseError("Failed to login into registry.", "ELOGIN", Some(err)))

        try
            do! access "./Cargo.toml" Node.Api.fs.constants.R_OK
            do! access "./Cargo.toml" Node.Api.fs.constants.W_OK
        with ex ->
            context.logger.error "Could not access Cargo.toml file."
            raise (SemanticReleaseError("Could not access Cargo.toml file.", "EACCESS", Some(ex.Message)))

        ()
    }
