open Fable.Core
open Fable.Core.JsInterop
open SemanticReleaseCargo.Config
open SemanticReleaseCargo.SemanticRelease
open SemanticReleaseCargo.VerifyConditions

type Plugin =
    abstract verifyConditions: (PluginConfig -> VerifyReleaseContext -> JS.Promise<unit>) with get, set

[<ExportDefault>]
let export: Plugin =
    jsOptions<Plugin> (fun o ->
        o.verifyConditions <- (fun config context -> verifyConditions config context |> Async.StartAsPromise))
