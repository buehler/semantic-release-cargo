open Fable.Core
open Fable.Core.JsInterop
open SemanticReleaseCargo.Config
open SemanticReleaseCargo.ExternalApi
open SemanticReleaseCargo.SemanticRelease
open SemanticReleaseCargo.VerifyConditions
open SemanticReleaseCargo.Prepare
open SemanticReleaseCargo.Publish

type Plugin =
    abstract verifyConditions: (PluginConfig -> VerifyReleaseContext -> JS.Promise<unit>) with get, set
    abstract prepare: (PluginConfig -> PrepareContext -> JS.Promise<unit>) with get, set
    abstract publish: (PluginConfig -> Context -> JS.Promise<unit>) with get, set

[<ExportDefault>]
let export: Plugin =
    jsOptions<Plugin> (fun o ->
        o.verifyConditions <-
            fun config context -> verifyConditions NodeApi.api config context |> Async.StartAsPromise

        o.prepare <- fun config context -> prepare NodeApi.api config context |> Async.StartAsPromise
        o.publish <- fun config context -> publish NodeApi.api config context |> Async.StartAsPromise)
