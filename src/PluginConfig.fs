module SemanticReleaseCargo.Config

type PluginConfig =
    interface
        abstract allFeatures: bool option with get
        abstract check: bool option with get
        abstract checkArgs: string array option with get
        abstract publish: bool option with get
        abstract publishArgs: string array option with get
        abstract alwaysVerifyToken: bool option with get
        abstract crates: string array option with get
    end
