module SemanticReleaseCargo.Prepare

open SemanticReleaseCargo.Config
open SemanticReleaseCargo.SemanticRelease


let prepare (config: PluginConfig) (context: PrepareContext) =
    context.logger.info $"Write new release version ({context.nextRelease.version}) into Cargo.toml."
    ()
