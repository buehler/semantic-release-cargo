module SemanticReleaseCargo.SemanticRelease

open Fable.Core

type Logger =
    interface
        abstract member log: string -> unit
        abstract member error: string -> unit
        abstract member success: string -> unit
        abstract member warn: string -> unit
        abstract member info: string -> unit
        abstract member debug: string -> unit
    end

[<StringEnum(CaseRules.LowerFirst)>]
type ReleaseType =
    | Prerelease
    | Prepatch
    | Patch
    | Preminor
    | Minor
    | Premajor
    | Major

type Release =
    interface
        abstract member version: string with get
        abstract member gitTag: string with get
        abstract member gitHead: string with get
        abstract member name: string with get
    end

type LastRelease =
    interface
        inherit Release
        abstract member channels: string list with get
    end

type NextRelease =
    interface
        inherit Release
        abstract member ``type``: ReleaseType with get
        abstract member channel: string with get
        abstract member notes: string option with get
    end

type Context =
    interface
        abstract member logger: Logger with get
        abstract member env: Map<string, string> with get
    end

type VerifyReleaseContext =
    interface
        inherit Context
        abstract member nextRelease: NextRelease with get
    end

type PrepareContext =
    interface
        inherit Context
        abstract member nextRelease: NextRelease with get
    end
