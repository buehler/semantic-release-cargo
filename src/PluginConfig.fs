module SemanticReleaseCargo.Config

type PluginConfig =
    interface
        abstract allFeatures: bool option with get
        abstract check: bool option with get
        abstract checkArgs: string list option with get
        abstract publishArgs: string list option with get
    end
