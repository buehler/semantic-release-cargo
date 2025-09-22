module SemanticReleaseCargo.ExternalApi

type ExecResult = Async<string * string * int>

type IExternalApi =
    interface
        abstract isReadable: string -> Async<unit>
        abstract isWritable: string -> Async<unit>
        abstract readFile: string -> Async<string>
        abstract writeFile: string -> string -> Async<unit>
        abstract cargoExecutable: string
        abstract exec: string array -> ExecResult
    end

module NodeApi =
    open Fable.Core
    open Fable.Core.JsInterop
    open Node.Base

    type private ExecaResult =
        interface
            abstract stdout: string with get
            abstract stderr: string with get
            abstract exitCode: int with get
        end

    let private execa (_: string, _: string array) : JS.Promise<ExecaResult> = import "execa" "execa"

    let private callExeca executable args =
        async {
            let! result = execa (executable, args) |> Async.AwaitPromise
            return result.stdout, result.stderr, result.exitCode
        }

    [<Import("access", "fs/promises")>]
    let private access (_: string) (_: double) : JS.Promise<unit> = jsNative

    [<Import("readFile", "fs/promises")>]
    let private readFile (_: string) (_: string) : JS.Promise<string> = jsNative

    [<Import("writeFile", "fs/promises")>]
    let private writeFile (_: string) (_: string) (_: string) : JS.Promise<unit> = jsNative

    type private Api() =
        interface IExternalApi with
            member this.isReadable path =
                access path Node.Api.fs.constants.R_OK |> Async.AwaitPromise

            member this.readFile path =
                readFile path "utf8" |> Async.AwaitPromise

            member this.isWritable path =
                access path Node.Api.fs.constants.W_OK |> Async.AwaitPromise

            member this.writeFile path content =
                writeFile path content "utf8" |> Async.AwaitPromise

            member this.cargoExecutable =
                match Node.Api.``process``.platform with
                | Platform.Win32 -> "cargo.exe"
                | _ -> "cargo"

            member this.exec args =
                callExeca (this :> IExternalApi).cargoExecutable args

    let api = Api() :> IExternalApi
