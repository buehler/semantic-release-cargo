module SemanticReleaseCargo.Config

type PluginConfig =
    abstract allFeatures: bool option with get, set
    abstract check: bool option with get, set
    abstract checkArgs: string list option with get, set
    abstract publishArgs: string list option with get, set
