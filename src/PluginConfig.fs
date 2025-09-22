module SemanticReleaseCargo.Config

type PluginConfig =
    interface
        abstract loginArgs: string array option with get
        abstract allFeatures: bool option with get
        abstract check: bool option with get
        abstract checkArgs: string array option with get
        abstract publish: bool option with get
        abstract publishArgs: string array option with get
        abstract alwaysVerifyToken: bool option with get
    end
